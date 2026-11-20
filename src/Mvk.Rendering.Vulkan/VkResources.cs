using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;
using VkBuffer = Silk.NET.Vulkan.Buffer;
using VkImage = Silk.NET.Vulkan.Image;

namespace Mvk.Rendering.Vulkan
{
    /// <summary>
    /// Буфер вершин или индексов в памяти, видимой процессору. Кадры идут в полёте (этап 10.2), поэтому SetData
    /// не трогает память, которую ещё читает GPU: занятая область уходит в запас до завершения своего кадра,
    /// данные пишутся в свободную область (из запаса буфера или новую). Для программы это та же семантика GL:
    /// команды, записанные до SetData, видят старые данные, после — новые.
    /// </summary>
    internal sealed unsafe class VkBufferObject : IGpuBuffer
    {
        private sealed class Area
        {
            public VkBuffer Buffer;
            public DeviceMemory Memory;
            public void* Mapped;
            public ulong Capacity;
            /// <summary>Последний кадр, в котором область использована командами</summary>
            public ulong LastUsed;
        }

        private readonly VulkanDevice _device;
        private Area? _current;
        /// <summary>Области, которые ещё может читать GPU (освобождаются по завершении их кадра)</summary>
        private readonly List<Area> _retired = new();
        /// <summary>Кадр последней записи SetData: буфер, который давно не пишут, не держит запас</summary>
        private ulong _lastSetSerial;
        /// <summary>Буфер в списке устройства на освобождение запаса</summary>
        internal bool InSweep;

        public VkBuffer Buffer => _current?.Buffer ?? default;
        public BufferKind Kind { get; }
        public int Size { get; private set; }

        /// <summary>Отметить использование текущей области в кадре с номером serial</summary>
        public ulong LastUsed
        {
            set { if (_current != null) _current.LastUsed = value; }
        }

        public VkBufferObject(VulkanDevice device, BufferKind kind)
        {
            _device = device;
            Kind = kind;
        }

        public void SetData<T>(ReadOnlySpan<T> data) where T : unmanaged
        {
            ulong bytes = (ulong)(data.Length * sizeof(T));
            Size = (int)bytes;
            if (bytes == 0) return;
            _lastSetSerial = _device.FrameSerial;
            bool busy = _current != null && _current.LastUsed > _device.CompletedSerial;
            if (_current == null || busy || bytes > _current.Capacity)
            {
                if (_current != null)
                {
                    if (busy)
                    {
                        _retired.Add(_current);
                        _device.TrackRetired(this);
                    }
                    else _Free(_current);
                }
                _current = _TakeFree(bytes) ?? _Allocate(Math.Max(bytes, _current?.Capacity ?? 0));
            }
            fixed (T* src = data) System.Buffer.MemoryCopy(src, _current.Mapped, (long)_current.Capacity, (long)bytes);
        }

        /// <summary>Свободная область из запаса, которую GPU уже не читает</summary>
        private Area? _TakeFree(ulong bytes)
        {
            Area? found = null;
            for (int i = _retired.Count - 1; i >= 0; i--)
            {
                Area a = _retired[i];
                if (a.LastUsed > _device.CompletedSerial) continue;
                if (found == null && a.Capacity >= bytes)
                {
                    found = a;
                    _retired.RemoveAt(i);
                }
                else if (_retired.Count > 4)
                {
                    // Лишний запас не держим
                    _retired.RemoveAt(i);
                    _Free(a);
                }
            }
            if (found != null) found.LastUsed = 0;
            return found;
        }

        private Area _Allocate(ulong capacity)
        {
            var a = new Area { Capacity = Math.Max(capacity, 256) };
            _device.CreateBuffer(a.Capacity,
                Kind == BufferKind.Index ? BufferUsageFlags.IndexBufferBit : BufferUsageFlags.VertexBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out a.Buffer, out a.Memory);
            void* p;
            _device.Vk.MapMemory(_device.Device, a.Memory, 0, a.Capacity, 0, &p);
            a.Mapped = p;
            return a;
        }

        private void _Free(Area a)
        {
            _device.Vk.UnmapMemory(_device.Device, a.Memory);
            _device.Vk.DestroyBuffer(_device.Device, a.Buffer, null);
            _device.Vk.FreeMemory(_device.Device, a.Memory, null);
        }

