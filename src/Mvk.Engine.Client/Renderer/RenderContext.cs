using System;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer
{
    /// <summary>
    /// Общие объекты рендера клиента: устройство RHI и общий индексный буфер квадов.
    /// Заменяет доступ к GL через окно (window.GetOpenGL()) из оригинала.
    /// </summary>
    public sealed class RenderContext : IDisposable
    {
        /// <summary>
        /// Раскладка вершин блоков, слот 0: позиция (3 float) и текстурные координаты (2 float)
        /// </summary>
        public static readonly VertexBufferLayout BlockFloats = new VertexBufferLayout(20,
            new VertexAttribute(0, VertexFormat.Float3, 0),
            new VertexAttribute(1, VertexFormat.Float2, 12));
        /// <summary>
        /// Раскладка вершин блоков, слот 1: v_rgbl, v_anim, v_normal (по одному int)
        /// </summary>
        public static readonly VertexBufferLayout BlockInts = new VertexBufferLayout(12,
            new VertexAttribute(2, VertexFormat.Int1, 0),
            new VertexAttribute(3, VertexFormat.Int1, 4),
            new VertexAttribute(4, VertexFormat.Int1, 8));

        /// <summary>
        /// Графическое устройство
        /// </summary>
        public readonly IGraphicsDevice Device;
        /// <summary>
        /// Общие индексы квадов 0,1,2, 0,2,3 для всех мешей из четырёхугольников
        /// </summary>
        public readonly QuadIndexBuffer Quads;

        public RenderContext(IGraphicsDevice device)
        {
            Device = device;
            Quads = new QuadIndexBuffer(device);
        }

        public void Dispose() => Quads.Dispose();
    }

    /// <summary>
    /// Один индексный буфер квадов на всё устройство. В оригинале каждый меш создавал
    /// и заливал такой же массив индексов сам (MeshBase._QuadIndices) при каждой перезаливке.
    /// </summary>
    public sealed class QuadIndexBuffer : IDisposable
    {
        /// <summary>
        /// Буфер индексов (uint)
        /// </summary>
        public readonly IGpuBuffer Buffer;
        /// <summary>
        /// На сколько квадов сейчас хватает индексов
        /// </summary>
        public int QuadCapacity { get; private set; }

        public QuadIndexBuffer(IGraphicsDevice device)
        {
            Buffer = device.CreateBuffer(BufferKind.Index, BufferUsage.Static);
            Ensure(16384);
        }

        /// <summary>
        /// Гарантировать индексы на столько квадов. Вызывать в потоке рендера.
        /// Начало буфера при росте не меняется, поэтому уже записанные команды остаются верными.
        /// </summary>
        public void Ensure(int quadCount)
        {
            if (quadCount <= QuadCapacity) return;
            int capacity = Math.Max(quadCount, QuadCapacity + QuadCapacity / 2);
            uint[] indices = new uint[capacity * 6];
            for (int q = 0; q < capacity; q++)
            {
                uint v = (uint)(q * 4);
                int i = q * 6;
                indices[i] = v;
                indices[i + 1] = v + 1;
                indices[i + 2] = v + 2;
                indices[i + 3] = v;
                indices[i + 4] = v + 2;
                indices[i + 5] = v + 3;
            }
            Buffer.SetData<uint>(indices);
            QuadCapacity = capacity;
        }

        public void Dispose() => Buffer.Dispose();
    }
}
