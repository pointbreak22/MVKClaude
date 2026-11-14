namespace Mvk.Engine.Renderer.World
{
    /// <summary>
    /// Буфер секций чанка
    /// </summary>
    public class ChunkSectionBuffer
    {
        /// <summary>
        /// Пометка изменения
        /// </summary>
        public bool IsModifiedRender;

        /// <summary>
        /// Буфер для склейки рендера, Float данных
        /// </summary>
        public float[] BufferFloat;

        /// <summary>
        /// Буфер для склейки рендера, Byte данных
        /// </summary>
        public byte[] BufferByte;

        /// <summary>
        /// Сколько значений Float в буфере (массив может быть больше)
        /// </summary>
        public int CountFloat;
        /// <summary>
        /// Сколько значений Byte в буфере (массив может быть больше)
        /// </summary>
        public int CountByte;

        /// <summary>
        /// Скопировать сетку секции из буфера построителя, массивы растут только при нехватке
        /// </summary>
        public void Set(VertexBlockBuffer vertex)
        {
            CountFloat = vertex.BufferFloat.Count;
            CountByte = vertex.BufferByte.Count;
            if (BufferFloat == null || BufferFloat.Length < CountFloat) BufferFloat = new float[CountFloat];
            if (BufferByte == null || BufferByte.Length < CountByte) BufferByte = new byte[CountByte];
            vertex.BufferFloat.CopyTo(BufferFloat, CountFloat);
            vertex.BufferByte.CopyTo(BufferByte, CountByte);
        }

        /// <summary>
        /// Секция пуста (массивы остаются для следующей сетки)
        /// </summary>
        public void SetEmpty()
        {
            CountFloat = 0;
            CountByte = 0;
        }

        public void Clear()
        {
            IsModifiedRender = false;
            BufferFloat = new float[0];
            BufferByte = new byte[0];
            CountFloat = 0;
            CountByte = 0;
        }

        public override string ToString()
            => "F:" + (BufferFloat == null ? "Null" : BufferFloat.Length.ToString())
               + " B:" + (BufferByte == null ? "Null" : BufferByte.Length.ToString());
    }
}
