#nullable enable
using System;
using System.Runtime.CompilerServices;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.Mesh
{
    /// <summary>
    /// Сетка линий в мире на RHI: позиция (3 float) и цвет (4 float), GL_LINES без индексов.
    /// API как у MeshLine оригинала; шейдер Line выбирает RenderMain.ShaderBindLine.
    /// </summary>
    public sealed class MeshLine : IDisposable
    {
        /// <summary>Чисел на вершину</summary>
        public const int VertexSize = 7;

        public static readonly VertexBufferLayout Layout = new VertexBufferLayout(28,
            new VertexAttribute(0, VertexFormat.Float3, 0),
            new VertexAttribute(1, VertexFormat.Float4, 12));

        private readonly RenderMain _render;
        private readonly IGpuBuffer _vbo;
        private int _countVertices;
        private long _drawnFrame = -1;

        public MeshLine(RenderMain render, BufferUsage usage)
        {
            _render = render;
            _vbo = render.Context.Device.CreateBuffer(BufferKind.Vertex, usage);
        }

        /// <summary>
        /// Перезаписать линии. Если сетка уже нарисована в этом кадре (хитбоксы перезаливают одну сетку
        /// для каждой сущности), записанное сначала отправляется: в оригинале GL рисовал сразу.
        /// </summary>
        public void Reload(float[] vertices)
        {
            if (_drawnFrame == _render.Frame) _render.Flush();
            _countVertices = vertices.Length / VertexSize;
            if (vertices.Length > 0) _vbo.SetData<float>(vertices);
        }

        /// <summary>
        /// Нарисовать линии
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

        /// <summary>
        /// Линии куба (24 вершины)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] CubeLine(float x1, float y1, float z1,
            float x2, float y2, float z2, float r, float g, float b, float a)
        {
            return new float[]
            {
                // Квад низа
                x1, y1, z1, r, g, b, a,
                x2, y1, z1, r, g, b, a,
                x1, y1, z1, r, g, b, a,
                x1, y1, z2, r, g, b, a,
                x2, y1, z1, r, g, b, a,
                x2, y1, z2, r, g, b, a,
                x1, y1, z2, r, g, b, a,
                x2, y1, z2, r, g, b, a,
                // Квад вверха
                x1, y2, z1, r, g, b, a,
                x2, y2, z1, r, g, b, a,
                x1, y2, z1, r, g, b, a,
                x1, y2, z2, r, g, b, a,
                x2, y2, z1, r, g, b, a,
                x2, y2, z2, r, g, b, a,
                x1, y2, z2, r, g, b, a,
                x2, y2, z2, r, g, b, a,
                // Стойки
                x1, y1, z1, r, g, b, a,
                x1, y2, z1, r, g, b, a,
                x2, y1, z1, r, g, b, a,
                x2, y2, z1, r, g, b, a,
                x1, y1, z2, r, g, b, a,
                x1, y2, z2, r, g, b, a,
                x2, y1, z2, r, g, b, a,
                x2, y2, z2, r, g, b, a,
            };
        }
    }
}
