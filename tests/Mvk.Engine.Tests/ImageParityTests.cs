using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoldenGen;

namespace Mvk.Engine.Tests
{
    /// <summary>
    /// PNG через StbImageSharp дают те же пиксели, что GDI+ в оригинале (атлас блоков, модели, шрифты)
    /// </summary>
    public class ImageParityTests
    {
        public static IEnumerable<object[]> Names()
            => _LoadGolden().Keys.Select(k => new object[] { k });

        [Theory]
        [MemberData(nameof(Names))]
        public void SameAsOriginal(string rel)
        {
            string assets = _AssetsDir();
            string expected = _LoadGolden()[rel];
            string actual = ImageGolden.Compute(assets).First(p => p.Key == rel).Value;
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Все картинки оригинала на месте и сверены; свои ассеты порта (assets/port-assets.txt) не сверяются
        /// </summary>
        [Fact]
        public void AllCovered()
        {
            string assets = _AssetsDir();
            HashSet<string> own = File.ReadAllLines(Path.Combine(assets, "port-assets.txt"))
                .Where(l => l.Length > 0 && !l.StartsWith('#')).ToHashSet(StringComparer.Ordinal);
            Assert.Equal(_LoadGolden().Keys.OrderBy(k => k, StringComparer.Ordinal),
                ImageGolden.Compute(assets).Select(p => p.Key).Where(k => !own.Contains(k)));
        }

        /// <summary>
        /// Ассеты игры (assets/, байты как в res/Assets оригинала)
        /// </summary>
        private static string _AssetsDir()
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MvkClaude.slnx"))) dir = dir.Parent;
            Assert.NotNull(dir);
            string assets = Path.Combine(dir.FullName, "assets");
            Assert.True(Directory.Exists(assets), "Нет ассетов оригинала: " + assets);
            return assets;
        }

        private static Dictionary<string, string> _LoadGolden()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Golden", "png.csv");
            return File.ReadAllLines(path)
                .Where(l => l.Length > 0 && l[0] != '#')
                .Select(l => l.Split(';'))
                .ToDictionary(p => p[0], p => p[1]);
        }
    }
}
