#nullable enable
using System.Collections.Generic;

namespace Mvk.Engine.Renderer.World
{
    /// <summary>
    /// Свёртка вершин сеток чанков для проверки оптимизаций этапа 7 (Mvk.Desktop --bench): после загрузки мира
    /// свёртка одинакова, только если каждая сетка совпала байт в байт. Хранится последняя сетка каждого чанка,
    /// поэтому порядок построения не важен. Выключена вне замера.
    /// </summary>
    public static class MeshDigest
    {
        /// <summary>Считать свёртку (включает хост до входа в мир)</summary>
        public static bool Enabled;

        private static readonly Dictionary<long, ulong> _chunks = new Dictionary<long, ulong>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Запомнить сетку чанка: плотную или альфа
        /// </summary>
        public static void Add(int chunkX, int chunkZ, bool alpha, VertexBlockBuffer vertex)
        {
            ulong hash = 14695981039346656037UL;
            float[] floats = vertex.BufferFloat.GetBufferAll();
            for (int i = 0; i < vertex.BufferFloat.Count; i++)
            {
                hash = (hash ^ (uint)System.BitConverter.SingleToInt32Bits(floats[i])) * 1099511628211UL;
            }
            byte[] bytes = vertex.BufferByte.GetBufferAll();
            for (int i = 0; i < vertex.BufferByte.Count; i++)
            {
                hash = (hash ^ bytes[i]) * 1099511628211UL;
            }
            long key = ((long)chunkX << 33) ^ ((long)chunkZ << 1) ^ (alpha ? 1L : 0L);
            lock (_lock) _chunks[key] = hash;
        }

        /// <summary>
        /// Сетка чанка пуста
        /// </summary>
        public static void AddEmpty(int chunkX, int chunkZ, bool alpha)
        {
            long key = ((long)chunkX << 33) ^ ((long)chunkZ << 1) ^ (alpha ? 1L : 0L);
            lock (_lock) _chunks[key] = 0;
        }

        /// <summary>Забыть сетки (новый мир)</summary>
        public static void Clear()
        {
            lock (_lock) _chunks.Clear();
        }

        /// <summary>
        /// Свёртка всех чанков (сумма, порядок не важен) и число сеток
        /// </summary>
        public static string Report()
        {
            lock (_lock)
            {
                ulong sum = 0;
                foreach (KeyValuePair<long, ulong> pair in _chunks)
                {
                    sum += (pair.Value ^ (ulong)pair.Key) * 0x9E3779B97F4A7C15UL;
                }
                return _chunks.Count + ":" + sum.ToString("x16");
            }
        }
    }
}
