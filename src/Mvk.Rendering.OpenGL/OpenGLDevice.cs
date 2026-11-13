using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace Mvk.Rendering.OpenGL
{
    /// <summary>
    /// Реализация графического устройства на OpenGL 3.3 core / OpenGL ES 3.0.
    /// Проигрывает CommandList вызовами GL, помня текущее состояние, чтобы не дёргать драйвер зря.
    /// VAO создаются лениво и кешируются по сочетанию (конвейер, вершинные буферы, индексный буфер).
    /// </summary>
    public sealed class OpenGLDevice : IGraphicsDevice
    {
        /// <summary>Сколько слотов вершинных буферов поддерживается</summary>
        public const int MaxVertexSlots = 4;
        /// <summary>Сколько текстурных слотов отслеживается</summary>
        private const int MaxTextureSlots = 16;

        internal GL Gl { get; }
        /// <summary>Контекст OpenGL ES (нет BGRA, CLAMP_TO_BORDER, PolygonMode)</summary>
        internal bool IsGles { get; }

        private readonly Dictionary<VaoKey, uint> _vaos = new();
        private readonly List<VaoKey> _removeKeys = new();

        // Текущее состояние GL
        private GLPipeline? _pipeline;
        private uint _program;
        private uint _vao;
        private bool _stateKnown;
        private RasterState _raster;
        private BlendState _blend;
        private bool _colorWrite;
        private readonly GLBuffer?[] _vertexBuffers = new GLBuffer?[MaxVertexSlots];
        private GLBuffer? _indexBuffer;
        private IndexFormat _indexFormat;
        private bool _vaoDirty = true;
        private readonly uint[] _textures = new uint[MaxTextureSlots];

        public GraphicsBackend Backend => GraphicsBackend.OpenGL;
        public string Description { get; }

        public OpenGLDevice(GL gl)
        {
            Gl = gl;
            string version = gl.GetStringS(StringName.Version) ?? "";
            string renderer = gl.GetStringS(StringName.Renderer) ?? "";
            IsGles = version.Contains("OpenGL ES", StringComparison.Ordinal);
            Description = $"OpenGL {version} / {renderer}";
            // Строки текстур не выравниваются на 4 байта
            gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            gl.PixelStore(PixelStoreParameter.PackAlignment, 1);
        }

        public IGpuBuffer CreateBuffer(BufferKind kind, BufferUsage usage) => new GLBuffer(this, kind, usage);

        public ITexture CreateTexture(in TextureDescription description) => new GLTexture(this, description);

        public IShader CreateShader(ShaderDescription description) => new GLShader(this, description);

        public IPipeline CreatePipeline(PipelineDescription description)
        {
            int slots = description.VertexLayouts.Count;
            if (slots > MaxVertexSlots)
                throw new ArgumentException($"Слотов вершин больше {MaxVertexSlots}", nameof(description));
            if (IsGles && description.Raster.Fill == FillMode.Wireframe)
                throw new NotSupportedException("Wireframe недоступен в OpenGL ES");
            return new GLPipeline(this, description);
        }

        public unsafe void Submit(CommandList commands)
        {
            ReadOnlySpan<RecordedCommand> list = commands.Commands;
            ReadOnlySpan<float> data = commands.Data;
            for (int i = 0; i < list.Length; i++)
            {
                ref readonly RecordedCommand c = ref list[i];
                switch (c.Type)
                {
                    case CommandType.Viewport:
                        Gl.Viewport(c.A, c.B, (uint)c.C, (uint)c.D);
                        break;
                    case CommandType.Clear:
                        DoClear(data.Slice(c.A, 4), c.B != 0, c.C != 0);
                        break;
                    case CommandType.SetPipeline:
                        BindPipeline((GLPipeline)c.Ref!);
                        break;
                    case CommandType.SetVertexBuffer:
                        {
                            var b = (GLBuffer)c.Ref!;
                            if (_vertexBuffers[c.A] != b) { _vertexBuffers[c.A] = b; _vaoDirty = true; }
                            break;
                        }
                    case CommandType.SetIndexBuffer:
                        {
                            var b = (GLBuffer)c.Ref!;
                            if (_indexBuffer != b) { _indexBuffer = b; _vaoDirty = true; }
                            _indexFormat = (IndexFormat)c.A;
                            break;
                        }
                    case CommandType.SetTexture:
                        {
                            var t = (GLTexture)c.Ref!;
                            if (_textures[c.A] != t.Handle)
                            {
                                Gl.ActiveTexture(TextureUnit.Texture0 + c.A);
                                Gl.BindTexture(t.Target, t.Handle);
                                _textures[c.A] = t.Handle;
                            }
                            break;
                        }
                    case CommandType.UniformInt:
                        Gl.Uniform1(c.A, c.B);
                        break;
                    case CommandType.UniformFloat:
                        {
                            ReadOnlySpan<float> v = data.Slice(c.B, c.C);
                            switch (c.C)
                            {
                                case 1: Gl.Uniform1(c.A, v[0]); break;
                                case 2: Gl.Uniform2(c.A, v[0], v[1]); break;
                                case 3: Gl.Uniform3(c.A, v[0], v[1], v[2]); break;
                                default: Gl.Uniform4(c.A, v[0], v[1], v[2], v[3]); break;
                            }
                            break;
                        }
                    case CommandType.UniformMatrix4:
                        fixed (float* p = data.Slice(c.B, 16))
                        {
                            Gl.UniformMatrix4(c.A, (uint)c.C, false, p);
                        }
                        break;
                    case CommandType.UniformMatrix4x3:
                        fixed (float* p = data.Slice(c.B, c.C * 12))
                        {
                            Gl.UniformMatrix4x3(c.A, (uint)c.C, false, p);
                        }
                        break;
                    case CommandType.Draw:
                        PrepareDraw();
                        Gl.DrawArrays(_pipeline!.Primitive, c.B, (uint)c.A);
                        break;
                    case CommandType.DrawIndexed:
                        {
                            PrepareDraw();
                            if (_indexBuffer == null) throw new InvalidOperationException("DrawIndexed без индексного буфера");
                            bool u32 = _indexFormat == IndexFormat.UInt32;
                            Gl.DrawElements(_pipeline!.Primitive, (uint)c.A,
                                u32 ? DrawElementsType.UnsignedInt : DrawElementsType.UnsignedShort,
                                (void*)(nint)(c.B * (u32 ? 4 : 2)));
                            break;
                        }
                }
            }
        }

        public unsafe void ReadPixels(int x, int y, int width, int height, Span<byte> rgba)
        {
            int row = width * 4;
            if (rgba.Length < row * height) throw new ArgumentException("Мало места под пиксели", nameof(rgba));
            fixed (byte* p = rgba)
            {
                Gl.ReadPixels(x, y, (uint)width, (uint)height,
                    Silk.NET.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, p);
            }
            // GL отдаёт строки снизу вверх, переворачиваем
            Span<byte> tmp = row <= 16384 ? stackalloc byte[row] : new byte[row];
            for (int top = 0, bottom = height - 1; top < bottom; top++, bottom--)
            {
                Span<byte> a = rgba.Slice(top * row, row);
                Span<byte> b = rgba.Slice(bottom * row, row);
                a.CopyTo(tmp);
                b.CopyTo(a);
                tmp.CopyTo(b);
            }
        }

        private void DoClear(ReadOnlySpan<float> color, bool clearColor, bool clearDepth)
        {
            ClearBufferMask mask = 0;
            if (clearColor)
            {
                Gl.ClearColor(color[0], color[1], color[2], color[3]);
                // Маски записи влияют на glClear, поэтому временно открываем их
                if (!_colorWrite || !_stateKnown) Gl.ColorMask(true, true, true, true);
                mask |= ClearBufferMask.ColorBufferBit;
            }
            if (clearDepth)
            {
                if (!_raster.DepthWrite || !_stateKnown) Gl.DepthMask(true);
                mask |= ClearBufferMask.DepthBufferBit;
            }
            if (mask != 0) Gl.Clear(mask);
            // Следующий конвейер выставит состояние заново
            _stateKnown = false;
            _pipeline = null;
        }

        private void BindPipeline(GLPipeline p)
        {
            if (_pipeline == p) return;
            _pipeline = p;
            _vaoDirty = true;
            if (_program != p.Shader.Program)
            {
                _program = p.Shader.Program;
                Gl.UseProgram(_program);
            }
            PipelineDescription d = p.Description;
            RasterState r = d.Raster;
            bool all = !_stateKnown;

            if (all || r.Cull != _raster.Cull)
            {
                if (r.Cull == CullMode.None) Gl.Disable(EnableCap.CullFace);
                else
                {
                    Gl.Enable(EnableCap.CullFace);
                    Gl.CullFace(r.Cull == CullMode.Back ? TriangleFace.Back : TriangleFace.Front);
                }
            }
            if (!IsGles && (all || r.Fill != _raster.Fill))
            {
                Gl.PolygonMode(TriangleFace.FrontAndBack,
                    r.Fill == FillMode.Wireframe ? PolygonMode.Line : PolygonMode.Fill);
            }
            if (all || r.DepthTest != _raster.DepthTest)
            {
                if (r.DepthTest) Gl.Enable(EnableCap.DepthTest); else Gl.Disable(EnableCap.DepthTest);
            }
            if (all || r.DepthWrite != _raster.DepthWrite) Gl.DepthMask(r.DepthWrite);
            if (all || r.DepthFunc != _raster.DepthFunc) Gl.DepthFunc(GLConvert.Depth(r.DepthFunc));
            if (all || r.DepthBiasFactor != _raster.DepthBiasFactor || r.DepthBiasUnits != _raster.DepthBiasUnits)
            {
                if (r.HasDepthBias)
                {
                    Gl.Enable(EnableCap.PolygonOffsetFill);
                    Gl.PolygonOffset(r.DepthBiasFactor, r.DepthBiasUnits);
                }
                else Gl.Disable(EnableCap.PolygonOffsetFill);
            }
            _raster = r;

            BlendState b = d.Blend;
            if (all || b != _blend)
            {
                if (b.Enabled)
                {
                    Gl.Enable(EnableCap.Blend);
                    Gl.BlendFuncSeparate(GLConvert.Blend(b.SrcColor), GLConvert.Blend(b.DstColor),
                        GLConvert.Blend(b.SrcAlpha), GLConvert.Blend(b.DstAlpha));
                }
                else Gl.Disable(EnableCap.Blend);
                _blend = b;
            }
            if (all || d.ColorWrite != _colorWrite)
            {
                Gl.ColorMask(d.ColorWrite, d.ColorWrite, d.ColorWrite, d.ColorWrite);
                _colorWrite = d.ColorWrite;
            }
            _stateKnown = true;
        }

        /// <summary>Привязать VAO под текущий конвейер и буферы</summary>
        private void PrepareDraw()
        {
            if (_pipeline == null) throw new InvalidOperationException("Рисование без конвейера");
            if (!_vaoDirty) return;
            var key = new VaoKey(_pipeline, _vertexBuffers[0], _vertexBuffers[1], _vertexBuffers[2],
                _vertexBuffers[3], _indexBuffer);
            if (!_vaos.TryGetValue(key, out uint vao))
            {
                vao = CreateVao(_pipeline);
                _vaos.Add(key, vao);
            }
            if (_vao != vao)
            {
                Gl.BindVertexArray(vao);
                _vao = vao;
            }
            _vaoDirty = false;
        }

        private unsafe uint CreateVao(GLPipeline p)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            _vao = vao;
            IReadOnlyList<VertexBufferLayout> layouts = p.Description.VertexLayouts;
            for (int slot = 0; slot < layouts.Count; slot++)
            {
                GLBuffer buffer = _vertexBuffers[slot]
                    ?? throw new InvalidOperationException($"Конвейер {p.Shader.Name}: нет вершинного буфера в слоте {slot}");
                VertexBufferLayout layout = layouts[slot];
                Gl.BindBuffer(BufferTargetARB.ArrayBuffer, buffer.Handle);
                foreach (VertexAttribute a in layout.Attributes)
                {
                    uint loc = (uint)a.Location;
                    Gl.EnableVertexAttribArray(loc);
                    (int size, bool integer) = GLConvert.Attribute(a.Format);
                    void* offset = (void*)(nint)a.Offset;
                    if (integer)
                    {
                        Gl.VertexAttribIPointer(loc, size, VertexAttribIType.Int, (uint)layout.Stride, offset);
                    }
                    else if (a.Format == VertexFormat.UByte4Norm)
                    {
                        Gl.VertexAttribPointer(loc, 4, VertexAttribPointerType.UnsignedByte, true, (uint)layout.Stride, offset);
                    }
                    else
                    {
                        Gl.VertexAttribPointer(loc, size, VertexAttribPointerType.Float, false, (uint)layout.Stride, offset);
                    }
                }
            }
            if (_indexBuffer != null)
            {
                Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer.Handle);
            }
            return vao;
        }

        internal void OnBufferDisposed(GLBuffer buffer)
        {
            for (int i = 0; i < MaxVertexSlots; i++)
            {
                if (_vertexBuffers[i] == buffer) { _vertexBuffers[i] = null; _vaoDirty = true; }
            }
            if (_indexBuffer == buffer) { _indexBuffer = null; _vaoDirty = true; }
            RemoveVaos(k => k.Uses(buffer));
        }

        internal void OnPipelineDisposed(GLPipeline pipeline)
        {
            if (_pipeline == pipeline) { _pipeline = null; _vaoDirty = true; }
            RemoveVaos(k => k.Pipeline == pipeline);
        }

        /// <summary>Привязка текстур изменена в обход кеша</summary>
        internal void ForgetTextureBinding() => Array.Clear(_textures);

        /// <summary>Программа изменена в обход кеша</summary>
        internal void ForgetProgram()
        {
            _program = 0;
            _pipeline = null;
            Gl.UseProgram(0);
        }

        private void RemoveVaos(Predicate<VaoKey> match)
        {
            _removeKeys.Clear();
            foreach (KeyValuePair<VaoKey, uint> kv in _vaos)
            {
                if (match(kv.Key)) _removeKeys.Add(kv.Key);
            }
            foreach (VaoKey k in _removeKeys)
            {
                uint vao = _vaos[k];
                if (_vao == vao) { Gl.BindVertexArray(0); _vao = 0; }
                Gl.DeleteVertexArray(vao);
                _vaos.Remove(k);
            }
        }

        public void Dispose()
        {
            foreach (uint vao in _vaos.Values) Gl.DeleteVertexArray(vao);
            _vaos.Clear();
            Gl.Dispose();
        }

        /// <summary>Ключ кеша VAO</summary>
        private readonly record struct VaoKey(GLPipeline Pipeline,
            GLBuffer? V0, GLBuffer? V1, GLBuffer? V2, GLBuffer? V3, GLBuffer? Index)
        {
            public bool Uses(GLBuffer b) => V0 == b || V1 == b || V2 == b || V3 == b || Index == b;
        }
    }
}
