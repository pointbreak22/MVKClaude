using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using VkBuffer = Silk.NET.Vulkan.Buffer;
using VkImage = Silk.NET.Vulkan.Image;

namespace Mvk.Rendering.Vulkan
{
    /// <summary>
    /// Устройство RHI на Vulkan (этап 10). Повторяет поведение OpenGL-бэкенда:
    /// - состояние (конвейер, буферы, текстуры по слотам, uniform шейдеров) живёт между Submit, как в GL;
    /// - Submit записывает список команд в командный буфер и ждёт его выполнения (синхронно, как GL с точки
    ///   зрения программы: после Submit буферы и текстуры можно менять);
    /// - кадр окна: BeginFrame (взять изображение цепочки показа) и EndFrame (показать) вызывает хост;
    /// - система координат GL: окно рисуется с перевёрнутым видовым окном (ось Y вверх, начало внизу слева),
    ///   цели рендера (карта теней) без переворота, чтобы выборка в шейдере совпала с GL.
    /// </summary>
    public sealed unsafe partial class VulkanDevice : IGraphicsDevice
    {
        internal readonly Vk Vk;
        internal readonly Device Device;
        internal readonly PhysicalDevice PhysicalDevice;
        private readonly Instance _instance;
        private readonly Queue _queue;
        private readonly uint _queueFamily;
        private readonly KhrSurface _khrSurface;
        private readonly SurfaceKHR _surface;
        private readonly KhrSwapchain _khrSwapchain;
        private readonly CommandPool _commandPool;
        private readonly CommandBuffer _cmd;
        private readonly Fence _fence;
        private readonly Semaphore _acquireSemaphore;
        private readonly PhysicalDeviceMemoryProperties _memory;
        private readonly ulong _uniformAlign;

        private SwapchainKHR _swapchain;
        private VkImage[] _swapImages = Array.Empty<VkImage>();
        private ImageView[] _swapViews = Array.Empty<ImageView>();
        private Framebuffer[] _framebuffers = Array.Empty<Framebuffer>();
        private Format _swapFormat;
        private Extent2D _extent;
        private VkImage _depthImage;
        private DeviceMemory _depthMemory;
        private ImageView _depthView;
        private bool _vsync;

        /// <summary>Проход окна: цвет и глубина, содержимое сохраняется между проходами</summary>
        internal RenderPass MainPass;
        /// <summary>Проход цели рендера: только глубина (карта теней)</summary>
        internal RenderPass DepthPass;

        // Кольцевой буфер uniform: блок шейдера копируется перед каждым вызовом отрисовки
        private const ulong _RingSize = 16 * 1024 * 1024;
        private VkBuffer _ring;
        private DeviceMemory _ringMemory;
        private byte* _ringPtr;
        private ulong _ringOffset;
        private DescriptorPool _descriptorPool;

        private uint _imageIndex;
        private bool _frameActive;
        private bool _waitAcquire;

        // Состояние между Submit, как в GL
        private const int _MaxTextureSlots = 16;
        private const int _MaxVertexSlots = 4;
        private VkPipelineObject? _pipeline;
        private readonly VkBufferObject?[] _vertexBuffers = new VkBufferObject?[_MaxVertexSlots];
        private VkBufferObject? _indexBuffer;
        private IndexFormat _indexFormat;
        private readonly VkTexture?[] _textures = new VkTexture?[_MaxTextureSlots];
        private VkRenderTarget? _target;
        private int _vx, _vy, _vw, _vh;
        private VkTexture _white = null!;
        private VkTexture _whiteArray = null!;

        // Состояние записи текущего Submit
        private bool _inPass;
        private Pipeline _boundPipeline;

        public GraphicsBackend Backend => GraphicsBackend.Vulkan;
        public string Description { get; }

        /// <summary>Размер изображения окна</summary>
        public int Width => (int)_extent.Width;
        public int Height => (int)_extent.Height;

        /// <param name="instanceExtensions">Расширения экземпляра, которые нужны поверхности окна</param>
        /// <param name="createSurface">Создать поверхность окна для экземпляра</param>
        public VulkanDevice(IReadOnlyList<string> instanceExtensions, Func<Instance, SurfaceKHR> createSurface,
            int width, int height, bool vsync)
        {
            Vk = Vk.GetApi();
            _vsync = vsync;

            // Экземпляр
            var appName = (byte*)SilkMarshal.StringToPtr("Malyuveki 2");
            var appInfo = new ApplicationInfo
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = appName,
                ApplicationVersion = Vk.MakeVersion(2, 0),
                PEngineName = appName,
                EngineVersion = Vk.MakeVersion(2, 0),
                ApiVersion = Vk.Version11
            };
            bool validation = Environment.GetEnvironmentVariable("MVK_VK_VALIDATION") == "1";
            var layers = validation ? new[] { "VK_LAYER_KHRONOS_validation" } : Array.Empty<string>();
            var extensions = new List<string>(instanceExtensions);
            var extPtr = (byte**)SilkMarshal.StringArrayToPtr(extensions);
            var layerPtr = (byte**)SilkMarshal.StringArrayToPtr(layers);
            var instanceInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo,
                EnabledExtensionCount = (uint)extensions.Count,
                PpEnabledExtensionNames = extPtr,
                EnabledLayerCount = (uint)layers.Length,
                PpEnabledLayerNames = layerPtr
            };
            _Check(Vk.CreateInstance(in instanceInfo, null, out _instance), "vkCreateInstance");
            SilkMarshal.Free((nint)extPtr);
            SilkMarshal.Free((nint)layerPtr);
            SilkMarshal.Free((nint)appName);