        /// <summary>
        /// Освободить запас, который GPU уже не читает, если буфер давно не пишут (сетка секции после перестройки).
        /// Буферы, которые пишутся каждый кадр, сохраняют запас для повторного использования.
        /// </summary>
        /// <returns>Запаса не осталось, буфер можно убрать из списка устройства</returns>
        internal bool SweepRetired(ulong completedSerial, ulong frameSerial)
        {
            if (frameSerial - _lastSetSerial <= 2) return _retired.Count == 0;
            for (int i = _retired.Count - 1; i >= 0; i--)
            {
                Area a = _retired[i];
                if (a.LastUsed > completedSerial) continue;
                _retired.RemoveAt(i);
                _Free(a);
            }
            return _retired.Count == 0;
        }

        public void Dispose()
        {
            _device.OnBufferDisposed(this);
            var areas = new List<Area>(_retired);
            if (_current != null) areas.Add(_current);
            _retired.Clear();
            _current = null;
            // Команды текущего и прошлого кадров ещё могут читать буфер
            _device.DeferDestroy(() => { foreach (Area a in areas) _Free(a); });
        }
    }

    /// <summary>
    /// Текстура RGBA8 (2D или массив слоёв) или глубина D32. Вне загрузок всегда в раскладке для чтения шейдером.
    /// </summary>
    internal sealed unsafe class VkTexture : ITexture
    {
        private readonly VulkanDevice _device;
        private readonly DeviceMemory _memory;
        private readonly Format _format;
        private readonly ImageAspectFlags _aspect;

        public TextureDescription Description { get; }
        public VkImage Image { get; }
        public ImageView View { get; }
        public Sampler Sampler { get; }
        public bool IsArray { get; }

        public VkTexture(VulkanDevice device, in TextureDescription d)
        {
            _device = device;
            Description = d;
            IsArray = d.IsArray || d.Layers > 1;
            bool depth = d.Format == TextureFormat.Depth32F;
            _format = depth ? Format.D32Sfloat : Format.R8G8B8A8Unorm;
            _aspect = depth ? ImageAspectFlags.DepthBit : ImageAspectFlags.ColorBit;
            ImageUsageFlags usage = ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit;
            if (depth) usage = ImageUsageFlags.SampledBit | ImageUsageFlags.DepthStencilAttachmentBit;
            device.CreateImage((uint)d.Width, (uint)d.Height, (uint)d.MipLevels, (uint)d.Layers, _format, usage, IsArray,
                out VkImage image, out _memory);
            Image = image;
            View = device.CreateView(image, _format, _aspect, IsArray ? ImageViewType.Type2DArray : ImageViewType.Type2D,
                (uint)d.MipLevels, (uint)d.Layers);
            uint mips = (uint)d.MipLevels, layers = (uint)d.Layers;
            ImageAspectFlags aspect = _aspect;
            device.RunOnce(cb => device.Transition(cb, image, aspect, mips, layers,
                ImageLayout.Undefined, ImageLayout.ShaderReadOnlyOptimal));

            SamplerState s = d.Sampler;
            SamplerAddressMode wrap = s.Wrap switch
            {
                TextureWrap.Repeat => SamplerAddressMode.Repeat,
                TextureWrap.ClampToEdge => SamplerAddressMode.ClampToEdge,
                _ => SamplerAddressMode.ClampToBorder
            };
            bool mip = d.MipLevels > 1 && s.MipFilter != MipFilter.None;
            var info = new SamplerCreateInfo
            {
                SType = StructureType.SamplerCreateInfo,
                MinFilter = s.MinFilter == TextureFilter.Linear ? Filter.Linear : Filter.Nearest,
                MagFilter = s.MagFilter == TextureFilter.Linear ? Filter.Linear : Filter.Nearest,
                MipmapMode = s.MipFilter == MipFilter.Linear ? SamplerMipmapMode.Linear : SamplerMipmapMode.Nearest,
                AddressModeU = wrap,
                AddressModeV = wrap,
                AddressModeW = wrap,
                MinLod = 0,
                // Без мипмапов GL берёт только уровень 0
                MaxLod = mip ? d.MipLevels - 1 : 0,
                BorderColor = s.BorderWhite ? BorderColor.FloatOpaqueWhite : BorderColor.FloatTransparentBlack
            };
            VulkanDevice._Check(device.Vk.CreateSampler(device.Device, in info, null, out Sampler sampler), "vkCreateSampler");
            Sampler = sampler;
        }

