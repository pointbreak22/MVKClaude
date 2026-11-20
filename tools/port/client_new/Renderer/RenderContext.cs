using System;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer
{
    /// <summary>
    /// Общие объекты рендера клиента: устройство RHI, общий индексный буфер квадов и состояние вида окна.
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
        /// <summary>
        /// Состояние вида окна: размер, Si, матрицы (в оригинале статический Gi)
        /// </summary>
        public readonly ViewState View;

        public RenderContext(IGraphicsDevice device, ViewState view)
        {
            Device = device;
            View = view;
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
        /// Сколько квадов покрывают 16-битные индексы (65536 вершин)
        /// </summary>
        public const int MaxQuads16 = 16384;
        /// <summary>
        /// Буфер индексов (uint)
        /// </summary>
        public readonly IGpuBuffer Buffer;
        /// <summary>
        /// Буфер 16-битных индексов на MaxQuads16 квадов: вдвое меньше чтения индексов у сеток до 65536 вершин
        /// (сетки секций, сущностей, GUI, частиц)
        /// </summary>
        public readonly IGpuBuffer Buffer16;
        /// <summary>
        /// На сколько квадов сейчас хватает индексов
        /// </summary>
        public int QuadCapacity { get; private set; }

        public QuadIndexBuffer(IGraphicsDevice device)
        {
            Buffer = device.CreateBuffer(BufferKind.Index, BufferUsage.Static);
            Ensure(MaxQuads16);
            Buffer16 = device.CreateBuffer(BufferKind.Index, BufferUsage.Static);
            ushort[] indices = new ushort[MaxQuads16 * 6];
            for (int q = 0; q < MaxQuads16; q++)
            {
                ushort v = (ushort)(q * 4);
                int i = q * 6;
                indices[i] = v;
                indices[i + 1] = (ushort)(v + 1);
                indices[i + 2] = (ushort)(v + 2);
                indices[i + 3] = v;
                indices[i + 4] = (ushort)(v + 2);
                indices[i + 5] = (ushort)(v + 3);
            }
            Buffer16.SetData<ushort>(indices);
        }

        /// <summary>
        /// Выбрать индексы для сетки из quadCount квадов: 16-битные, пока вершин не больше 65536, иначе 32-битные
        /// </summary>
        public void Bind(CommandList commands, int quadCount)
        {
            if (quadCount <= MaxQuads16) commands.SetIndexBuffer(Buffer16, IndexFormat.UInt16);
            else commands.SetIndexBuffer(Buffer, IndexFormat.UInt32);
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

        public void Dispose()
        {
            Buffer.Dispose();
            Buffer16.Dispose();
        }
    }
}
