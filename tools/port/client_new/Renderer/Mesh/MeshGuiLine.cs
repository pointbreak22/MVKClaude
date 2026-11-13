#nullable enable
using System;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.Mesh
{
    /// <summary>
    /// Сетка GUI из линий на RHI: позиция (2 float), цвет (4 float), рисуется GL_LINES без индексов
    /// </summary>
    public class MeshGuiLine : IDisposable
    {
        /// <summary>Чисел на вершину</summary>
        public const int VertexSize = 6;

        public static readonly VertexBufferLayout Layout = new VertexBufferLayout(24,
            new VertexAttribute(0, VertexFormat.Float2, 0),
            new VertexAttribute(1, VertexFormat.Float4, 8));

        private readonly RenderMain _render;
        private readonly IGpuBuffer _vbo;
        private int _countVertices;
        private long _drawnFrame = -1;

        public MeshGuiLine(RenderMain render)
        {
            _render = render;
            _vbo = render.Context.Device.CreateBuffer(BufferKind.Vertex, BufferUsage.Dynamic);
        }

        /// <summary>
        /// Перезаписать линии
        /// </summary>
        public void Reload(float[] vertices)
        {
            if (_drawnFrame == _render.Frame) _render.Flush();
            _countVertices = vertices.Length / VertexSize;
            if (vertices.Length > 0) _vbo.SetData<float>(vertices);
        }

        /// <summary>
        /// Нарисовать линии: шейдер уже выбран через RenderMain.ShaderBindGuiLine
        /// </summary>
        public void Draw()
        {
            if (_countVertices > 0)
            {
                _drawnFrame = _render.Frame;
                _render.Commands.SetVertexBuffer(0, _vbo);
                _render.Commands.Draw(_countVertices);
            }
        }

        public void Dispose() => _vbo.Dispose();
    }
}
