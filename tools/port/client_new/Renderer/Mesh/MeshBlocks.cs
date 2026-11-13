using System;
using System.Runtime.CompilerServices;
using Mvk.Engine.Renderer.World;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.Mesh
{
    /// <summary>
    /// Сетка блоков чанка на RHI: два вершинных буфера (float и int), общий индексный буфер квадов.
    /// Логика статусов и передачи буфера из потока построения сеток как в MeshBlocks оригинала.
    /// </summary>
    public class MeshBlocks : IDisposable
    {
        /// <summary>
        /// Пометка изменения, требуется рендер
        /// </summary>
        public bool IsModifiedRender = false;
        /// <summary>
        /// Статус обработки сетки
        /// </summary>
        public StatusMesh Status { get; private set; } = StatusMesh.Null;

        private readonly RenderContext _context;
        private readonly BufferSlot _bufferFloat = new BufferSlot();
        private readonly BufferSlot _bufferByte = new BufferSlot();
        private readonly IGpuBuffer _vboFloat;
        private readonly IGpuBuffer _vboByte;
        /// <summary>
        /// Количество вершин в буфере, ожидающем загрузки
        /// </summary>
        private int _cv;
        /// <summary>
        /// Количество индексов загруженной сетки
        /// </summary>
        private int _countIndices;

        public MeshBlocks(RenderContext context)
        {
            _context = context;
            _vboFloat = context.Device.CreateBuffer(BufferKind.Vertex, BufferUsage.Dynamic);
            _vboByte = context.Device.CreateBuffer(BufferKind.Vertex, BufferUsage.Dynamic);
        }

        /// <summary>
        /// Записать отрисовку сетки. Конвейер и униформы чанка уже должны быть выбраны.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(CommandList commands)
        {
            if (_countIndices > 0)
            {
                Debug.MeshCount++;
                commands.SetVertexBuffer(0, _vboFloat);
                commands.SetVertexBuffer(1, _vboByte);
                commands.SetIndexBuffer(_context.Quads.Buffer, IndexFormat.UInt32);
                commands.DrawIndexed(_countIndices);
            }
        }

        /// <summary>
        /// Загрузить ожидающие буферы на GPU (поток рендера)
        /// </summary>
        private unsafe void _Reload(int countVertices)
        {
            _vboFloat.SetData(new ReadOnlySpan<byte>((void*)_bufferFloat.Buffer, _bufferFloat.Size));
            _vboByte.SetData(new ReadOnlySpan<byte>((void*)_bufferByte.Buffer, _bufferByte.Size));
            int quads = countVertices / 4;
            _context.Quads.Ensure(quads);
            _countIndices = quads * 6;
        }

        public void Dispose()
        {
            _bufferFloat.Dispose();
            _bufferByte.Dispose();
            _vboFloat.Dispose();
            _vboByte.Dispose();
            _countIndices = 0;
            Status = StatusMesh.Null;
        }

        /// <summary>
        /// Изменить статус на рендеринг
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StatusRendering()
        {
            IsModifiedRender = false;
            Status = StatusMesh.Rendering;
        }

        /// <summary>
        /// Буфер внесён (поток построения сеток)
        /// </summary>
        public void SetBuffer(VertexBlockBuffer vertexBuffer)
        {
            _cv = vertexBuffer.GetCountVertices();
            if (_cv > 0)
            {
                _bufferFloat.Set(vertexBuffer.BufferFloat);
                _bufferByte.Set(vertexBuffer.BufferByte);
            }
            else
            {
                _bufferFloat.Clear();
                _bufferByte.Clear();
            }
            Status = StatusMesh.Binding;
        }

        /// <summary>
        /// Буфер пуст
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBufferClear()
        {
            _bufferFloat.Clear();
            _bufferByte.Clear();
            Status = StatusMesh.Binding;
        }

        /// <summary>
        /// Занести буфер в GPU (поток рендера)
        /// </summary>
        public void BindBuffer()
        {
            if (!_bufferFloat.Empty)
            {
                _Reload(_cv);
                _bufferFloat.Clear();
                _bufferByte.Clear();
                Status = StatusMesh.Wait;
            }
            else
            {
                // Как в оригинале: пустая сетка не рисуется (там оставались старые индексы, но статус Null)
                _countIndices = 0;
                Status = StatusMesh.Null;
            }
        }

        /// <summary>
        /// Статус обработки сетки
        /// </summary>
        public enum StatusMesh
        {
            /// <summary>Пустой</summary>
            Null,
            /// <summary>Ждём</summary>
            Wait,
            /// <summary>Процесс рендеринга</summary>
            Rendering,
            /// <summary>Процесс связывания сетки с GPU</summary>
            Binding
        }
    }
}
