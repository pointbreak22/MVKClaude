using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace Mvk.Rendering.OpenGL
{
    /// <summary>
    /// Буфер OpenGL. Загрузка всегда идёт через цель ARRAY_BUFFER,
    /// чтобы не трогать привязку индексов текущего VAO.
    /// </summary>
    internal sealed class GLBuffer : IGpuBuffer
    {
        private readonly OpenGLDevice _device;
        private readonly GL _gl;
        private readonly BufferUsageARB _usage;
        /// <summary>Сколько байт выделено на GPU</summary>
        private int _capacity;

        public uint Handle { get; private set; }
        public BufferKind Kind { get; }
        public int Size { get; private set; }

        public GLBuffer(OpenGLDevice device, BufferKind kind, BufferUsage usage)
        {
            _device = device;
            _gl = device.Gl;
            Kind = kind;
            _usage = usage == BufferUsage.Static ? BufferUsageARB.StaticDraw : BufferUsageARB.DynamicDraw;
            Handle = _gl.GenBuffer();
        }

        public unsafe void SetData<T>(ReadOnlySpan<T> data) where T : unmanaged
        {
            int bytes = data.Length * sizeof(T);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, Handle);
            fixed (T* p = data)
            {
                if (bytes > _capacity)
                {
                    _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)bytes, p, _usage);
                    _capacity = bytes;
                }
                else if (bytes > 0)
                {
                    _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)bytes, p);
                }
            }
            Size = bytes;
        }

        public void Dispose()
        {
            if (Handle == 0) return;
            _device.OnBufferDisposed(this);
            _gl.DeleteBuffer(Handle);
            Handle = 0;
        }
    }

    /// <summary>
    /// Текстура OpenGL: TEXTURE_2D или TEXTURE_2D_ARRAY, хранение RGBA8.
    /// Все уровни выделяются при создании, SetData только дописывает пиксели.
    /// </summary>
    internal sealed class GLTexture : ITexture
    {
        private readonly GL _gl;

        public uint Handle { get; private set; }
        public TextureTarget Target { get; }
        public TextureDescription Description { get; }

        public unsafe GLTexture(OpenGLDevice device, in TextureDescription d)
        {
            if (d.Width <= 0 || d.Height <= 0 || d.Layers <= 0 || d.MipLevels <= 0)
                throw new ArgumentException("Неверный размер текстуры " + d);
            _gl = device.Gl;
            Description = d;
            Target = d.Layers > 1 ? TextureTarget.Texture2DArray : TextureTarget.Texture2D;
            Handle = _gl.GenTexture();
            _gl.BindTexture(Target, Handle);

            for (int level = 0; level < d.MipLevels; level++)
            {
                uint w = (uint)Math.Max(1, d.Width >> level);
                uint h = (uint)Math.Max(1, d.Height >> level);
                if (Target == TextureTarget.Texture2D)
                {
                    _gl.TexImage2D(Target, level, InternalFormat.Rgba8, w, h, 0,
                        Silk.NET.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, null);
                }
                else
                {
                    _gl.TexImage3D(Target, level, InternalFormat.Rgba8, w, h, (uint)d.Layers, 0,
                        Silk.NET.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, null);
                }
            }

            SamplerState s = d.Sampler;
            MipFilter mip = d.MipLevels > 1 ? s.MipFilter : MipFilter.None;
            _gl.TexParameter(Target, TextureParameterName.TextureMinFilter, GLConvert.MinFilter(s.MinFilter, mip));
            _gl.TexParameter(Target, TextureParameterName.TextureMagFilter, GLConvert.MagFilter(s.MagFilter));
            int wrap = GLConvert.Wrap(s.Wrap, device.IsGles);
            _gl.TexParameter(Target, TextureParameterName.TextureWrapS, wrap);
            _gl.TexParameter(Target, TextureParameterName.TextureWrapT, wrap);
            _gl.TexParameter(Target, TextureParameterName.TextureBaseLevel, 0);
            _gl.TexParameter(Target, TextureParameterName.TextureMaxLevel, d.MipLevels - 1);
            device.ForgetTextureBinding();
        }

        public unsafe void SetData(ReadOnlySpan<byte> pixels, PixelFormat format, int level = 0, int layer = 0)
        {
            TextureDescription d = Description;
            if ((uint)level >= (uint)d.MipLevels) throw new ArgumentOutOfRangeException(nameof(level));
            if ((uint)layer >= (uint)d.Layers) throw new ArgumentOutOfRangeException(nameof(layer));
            uint w = (uint)Math.Max(1, d.Width >> level);
            uint h = (uint)Math.Max(1, d.Height >> level);
            if (pixels.Length < w * h * 4)
                throw new ArgumentException($"Мало данных для уровня {level}: {pixels.Length} < {w * h * 4}");

            _gl.BindTexture(Target, Handle);
            fixed (byte* p = pixels)
            {
                if (Target == TextureTarget.Texture2D)
                {
                    _gl.TexSubImage2D(Target, level, 0, 0, w, h, GLConvert.Pixel(format), PixelType.UnsignedByte, p);
                }
                else
                {
                    _gl.TexSubImage3D(Target, level, 0, 0, layer, w, h, 1, GLConvert.Pixel(format), PixelType.UnsignedByte, p);
                }
            }
        }

        public void GenerateMipmaps()
        {
            _gl.BindTexture(Target, Handle);
            _gl.GenerateMipmap(Target);
        }

        public void Dispose()
        {
            if (Handle == 0) return;
            _gl.DeleteTexture(Handle);
            Handle = 0;
        }
    }

    /// <summary>
    /// Программа шейдеров OpenGL. Входы привязываются к location по ShaderDescription.Attributes,
    /// sampler-униформы к слотам по ShaderDescription.Samplers (как ShaderProgram оригинала).
    /// </summary>
    internal sealed class GLShader : IShader
    {
        private readonly GL _gl;
        private readonly Dictionary<string, int> _uniforms = new();

        public uint Program { get; private set; }
        public string Name { get; }

        public GLShader(OpenGLDevice device, ShaderDescription d)
        {
            _gl = device.Gl;
            Name = d.Name;
            uint vs = Compile(ShaderType.VertexShader, d.VertexSource, "vertex");
            uint fs = Compile(ShaderType.FragmentShader, d.FragmentSource, "fragment");
            Program = _gl.CreateProgram();
            _gl.AttachShader(Program, vs);
            _gl.AttachShader(Program, fs);
            for (int i = 0; i < d.Attributes.Count; i++)
            {
                _gl.BindAttribLocation(Program, (uint)i, d.Attributes[i]);
            }
            _gl.LinkProgram(Program);
            _gl.GetProgram(Program, ProgramPropertyARB.LinkStatus, out int ok);
            _gl.DetachShader(Program, vs);
            _gl.DetachShader(Program, fs);
            _gl.DeleteShader(vs);
            _gl.DeleteShader(fs);
            if (ok == 0)
            {
                string log = _gl.GetProgramInfoLog(Program);
                _gl.DeleteProgram(Program);
                throw new InvalidOperationException($"Шейдер {Name}: ошибка линковки\n{log}");
            }

            if (d.Samplers.Count > 0)
            {
                _gl.UseProgram(Program);
                for (int i = 0; i < d.Samplers.Count; i++)
                {
                    int loc = _gl.GetUniformLocation(Program, d.Samplers[i]);
                    if (loc >= 0) _gl.Uniform1(loc, i);
                }
                device.ForgetProgram();
            }
        }

        private uint Compile(ShaderType type, string source, string kind)
        {
            uint h = _gl.CreateShader(type);
            _gl.ShaderSource(h, source);
            _gl.CompileShader(h);
            _gl.GetShader(h, ShaderParameterName.CompileStatus, out int ok);
            if (ok == 0)
            {
                string log = _gl.GetShaderInfoLog(h);
                _gl.DeleteShader(h);
                throw new InvalidOperationException($"Шейдер {Name} ({kind}): ошибка компиляции\n{log}");
            }
            return h;
        }

        public UniformHandle GetUniform(string name)
        {
            if (!_uniforms.TryGetValue(name, out int loc))
            {
                loc = _gl.GetUniformLocation(Program, name);
                _uniforms[name] = loc;
            }
            return new UniformHandle(loc);
        }

        public void Dispose()
        {
            if (Program == 0) return;
            _gl.DeleteProgram(Program);
            Program = 0;
        }
    }

    /// <summary>
    /// Конвейер OpenGL: описание плюс заранее посчитанные константы
    /// </summary>
    internal sealed class GLPipeline : IPipeline
    {
        private readonly OpenGLDevice _device;

        public PipelineDescription Description { get; }
        public GLShader Shader { get; }
        public PrimitiveType Primitive { get; }

        public GLPipeline(OpenGLDevice device, PipelineDescription d)
        {
            _device = device;
            Description = d;
            Shader = d.Shader as GLShader
                ?? throw new ArgumentException("Шейдер создан не этим устройством", nameof(d));
            Primitive = GLConvert.Topology(d.Topology);
        }

        public void Dispose() => _device.OnPipelineDisposed(this);
    }
}
