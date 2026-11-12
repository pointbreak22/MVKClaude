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
        /// Центры проверяемых областей (в чанках): начало координат, точка первого спавна
        /// игрока (-320, 130 в блоках) и разные стороны острова, чтобы попасть в разные биомы
        /// </summary>
        private static readonly Vector2i[] _centers =
        {
            new Vector2i(0, 0), new Vector2i(-20, 8), new Vector2i(-40, -40), new Vector2i(40, 40),
            new Vector2i(-40, 40), new Vector2i(40, -40), new Vector2i(0, -30), new Vector2i(25, 0)
        };

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
            if (seeds.Count == 0) seeds.AddRange(new long[] { 1, 2, 20241004, -987654321 });

            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            using (StreamWriter file = new StreamWriter(output))
            {
                file.NewLine = "\n";
                file.WriteLine("# Эталон генерации мира «Остров» со старого кода (original dfbd814)");
                file.WriteLine("# seed;chunkX;chunkZ;blocks;biomes;heightMapGen;light;blockEntities");
                int slot = 90;
                foreach (long seed in seeds)
                {
                    _Generate(file, seed, slot++, radius);
                }
            }
            Console.WriteLine("Готово: " + output);
            // Вторичные миры держат свои потоки, завершаем процесс явно
            Environment.Exit(0);
            return 0;
        }

        /// <summary>
        /// Регистрация блоков, предметов и сущностей, как в WindowMvk, но без окна и атласа
        /// </summary>
        private static void _InitRegistries()
        {
            new OptionsFileMvk().Load();
            // Только ключи звуков (без OpenAL): материалы блоков ссылаются на них
            Vge.Audio.AudioReg.Initialization(false);
            Ce.SetSpriteAtlasSize(64, 16);
            BlocksReg.Initialization();
            BlocksRegMvk.Initialization();
            ItemsReg.SizeShape = 32;
            ItemsReg.Initialization();
            ItemsRegMvk.Initialization();
            EntitiesReg.Initialization();
            EntitiesRegMvk.Initialization();
            BlocksEntityReg.Initialization();
            BlocksEntityRegMvk.Initialization();
            EntitiesFXReg.Initialization();
            EntitiesFXRegMvk.Initialization();
        }

        private static void _Generate(StreamWriter file, long seed, int slot, int radius)
        {
            string pathGame = Options.PathGames + (slot + 1).ToString();
            if (Directory.Exists(pathGame)) Directory.Delete(pathGame, true);
            // Как в оригинале при каждом запуске игры (WindowMain.GameLocalRun):
            // реестры заново, потому что GameServer.Init корректирует и очищает их таблицы
            _InitRegistries();

            GameServer server = new GameServer(new Logger("Logs"), new GameModServerMvk(),
                new GameSettings(slot, seed), new AllWorldsMvk());
            if (!server.Init())
            {
                throw new Exception("Не удалось инициализировать сервер для seed " + seed);
            }
            WorldServer world = server.Worlds.GetWorld(0);

            int count = 0;
            foreach (Vector2i center in _centers)
            {
                // Грузим с запасом в 1 чанк, чтобы внутренние чанки прошли декорацию
                for (int x = center.X - radius - 1; x <= center.X + radius + 1; x++)
                    for (int z = center.Y - radius - 1; z <= center.Y + radius + 1; z++)
                        if (world.ChunkPrServ.GetChunkServer(new Vector2i(x, z)) == null)
                            world.ChunkPrServ.InitialLoadingChunk(x, z);

                for (int x = center.X - radius; x <= center.X + radius; x++)
                {
                    for (int z = center.Y - radius; z <= center.Y + radius; z++)
                    {
                        ChunkServer chunk = world.ChunkPrServ.GetChunkServer(new Vector2i(x, z));
                        if (chunk == null || !chunk.IsDecorated)
                        {
                            throw new Exception($"Чанк {x},{z} не сгенерирован или не декорирован");
                        }
                        file.WriteLine(string.Join(";", seed.ToString(CultureInfo.InvariantCulture),
                            x, z, _HashBlocks(chunk), _Hash(chunk.Biome),
                            _HashUShort(chunk.HeightMapGen), _HashLight(chunk), _CountBlockEntities(chunk)));
                        count++;
                    }
                }
            }
            Console.WriteLine($"seed {seed}: {count} чанков");
        }

        private static string _HashBlocks(ChunkServer chunk)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            byte[] buffer = new byte[4096 * 4];
            for (int i = 0; i < chunk.NumberSections; i++)
            {
                int[] data = chunk.StorageArrays[i].Data;
                if (data == null)
                {
                    hash.AppendData(new byte[] { 0 });
                }
                else
                {
                    hash.AppendData(new byte[] { 1 });
                    Buffer.BlockCopy(data, 0, buffer, 0, 4096 * 4);
                    hash.AppendData(buffer);
                }
            }
            return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        }

        private static string _HashLight(ChunkServer chunk)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            for (int i = 0; i < chunk.NumberSections; i++)
            {
                hash.AppendData(chunk.StorageArrays[i].Light);
            }
            return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        }

        private static string _HashUShort(ushort[] array)
        {
            byte[] bytes = new byte[array.Length * 2];
            Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
            return _Hash(bytes);
        }

        private static string _Hash(byte[] bytes)
            => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        private static int _CountBlockEntities(ChunkServer chunk)
            => chunk.GetType().GetField("_mapBlocksEntity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(chunk) is System.Collections.ICollection c ? c.Count : -1;
    }
}
