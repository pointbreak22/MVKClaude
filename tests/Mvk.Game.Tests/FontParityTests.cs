using System;
using System.IO;
using System.Linq;
using GoldenGen;

namespace Mvk.Game.Tests
{
    /// <summary>
    /// Шрифты клиента (Mvk.Engine.Client) дают те же ширины символов, ширины строк и перенос текста,
    /// что старый код (Golden/font.csv, GoldenGen режим font)
    /// </summary>
    public class FontParityTests
    {
        [Fact]
        public void SameAsOriginal()
        {
            string[] expected = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Golden", "font.csv"))
                .Where(l => l.Length > 0 && l[0] != '#').ToArray();
            string[] actual = FontGolden.Compute(Path.Combine(AppContext.BaseDirectory, "Assets", "Mvk2Textures")).ToArray();
            Assert.Equal(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], actual[i]);
        }
    }
}
