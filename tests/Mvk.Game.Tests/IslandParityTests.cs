using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoldenGen;

namespace Mvk.Game.Tests
{
    /// <summary>
    /// Новый генератор «Острова» (Mvk.Game на Mvk.Engine) даёт те же чанки, что старый код:
    /// блоки, биомы, карта высот, освещение, число блок-сущностей (Golden/island.csv)
    /// </summary>
    public class IslandParityTests
    {
        private const int Radius = 2;

        public static IEnumerable<object[]> Seeds()
            => IslandGolden.DefaultSeeds.Select((seed, i) => new object[] { seed, i });

        [Theory]
        [MemberData(nameof(Seeds))]
        public void SameAsOriginal(long seed, int index)
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            string prefix = seed.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";";
            string[] expected = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Golden", "island.csv"))
                .Where(l => l.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            List<string> actual = IslandGolden.Generate(seed, 90 + index, Radius);

            Assert.Equal(expected.Length, actual.Count);
            string[] columns = IslandGolden.Header.TrimStart('#', ' ').Split(';');
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] == actual[i]) continue;
                string[] e = expected[i].Split(';'), a = actual[i].Split(';');
                string diff = string.Join(", ", Enumerable.Range(0, columns.Length).Where(c => e[c] != a[c]).Select(c => columns[c]));
                Assert.Fail($"Чанк {e[1]},{e[2]} seed {seed}: отличаются {diff}");
            }
        }
    }
}
