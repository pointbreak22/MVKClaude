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
            public int GroupMin = 1, GroupMax = 3;
            public float GroupSpread = 3;
            public float SecondSiteChance = .35f;

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
                    if (s.IsKey("GroupMin")) c.GroupMin = Math.Max(1, s.GetInt("GroupMin"));
                    if (s.IsKey("GroupMax")) c.GroupMax = Math.Max(c.GroupMin, s.GetInt("GroupMax"));
                    if (s.IsKey("GroupSpread")) c.GroupSpread = Math.Max(0, s.GetFloat("GroupSpread"));
                    if (s.IsKey("SecondSiteChance")) c.SecondSiteChance = Math.Clamp(s.GetFloat("SecondSiteChance"), 0, 1);
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
            int free = c.MaxNearby - _Nearby(world, player, c);
            if (free <= 0) return;
            // Группа в одном месте, иногда ещё одна в другом
            free -= _SpawnGroup(world, player, c, free);
            if (free > 0 && world.Rnd.NextFloat() < c.SecondSiteChance) _SpawnGroup(world, player, c, free);
        }

        /// <summary>
        /// Группа врагов (GroupMin..GroupMax, не больше limit) вокруг одной точки в DistanceMin..DistanceMax от игрока;
        /// возвращает число появившихся
        /// </summary>
        private static int _SpawnGroup(WorldServer world, PlayerServerMvk player, Config c, int limit)
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float angle = world.Rnd.NextFloat() * MathF.PI * 2;
                float distance = c.DistanceMin + world.Rnd.NextFloat() * (c.DistanceMax - c.DistanceMin);
                float cx = player.PosX + MathF.Sin(angle) * distance;
                float cz = player.PosZ + MathF.Cos(angle) * distance;
                if (!_Place(world, cx, cz, out _)) continue;
                int count = Math.Min(limit, c.GroupMin + world.Rnd.Next(c.GroupMax - c.GroupMin + 1));
                int spawned = 0, zombies = 0, bandits = 0;
                for (int n = 0; n < count * 3 && spawned < count; n++)
                {
                    float x = cx + (world.Rnd.NextFloat() * 2 - 1) * c.GroupSpread;
                    float z = cz + (world.Rnd.NextFloat() * 2 - 1) * c.GroupSpread;
                    if (!_Place(world, x, z, out int y)) continue;
                    int all = c.ZombieWeight + c.BanditWeight;
                    if (all <= 0) return spawned;
                    ushort id = world.Rnd.Next(all) < c.ZombieWeight ? EntitiesRegMvk.ZombieId : EntitiesRegMvk.BanditId;
                    if (Ce.Entities.CreateEntityServer(id, world) is not EntityHostileMvk hostile) return spawned;
                    hostile.PosX = MathF.Floor(x) + .5f;
                    hostile.PosY = y;
                    hostile.PosZ = MathF.Floor(z) + .5f;
                    hostile.Hunt = player;
                    world.SpawnEntityInWorld(hostile);
                    spawned++;
                    if (hostile is EntityZombieMvk) zombies++; else bandits++;
                }
                if (spawned == 0) continue;
                // Подсказка в чате: кто, с какой стороны и как далеко
                string who = zombies > 0 && bandits > 0 ? "зомби " + zombies + ", разбойники " + bandits
                    : zombies > 0 ? (zombies > 1 ? "зомби " + zombies : "зомби") : (bandits > 1 ? "разбойники " + bandits : "разбойник");
                player.SendMessage(Mvk.Engine.Realms.ChatStyle.Red + (spawned > 1 ? "Появились враги: " : "Появился враг: ")
                    + who + ", " + _Side(cx - player.PosX, cz - player.PosZ) + ", " + (int)distance + " блоков");
                return spawned;
            }
            return 0;
        }

        /// <summary>
        /// Место для врага в точке x, z: загруженный чанк, твёрдая земля, свободно над ней; y — высота ног
        /// </summary>
        private static bool _Place(WorldServer world, float x, float z, out int y)
        {
            int bx = (int)MathF.Floor(x), bz = (int)MathF.Floor(z);
            y = world.GetHeightMap(bx, bz);
            return y > 1 && _Free(world, bx, y, bz);
        }

        /// <summary>
        /// Сторона света по смещению (север — −Z)
        /// </summary>
        private static string _Side(float dx, float dz)
        {
            string ns = dz < -Math.Abs(dx) * .4f ? "север" : dz > Math.Abs(dx) * .4f ? "юг" : "";
            string ew = dx > Math.Abs(dz) * .4f ? "восток" : dx < -Math.Abs(dz) * .4f ? "запад" : "";
            return ns != "" && ew != "" ? ns + "о-" + ew : ns + ew;
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
