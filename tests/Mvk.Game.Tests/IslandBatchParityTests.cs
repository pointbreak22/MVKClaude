using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoldenGen;

namespace Mvk.Game.Tests
{
    /// <summary>
    /// Пакетная загрузка чанков сервером, как в игре (NeededChunk, LoadQueuedChunks), даёт те же чанки, что старый
    /// код по одному (Golden/island.csv). Порядок «рельеф чанка, его ChunkPresent, следующий чанк» важен:
    /// трава рельефа берёт общий Rand генератора без пересева (BiomeIsland._GenTallGrass), состояние которого
    /// оставляет декор предыдущих чанков (этап 7, docs/improvements.md R24)
    /// </summary>
    [Collection("Registries")]
    public class IslandBatchParityTests
    {
        private const int Radius = 2;

        public static IEnumerable<object[]> Seeds()
            => IslandGolden.DefaultSeeds.Select((seed, i) => new object[] { seed, i });

        [Theory]
        [MemberData(nameof(Seeds))]
        public void BatchSameAsOriginal(long seed, int index)
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            string prefix = seed.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";";
            string[] expected = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Golden", "island.csv"))
                .Where(l => l.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            List<string> actual = IslandGolden.Generate(seed, 80 + index, Radius, true);

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
