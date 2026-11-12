using System;
using System.IO;
using NVorbis;

namespace Mvk.Platform.Audio
{
    /// <summary>
    /// Звук в памяти: 16 бит со знаком, little-endian, каналы чередуются
    /// </summary>
    public sealed class PcmSample
    {
        /// <summary>1 моно, 2 стерео</summary>
        public int Channels { get; }
        /// <summary>Частота дискретизации</summary>
        public int SampleRate { get; }
        /// <summary>Байты PCM16</summary>
        public byte[] Data { get; }

        public PcmSample(int channels, int sampleRate, byte[] data)
        {
            Channels = channels;
            SampleRate = sampleRate;
            Data = data;
        }

        /// <summary>
        /// Декодировать OGG Vorbis так же, как AudioSample.LoadOgg оригинала: float * 32767 в short.
        /// </summary>
        /// <remarks>
        /// Отличия от оригинала (см. docs/improvements.md, A1, A2):
        /// значения за пределами [-1, 1] явно ограничиваются (в оригинале проверка после приведения к short
        /// никогда не срабатывала, а результат приведения зависел от рантайма);
        /// буфер выделяется один раз по длине потока вместо List&lt;byte&gt; по байту.
        /// </remarks>
        public static PcmSample DecodeOgg(Stream stream)
        {
            using var vorbis = new VorbisReader(stream, false);
            int channels = vorbis.Channels;
            long total = vorbis.TotalSamples * channels;
            var output = new MemoryStream(total > 0 && total < int.MaxValue / 2 ? (int)total * 2 : 0);
            float[] buffer = new float[1024];
            byte[] bytes = new byte[buffer.Length * 2];
            int count;
            while ((count = vorbis.ReadSamples(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    float f = 32767f * buffer[i];
                    short s = f >= 32767f ? (short)32767 : f <= -32768f ? (short)-32768 : (short)f;
                    bytes[i * 2] = (byte)s;
                    bytes[i * 2 + 1] = (byte)(s >> 8);
                }
                output.Write(bytes, 0, count * 2);
            }
            return new PcmSample(channels, vorbis.SampleRate, output.ToArray());
        }

        /// <summary>Декодировать OGG-файл</summary>
        public static PcmSample DecodeOgg(string path)
        {
            using FileStream fs = File.OpenRead(path);
            return DecodeOgg(fs);
        }
    }
}
