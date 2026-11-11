using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mvk.Game.Tests
{
    /// <summary>
    /// Проверка, что эталон генерации на месте и в ожидаемом формате.
    /// Сравнение нового генератора с эталоном добавляется на этапе 3.
    /// </summary>
    public class GoldenFileTests
    {
        private static readonly string _path = Path.Combine(AppContext.BaseDirectory, "Golden", "island.csv");

        [Fact]
        public void IslandGoldenHasExpectedShape()
        {
            Assert.True(File.Exists(_path), "Нет эталона: запустите tools/GoldenGen");

            string[][] rows = File.ReadAllLines(_path)
                .Where(l => l.Length > 0 && l[0] != '#')
                .Select(l => l.Split(';'))
                .ToArray();

            Assert.Equal(800, rows.Length);
            Assert.All(rows, r =>
            {
                Assert.Equal(8, r.Length);
                long.Parse(r[0], CultureInfo.InvariantCulture);
                Assert.All(r.Skip(3).Take(4), h => Assert.Equal(64, h.Length));
            });
            // Каждый чанк уникален: эталон не выродился (например, весь мир — океан)
            Assert.Equal(rows.Length, rows.Select(r => r[3]).Distinct().Count());
        }
    }
}
