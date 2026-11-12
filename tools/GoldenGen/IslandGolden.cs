using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
#if GOLDEN_ORIGINAL
using Mvk2.Entity;
using Mvk2.Entity.Particle;
using Mvk2.Games;
using Mvk2.Item;
using Mvk2.Util;
using Mvk2.World;
using Mvk2.World.Block;
using Mvk2.World.BlockEntity;
using Vge.Audio;
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
#else
using Mvk.Core.Maths;
using Mvk.Engine.Audio;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Particle;
using Mvk.Engine.Games;
using Mvk.Engine.Item;
using Mvk.Engine.Util;
using Mvk.Engine.World;
using Mvk.Engine.World.Block;
using Mvk.Engine.World.BlockEntity;
using Mvk.Engine.World.Chunk;
using Mvk.Game.Entity;
using Mvk.Game.Entity.Particle;
using Mvk.Game.Games;
using Mvk.Game.Item;
using Mvk.Game.Util;
using Mvk.Game.World;
using Mvk.Game.World.Block;
using Mvk.Game.World.BlockEntity;
#endif

namespace GoldenGen
{
    /// <summary>
    /// Эталон генерации мира «Остров»: хэши чанков после генерации и декорации.
    /// Один и тот же файл компилируется в GoldenGen (старый код) и в Mvk.Game.Tests (новый код).
    /// Рабочая папка должна быть папкой сборки (там Assets и сохранения).
    /// </summary>
    public static class IslandGolden
    {
        public const string Header = "# seed;chunkX;chunkZ;blocks;biomes;heightMapGen;light;blockEntities";

        /// <summary>
        /// seed по умолчанию; слот сохранения seed[i] = 90 + i
        /// </summary>
        public static readonly long[] DefaultSeeds = { 1, 2, 20241004, -987654321 };

        /// <summary>
        /// Центры проверяемых областей (в чанках): начало координат, точка первого спавна
        /// игрока (-320, 130 в блоках) и разные стороны острова, чтобы попасть в разные биомы
        /// </summary>
        public static readonly Vector2i[] Centers =
        {
            new Vector2i(0, 0), new Vector2i(-20, 8), new Vector2i(-40, -40), new Vector2i(40, 40),
            new Vector2i(-40, 40), new Vector2i(40, -40), new Vector2i(0, -30), new Vector2i(25, 0)
        };

        /// <summary>
        /// Регистрация блоков, предметов и сущностей, как в WindowMvk, но без окна и атласа
        /// </summary>
        public static void InitRegistries()
        {
            new OptionsFileMvk().Load();
            // Только ключи звуков (без OpenAL): материалы блоков ссылаются на них
            AudioReg.Initialization(false);
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

        /// <summary>
        /// Сгенерировать области вокруг Centers и вернуть строки эталона
        /// </summary>
        public static List<string> Generate(long seed, int slot, int radius)
        {
            string pathGame = Options.PathGames + (slot + 1).ToString();
            if (Directory.Exists(pathGame)) Directory.Delete(pathGame, true);
            // Как в оригинале при каждом запуске игры (WindowMain.GameLocalRun):
            // реестры заново, потому что GameServer.Init корректирует и очищает их таблицы
            InitRegistries();

            GameServer server = new GameServer(new Logger("Logs"), new GameModServerMvk(),
                new GameSettings(slot, seed), new AllWorldsMvk());
            if (!server.Init())
            {
                throw new Exception("Не удалось инициализировать сервер для seed " + seed);
            }
            WorldServer world = server.Worlds.GetWorld(0);

            List<string> rows = new List<string>();
            foreach (Vector2i center in Centers)
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
                        rows.Add(string.Join(";", seed.ToString(CultureInfo.InvariantCulture),
                            x, z, _HashBlocks(chunk), _Hash(chunk.Biome),
                            _HashUShort(chunk.HeightMapGen), _HashLight(chunk), _CountBlockEntities(chunk)));
                    }
                }
            }
            return rows;
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
