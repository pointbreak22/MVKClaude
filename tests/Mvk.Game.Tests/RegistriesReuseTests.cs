using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoldenGen;
using Mvk.Engine;

namespace Mvk.Game.Tests
{
    /// <summary>
    /// Технический п. 1б: несколько миров в одном процессе без повторной регистрации блоков, предметов и сущностей.
    /// Регистрация один раз, затем миры с разными зёрнами подряд; каждый совпадает с эталоном оригинала
    /// (Golden/island.csv), у каждого сервера свои реестры.
    /// </summary>
    [Collection("Registries")]
    public class RegistriesReuseTests
    {
        [Fact]
        public void WorldsWithoutReregistration()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            string[] golden = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Golden", "island.csv"));
            IslandGolden.InitRegistries();
            Registries? first = null;
            for (int i = 0; i < 2; i++)
            {
                long seed = IslandGolden.DefaultSeeds[i];
                string prefix = seed.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";";
                string[] expected = golden.Where(l => l.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
                List<string> actual = IslandGolden.Generate(seed, 70 + i, 2, false, false);
                Assert.Equal(expected, actual);
                Assert.NotNull(Registries.Active);
                if (first == null) first = Registries.Active;
                else Assert.NotSame(first, Registries.Active);
            }
        }
    }
}