        public void SetData(ReadOnlySpan<byte> pixels, PixelFormat format, int level = 0, int layer = 0)
        {
            TextureDescription d = Description;
            int w = Math.Max(1, d.Width >> level);
            int h = Math.Max(1, d.Height >> level);
            _Upload(pixels, format, 0, 0, w, h, level, layer);
        }

        public void SetSubData(ReadOnlySpan<byte> pixels, PixelFormat format, int x, int y, int width, int height, int layer = 0)
            => _Upload(pixels, format, x, y, width, height, 0, layer);

        private void _Upload(ReadOnlySpan<byte> pixels, PixelFormat format, int x, int y, int width, int height, int level, int layer)
        {
            ulong size = (ulong)(width * height * 4);
            if ((ulong)pixels.Length < size) throw new ArgumentException($"Мало данных: {pixels.Length} < {size}");
            _device.CreateBuffer(size, BufferUsageFlags.TransferSrcBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out VkBuffer staging, out DeviceMemory memory);
            void* p;
            _device.Vk.MapMemory(_device.Device, memory, 0, size, 0, &p);
            var dst = new Span<byte>(p, (int)size);
            pixels.Slice(0, (int)size).CopyTo(dst);
            if (format == PixelFormat.Bgra8)
            {
                for (int i = 0; i < (int)size; i += 4) (dst[i], dst[i + 2]) = (dst[i + 2], dst[i]);
            }
            _device.Vk.UnmapMemory(_device.Device, memory);
            TextureDescription d = Description;
            VkImage image = Image;
            VulkanDevice device = _device;
            _device.RunOnce(cb =>
            {
                _device.Transition(cb, image, _aspect, (uint)d.MipLevels, (uint)d.Layers,
                    ImageLayout.ShaderReadOnlyOptimal, ImageLayout.TransferDstOptimal);
                var region = new BufferImageCopy
                {
                    ImageSubresource = new ImageSubresourceLayers(_aspect, (uint)level, (uint)layer, 1),
                    ImageOffset = new Offset3D(x, y, 0),
                    ImageExtent = new Extent3D((uint)width, (uint)height, 1)
                };
                _device.Vk.CmdCopyBufferToImage(cb, staging, image, ImageLayout.TransferDstOptimal, 1, in region);
                _device.Transition(cb, image, _aspect, (uint)d.MipLevels, (uint)d.Layers,
                    ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);
            }, () =>
            {
                // Промежуточный буфер освобождается, когда загрузка выполнена
                device.Vk.DestroyBuffer(device.Device, staging, null);
                device.Vk.FreeMemory(device.Device, memory, null);
            });
        }

        public void GenerateMipmaps()
        {
            TextureDescription d = Description;
            if (d.MipLevels <= 1) return;
            VkImage image = Image;
            _device.RunOnce(cb =>
            {
                _device.Transition(cb, image, _aspect, (uint)d.MipLevels, (uint)d.Layers,
                    ImageLayout.ShaderReadOnlyOptimal, ImageLayout.TransferDstOptimal);
                int w = d.Width, h = d.Height;
                for (int level = 1; level < d.MipLevels; level++)
                {
                    _device.Transition(cb, image, _aspect, (uint)d.MipLevels, (uint)d.Layers,
                        ImageLayout.TransferDstOptimal, ImageLayout.TransferSrcOptimal, (uint)(level - 1), 1);
                    int nw = Math.Max(1, w / 2), nh = Math.Max(1, h / 2);
                    var blit = new ImageBlit
                    {
                        SrcSubresource = new ImageSubresourceLayers(_aspect, (uint)(level - 1), 0, (uint)d.Layers),
                        DstSubresource = new ImageSubresourceLayers(_aspect, (uint)level, 0, (uint)d.Layers)
                    };
                    blit.SrcOffsets[1] = new Offset3D(w, h, 1);
                    blit.DstOffsets[1] = new Offset3D(nw, nh, 1);
                    _device.Vk.CmdBlitImage(cb, image, ImageLayout.TransferSrcOptimal, image, ImageLayout.TransferDstOptimal,
                        1, in blit, Filter.Linear);
                    _device.Transition(cb, image, _aspect, (uint)d.MipLevels, (uint)d.Layers,
                        ImageLayout.TransferSrcOptimal, ImageLayout.TransferDstOptimal, (uint)(level - 1), 1);
                    w = nw; h = nh;
                }
                _device.Transition(cb, image, _aspect, (uint)d.MipLevels, (uint)d.Layers,
                    ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);
            });
        }

