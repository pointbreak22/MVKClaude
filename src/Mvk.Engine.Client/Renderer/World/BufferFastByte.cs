using System;
using System.Runtime.InteropServices;

namespace Mvk.Engine.Renderer.World
{
    /// <summary>
    /// Быстрый буферный массив для склейки рендера.
    /// Выделяем объём кеша, но увеличивать не может!!!
    /// </summary>
    public class BufferFastByte : BufferFast<byte>
    {
        /// <summary>
        /// Создаём, с выделенным объёмом
        /// </summary>
        public BufferFastByte(int maxSize = 1000) : base(maxSize)
            => _hBuffer = Marshal.AllocHGlobal(maxSize);

        /// <summary>
        /// Добавить массив значений
        /// </summary>
        /// <summary>
        /// Добавить count значений массива с позиции start
        /// </summary>
        public void AddRange(byte[] items, int start, int count)
        {
            Buffer.BlockCopy(items, start, _buffer, Count, count);
            Count += count;
        }

        public void AddRange(byte[] items)
        {
            try
            {
                int count = items.Length;
                Buffer.BlockCopy(items, 0, _buffer, Count, count);
                Count += count;
            }
            catch
            {
                throw new Exception(ClientStrings.GetString(ClientStrings.OutOfRangeArray, Count));
            }
        }

        /// <summary>
        /// Получить адрес буфера
        /// </summary>
        public override IntPtr ToBuffer()
        {
            Marshal.Copy(_buffer, 0, _hBuffer, Count);
            return _hBuffer;
        }

        /// <summary>
        /// Копировать в буффер
        /// </summary>
        public override void CopyBuffer(IntPtr intPtr)
            => Marshal.Copy(_buffer, 0, intPtr, Count);

        /// <summary>
        /// Размер буфера
        /// </summary>
        public override int ToSize() => Count;

        public override void Dispose()
        {
            base.Dispose();
            _buffer = null;
        }
    }
}