            if (!Vk.TryGetInstanceExtension(_instance, out _khrSurface))
                throw new NotSupportedException("Нет VK_KHR_surface");
            _surface = createSurface(_instance);

            // Видеокарта с очередью графики и показа (дискретная предпочтительнее)
            uint count = 0;
            Vk.EnumeratePhysicalDevices(_instance, ref count, null);
            var devices = new PhysicalDevice[count];
            fixed (PhysicalDevice* p = devices) Vk.EnumeratePhysicalDevices(_instance, ref count, p);
            PhysicalDevice chosen = default;
            uint family = uint.MaxValue;
            int bestScore = -1;
            foreach (PhysicalDevice pd in devices)
            {
                uint qCount = 0;
                Vk.GetPhysicalDeviceQueueFamilyProperties(pd, ref qCount, null);
                var families = new QueueFamilyProperties[qCount];
                fixed (QueueFamilyProperties* f = families) Vk.GetPhysicalDeviceQueueFamilyProperties(pd, ref qCount, f);
                for (uint i = 0; i < qCount; i++)
                {
                    _khrSurface.GetPhysicalDeviceSurfaceSupport(pd, i, _surface, out Silk.NET.Core.Bool32 present);
                    if ((families[i].QueueFlags & QueueFlags.GraphicsBit) != 0 && present)
                    {
                        Vk.GetPhysicalDeviceProperties(pd, out PhysicalDeviceProperties props);
                        int score = props.DeviceType == PhysicalDeviceType.DiscreteGpu ? 2
                            : props.DeviceType == PhysicalDeviceType.IntegratedGpu ? 1 : 0;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            chosen = pd;
                            family = i;
                        }
                        break;
                    }
                }
            }
            if (family == uint.MaxValue) throw new NotSupportedException("Нет видеокарты Vulkan с показом в окно");
            PhysicalDevice = chosen;
            _queueFamily = family;
            Vk.GetPhysicalDeviceProperties(PhysicalDevice, out PhysicalDeviceProperties deviceProps);
            Vk.GetPhysicalDeviceFeatures(PhysicalDevice, out PhysicalDeviceFeatures available);
            Vk.GetPhysicalDeviceMemoryProperties(PhysicalDevice, out _memory);
            _uniformAlign = Math.Max(16, deviceProps.Limits.MinUniformBufferOffsetAlignment);
            Description = "Vulkan " + _Version(deviceProps.ApiVersion) + " / "
                + SilkMarshal.PtrToString((nint)deviceProps.DeviceName);

            // Логическое устройство
            float priority = 1f;
            var queueInfo = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = _queueFamily,
                QueueCount = 1,
                PQueuePriorities = &priority
            };
            var features = new PhysicalDeviceFeatures
            {
                // Каркас (отладка Debug.IsDrawVoxelLine)
                FillModeNonSolid = available.FillModeNonSolid
            };
            var deviceExt = (byte**)SilkMarshal.StringArrayToPtr(new[] { KhrSwapchain.ExtensionName });
            var deviceInfo = new DeviceCreateInfo
            {
                SType = StructureType.DeviceCreateInfo,
                QueueCreateInfoCount = 1,
                PQueueCreateInfos = &queueInfo,
                PEnabledFeatures = &features,
                EnabledExtensionCount = 1,
                PpEnabledExtensionNames = deviceExt
            };
            _Check(Vk.CreateDevice(PhysicalDevice, in deviceInfo, null, out Device), "vkCreateDevice");
            SilkMarshal.Free((nint)deviceExt);
            Vk.GetDeviceQueue(Device, _queueFamily, 0, out _queue);
            if (!Vk.TryGetDeviceExtension(_instance, Device, out _khrSwapchain))
                throw new NotSupportedException("Нет VK_KHR_swapchain");

            var poolInfo = new CommandPoolCreateInfo
            {
                SType = StructureType.CommandPoolCreateInfo,
                Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
                QueueFamilyIndex = _queueFamily
            };
            _Check(Vk.CreateCommandPool(Device, in poolInfo, null, out _commandPool), "vkCreateCommandPool");
            var allocInfo = new CommandBufferAllocateInfo
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = _commandPool,
                Level = CommandBufferLevel.Primary,
                CommandBufferCount = 1
            };
            _Check(Vk.AllocateCommandBuffers(Device, in allocInfo, out _cmd), "vkAllocateCommandBuffers");
            var fenceInfo = new FenceCreateInfo { SType = StructureType.FenceCreateInfo };
            _Check(Vk.CreateFence(Device, in fenceInfo, null, out _fence), "vkCreateFence");
            var semInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo };
            _Check(Vk.CreateSemaphore(Device, in semInfo, null, out _acquireSemaphore), "vkCreateSemaphore");

            _ChooseSwapFormat();
            _CreateRenderPasses();
            _CreateSwapchain(width, height);
            _CreateRing();
            _CreateDescriptorPool();
            _white = new VkTexture(this, TextureDescription.Texture2D(1, 1, SamplerState.PointClamp));
            _white.SetData(new byte[] { 255, 255, 255, 255 }, PixelFormat.Rgba8);
            _whiteArray = new VkTexture(this, TextureDescription.Texture2DArray(1, 1, 1, SamplerState.PointClamp));
            _whiteArray.SetData(new byte[] { 255, 255, 255, 255 }, PixelFormat.Rgba8);
        }

        private static string _Version(uint v) => $"{v >> 22}.{(v >> 12) & 0x3FF}.{v & 0xFFF}";

        internal static void _Check(Result result, string what)
        {
            if (result != Result.Success) throw new InvalidOperationException($"Vulkan: {what} вернул {result}");
        }

        #region Ресурсы

        public IGpuBuffer CreateBuffer(BufferKind kind, BufferUsage usage) => new VkBufferObject(this, kind);

        public ITexture CreateTexture(in TextureDescription description) => new VkTexture(this, description);

        public IShader CreateShader(ShaderDescription description) => new VkShader(this, description);

        public IPipeline CreatePipeline(PipelineDescription description)
        {
            if (description.VertexLayouts.Count > _MaxVertexSlots)
                throw new ArgumentException($"Слотов вершин больше {_MaxVertexSlots}", nameof(description));
            return new VkPipelineObject(this, description);
        }

        public IRenderTarget CreateRenderTarget(ITexture depth) => new VkRenderTarget(this, (VkTexture)depth);

        /// <summary>Номер типа памяти с нужными свойствами</summary>
        internal uint FindMemory(uint typeBits, MemoryPropertyFlags flags)
        {
            for (int i = 0; i < _memory.MemoryTypeCount; i++)
            {
                if ((typeBits & (1u << i)) != 0 && (_memory.MemoryTypes[i].PropertyFlags & flags) == flags) return (uint)i;
            }
            throw new NotSupportedException("Нет подходящей памяти Vulkan: " + flags);
        }

        /// <summary>Буфер с памятью</summary>
        internal void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags flags,
            out VkBuffer buffer, out DeviceMemory memory)
        {
            var info = new BufferCreateInfo
            {
                SType = StructureType.BufferCreateInfo,
                Size = size,
                Usage = usage,
                SharingMode = SharingMode.Exclusive
            };
            _Check(Vk.CreateBuffer(Device, in info, null, out buffer), "vkCreateBuffer");
            Vk.GetBufferMemoryRequirements(Device, buffer, out MemoryRequirements req);
            var alloc = new MemoryAllocateInfo
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = req.Size,
                MemoryTypeIndex = FindMemory(req.MemoryTypeBits, flags)
            };
            _Check(Vk.AllocateMemory(Device, in alloc, null, out memory), "vkAllocateMemory");
            Vk.BindBufferMemory(Device, buffer, memory, 0);
        }

        /// <summary>
        /// Разовая запись команд (загрузка текстур, переходы раскладок) с ожиданием выполнения
        /// </summary>
        internal void RunOnce(Action<CommandBuffer> record)
        {
            var alloc = new CommandBufferAllocateInfo
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = _commandPool,
                Level = CommandBufferLevel.Primary,
                CommandBufferCount = 1
            };
            _Check(Vk.AllocateCommandBuffers(Device, in alloc, out CommandBuffer cb), "vkAllocateCommandBuffers");
            var begin = new CommandBufferBeginInfo
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit
            };
            Vk.BeginCommandBuffer(cb, in begin);
            record(cb);
            Vk.EndCommandBuffer(cb);
            var submit = new SubmitInfo
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &cb
            };
            _Check(Vk.QueueSubmit(_queue, 1, in submit, default), "vkQueueSubmit");
            Vk.QueueWaitIdle(_queue);
            Vk.FreeCommandBuffers(Device, _commandPool, 1, in cb);
        }

        /// <summary>Барьер смены раскладки изображения</summary>
        internal void Transition(CommandBuffer cb, VkImage image, ImageAspectFlags aspect, uint mipLevels, uint layers,
            ImageLayout from, ImageLayout to, uint baseMip = 0, uint mipCount = 0)
        {
            var barrier = new ImageMemoryBarrier
            {
                SType = StructureType.ImageMemoryBarrier,
                OldLayout = from,
                NewLayout = to,
                SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
                DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
                Image = image,
                SubresourceRange = new ImageSubresourceRange(aspect, baseMip, mipCount == 0 ? mipLevels : mipCount, 0, layers),
                SrcAccessMask = AccessFlags.MemoryWriteBit,
                DstAccessMask = AccessFlags.MemoryReadBit | AccessFlags.MemoryWriteBit
            };
            Vk.CmdPipelineBarrier(cb, PipelineStageFlags.AllCommandsBit, PipelineStageFlags.AllCommandsBit,
                0, 0, null, 0, null, 1, in barrier);
        }

        #endregion

        #region Цепочка показа

        private void _ChooseSwapFormat()
        {
            uint count = 0;
            _khrSurface.GetPhysicalDeviceSurfaceFormats(PhysicalDevice, _surface, ref count, null);
            var formats = new SurfaceFormatKHR[count];
            fixed (SurfaceFormatKHR* p = formats) _khrSurface.GetPhysicalDeviceSurfaceFormats(PhysicalDevice, _surface, ref count, p);
            _swapFormat = formats[0].Format;
            foreach (SurfaceFormatKHR f in formats)
            {
                // Без sRGB: кадровый буфер окна GL тоже линейный
                if (f.Format == Format.B8G8R8A8Unorm || f.Format == Format.R8G8B8A8Unorm)
                {
                    _swapFormat = f.Format;
                    break;
                }
            }
        }

        /// <summary>Вертикальная синхронизация (пересоздаёт цепочку показа)</summary>
        public void SetVSync(bool on)
        {
            if (_vsync == on) return;
            _vsync = on;
            Resize(Width, Height);
        }

        /// <summary>Изменился размер окна: пересоздать цепочку показа</summary>
        public void Resize(int width, int height)
        {
            if (width <= 0 || height <= 0) return;
            Vk.DeviceWaitIdle(Device);
            _DestroySwapchain();
            _CreateSwapchain(width, height);
        }

        private void _CreateSwapchain(int width, int height)
        {
            _khrSurface.GetPhysicalDeviceSurfaceCapabilities(PhysicalDevice, _surface, out SurfaceCapabilitiesKHR caps);
            _extent = caps.CurrentExtent.Width != uint.MaxValue ? caps.CurrentExtent
                : new Extent2D(
                    (uint)Math.Clamp(width, (int)caps.MinImageExtent.Width, (int)caps.MaxImageExtent.Width),
                    (uint)Math.Clamp(height, (int)caps.MinImageExtent.Height, (int)caps.MaxImageExtent.Height));
            uint images = caps.MinImageCount + 1;
            if (caps.MaxImageCount > 0 && images > caps.MaxImageCount) images = caps.MaxImageCount;

            PresentModeKHR mode = PresentModeKHR.FifoKhr;
            if (!_vsync)
            {
                uint count = 0;
                _khrSurface.GetPhysicalDeviceSurfacePresentModes(PhysicalDevice, _surface, ref count, null);
                var modes = new PresentModeKHR[count];
                fixed (PresentModeKHR* p = modes) _khrSurface.GetPhysicalDeviceSurfacePresentModes(PhysicalDevice, _surface, ref count, p);
                if (Array.IndexOf(modes, PresentModeKHR.ImmediateKhr) >= 0) mode = PresentModeKHR.ImmediateKhr;
                else if (Array.IndexOf(modes, PresentModeKHR.MailboxKhr) >= 0) mode = PresentModeKHR.MailboxKhr;
            }

            var info = new SwapchainCreateInfoKHR
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = _surface,
                MinImageCount = images,
                ImageFormat = _swapFormat,
                ImageColorSpace = ColorSpaceKHR.SpaceSrgbNonlinearKhr,
                ImageExtent = _extent,
                ImageArrayLayers = 1,
                // Перенос из изображения нужен для снимков (ReadPixels)
                ImageUsage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit,
                ImageSharingMode = SharingMode.Exclusive,
                PreTransform = caps.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
                PresentMode = mode,
                Clipped = true
            };
            _Check(_khrSwapchain.CreateSwapchain(Device, in info, null, out _swapchain), "vkCreateSwapchainKHR");
            uint count2 = 0;
            _khrSwapchain.GetSwapchainImages(Device, _swapchain, ref count2, null);
            _swapImages = new VkImage[count2];
            fixed (VkImage* p = _swapImages) _khrSwapchain.GetSwapchainImages(Device, _swapchain, ref count2, p);

            _swapViews = new ImageView[count2];
            for (int i = 0; i < count2; i++)
            {
                _swapViews[i] = CreateView(_swapImages[i], _swapFormat, ImageAspectFlags.ColorBit, ImageViewType.Type2D, 1, 1);
            }

            // Глубина окна
            CreateImage((uint)_extent.Width, (uint)_extent.Height, 1, 1, Format.D32Sfloat,
                ImageUsageFlags.DepthStencilAttachmentBit, false, out _depthImage, out _depthMemory);
            _depthView = CreateView(_depthImage, Format.D32Sfloat, ImageAspectFlags.DepthBit, ImageViewType.Type2D, 1, 1);
            RunOnce(cb => Transition(cb, _depthImage, ImageAspectFlags.DepthBit, 1, 1,
                ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal));

            _framebuffers = new Framebuffer[count2];
            var attachments = stackalloc ImageView[2];
            for (int i = 0; i < count2; i++)
            {
                attachments[0] = _swapViews[i];
                attachments[1] = _depthView;
                var fb = new FramebufferCreateInfo
                {
                    SType = StructureType.FramebufferCreateInfo,
                    RenderPass = MainPass,
                    AttachmentCount = 2,
                    PAttachments = attachments,
                    Width = _extent.Width,
                    Height = _extent.Height,
                    Layers = 1
                };
                _Check(Vk.CreateFramebuffer(Device, in fb, null, out _framebuffers[i]), "vkCreateFramebuffer");
            }
        }

        private void _DestroySwapchain()
        {
            foreach (Framebuffer fb in _framebuffers) Vk.DestroyFramebuffer(Device, fb, null);
            foreach (ImageView v in _swapViews) Vk.DestroyImageView(Device, v, null);
            Vk.DestroyImageView(Device, _depthView, null);
            Vk.DestroyImage(Device, _depthImage, null);
            Vk.FreeMemory(Device, _depthMemory, null);
            _khrSwapchain.DestroySwapchain(Device, _swapchain, null);
        }

        internal void CreateImage(uint width, uint height, uint mipLevels, uint layers, Format format, ImageUsageFlags usage,
            bool array, out VkImage image, out DeviceMemory memory)
        {
            var info = new ImageCreateInfo
            {
                SType = StructureType.ImageCreateInfo,
                ImageType = ImageType.Type2D,
                Format = format,
                Extent = new Extent3D(width, height, 1),
                MipLevels = mipLevels,
                ArrayLayers = layers,
                Samples = SampleCountFlags.Count1Bit,
                Tiling = ImageTiling.Optimal,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined
            };
            _Check(Vk.CreateImage(Device, in info, null, out image), "vkCreateImage");
            Vk.GetImageMemoryRequirements(Device, image, out MemoryRequirements req);
            var alloc = new MemoryAllocateInfo
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = req.Size,
                MemoryTypeIndex = FindMemory(req.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
            };
            _Check(Vk.AllocateMemory(Device, in alloc, null, out memory), "vkAllocateMemory");
            Vk.BindImageMemory(Device, image, memory, 0);
        }

        internal ImageView CreateView(VkImage image, Format format, ImageAspectFlags aspect, ImageViewType type,
            uint mipLevels, uint layers)
        {
            var info = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = image,
                ViewType = type,
                Format = format,
                SubresourceRange = new ImageSubresourceRange(aspect, 0, mipLevels, 0, layers)
            };
            _Check(Vk.CreateImageView(Device, in info, null, out ImageView view), "vkCreateImageView");
            return view;
        }

        private void _CreateRenderPasses()
        {
            // Окно: цвет и глубина загружаются и сохраняются (за кадр бывает несколько Submit)
            var color = new AttachmentDescription
            {
                Format = _swapFormat,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Load,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.ColorAttachmentOptimal,
                FinalLayout = ImageLayout.ColorAttachmentOptimal
            };
            var depth = new AttachmentDescription
            {
                Format = Format.D32Sfloat,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Load,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.DepthStencilAttachmentOptimal,
                FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
            };
            var colorRef = new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal);
            var depthRef = new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal);
            var subpass = new SubpassDescription
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorRef,
                PDepthStencilAttachment = &depthRef
            };
            var deps = stackalloc SubpassDependency[2];
            deps[0] = new SubpassDependency(Vk.SubpassExternal, 0,
                PipelineStageFlags.AllCommandsBit, PipelineStageFlags.AllGraphicsBit,
                AccessFlags.MemoryWriteBit, AccessFlags.MemoryReadBit | AccessFlags.MemoryWriteBit);
            deps[1] = new SubpassDependency(0, Vk.SubpassExternal,
                PipelineStageFlags.AllGraphicsBit, PipelineStageFlags.AllCommandsBit,
                AccessFlags.MemoryWriteBit, AccessFlags.MemoryReadBit | AccessFlags.MemoryWriteBit);
            var attachments = stackalloc AttachmentDescription[] { color, depth };
            var info = new RenderPassCreateInfo
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = 2,
                PAttachments = attachments,
                SubpassCount = 1,
                PSubpasses = &subpass,
                DependencyCount = 2,
                PDependencies = deps
            };
            _Check(Vk.CreateRenderPass(Device, in info, null, out MainPass), "vkCreateRenderPass");

            // Карта теней: только глубина, до и после прохода текстура читается шейдером
            var shadow = depth;
            shadow.InitialLayout = ImageLayout.ShaderReadOnlyOptimal;
            shadow.FinalLayout = ImageLayout.ShaderReadOnlyOptimal;
            var shadowRef = new AttachmentReference(0, ImageLayout.DepthStencilAttachmentOptimal);
            var shadowSubpass = new SubpassDescription
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                PDepthStencilAttachment = &shadowRef
            };
            var shadowInfo = new RenderPassCreateInfo
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = 1,
                PAttachments = &shadow,
                SubpassCount = 1,
                PSubpasses = &shadowSubpass,
                DependencyCount = 2,
                PDependencies = deps
            };
            _Check(Vk.CreateRenderPass(Device, in shadowInfo, null, out DepthPass), "vkCreateRenderPass");
        }

        private void _CreateRing()
        {
            CreateBuffer(_RingSize, BufferUsageFlags.UniformBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out _ring, out _ringMemory);
            void* p;
            Vk.MapMemory(Device, _ringMemory, 0, _RingSize, 0, &p);
            _ringPtr = (byte*)p;
        }

        private void _CreateDescriptorPool()
        {
            var sizes = stackalloc DescriptorPoolSize[]
            {
                new DescriptorPoolSize(DescriptorType.UniformBufferDynamic, 16384),
                new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 65536)
            };
            var info = new DescriptorPoolCreateInfo
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                MaxSets = 16384,
                PoolSizeCount = 2,
                PPoolSizes = sizes
            };
            _Check(Vk.CreateDescriptorPool(Device, in info, null, out _descriptorPool), "vkCreateDescriptorPool");
        }

        /// <summary>
        /// Начало кадра окна: взять изображение цепочки показа (хост перед рисованием кадра)
        /// </summary>
        public void BeginFrame()
        {
            if (_frameActive || _disposed) return;
            Result r = _khrSwapchain.AcquireNextImage(Device, _swapchain, ulong.MaxValue, _acquireSemaphore, default, ref _imageIndex);
            if (r == Result.ErrorOutOfDateKhr)
            {
                Resize(Width, Height);
                r = _khrSwapchain.AcquireNextImage(Device, _swapchain, ulong.MaxValue, _acquireSemaphore, default, ref _imageIndex);
            }
            if (r != Result.Success && r != Result.SuboptimalKhr) _Check(r, "vkAcquireNextImageKHR");
            _frameActive = true;
            _waitAcquire = true;
            // Изображение в раскладку цели рисования (содержимое прошлого кадра не нужно, кадр очищается)
            _RecordBegin();
            Transition(_cmd, _swapImages[_imageIndex], ImageAspectFlags.ColorBit, 1, 1,
                ImageLayout.Undefined, ImageLayout.ColorAttachmentOptimal);
            _RecordSubmit();
        }

        /// <summary>
        /// Конец кадра окна: показать изображение
        /// </summary>
        public void EndFrame()
        {
            if (!_frameActive || _disposed) return;
            _RecordBegin();
            Transition(_cmd, _swapImages[_imageIndex], ImageAspectFlags.ColorBit, 1, 1,
                ImageLayout.ColorAttachmentOptimal, ImageLayout.PresentSrcKhr);
            _RecordSubmit();
            uint index = _imageIndex;
            SwapchainKHR swapchain = _swapchain;
            var present = new PresentInfoKHR
            {
                SType = StructureType.PresentInfoKhr,
                SwapchainCount = 1,
                PSwapchains = &swapchain,
                PImageIndices = &index
            };
            Result r = _khrSwapchain.QueuePresent(_queue, in present);
            _frameActive = false;
            if (r == Result.ErrorOutOfDateKhr || r == Result.SuboptimalKhr) Resize(Width, Height);
        }

        #endregion

        #region Submit

        private void _RecordBegin()
        {
            Vk.ResetCommandBuffer(_cmd, 0);
            var begin = new CommandBufferBeginInfo
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit
            };
            Vk.BeginCommandBuffer(_cmd, in begin);
        }

        /// <summary>Отправить командный буфер и дождаться выполнения</summary>
        private void _RecordSubmit()
        {
            Vk.EndCommandBuffer(_cmd);
            CommandBuffer cb = _cmd;
            Semaphore wait = _acquireSemaphore;
            PipelineStageFlags stage = PipelineStageFlags.AllCommandsBit;
            var submit = new SubmitInfo
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &cb,
                WaitSemaphoreCount = _waitAcquire ? 1u : 0u,
                PWaitSemaphores = &wait,
                PWaitDstStageMask = &stage
            };
            _waitAcquire = false;
            Vk.ResetFences(Device, 1, in _fence);
            _Check(Vk.QueueSubmit(_queue, 1, in submit, _fence), "vkQueueSubmit");
            Vk.WaitForFences(Device, 1, in _fence, true, ulong.MaxValue);
            // GPU всё выполнил: кольцо uniform и наборы дескрипторов можно переиспользовать
            _ringOffset = 0;
            Vk.ResetDescriptorPool(Device, _descriptorPool, 0);
        }

        public void Submit(CommandList commands)
        {
            if (_disposed) return;
            if (!_frameActive) BeginFrame();
            ReadOnlySpan<RecordedCommand> list = commands.Commands;
            ReadOnlySpan<float> data = commands.Data;
            _RecordBegin();
            _inPass = false;
            _boundPipeline = default;
            for (int i = 0; i < list.Length; i++)
            {
                ref readonly RecordedCommand c = ref list[i];
                switch (c.Type)
                {
                    case CommandType.SetRenderTarget:
                        _EndPass();
                        _target = (VkRenderTarget?)c.Ref;
                        break;
                    case CommandType.Viewport:
                        _vx = c.A; _vy = c.B; _vw = c.C; _vh = c.D;
                        break;
                    case CommandType.Clear:
                        _Clear(data.Slice(c.A, 4), c.B != 0, c.C != 0);
                        break;
                    case CommandType.SetPipeline:
                        _pipeline = (VkPipelineObject)c.Ref!;
                        break;
                    case CommandType.SetVertexBuffer:
                        _vertexBuffers[c.A] = (VkBufferObject)c.Ref!;
                        break;
                    case CommandType.SetIndexBuffer:
                        _indexBuffer = (VkBufferObject)c.Ref!;
                        _indexFormat = (IndexFormat)c.A;
                        break;
                    case CommandType.SetTexture:
                        _textures[c.A] = (VkTexture)c.Ref!;
                        break;
                    case CommandType.UniformInt:
                        _pipeline?.Shader.SetInt(c.A, c.B);
                        break;
                    case CommandType.UniformFloat:
                        _pipeline?.Shader.SetFloats(c.A, data.Slice(c.B, c.C));
                        break;
                    case CommandType.UniformMatrix4:
                        _pipeline?.Shader.SetFloats(c.A, data.Slice(c.B, 16));
                        break;
                    case CommandType.UniformMatrix4x3:
                        _pipeline?.Shader.SetMatrix4x3(c.A, data.Slice(c.B, c.C * 12), c.C);
                        break;
                    case CommandType.Draw:
                        if (_PrepareDraw()) Vk.CmdDraw(_cmd, (uint)c.A, 1, (uint)c.B, 0);
                        break;
                    case CommandType.DrawIndexed:
                        if (_indexBuffer == null) throw new InvalidOperationException("DrawIndexed без индексного буфера");
                        if (_PrepareDraw())
                        {
                            Vk.CmdBindIndexBuffer(_cmd, _indexBuffer.Buffer, 0,
                                _indexFormat == IndexFormat.UInt32 ? IndexType.Uint32 : IndexType.Uint16);
                            Vk.CmdDrawIndexed(_cmd, (uint)c.A, 1, (uint)c.B, 0, 0);
                        }
                        break;
                }
            }
            _EndPass();
            _RecordSubmit();
        }

        private void _BeginPass()
        {
            if (_inPass) return;
            RenderPassBeginInfo info;
            if (_target == null)
            {
                info = new RenderPassBeginInfo
                {
                    SType = StructureType.RenderPassBeginInfo,
                    RenderPass = MainPass,
                    Framebuffer = _framebuffers[_imageIndex],
                    RenderArea = new Rect2D(new Offset2D(0, 0), _extent)
                };
            }
            else
            {
                info = new RenderPassBeginInfo
                {
                    SType = StructureType.RenderPassBeginInfo,
                    RenderPass = DepthPass,
                    Framebuffer = _target.Framebuffer,
                    RenderArea = new Rect2D(new Offset2D(0, 0), new Extent2D(_target.Width, _target.Height))
                };
            }
            Vk.CmdBeginRenderPass(_cmd, in info, SubpassContents.Inline);
            _inPass = true;
            _boundPipeline = default;
        }

        private void _EndPass()
        {
            if (!_inPass) return;
            Vk.CmdEndRenderPass(_cmd);
            _inPass = false;
        }

        private void _Clear(ReadOnlySpan<float> color, bool clearColor, bool clearDepth)
        {
            _BeginPass();
            var attachments = stackalloc ClearAttachment[2];
            uint n = 0;
            if (clearColor && _target == null)
            {
                var value = new ClearValue();
                value.Color.Float32_0 = color[0];
                value.Color.Float32_1 = color[1];
                value.Color.Float32_2 = color[2];
                value.Color.Float32_3 = color[3];
                attachments[n++] = new ClearAttachment(ImageAspectFlags.ColorBit, 0, value);
            }
            if (clearDepth)
            {
                var value = new ClearValue();
                value.DepthStencil.Depth = 1f;
                attachments[n++] = new ClearAttachment(ImageAspectFlags.DepthBit, 0, value);
            }
            if (n == 0) return;
            var rect = new ClearRect(new Rect2D(new Offset2D(0, 0),
                _target == null ? _extent : new Extent2D(_target.Width, _target.Height)), 0, 1);
            Vk.CmdClearAttachments(_cmd, n, attachments, 1, in rect);
        }

        /// <summary>Конвейер, видовое окно, uniform и текстуры перед вызовом отрисовки</summary>
        private bool _PrepareDraw()
        {
            if (_pipeline == null) throw new InvalidOperationException("Рисование без конвейера");
            _BeginPass();
            VkPipelineObject p = _pipeline;
            bool shadowPass = _target != null;
            Pipeline vk = p.Get(shadowPass);
            if (vk.Handle != _boundPipeline.Handle)
            {
                Vk.CmdBindPipeline(_cmd, PipelineBindPoint.Graphics, vk);
                _boundPipeline = vk;
            }

            // Видовое окно GL (начало внизу слева): окно рисуется с переворотом, цель рендера без
            Viewport viewport;
            uint fbHeight = shadowPass ? _target!.Height : _extent.Height;
            uint fbWidth = shadowPass ? _target!.Width : _extent.Width;
            if (shadowPass)
            {
                viewport = new Viewport(_vx, _vy, _vw, _vh, 0, 1);
            }
            else
            {
                viewport = new Viewport(_vx, fbHeight - _vy, _vw, -_vh, 0, 1);
            }
            Vk.CmdSetViewport(_cmd, 0, 1, in viewport);
            var scissor = new Rect2D(new Offset2D(0, 0), new Extent2D(fbWidth, fbHeight));
            Vk.CmdSetScissor(_cmd, 0, 1, in scissor);

            // Буферы вершин по слотам раскладок конвейера
            int slots = p.Description.VertexLayouts.Count;
            for (int s = 0; s < slots; s++)
            {
                VkBufferObject? b = _vertexBuffers[s];
                if (b == null || b.Buffer.Handle == 0) return false;
                VkBuffer buffer = b.Buffer;
                ulong offset = 0;
                Vk.CmdBindVertexBuffers(_cmd, (uint)s, 1, in buffer, in offset);
            }

            // Uniform шейдера в кольцо
            VkShader shader = p.Shader;
            ulong size = (ulong)shader.BlockSize;
            if (_ringOffset + size > _RingSize) throw new InvalidOperationException("Кольцо uniform Vulkan переполнено");
            fixed (byte* src = shader.Block) System.Buffer.MemoryCopy(src, _ringPtr + _ringOffset, size, size);
            uint dynamicOffset = (uint)_ringOffset;
            _ringOffset = (_ringOffset + size + _uniformAlign - 1) / _uniformAlign * _uniformAlign;

            // Набор дескрипторов: блок uniform и текстуры слотов
            DescriptorSetLayout layout = shader.SetLayout;
            var alloc = new DescriptorSetAllocateInfo
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = _descriptorPool,
                DescriptorSetCount = 1,
                PSetLayouts = &layout
            };
            _Check(Vk.AllocateDescriptorSets(Device, in alloc, out DescriptorSet set), "vkAllocateDescriptorSets");
            int samplers = shader.SamplerCount;
            var writes = stackalloc WriteDescriptorSet[1 + samplers];
            var images = stackalloc DescriptorImageInfo[Math.Max(1, samplers)];
            var bufferInfo = new DescriptorBufferInfo(_ring, 0, size);
            writes[0] = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = set,
                DstBinding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.UniformBufferDynamic,
                PBufferInfo = &bufferInfo
            };
            for (int t = 0; t < samplers; t++)
            {
                // Пустой слот: белая текстура нужного вида (в GL пустой блок даёт чёрный цвет или ошибку)
                VkTexture tex = (t < _MaxTextureSlots ? _textures[t] : null)
                    ?? (shader.SamplerIsArray(t) ? _whiteArray : _white);
                images[t] = new DescriptorImageInfo(tex.Sampler, tex.View, ImageLayout.ShaderReadOnlyOptimal);
                writes[1 + t] = new WriteDescriptorSet
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = set,
                    DstBinding = (uint)(1 + t),
                    DescriptorCount = 1,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    PImageInfo = &images[t]
                };
            }
            Vk.UpdateDescriptorSets(Device, (uint)(1 + samplers), writes, 0, null);
            Vk.CmdBindDescriptorSets(_cmd, PipelineBindPoint.Graphics, shader.PipelineLayout, 0, 1, in set, 1, in dynamicOffset);
            return true;
        }

        #endregion

        public void ReadPixels(int x, int y, int width, int height, Span<byte> rgba)
        {
            int row = width * 4;
            if (rgba.Length < row * height) throw new ArgumentException("Мало места под пиксели", nameof(rgba));
            if (!_frameActive) return;
            ulong size = (ulong)(row * height);
            CreateBuffer(size, BufferUsageFlags.TransferDstBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out VkBuffer buffer, out DeviceMemory memory);
            VkImage image = _swapImages[_imageIndex];
            // Строки сверху вниз (окно нарисовано с переворотом), y задан от низа, как в GL
            int top = (int)_extent.Height - y - height;
            RunOnce(cb =>
            {
                Transition(cb, image, ImageAspectFlags.ColorBit, 1, 1, ImageLayout.ColorAttachmentOptimal, ImageLayout.TransferSrcOptimal);
                var region = new BufferImageCopy
                {
                    BufferOffset = 0,
                    ImageSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1),
                    ImageOffset = new Offset3D(x, top, 0),
                    ImageExtent = new Extent3D((uint)width, (uint)height, 1)
                };
                Vk.CmdCopyImageToBuffer(cb, image, ImageLayout.TransferSrcOptimal, buffer, 1, in region);
                Transition(cb, image, ImageAspectFlags.ColorBit, 1, 1, ImageLayout.TransferSrcOptimal, ImageLayout.ColorAttachmentOptimal);
            });
            void* p;
            Vk.MapMemory(Device, memory, 0, size, 0, &p);
            new ReadOnlySpan<byte>(p, (int)size).CopyTo(rgba);
            Vk.UnmapMemory(Device, memory);
            Vk.DestroyBuffer(Device, buffer, null);
            Vk.FreeMemory(Device, memory, null);
            if (_swapFormat == Format.B8G8R8A8Unorm)
            {
                for (int i = 0; i < (int)size; i += 4) (rgba[i], rgba[i + 2]) = (rgba[i + 2], rgba[i]);
            }
        }

        internal void OnBufferDisposed(VkBufferObject buffer)
        {
            for (int i = 0; i < _vertexBuffers.Length; i++) if (_vertexBuffers[i] == buffer) _vertexBuffers[i] = null;
            if (_indexBuffer == buffer) _indexBuffer = null;
        }

        internal void OnTextureDisposed(VkTexture texture)
        {
            for (int i = 0; i < _textures.Length; i++) if (_textures[i] == texture) _textures[i] = null;
        }

        internal void OnPipelineDisposed(VkPipelineObject pipeline)
        {
            if (_pipeline == pipeline) _pipeline = null;
        }

        /// <summary>Устройство освобождено (окно закрывается раньше конца кадра)</summary>
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Vk.DeviceWaitIdle(Device);
            _white.Dispose();
            _whiteArray.Dispose();
            Vk.DestroyDescriptorPool(Device, _descriptorPool, null);
            Vk.UnmapMemory(Device, _ringMemory);
            Vk.DestroyBuffer(Device, _ring, null);
            Vk.FreeMemory(Device, _ringMemory, null);
            _DestroySwapchain();
            Vk.DestroyRenderPass(Device, MainPass, null);
            Vk.DestroyRenderPass(Device, DepthPass, null);
            Vk.DestroySemaphore(Device, _acquireSemaphore, null);
            Vk.DestroyFence(Device, _fence, null);
            Vk.DestroyCommandPool(Device, _commandPool, null);
            Vk.DestroyDevice(Device, null);
            _khrSurface.DestroySurface(_instance, _surface, null);
            Vk.DestroyInstance(_instance, null);
        }
    }
}