        public void Dispose()
        {
            _device.OnTextureDisposed(this);
            _device.Vk.DeviceWaitIdle(_device.Device);
            _device.Vk.DestroySampler(_device.Device, Sampler, null);
            _device.Vk.DestroyImageView(_device.Device, View, null);
            _device.Vk.DestroyImage(_device.Device, Image, null);
            _device.Vk.FreeMemory(_device.Device, _memory, null);
        }
    }

    /// <summary>
    /// Цель рендера с одной глубиной (карта теней)
    /// </summary>
    internal sealed unsafe class VkRenderTarget : IRenderTarget
    {
        private readonly VulkanDevice _device;
        public ITexture Depth { get; }
        public Framebuffer Framebuffer { get; }
        public uint Width { get; }
        public uint Height { get; }

        public VkRenderTarget(VulkanDevice device, VkTexture depth)
        {
            _device = device;
            Depth = depth;
            Width = (uint)depth.Description.Width;
            Height = (uint)depth.Description.Height;
            ImageView view = depth.View;
            var info = new FramebufferCreateInfo
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = device.DepthPass,
                AttachmentCount = 1,
                PAttachments = &view,
                Width = Width,
                Height = Height,
                Layers = 1
            };
            VulkanDevice._Check(device.Vk.CreateFramebuffer(device.Device, in info, null, out Framebuffer fb), "vkCreateFramebuffer");
            Framebuffer = fb;
        }

