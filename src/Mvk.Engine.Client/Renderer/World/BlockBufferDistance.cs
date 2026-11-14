using System;

namespace Mvk.Engine.Renderer.World
{
    /// <summary>
    /// Блок буфера, с дистанцией до камеры, нужен для сортировки альфа блоков
    /// </summary>
    public struct BlockBufferDistance : IComparable, IComparable<BlockBufferDistance>
    {
        /// <summary>
        /// Начало Float данных блока в общем буфере построителя (ChunkMesher.AlphaScratch)
        /// </summary>
        public int StartFloat;
        /// <summary>
        /// Число Float данных блока
        /// </summary>
        public int CountFloat;
        /// <summary>
        /// Начало Byte данных блока в общем буфере построителя
        /// </summary>
        public int StartByte;
        /// <summary>
        /// Число Byte данных блока
        /// </summary>
        public int CountByte;
        /// <summary>
        /// Дистанция
        /// </summary>
        public float Distance;

        public int CompareTo(BlockBufferDistance other) => Distance.CompareTo(other.Distance);

        public int CompareTo(object obj)
        {
            if (obj is BlockBufferDistance v) return Distance.CompareTo(v.Distance);
            else throw new Exception(ClientStrings.ItIsImpossibleToCompareTwoObjects);
        }
    }
}
