using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace Mvk.Core.Util
{
    /// <summary>
    /// Запись PNG (RGB8 или RGBA8, без фильтров строк): снимки экрана и отладочные карты. Без сторонних библиотек.
    /// </summary>
    public static class PngWriter
    {
        private static readonly byte[] _signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>
        /// Записать кадр в PNG без альфа-канала: окно показывает кадр непрозрачным, а альфа
        /// кадрового буфера (её пишут смешивание неба и облаков) в снимке дала бы дыры
        /// </summary>
        /// <param name="rgba">Пиксели RGBA, строки сверху вниз</param>
        /// <param name="alpha">Сохранить альфа-канал (RGBA), иначе RGB</param>
        public static void Write(string path, int width, int height, ReadOnlySpan<byte> rgba, bool alpha = false)
        {
            using FileStream file = File.Create(path);
            file.Write(_signature);

            Span<byte> header = stackalloc byte[13];
            BinaryPrimitives.WriteInt32BigEndian(header, width);
            BinaryPrimitives.WriteInt32BigEndian(header.Slice(4), height);
            header[8] = 8;  // бит на канал
            header[9] = (byte)(alpha ? 6 : 2);  // RGBA или RGB
            header[10] = 0; // сжатие deflate
            header[11] = 0; // фильтр
            header[12] = 0; // без чересстрочности
            WriteChunk(file, "IHDR", header);

            using var data = new MemoryStream();
            using (var zlib = new ZLibStream(data, CompressionLevel.Fastest, true))
            {
                int row = width * 4;
                int channels = alpha ? 4 : 3;
                byte[] line = new byte[width * channels + 1]; // тип фильтра строки 0: нет
                for (int y = 0; y < height; y++)
                {
                    ReadOnlySpan<byte> src = rgba.Slice(y * row, row);
                    for (int x = 0, i = 1; x < row; x += 4)
                    {
                        line[i++] = src[x];
                        line[i++] = src[x + 1];
                        line[i++] = src[x + 2];
                        if (alpha) line[i++] = src[x + 3];
                    }
                    zlib.Write(line);
                }
            }
            WriteChunk(file, "IDAT", data.GetBuffer().AsSpan(0, (int)data.Length));
            WriteChunk(file, "IEND", ReadOnlySpan<byte>.Empty);
        }

        private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
        {
            Span<byte> buf = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(buf, data.Length);
            stream.Write(buf);

            Span<byte> name = stackalloc byte[4];
            for (int i = 0; i < 4; i++) name[i] = (byte)type[i];
            stream.Write(name);
            stream.Write(data);

            uint crc = Crc(Crc(0xFFFFFFFF, name), data) ^ 0xFFFFFFFF;
            BinaryPrimitives.WriteUInt32BigEndian(buf, crc);
            stream.Write(buf);
        }

        private static readonly uint[] _crcTable = MakeCrcTable();

        private static uint[] MakeCrcTable()
        {
            uint[] table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }

        private static uint Crc(uint crc, ReadOnlySpan<byte> data)
        {
            foreach (byte b in data) crc = _crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            return crc;
        }
    }
}
