#nullable enable
using System;
using Mvk.Core.Json;
using Mvk.Engine.Entity;
using Mvk.Engine.Util;
using Mvk.Engine.World;
using Mvk.Engine.World.Block;
using Mvk.Game.Entity.List;

namespace Mvk.Game.Entity
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Периодический спавн врагов вокруг игрока в выживании (просьба пользователя 21.11): раз в случайный
    /// интервал (по умолчанию 30 с – 2 мин) на загруженной твёрдой поверхности в 50–100 блоках от игрока появляется
    /// зомби или разбойник и идёт к игроку (EntityHostileMvk.Hunt). Не больше MaxNearby врагов в радиусе NearbyRadius.
    /// Далёкие враги исчезают обычным деспавном мобов (дальше 160 блоков). Значения — Mvk2Entities/Spawn.json.
    /// Выключается флагом Enabled (хост: --no-spawn, --no-kit для сверки с оригиналом, команда /spawning off).
    /// </summary>
    public sealed class EnemySpawner
    {
        /// <summary>
        /// Спавн включён (на процесс)
        /// </summary>
        public static bool Enabled = true;

        private sealed class Config
        {
            public int IntervalMin = 600, IntervalMax = 2400;
            public float DistanceMin = 50, DistanceMax = 100;
            public int MaxNearby = 6;
            public float NearbyRadius = 128;
            public int ZombieWeight = 2, BanditWeight = 1;

            public static Config Load()
            {
                Config c = new Config();
                try
                {
                    JsonRead json = new JsonRead(Options.PathEntities + "Spawn.json");
                    if (!json.IsThereFile) return c;
                    JsonCompound s = json.Compound;
                    if (s.IsKey("IntervalMin")) c.IntervalMin = Math.Max(20, s.GetInt("IntervalMin"));
                    if (s.IsKey("IntervalMax")) c.IntervalMax = Math.Max(c.IntervalMin, s.GetInt("IntervalMax"));
                    if (s.IsKey("DistanceMin")) c.DistanceMin = s.GetFloat("DistanceMin");
                    if (s.IsKey("DistanceMax")) c.DistanceMax = Math.Max(c.DistanceMin, s.GetFloat("DistanceMax"));
                    if (s.IsKey("MaxNearby")) c.MaxNearby = s.GetInt("MaxNearby");
                    if (s.IsKey("NearbyRadius")) c.NearbyRadius = s.GetFloat("NearbyRadius");
                    if (s.IsKey("ZombieWeight")) c.ZombieWeight = Math.Max(0, s.GetInt("ZombieWeight"));
                    if (s.IsKey("BanditWeight")) c.BanditWeight = Math.Max(0, s.GetInt("BanditWeight"));
                }
                catch (Exception)
                {
                    // Ошибка в файле — значения по умолчанию
                }
                return c;
            }
        }

        private static Config? _config;
        private static Config _Cfg => _config ??= Config.Load();

        /// <summary>
        /// Тактов до следующей попытки (0 — ещё не задано)
        /// </summary>
        private int _ticks;

        /// <summary>
        /// Такт игрока на сервере
        /// </summary>
        public void Update(PlayerServerMvk player)
        {
            if (!Enabled || player.CreativeMode || player.IsSpectator() || player.GetHealth() <= 0) return;
            WorldServer world = player.GetWorldServer();
            if (world == null) return;
            Config c = _Cfg;
            if (_ticks <= 0) _ticks = c.IntervalMin + world.Rnd.Next(c.IntervalMax - c.IntervalMin + 1);
            if (--_ticks > 0) return;
            if (_Nearby(world, player, c) >= c.MaxNearby) return;
            // Несколько попыток найти место
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float angle = world.Rnd.NextFloat() * MathF.PI * 2;
                float distance = c.DistanceMin + world.Rnd.NextFloat() * (c.DistanceMax - c.DistanceMin);
                int x = (int)MathF.Floor(player.PosX + MathF.Sin(angle) * distance);
                int z = (int)MathF.Floor(player.PosZ + MathF.Cos(angle) * distance);
                int y = world.GetHeightMap(x, z);
                if (y <= 1 || !_Free(world, x, y, z)) continue;
                int all = c.ZombieWeight + c.BanditWeight;
                if (all <= 0) return;
                ushort id = world.Rnd.Next(all) < c.ZombieWeight ? EntitiesRegMvk.ZombieId : EntitiesRegMvk.BanditId;
                if (Ce.Entities.CreateEntityServer(id, world) is not EntityHostileMvk hostile) return;
                hostile.PosX = x + .5f;
                hostile.PosY = y;
                hostile.PosZ = z + .5f;
                hostile.Hunt = player;
                world.SpawnEntityInWorld(hostile);
                return;
            }
        }

        /// <summary>
        /// Твёрдый блок (не жидкость) под ногами и четыре свободных блока над ним (рост моба)
        /// </summary>
        private static bool _Free(WorldServer world, int x, int y, int z)
        {
            BlockState ground = world.GetBlockState(new BlockPos(x, y - 1, z));
            if (ground.IsAir() || ground.GetBlock().Material.Liquid) return false;
            for (int i = 0; i < 4; i++)
            {
                if (!world.GetBlockState(new BlockPos(x, y + i, z)).IsAir()) return false;
            }
            return true;
        }

        /// <summary>
        /// Число живых врагов рядом с игроком
        /// </summary>
        private static int _Nearby(WorldServer world, PlayerServerMvk player, Config c)
        {
            int count = 0;
            for (int i = 0; i < world.LoadedEntityList.Count; i++)
            {
                if (world.LoadedEntityList.GetAt(i) is EntityHostileMvk hostile && !hostile.IsDead
                    && hostile.Distance(player) < c.NearbyRadius) count++;
            }
            return count;
        }
    }
}
