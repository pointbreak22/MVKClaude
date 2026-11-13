#nullable enable
using System;
using Mvk.Engine.Entity.Render;
using Mvk.Engine.Renderer.Shaders;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.Mesh
{
    /// <summary>
    /// Сетка предмета в GUI на RHI: только float-часть буфера сущности (позиция, нормаль, текстура),
    /// как MeshGuiEntity оригинала. Шейдер EntityGui уже выбран через ShadersEntity.BindUniformBeginGui.
    /// </summary>
    public sealed class MeshGuiEntity : IDisposable
    {
        private readonly RenderMain _render;
        private readonly IGpuBuffer _vbo;
        private int _countIndices;

        public MeshGuiEntity(RenderMain render)
        {
            _render = render;
            _vbo = render.Context.Device.CreateBuffer(BufferKind.Vertex, BufferUsage.Static);
        }

        /// <summary>
        /// Перезаписать полигоны
        /// </summary>
        public void Reload(VertexEntityBuffer buffers)
        {
            int quads = buffers.GetCountVertices() / 4;
            if (quads > 0)
            {
                _vbo.SetData<float>(buffers.BufferFloat);
                _render.Context.Quads.Ensure(quads);
            }
            _countIndices = quads * 6;
        }

        /// <summary>
        /// Нарисовать сетку
        /// </summary>
        public void Draw()
        {
            if (_countIndices > 0)
            {
                CommandList commands = _render.Commands;
                commands.SetVertexBuffer(0, _vbo);
                commands.SetIndexBuffer(_render.Context.Quads.Buffer, IndexFormat.UInt32);
                commands.DrawIndexed(_countIndices);
            }
        }

        public void Dispose() => _vbo.Dispose();
    }
}
