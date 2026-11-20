#nullable enable
using Mvk.Core.Maths;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.World.Block;

namespace Mvk.Game.Entity.List
{
    /// <summary>
    /// Пуля огнестрельного оружия: быстрый снаряд, урон задаёт оружие при выстреле. Попадание считает сервер
    /// (EntityProjectile), после попадания в сущность или блок пуля исчезает. [ПОВЕДЕНИЕ] Вблизи (до CloseRange блоков
    /// от точки выстрела) урон умножается на CloseMultiplier; по блоку пуля бьёт как удар рукой силой BlockStrength
    /// (общий счёт добычи сервера, BlockHits), так блок можно расстрелять.
    /// </summary>
    public class EntityBulletMvk : EntityProjectileMvk
    {
        public const string AliasBullet = "Bullet";

        /// <summary>
        /// Урон пули (задаёт оружие на сервере)
        /// </summary>
        public float BulletDamage = 10;
        /// <summary>
        /// Сила удара пули по блоку (как удар рукой силой 1)
        /// </summary>
        public float BlockStrength = 1;
        /// <summary>
        /// Дистанция ближнего выстрела в блоках (0 — нет)
        /// </summary>
        public float CloseRange;
        /// <summary>
        /// Множитель урона ближнего выстрела
        /// </summary>
        public float CloseMultiplier = 1;
        /// <summary>
        /// Точка выстрела
        /// </summary>
        public Vector3 Origin;

        protected override void _OnImpact(EntityLiving? entity, Vector3 pos, float hitY, EntityLiving? thrower)
        {
            Vector3 d = pos - Origin;
            float multiplier = CloseRange > 0 && d.X * d.X + d.Y * d.Y + d.Z * d.Z <= CloseRange * CloseRange
                ? CloseMultiplier : 1;
            if (entity != null)
            {
                entity.Damage?.OnAttack((byte)EnumDamageSource.Piece, hitY, BulletDamage * multiplier, thrower!);
            }
            else
            {
                _HitBlock(pos, BlockStrength * multiplier, thrower);
            }
            SetDead();
        }

        /// <summary>
        /// Удар пули по блоку в точке попадания (чуть дальше по полёту)
        /// </summary>
        private void _HitBlock(Vector3 pos, float strength, EntityLiving? thrower)
        {
            float mx = Physics.MotionX, my = Physics.MotionY, mz = Physics.MotionZ;
            float length = Mth.Sqrt(mx * mx + my * my + mz * mz);
            if (length > .001f)
            {
                pos = new Vector3(pos.X + mx / length * .05f, pos.Y + my / length * .05f, pos.Z + mz / length * .05f);
            }
            BlockPos blockPos = new BlockPos(pos.X, pos.Y, pos.Z);
            BlockState state = _worldServer.GetBlockState(blockPos);
            if (state.IsAir()) return;
            BlockBase block = state.GetBlock();
            if (block.Material.Liquid || block.Hardness <= 0) return;
            if (!_worldServer.BlockHits.Hit(_worldServer, blockPos, block.Hardness, strength)) return;
            if (thrower is PlayerServer player)
            {
                if (_worldServer.SetBreakBlock(blockPos, player) && player.IsDropBreakingBlock())
                {
                    block.DropBlockAsItem(_worldServer, player, blockPos, state);
                }
            }
            else
            {
                _worldServer.SetBlockToAir(blockPos);
            }
        }
    }
}
