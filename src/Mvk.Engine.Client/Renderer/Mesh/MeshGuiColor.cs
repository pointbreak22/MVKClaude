#nullable enable
using System;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.Mesh
{
    /// <summary>
    /// Сетка GUI из квадов на RHI: позиция (2 float), текстура (2 float), цвет (4 float).
    /// API как у MeshGuiColor оригинала: Reload в любой момент такта, Draw в проходе GUI кадра.
    /// </summary>
    public class MeshGuiColor : IDisposable
    {
        /// <summary>Чисел на вершину</summary>
        public const int VertexSize = 8;

        public static readonly VertexBufferLayout Layout = new VertexBufferLayout(32,
            new VertexAttribute(0, VertexFormat.Float2, 0),
            new VertexAttribute(1, VertexFormat.Float2, 8),
            new VertexAttribute(2, VertexFormat.Float4, 16));

        private readonly RenderMain _render;
        private readonly IGpuBuffer _vbo;
        private int _countVertices;
        /// <summary>Номер кадра, в котором сетка последний раз записана в список команд</summary>
        private long _drawnFrame = -1;

        public MeshGuiColor(RenderMain render)
        {
            _render = render;
            _vbo = render.Context.Device.CreateBuffer(BufferKind.Vertex, BufferUsage.Dynamic);
        }

        /// <summary>
        /// Перезаписать полигоны
        /// </summary>
        public void Reload(float[] vertices) => Reload(vertices, vertices.Length);

        /// <summary>
        /// Перезаписать полигоны, count чисел из массива.
        /// Список команд выполняется после записи кадра: если сетка уже нарисована в этом кадре,
        /// сначала отправляем записанное, иначе прошлая отрисовка увидит новые полигоны
        /// (в оригинале GL рисовал сразу).
        /// </summary>
        public void Reload(float[] vertices, int count)
        {
            if (_drawnFrame == _render.Frame) _render.Flush();
            _countVertices = count / VertexSize;
            if (count > 0) _vbo.SetData<float>(vertices.AsSpan(0, count));
            _render.Context.Quads.Ensure(_countVertices / 4);
        }

        /// <summary>
        /// Нарисовать сетку: шейдер и текстура уже выбраны через RenderMain
        /// </summary>
        public void Draw()
        {
            if (_countVertices > 0)
            {
                _drawnFrame = _render.Frame;
                CommandList commands = _render.Commands;
                commands.SetVertexBuffer(0, _vbo);
                commands.SetIndexBuffer(_render.Context.Quads.Buffer, IndexFormat.UInt32);
                commands.DrawIndexed(_countVertices / 4 * 6);
            }
        }

        public void Dispose() => _vbo.Dispose();
    }
}
