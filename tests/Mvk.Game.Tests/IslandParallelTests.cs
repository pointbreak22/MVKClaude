using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoldenGen;
using Mvk.Engine.Util;

namespace Mvk.Game.Tests
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Режим генерации Parallel (включаемый, по умолчанию Original). Эталон Golden/island-parallel.csv
    /// снят портом (у оригинала такого режима нет): MVK_WRITE_GOLDEN=1 перезаписывает его в папке тестов.
    /// Пакет чанков (рельеф пулом из 4 потоков) и загрузка по одному чанку одним генератором дают один мир:
    /// в режиме Parallel мир не зависит от порядка генерации. От оригинала он отличается (трава).
    /// </summary>
    [Collection("Registries")]
    public class IslandParallelTests
    {
        private const int Radius = 2;
        private const string File = "island-parallel.csv";

        [Fact]
        public void ParallelWorldIndependentOfOrder()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            bool parallel = Options.GenerationParallel;
            int threads = Options.GenerationThreads;
            try
            {
                Options.GenerationParallel = true;
                Options.GenerationThreads = 4;
                List<string> batch = new List<string>(), single = new List<string>();
                for (int i = 0; i < IslandGolden.DefaultSeeds.Length; i++)
                {
                    long seed = IslandGolden.DefaultSeeds[i];
                    batch.AddRange(IslandGolden.Generate(seed, 60 + i, Radius, true));
                    single.AddRange(IslandGolden.Generate(seed, 60 + i, Radius, false));
                }
                Assert.Equal(batch, single);

                if (Environment.GetEnvironmentVariable("MVK_WRITE_GOLDEN") == "1")
                {
                    string[] lines = new[]
                    {
                        "# Эталон режима генерации Parallel (порт, не оригинал): мир не зависит от порядка чанков",
                        IslandGolden.Header
                    }.Concat(batch).ToArray();
                    System.IO.File.WriteAllLines(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                        "..", "..", "..", "Golden", File)), lines);
                    System.IO.File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "Golden", File), lines);
                }
                string[] expected = System.IO.File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Golden", File))
                    .Where(l => !l.StartsWith('#')).ToArray();
                Assert.Equal(expected, batch);

                // Режим действительно другой: трава рельефа не как в оригинале
                string[] original = System.IO.File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Golden", "island.csv"))
                    .Where(l => !l.StartsWith('#')).ToArray();
                Assert.NotEqual(original, batch);
            }
            finally
            {
                Options.GenerationParallel = parallel;
                Options.GenerationThreads = threads;
            }
        }
    }
}
