#nullable enable
using System;
using System.Runtime.CompilerServices;
using Mvk.Engine.Entity.Render;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.Mesh
{
    /// <summary>
    /// Сетка сущности с текстурой на RHI: два вершинных буфера (float и int, как MeshEntity оригинала)
    /// и общий индексный буфер квадов.
    /// </summary>
    public sealed class MeshEntity : IDisposable
    {
        private readonly RenderContext _context;
        private readonly IGpuBuffer _vboFloat;
        private readonly IGpuBuffer _vboInt;
        /// <summary>
        /// Количество индексов загруженной сетки
        /// </summary>
        private int _countIndices;

        public MeshEntity(RenderContext context, BufferUsage usage)
        {
            _context = context;
            _vboFloat = context.Device.CreateBuffer(BufferKind.Vertex, usage);
            _vboInt = context.Device.CreateBuffer(BufferKind.Vertex, usage);
        }

        /// <summary>
        /// Перезаписать полигоны
        /// </summary>
        public void Reload(VertexEntityBuffer buffers) => Reload(buffers.BufferFloat, buffers.BufferInt, buffers.GetCountVertices());

        /// <summary>
        /// Перезаписать полигоны из массивов float и int
        /// </summary>
        public void Reload(ReadOnlySpan<float> bufferFloat, ReadOnlySpan<int> bufferInt, int countVertices)
        {
            int quads = countVertices / 4;
            if (quads > 0)
            {
                _vboFloat.SetData(bufferFloat);
                _vboInt.SetData(bufferInt);
                _context.Quads.Ensure(quads);
            }
            _countIndices = quads * 6;
        }

        /// <summary>
        /// Записать отрисовку сетки. Конвейер и юниформы уже должны быть заданы.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(CommandList commands)
        {
            if (_countIndices > 0)
            {
                commands.SetVertexBuffer(0, _vboFloat);
                commands.SetVertexBuffer(1, _vboInt);
                commands.SetIndexBuffer(_context.Quads.Buffer, IndexFormat.UInt32);
                commands.DrawIndexed(_countIndices);
            }
        }

        public void Dispose()
        {
            _vboFloat.Dispose();
            _vboInt.Dispose();
            _countIndices = 0;
        }
    }
}
