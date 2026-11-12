using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using Mvk2.Entity;
using Mvk2.Entity.Particle;
using Mvk2.Games;
using Mvk2.Item;
using Mvk2.Util;
using Mvk2.World;
using Mvk2.World.Block;
using Mvk2.World.BlockEntity;
using Vge.Entity;
using Vge.Entity.Particle;
using Vge.Games;
using Vge.Item;
using Vge.Util;
using Vge.World;
using Vge.World.Block;
using Vge.World.BlockEntity;
using Vge.World.Chunk;
using WinGL.Util;

namespace GoldenGen
{
    /// <summary>
    /// Снимает хэши чанков, сгенерированных СТАРЫМ кодом, для проверки паритета нового генератора.
    /// Аргументы: [файл вывода] [радиус] [seed1 seed2 ...]
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Папка, из которой запущена утилита (для относительных путей вывода)
        /// </summary>
        private static string _startDirectory = "";

        private static int Main(string[] args)
        {
            _startDirectory = Environment.CurrentDirectory;
            // В оригинале таблицу синусов заполняет WinGL.Window при старте.
            // Без этого Glm.Sin/Cos возвращают 0, а генерация руд (FeatureMinable) их использует.
            Glm.Initialized();

            if (args.Length > 0 && args[0] == "math")
            {
                string mathOutput = Path.GetFullPath(args.Length > 1 ? args[1] : "math.csv", _startDirectory);
                MathGolden.Write(mathOutput);
                Console.WriteLine("Готово: " + mathOutput);
                return 0;
            }
            if (args.Length > 0 && args[0] == "core")
            {
                string coreOutput = Path.GetFullPath(args.Length > 1 ? args[1] : "core.csv", _startDirectory);
                CoreGolden.Write(coreOutput);
                Console.WriteLine("Готово: " + coreOutput);
                return 0;
            }

            if (args.Length > 0 && args[0] == "png")
            {
                string pngOutput = Path.GetFullPath(args.Length > 1 ? args[1] : "png.csv", _startDirectory);
                ImageGolden.Write(Path.Combine(AppContext.BaseDirectory, "Assets"), pngOutput);
                Console.WriteLine("Готово: " + pngOutput);
                return 0;
            }

            string output = Path.GetFullPath(args.Length > 0 ? args[0] : "island.csv");
            int radius = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 3;
            List<long> seeds = new List<long>();
            for (int i = 2; i < args.Length; i++) seeds.Add(long.Parse(args[i], CultureInfo.InvariantCulture));
            if (seeds.Count == 0) seeds.AddRange(IslandGolden.DefaultSeeds);

            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            using (StreamWriter file = new StreamWriter(output))
            {
                file.NewLine = "\n";
                file.WriteLine("# Эталон генерации мира «Остров» со старого кода (original dfbd814)");
                file.WriteLine(IslandGolden.Header);
                int slot = 90;
                foreach (long seed in seeds)
                {
                    List<string> rows = IslandGolden.Generate(seed, slot++, radius);
                    foreach (string row in rows) file.WriteLine(row);
                    Console.WriteLine($"seed {seed}: {rows.Count} чанков");
                }
            }
            Console.WriteLine("Готово: " + output);
            // Вторичные миры держат свои потоки, завершаем процесс явно
            Environment.Exit(0);
            return 0;
        }
    }
}
