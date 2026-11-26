#nullable enable
using System;
using System.Runtime.CompilerServices;
using Mvk.Core.Maths;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.AI;
using Mvk.Engine.Entity.AI.PathFinding;
using Mvk.Engine.Entity.Physics;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Entity.Sizes;
using Mvk.Engine.Item;
using Mvk.Engine.Network.Packets.Server;
using Mvk.Engine.World;
using Mvk.Engine.World.Block;
using Mvk.Game.Entity.AI;
using Mvk.Game.Entity.AI.PathFinding;
using Mvk.Game.Entity.Damage;

namespace Mvk.Game.Entity.List
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Враждебный моб порта (просьба пользователя 21.11): гуманоид, замечает игрока в выживании
    /// в пределах SightRange, преследует и атакует (задача атаки задаётся наследником), получает урон от оружия,
    /// умирает (труп, как у остальных живых) и при исчезновении трупа роняет добычу (_Loot). Полоска здоровья над
    /// ним видна всегда. Урон, откат атаки и отбрасывание — на сервере.
    /// </summary>
    public abstract class EntityHostileMvk : EntityMob
    {
        /// <summary>
        /// Дальность, на которой моб замечает игрока, блоков
        /// </summary>
        public float SightRange { get; protected set; } = 32;

        public override bool HealthBarAlways => true;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Игрок, за которым моб пришёл (EnemySpawner): цель на любом расстоянии до 160 блоков
        /// </summary>
        public EntityLiving? Hunt;

        public EntityHostileMvk() : base() => _persistenceRequired = true;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Пока моб идёт к своему игроку (Hunt, до 160 блоков), он не исчезает случайным деспавном
        /// дальних мобов; без цели — обычный деспавн
        /// </summary>
        protected override bool _CanDespawn() => Hunt == null || !_IsTarget(Hunt) || Distance(Hunt) > 160;

        /// <summary>
        /// Размер как у игрока
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void _InitSize()
            => Size = SizeLiving = new SizeEntityLiving(this, .6f, 3.6f, 3.36f, 80);

        protected override void _InitPhysics(CollisionBase collision)
        {
            PhysicsGroundLiving physicsGround = new PhysicsGroundLiving(collision, this);
            physicsGround.SetHeightAutoJump(1f);
            Physics = physicsGround;
            PresenceBlocks = new PresenceBlocksMvk(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override PathNavigate _InitNavigate(WorldServer world) => new PathNavigateGround(this, world);

        protected override void _InitServer()
        {
            base._InitServer();
            Damage = new DamageLiving(this);
            _tasks.AddTask(0, new EntityAISwimming(this));
            _AddAttackTask();
            _tasks.AddTask(5, new EntityAIWander(this, .006f));
            _tasks.AddTask(6, new EntityAIWatchClosest(this, 16f));
            _tasks.AddTask(7, new EntityAILookIdle(this));
        }

        /// <summary>
        /// Задача атаки (приоритет 1)
        /// </summary>
        protected abstract void _AddAttackTask();

        /// <summary>
        /// Добыча при исчезновении трупа: предмет и количество
        /// </summary>
        protected abstract (string Alias, int Min, int Max) _Loot();

        /// <summary>
        /// Цель: последний ударивший или ближайший игрок в выживании в пределах SightRange
        /// </summary>
        public EntityLiving? FindTarget()
        {
            // Пришедший за игроком (периодический спавн) идёт к нему издалека
            if (Hunt != null && _IsTarget(Hunt) && Distance(Hunt) < 160) return Hunt;
            if (GetAITarget() is EntityLiving attacker && _IsTarget(attacker)
                && Distance(attacker) < SightRange * 1.5f) return attacker;
            if (Tracker?.ClosestPlayer is PlayerServer player && _IsTarget(player)
                && Tracker.DistantionPlayer < SightRange) return player;
            return null;
        }

        private static bool _IsTarget(EntityLiving entity)
            => !entity.IsDead && entity.GetHealth() > 0
            && !(entity is PlayerBase player && (player.CreativeMode || player.IsSpectator()));

        /// <summary>
        /// [ПОВЕДЕНИЕ] Прямая видимость цели: луч по блокам от глаз моба к середине цели
        /// </summary>
        public bool CanSee(EntityLiving target)
        {
            float ex = PosX, ey = PosY + SizeLiving.GetEye(), ez = PosZ;
            float dx = target.PosX - ex, dy = target.PosY + target.Size.GetHeight() * .6f - ey, dz = target.PosZ - ez;
            float length = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
            if (length < .1f) return true;
            WorldServer world = GetWorldServer();
            world.Collision.RayCastBlock(ex, ey, ez, new Vector3(dx / length, dy / length, dz / length), length, true, false, true);
            return !world.Collision.MovingObject.IsBlock();
        }

        /// <summary>
        /// [ПОВЕДЕНИЕ] В воде плыть прямо к цели (навигация по земле воду не прокладывает)
        /// </summary>
        public void SwimTo(EntityLiving target, float speed)
        {
            if (!PresenceBlocks.IsInLiquid) return;
            MoveHelper.SetMoveTo(target.PosX, target.PosY, target.PosZ, speed);
            if (target.PosY > PosY) MoveHelper.SetJumping();
        }

        /// <summary>
        /// Такты ломания блока перед мобом
        /// </summary>
        private int _breakTicks;
        private BlockPos _breakPos;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Застрявший моб ломает дверь или мягкий блок (твёрдость до EnemySpawner.BreakHardness) на пути
        /// к цели в полный рост; ломание занимает EnemySpawner.BreakTicks тактов, с взмахами. false — ломать нечего
        /// </summary>
        public bool BreakAhead(EntityLiving target)
        {
            float dx = target.PosX - PosX, dz = target.PosZ - PosZ;
            float length = MathF.Sqrt(dx * dx + dz * dz);
            if (length < .1f) return false;
            WorldServer world = GetWorldServer();
            int y0 = (int)MathF.Floor(PosY);
            // Ближайший мешающий блок в полный рост на 1–3 блока вперёд по направлению к цели
            for (int k = 0; k < 12; k++)
            {
                float ahead = 1f + (k >> 2) * .9f;
                int x = (int)MathF.Floor(PosX + dx / length * ahead), z = (int)MathF.Floor(PosZ + dz / length * ahead);
                int i = k & 3;
                BlockPos pos = new BlockPos(x, y0 + i, z);
                BlockState state = world.GetBlockState(pos);
                if (state.IsAir()) continue;
                BlockBase block = state.GetBlock();
                // Трава, жидкость и прочее без твёрдости не мешают — смотрим выше
                if (block.Material.Liquid || block.Hardness <= 0) continue;
                if (block.Hardness > EnemySpawner.BreakHardness) return false;
                if (!pos.Equals(_breakPos)) { _breakPos = pos; _breakTicks = 0; }
                if (_breakTicks % 10 == 0) Swing();
                if (++_breakTicks >= EnemySpawner.BreakTicks)
                {
                    _breakTicks = 0;
                    world.SetBlockToAir(pos);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Взмах рукой у всех клиентов
        /// </summary>
        public void Swing(string animation = "AttackRight")
            => GetWorldServer().Tracker.SendToAllTrackingEntity(Id, new PacketS0BAnimation(Id, animation));

        /// <summary>
        /// Удар по цели: урон (блок щитом и броня считаются как у ударов игрока) и отбрасывание
        /// </summary>
        public void HitTarget(EntityLiving target, float damage, float knockback)
        {
            if (target.Damage == null) return;
            if (!target.Damage.OnAttack((byte)EnumDamageSource.Player, -1, damage, this)) return;
            float dx = target.PosX - PosX, dz = target.PosZ - PosZ;
            float len = MathF.Sqrt(dx * dx + dz * dz);
            if (len > .001f) target.SetPhysicsImpulse(dx / len * knockback, knockback * .6f, dz / len * knockback);
        }

        /// <summary>
        /// Исчезновение трупа: добыча на сервере, если моб убит (а не исчез из мира)
        /// </summary>
        public override void SetDead()
        {
            if (!IsDead && GetHealth() <= 0 && _worldServer is WorldServer world)
            {
                (string alias, int min, int max) = _Loot();
                int amount = min + world.Rnd.Next(max - min + 1);
                for (int i = 0; i < Ce.Items.ItemAlias.Length && amount > 0; i++)
                {
                    if (Ce.Items.ItemAlias[i] != alias) continue;
                    ItemStack.SpawnAsEntity(world, new BlockPos(PosX, PosY + .5f, PosZ),
                        Ce.Items.ItemObjects[i], (byte)amount);
                    break;
                }
            }
            base.SetDead();
        }

        public override void UpdateClient(WorldClient world, float deltaTime)
        {
            UpdatePositionServer();
            _RotationBody();
            Render.UpdateClient(world, deltaTime);
        }
    }

    /// <summary>
    /// [ПОВЕДЕНИЕ] Зомби: ближний бой — догоняет игрока и бьёт рукой (2 урона, откат 20 тактов, отбрасывание)
    /// </summary>
    public sealed class EntityZombieMvk : EntityHostileMvk
    {
        public const string AliasZombie = "Zombie";

        protected override float _GetHelathMax() => 20;

        protected override void _InitServer()
        {
            base._InitServer();
            Speed = .065f;
        }

        protected override void _AddAttackTask() => _tasks.AddTask(1, new EntityAIAttackMeleeMvk(this, 1.3f, 2f, 20, .6f));

        protected override (string Alias, int Min, int Max) _Loot() => ("Powder", 1, 3);
    }

    /// <summary>
    /// [ПОВЕДЕНИЕ] Разбойник: стрелок — держит дистанцию и стреляет из лука стрелами (снаряд Arrow с серверным
    /// попаданием), вблизи отступает
    /// </summary>
    public sealed class EntityBanditMvk : EntityHostileMvk
    {
        public const string AliasBandit = "Bandit";

        protected override float _GetHelathMax() => 14;

        protected override void _InitServer()
        {
            base._InitServer();
            Speed = .06f;
            SightRange = 40;
        }

        protected override void _AddAttackTask() => _tasks.AddTask(1, new EntityAIAttackRangedMvk(this, 1.2f, 10f, 26f, 40));

        protected override (string Alias, int Min, int Max) _Loot() => ("Arrow", 2, 5);
    }
}