        public void Dispose()
        {
            Framebuffer fb = Framebuffer;
            VulkanDevice device = _device;
            device.DeferDestroy(() => device.Vk.DestroyFramebuffer(device.Device, fb, null));
        }
    }

    /// <summary>
    /// Шейдер: GLSL 330 ассетов, переведённый в GLSL 450 (GlslToVulkan) и скомпилированный в SPIR-V.
    /// Uniform живут в блоке на процессоре, как значения uniform программы GL: сохраняются между Submit.
    /// </summary>
    internal sealed unsafe class VkShader : IShader
    {
        private static readonly Shaderc _shaderc = Shaderc.GetApi();

        private readonly VulkanDevice _device;
        private readonly UniformSlot[] _slots;
        private readonly Dictionary<string, int> _names = new();
        private readonly bool[] _samplerArray;

        public string Name { get; }
        public ShaderModule Vertex { get; }
        public ShaderModule Fragment { get; }
        public DescriptorSetLayout SetLayout { get; }
        public PipelineLayout PipelineLayout { get; }
        public byte[] Block { get; }
        public int BlockSize => Block.Length;
        public int SamplerCount { get; }

        public VkShader(VulkanDevice device, ShaderDescription d)
        {
            _device = device;
            Name = d.Name;
            GlslToVulkan.Result t = GlslToVulkan.Translate(d);
            _slots = t.Uniforms.ToArray();
            for (int i = 0; i < _slots.Length; i++) _names[_slots[i].Name] = i;
            Block = new byte[t.BlockSize];
            SamplerCount = t.SamplerCount;
            _samplerArray = new bool[SamplerCount];
            for (int i = 0; i < SamplerCount; i++)
            {
                string name = i < d.Samplers.Count ? d.Samplers[i] : "";
                _samplerArray[i] = t.Fragment.Contains("sampler2DArray " + name + ";") || t.Vertex.Contains("sampler2DArray " + name + ";");
            }

            Vertex = _Module(t.Vertex, ShaderKind.VertexShader, "vertex");
            Fragment = _Module(t.Fragment, ShaderKind.FragmentShader, "fragment");

            var bindings = stackalloc DescriptorSetLayoutBinding[1 + SamplerCount];
            bindings[0] = new DescriptorSetLayoutBinding(0, DescriptorType.UniformBufferDynamic, 1,
                ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit);
            for (int i = 0; i < SamplerCount; i++)
            {
                bindings[1 + i] = new DescriptorSetLayoutBinding((uint)(1 + i), DescriptorType.CombinedImageSampler, 1,
                    ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit);
            }
            var layoutInfo = new DescriptorSetLayoutCreateInfo
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)(1 + SamplerCount),
                PBindings = bindings
            };
            VulkanDevice._Check(device.Vk.CreateDescriptorSetLayout(device.Device, in layoutInfo, null, out DescriptorSetLayout setLayout),
                "vkCreateDescriptorSetLayout");
            SetLayout = setLayout;
            var pipelineLayoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = 1,
                PSetLayouts = &setLayout
            };
            VulkanDevice._Check(device.Vk.CreatePipelineLayout(device.Device, in pipelineLayoutInfo, null, out PipelineLayout pl),
                "vkCreatePipelineLayout");
            PipelineLayout = pl;
        }

        private ShaderModule _Module(string source, ShaderKind kind, string what)
        {
            Compiler* compiler = _shaderc.CompilerInitialize();
            CompileOptions* options = _shaderc.CompileOptionsInitialize();
            _shaderc.CompileOptionsSetTargetEnv(options, TargetEnv.Vulkan, (uint)EnvVersion.Vulkan11);
            byte[] bytes = Encoding.UTF8.GetBytes(source);
            CompilationResult* result;
            fixed (byte* src = bytes)
            {
                result = _shaderc.CompileIntoSpv(compiler, src, (nuint)bytes.Length, kind, Name, "main", options);
            }
            try
            {
                if (_shaderc.ResultGetCompilationStatus(result) != CompilationStatus.Success)
                {
                    string error = _shaderc.ResultGetErrorMessageS(result);
                    throw new InvalidOperationException($"Шейдер {Name} ({what}): ошибка компиляции SPIR-V\n{error}\n{source}");
                }
                var info = new ShaderModuleCreateInfo
                {
                    SType = StructureType.ShaderModuleCreateInfo,
                    CodeSize = _shaderc.ResultGetLength(result),
                    PCode = (uint*)_shaderc.ResultGetBytes(result)
                };
                VulkanDevice._Check(_device.Vk.CreateShaderModule(_device.Device, in info, null, out ShaderModule module),
                    "vkCreateShaderModule");
                return module;
            }
            finally
            {
                _shaderc.ResultRelease(result);
                _shaderc.CompileOptionsRelease(options);
                _shaderc.CompilerRelease(compiler);
            }
        }

        public bool SamplerIsArray(int index) => index < _samplerArray.Length && _samplerArray[index];

        public UniformHandle GetUniform(string name) => new UniformHandle(_names.TryGetValue(name, out int i) ? i : -1);

        public void SetInt(int slot, int value)
        {
            if ((uint)slot >= (uint)_slots.Length) return;
            fixed (byte* p = Block) *(int*)(p + _slots[slot].Offset) = value;
        }

        public void SetFloats(int slot, ReadOnlySpan<float> values)
        {
            if ((uint)slot >= (uint)_slots.Length) return;
            UniformSlot s = _slots[slot];
            int max = (Block.Length - s.Offset) / 4;
            fixed (byte* p = Block)
            {
                float* dst = (float*)(p + s.Offset);
                for (int i = 0; i < values.Length && i < max; i++) dst[i] = values[i];
            }
        }

        /// <summary>Массив mat4x3: 12 чисел на матрицу (4 столбца по 3), в std140 столбец выровнен на 16 байт</summary>
        public void SetMatrix4x3(int slot, ReadOnlySpan<float> values, int count)
        {
            if ((uint)slot >= (uint)_slots.Length) return;
            UniformSlot s = _slots[slot];
            int n = s.ArraySize > 0 ? Math.Min(count, s.ArraySize) : Math.Min(count, 1);
            fixed (byte* p = Block)
            {
                for (int m = 0; m < n; m++)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        float* dst = (float*)(p + s.Offset + m * s.Stride + c * 16);
                        int src = m * 12 + c * 3;
                        dst[0] = values[src];
                        dst[1] = values[src + 1];
                        dst[2] = values[src + 2];
                    }
                }
            }
        }

        public void Dispose()
        {
            _device.Vk.DeviceWaitIdle(_device.Device);
            _device.Vk.DestroyPipelineLayout(_device.Device, PipelineLayout, null);
            _device.Vk.DestroyDescriptorSetLayout(_device.Device, SetLayout, null);
            _device.Vk.DestroyShaderModule(_device.Device, Vertex, null);
            _device.Vk.DestroyShaderModule(_device.Device, Fragment, null);
        }
    }

    /// <summary>
    /// Конвейер: описание RHI и два объекта Vulkan, для окна и для карты теней (у них разный проход и
    /// разная ориентация: окно рисуется с переворотом оси Y, поэтому лицевая сторона там другая)
    /// </summary>
    internal sealed unsafe class VkPipelineObject : IPipeline
    {
        private readonly VulkanDevice _device;
        private Pipeline _main;
        private Pipeline _shadow;

        public PipelineDescription Description { get; }
        public VkShader Shader { get; }

        public VkPipelineObject(VulkanDevice device, PipelineDescription d)
        {
            _device = device;
            Description = d;
            Shader = d.Shader as VkShader ?? throw new ArgumentException("Шейдер создан не этим устройством", nameof(d));
        }

        /// <summary>Объект Vulkan для прохода (создаётся при первом использовании)</summary>
        public Pipeline Get(bool shadowPass)
        {
            if (shadowPass)
            {
                if (_shadow.Handle == 0) _shadow = _Create(true);
                return _shadow;
            }
            if (_main.Handle == 0) _main = _Create(false);
            return _main;
        }

        private Pipeline _Create(bool shadowPass)
        {
            PipelineDescription d = Description;
            var vk = _device.Vk;
            var entry = (byte*)SilkMarshal.StringToPtr("main");
            var stages = stackalloc PipelineShaderStageCreateInfo[2];
            stages[0] = new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.VertexBit,
                Module = Shader.Vertex,
                PName = entry
            };
            stages[1] = new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = Shader.Fragment,
                PName = entry
            };

            int layoutCount = d.VertexLayouts.Count;
            int attrCount = 0;
            foreach (VertexBufferLayout l in d.VertexLayouts) attrCount += l.Attributes.Count;
            var bindings = stackalloc VertexInputBindingDescription[Math.Max(1, layoutCount)];
            var attrs = stackalloc VertexInputAttributeDescription[Math.Max(1, attrCount)];
            int a = 0;
            for (int s = 0; s < layoutCount; s++)
            {
                VertexBufferLayout l = d.VertexLayouts[s];
                bindings[s] = new VertexInputBindingDescription((uint)s, (uint)l.Stride, VertexInputRate.Vertex);
                foreach (VertexAttribute attr in l.Attributes)
                {
                    attrs[a++] = new VertexInputAttributeDescription((uint)attr.Location, (uint)s, _Format(attr.Format), (uint)attr.Offset);
                }
            }
            var vertexInput = new PipelineVertexInputStateCreateInfo
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = (uint)layoutCount,
                PVertexBindingDescriptions = bindings,
                VertexAttributeDescriptionCount = (uint)attrCount,
                PVertexAttributeDescriptions = attrs
            };
            var assembly = new PipelineInputAssemblyStateCreateInfo
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = d.Topology switch
                {
                    PrimitiveTopology.Lines => Silk.NET.Vulkan.PrimitiveTopology.LineList,
                    PrimitiveTopology.LineStrip => Silk.NET.Vulkan.PrimitiveTopology.LineStrip,
                    PrimitiveTopology.Points => Silk.NET.Vulkan.PrimitiveTopology.PointList,
                    _ => Silk.NET.Vulkan.PrimitiveTopology.TriangleList
                }
            };
            var viewportState = new PipelineViewportStateCreateInfo
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                ScissorCount = 1
            };
            RasterState r = d.Raster;
            var raster = new PipelineRasterizationStateCreateInfo
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                PolygonMode = r.Fill == FillMode.Wireframe ? PolygonMode.Line : PolygonMode.Fill,
                CullMode = r.Cull switch
                {
                    CullMode.Back => CullModeFlags.BackBit,
                    CullMode.Front => CullModeFlags.FrontBit,
                    _ => CullModeFlags.None
                },
                // Окно с переворотом Y: лицевая сторона против часовой, как в GL; карта теней без переворота
                FrontFace = shadowPass ? FrontFace.Clockwise : FrontFace.CounterClockwise,
                LineWidth = 1f,
                DepthBiasEnable = r.HasDepthBias,
                DepthBiasConstantFactor = r.DepthBiasUnits,
                DepthBiasSlopeFactor = r.DepthBiasFactor
            };
            var multisample = new PipelineMultisampleStateCreateInfo
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                RasterizationSamples = SampleCountFlags.Count1Bit
            };
            var depth = new PipelineDepthStencilStateCreateInfo
            {
                SType = StructureType.PipelineDepthStencilStateCreateInfo,
                DepthTestEnable = r.DepthTest,
                DepthWriteEnable = r.DepthWrite,
                DepthCompareOp = r.DepthTest ? _Compare(r.DepthFunc) : CompareOp.Always
            };
            BlendState b = d.Blend;
            var blendAttachment = new PipelineColorBlendAttachmentState
            {
                BlendEnable = b.Enabled,
                SrcColorBlendFactor = _Blend(b.SrcColor),
                DstColorBlendFactor = _Blend(b.DstColor),
                ColorBlendOp = BlendOp.Add,
                SrcAlphaBlendFactor = _Blend(b.SrcAlpha),
                DstAlphaBlendFactor = _Blend(b.DstAlpha),
                AlphaBlendOp = BlendOp.Add,
                ColorWriteMask = d.ColorWrite
                    ? ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit
                    : 0
            };
            var blend = new PipelineColorBlendStateCreateInfo
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                AttachmentCount = shadowPass ? 0u : 1u,
                PAttachments = &blendAttachment
            };
            var dynamicStates = stackalloc DynamicState[] { DynamicState.Viewport, DynamicState.Scissor };
            var dynamic = new PipelineDynamicStateCreateInfo
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = 2,
                PDynamicStates = dynamicStates
            };
            var info = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = 2,
                PStages = stages,
                PVertexInputState = &vertexInput,
                PInputAssemblyState = &assembly,
                PViewportState = &viewportState,
                PRasterizationState = &raster,
                PMultisampleState = &multisample,
                PDepthStencilState = &depth,
                PColorBlendState = &blend,
                PDynamicState = &dynamic,
                Layout = Shader.PipelineLayout,
                RenderPass = shadowPass ? _device.DepthPass : _device.MainPass,
                Subpass = 0
            };
            VulkanDevice._Check(vk.CreateGraphicsPipelines(_device.Device, default, 1, in info, null, out Pipeline pipeline),
                "vkCreateGraphicsPipelines " + Shader.Name);
            SilkMarshal.Free((nint)entry);
            return pipeline;
        }

        private static Format _Format(VertexFormat f) => f switch
        {
            VertexFormat.Float1 => Format.R32Sfloat,
            VertexFormat.Float2 => Format.R32G32Sfloat,
            VertexFormat.Float3 => Format.R32G32B32Sfloat,
            VertexFormat.Float4 => Format.R32G32B32A32Sfloat,
            VertexFormat.Int1 => Format.R32Sint,
            VertexFormat.Int2 => Format.R32G32Sint,
            VertexFormat.Int3 => Format.R32G32B32Sint,
            VertexFormat.Int4 => Format.R32G32B32A32Sint,
            _ => Format.R8G8B8A8Unorm
        };

        private static CompareOp _Compare(CompareFunc f) => f switch
        {
            CompareFunc.Never => CompareOp.Never,
            CompareFunc.Less => CompareOp.Less,
            CompareFunc.Equal => CompareOp.Equal,
            CompareFunc.LessEqual => CompareOp.LessOrEqual,
            CompareFunc.Greater => CompareOp.Greater,
            CompareFunc.NotEqual => CompareOp.NotEqual,
            CompareFunc.GreaterEqual => CompareOp.GreaterOrEqual,
            _ => CompareOp.Always
        };

        private static Silk.NET.Vulkan.BlendFactor _Blend(BlendFactor f) => f switch
        {
            BlendFactor.Zero => Silk.NET.Vulkan.BlendFactor.Zero,
            BlendFactor.One => Silk.NET.Vulkan.BlendFactor.One,
            BlendFactor.SrcAlpha => Silk.NET.Vulkan.BlendFactor.SrcAlpha,
            BlendFactor.OneMinusSrcAlpha => Silk.NET.Vulkan.BlendFactor.OneMinusSrcAlpha,
            BlendFactor.SrcColor => Silk.NET.Vulkan.BlendFactor.SrcColor,
            BlendFactor.OneMinusSrcColor => Silk.NET.Vulkan.BlendFactor.OneMinusSrcColor,
            BlendFactor.DstAlpha => Silk.NET.Vulkan.BlendFactor.DstAlpha,
            _ => Silk.NET.Vulkan.BlendFactor.OneMinusDstAlpha
        };

        public void Dispose()
        {
            _device.OnPipelineDisposed(this);
            _device.Vk.DeviceWaitIdle(_device.Device);
            if (_main.Handle != 0) _device.Vk.DestroyPipeline(_device.Device, _main, null);
            if (_shadow.Handle != 0) _device.Vk.DestroyPipeline(_device.Device, _shadow, null);
        }
    }
}
