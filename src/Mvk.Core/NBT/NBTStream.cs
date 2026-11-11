using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Mvk.Core.Nbt
{
    /// <summary>
    /// Стрим для NBT. Числа хранятся в порядке big-endian, как в оригинале.
    /// </summary>
    public class NBTStream : MemoryStream
    {
        #region Read

        /// <summary>
        /// Прочесть текстовую строку utf8
        /// </summary>
        public string ReadUTF()
        {
            short length = ReadShort();
            if (length == 0) return string.Empty;
            byte[] data = new byte[length];
            ReadExactly(data, 0, length);
            return Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// Прочесть тип byte (0..255) 1 байт
        /// </summary>
        public byte Byte()
        {
            int value = ReadByte();
            if (value == -1) throw new EndOfStreamException();
            return (byte)value;
        }

        public short ReadShort() => (short)((Byte() << 8) | Byte());
        public int ReadInt() => (Byte() << 24) | (Byte() << 16) | (Byte() << 8) | Byte();
        public long ReadLong() => ((long)(Byte() << 8) | Byte()) << 48 | ((long)(Byte() << 8) | Byte()) << 32
                | ((long)(Byte() << 8) | Byte()) << 16 | ((long)(Byte() << 8) | Byte());

        /// <summary>
        /// Прочесть число с плавоющей запятой (биты float в big-endian)
        /// </summary>
        public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt());

        #endregion

        #region Write

        /// <summary>
        /// Записать текстовую строку utf8
        /// </summary>
        public void WriteUTF(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            WriteShort((short)buffer.Length);
            Write(buffer, 0, buffer.Length);
        }

        public void WriteShort(short number)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteInt16BigEndian(buffer, number);
            Write(buffer);
        }

        public void WriteInt(int number)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(buffer, number);
            Write(buffer);
        }

        public void WriteLong(long number)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteInt64BigEndian(buffer, number);
            Write(buffer);
        }

        /// <summary>
        /// Сохранить число с плавоющей запятой (биты float в big-endian)
        /// </summary>
        public void WriteFloat(float value) => WriteInt(BitConverter.SingleToInt32Bits(value));

        #endregion
    }
}
