using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if GOLDEN_ORIGINAL
using Vge.Realms;
using Vge.Renderer.Font;
using Vge.Util;
#else
using Mvk.Engine.Realms;
using Mvk.Engine.Renderer.Font;
using Mvk.Engine.Util;
#endif

// Файл общий для GoldenGen (старый Vge) и Mvk.Game.Tests (новый Mvk.Engine.Client)
namespace GoldenGen
{
    /// <summary>
    /// Эталон шрифтов: ширины символов по картинке шрифта, ширины строк и перенос текста (TransferText)
    /// для трёх шрифтов Малювек. От них зависит раскладка чата, подписей и окон.
    /// </summary>
    public static class FontGolden
    {
        /// <summary>Шрифты: имя файла и шаг между символами (как в Loading и RenderMvk оригинала)</summary>
        public static readonly (string Name, byte Step)[] Fonts = { ("FontMain", 1), ("FontSmall", 1), ("FontLarge", 2) };

        private static readonly string[] _samples =
        {
            "Команда установить время на день",
            "The quick brown fox jumps over the lazy dog 0123456789",
            ChatStyle.Yellow + "Жёлтый " + ChatStyle.Bolb + "жирный" + ChatStyle.Reset + " обычный, Ёё Йй §",
            "Очень длинная строка чата, которую придётся переносить на несколько строк по ширине окна " +
                "с пробелами, запятыми; точками. И ещё словами-через-дефис и числами 12345 67890",
            "",
            "ОднословоОченьДлинноеБезПробеловКотороеНеВлезаетВШирину"
        };

        private static readonly int[] _widths = { 40, 120, 300, 472 };

        /// <summary>
        /// Строки эталона: имя;ключ;значение
        /// </summary>
        public static IEnumerable<string> Compute(string assetsTextures)
        {
            foreach ((string name, byte step) in Fonts)
            {
                BufferedImage image = BufferedFileImage.FileToBufferedImage(Path.Combine(assetsTextures, name + ".png"));
                FontBase font = new FontBase(image, step, null);

                StringBuilder widths = new StringBuilder();
                for (char c = ' '; c < 'ѐ'; c++)
                {
                    if (font.IsPresent(c)) widths.Append(font.WidthChar(c)).Append(',');
                }
                yield return name + ";chars;" + widths;

                for (int i = 0; i < _samples.Length; i++)
                {
                    yield return name + ";width" + i + ";" + font.WidthString(_samples[i]);
                    foreach (int w in _widths)
                    {
                        font.Transfer.Run(_samples[i], w, 1);
                        yield return name + ";transfer" + i + "_" + w + ";" + font.Transfer.NumberLines + "|"
                            + font.Transfer.OutWidth + "|" + font.Transfer.OutText.Replace("\r", "\\r").Replace("\n", "\\n");
                    }
                }
            }
        }

        /// <summary>
        /// Записать эталон в файл
        /// </summary>
        public static void Write(string assetsTextures, string output)
        {
            using StreamWriter file = new StreamWriter(output, false, new UTF8Encoding(false));
            file.NewLine = "\n";
            file.WriteLine("# Эталон шрифтов Vge (original dfbd814): шрифт;ключ;значение");
            foreach (string line in Compute(assetsTextures)) file.WriteLine(line);
        }
    }
}
