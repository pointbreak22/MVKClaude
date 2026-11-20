#nullable enable
using System;
using System.Collections.Generic;
using Mvk.Core.Maths;
using Mvk.Core.Util;
using Mvk.Engine.Audio;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Util;
using Mvk.Engine.World;
using Mvk.Engine.World.Block;
using Mvk.Game.Entity.Particle;
using Mvk.Game.World.Block;

namespace Mvk.Game.World
{
    /// <summary>
    /// Взрыв на сервере (динамит). По идее Explosion старого проекта автора Mvk, код свой: лучи из центра к граням куба
    /// радиуса взрыва, сила луча случайно 0.7..1.3 от силы взрыва, каждый твёрдый блок гасит луч по своей твёрдости,
    /// пока сила остаётся, блок разрушается. Жидкость и коренная порода останавливают луч. Разрушенный блок выпадает
    /// с шансом 1/сила, если взрыв устроил игрок. Живые сущности в радиусе получают урон и толчок от центра.
    /// Всё решает сервер, клиенты получают блоки, частицы и звук обычными пакетами.
    /// </summary>
    public static class ExplosionMvk
    {
        /// <summary>
        /// Наибольший радиус лучей в блоках
        /// </summary>
        private const int _MaxRadius = 8;

        public static void Explode(WorldServer world, Vector3 pos, float strength, float radius, EntityLiving? source)
        {
            int r = Math.Min(_MaxRadius, (int)MathF.Ceiling(radius * 1.1f));
            var destroy = new List<BlockPos>();
            var seen = new HashSet<(int, int, int)>();
            Rand rnd = world.Rnd;
            for (int x = -r; x <= r; x++)
            {
                for (int y = -r; y <= r; y++)
                {
                    for (int z = -r; z <= r; z++)
                    {
                        if (x != -r && x != r && y != -r && y != r && z != -r && z != r) continue;
                        float len = MathF.Sqrt(x * x + y * y + z * z);
                        float dx = x / len, dy = y / len, dz = z / len;
                        float dist = radius * (.9f + rnd.NextFloat() * .2f);
                        float power = strength * (.7f + rnd.NextFloat() * .6f);
                        float fx = pos.X, fy = pos.Y, fz = pos.Z;
                        for (float d = 0; d < dist && power > 0; d++, fx += dx, fy += dy, fz += dz)
                        {
                            BlockPos blockPos = new BlockPos(fx, fy, fz);
                            if (blockPos.Y < 0) break;
                            BlockState state = world.GetBlockState(blockPos);
                            if (state.IsAir()) continue;
                            BlockBase block = state.GetBlock();
                            if (block.Material.Liquid || block.Material.IndexMaterial == (int)EnumMaterial.Bedrock) break;
                            power -= (block.Hardness / 4f + .3f) * .3f;
                            if (power > 0 && seen.Add((blockPos.X, blockPos.Y, blockPos.Z))) destroy.Add(blockPos);
                        }
                    }
                }
            }

            PlayerServer? player = source as PlayerServer;
            foreach (BlockPos blockPos in destroy)
            {
                BlockState state = world.GetBlockState(blockPos);
                if (state.IsAir()) continue;
                if (player != null && rnd.NextFloat() < 1f / strength)
                {
                    state.GetBlock().DropBlockAsItem(world, player, blockPos, state);
                }
                world.SetBlockToAir(blockPos);
            }

            _HurtEntities(world, pos, strength, radius * 2, source);

            world.SpawnParticle(EntitiesFXRegMvk.SmokeId, 30 + destroy.Count / 4, pos, new Vector3(radius), 0, 0);
            world.PlaySoundDistance(AudioIndexs.GetKey("Explode"), pos, 1, .9f + rnd.NextFloat() * .2f);
        }

        /// <summary>
        /// Урон и толчок живым сущностям в радиусе: чем ближе к центру, тем сильнее
        /// </summary>
        private static void _HurtEntities(WorldServer world, Vector3 pos, float strength, float radius, EntityLiving? source)
        {
            AxisAlignedBB aabb = new AxisAlignedBB(pos - new Vector3(radius), pos + new Vector3(radius));
            // Копия списка коллизии: урон и толчок могут снова обращаться к коллизии мира
            world.Collision.EntityBoundingBoxesFromSector(aabb, -1);
            var list = new List<EntityBase>(world.Collision.ListEntity.Count);
            for (int i = 0; i < world.Collision.ListEntity.Count; i++) list.Add(world.Collision.ListEntity[i]);
            world.Collision.ListEntity.Clear();
            foreach (EntityBase entity in list)
            {
                if (entity is not EntityLiving living || living.IsDead) continue;
                Vector3 center = living.GetPositionCenterVec();
                Vector3 v = center - pos;
                float d = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
                if (d >= radius) continue;
                float k = 1f - d / radius;
                living.Damage?.OnAttack((byte)EnumDamageSource.ExplosionSource, -1, strength * 2.5f * k, source!);
                if (d > .01f)
                {
                    float impulse = k * 1.2f;
                    living.SetPhysicsImpulse(v.X / d * impulse, .4f * k + v.Y / d * impulse * .5f, v.Z / d * impulse);
                }
            }
        }
    }
}
