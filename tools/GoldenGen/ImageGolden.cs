using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
#if GOLDEN_ORIGINAL
using Vge.Util;
#else
using Mvk.Engine.Util;
#endif

namespace GoldenGen
{
    /// <summary>
    /// Эталон декодирования PNG: для каждой картинки ассетов размер и SHA-256 пикселей (B, G, R, A построчно).
    /// Оригинал декодирует через GDI+ (System.Drawing), новый код через StbImageSharp.
    /// Один и тот же файл компилируется в GoldenGen (старый код) и в Mvk.Engine.Tests (новый код).
    /// </summary>
    public static class ImageGolden
    {
        public static IEnumerable<KeyValuePair<string, string>> Compute(string assetsDir)
        {
            IEnumerable<string> files = Directory.GetFiles(assetsDir, "*.png", SearchOption.AllDirectories)
                .Select(p => Path.GetRelativePath(assetsDir, p).Replace('\\', '/'))
                .OrderBy(p => p, StringComparer.Ordinal);
            foreach (string rel in files)
            {
                BufferedImage image = BufferedFileImage.FileToBufferedImage(Path.Combine(assetsDir, rel));
                yield return new KeyValuePair<string, string>(rel,
                    image.Width + "x" + image.Height + ":" + Convert.ToHexString(SHA256.HashData(image.Buffer)));
            }
        }

        /// <summary>
        /// Пиксели картинки, для поиска первого расхождения в тесте
        /// </summary>
        public static byte[] Pixels(string assetsDir, string rel)
            => BufferedFileImage.FileToBufferedImage(Path.Combine(assetsDir, rel)).Buffer;

        public static void Write(string assetsDir, string output)
        {
            using (StreamWriter file = new StreamWriter(output))
            {
                file.NewLine = "\n";
                file.WriteLine("# PNG ассетов, декодированные оригиналом (GDI+): путь;ШxВ:sha256 пикселей BGRA");
                foreach (KeyValuePair<string, string> pair in Compute(assetsDir))
                {
                    file.WriteLine(pair.Key + ";" + pair.Value);
                }
            }
        }
    }
}
