using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
#if GOLDEN_ORIGINAL
using Vge.Audio;
#else
using Mvk.Platform.Audio;
#endif

namespace GoldenGen
{
    /// <summary>
    /// Эталон декодирования OGG: для каждого звука ассетов каналы, частота, длина и SHA-256 байтов PCM16.
    /// Оригинал декодирует через AudioSample.LoadOgg, новый код через PcmSample.DecodeOgg.
    /// Один и тот же файл компилируется в GoldenGen (старый код) и в Mvk.Platform.Tests (новый код).
    /// </summary>
    public static class AudioGolden
    {
        public static IEnumerable<KeyValuePair<string, string>> Compute(string assetsDir)
        {
            IEnumerable<string> files = Directory.GetFiles(assetsDir, "*.ogg", SearchOption.AllDirectories)
                .Select(p => Path.GetRelativePath(assetsDir, p).Replace('\\', '/'))
                .OrderBy(p => p, StringComparer.Ordinal);
            foreach (string rel in files)
            {
                string path = Path.Combine(assetsDir, rel);
#if GOLDEN_ORIGINAL
                AudioSample sample = new AudioSample();
                sample.LoadOgg(path);
                int channels = sample.Channels;
                int rate = sample.SamplesPerSecond;
                byte[] data = sample.Buffer;
#else
                PcmSample sample = PcmSample.DecodeOgg(path);
                int channels = sample.Channels;
                int rate = sample.SampleRate;
                byte[] data = sample.Data;
#endif
                yield return new KeyValuePair<string, string>(rel,
                    channels + "ch," + rate + "Hz," + data.Length + ":" + Convert.ToHexString(SHA256.HashData(data)));
            }
        }

        public static void Write(string assetsDir, string output)
        {
            using (StreamWriter file = new StreamWriter(output))
            {
                file.NewLine = "\n";
                file.WriteLine("# OGG ассетов, декодированные оригиналом (NVorbis + AudioSample): путь;каналы,частота,байт:sha256 PCM16");
                foreach (KeyValuePair<string, string> pair in Compute(assetsDir))
                {
                    file.WriteLine(pair.Key + ";" + pair.Value);
                }
            }
        }
    }
}
