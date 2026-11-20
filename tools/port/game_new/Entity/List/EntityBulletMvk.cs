#nullable enable
using Mvk.Core.Maths;
using Mvk.Engine.Entity;

namespace Mvk.Game.Entity.List
{
    /// <summary>
    /// Пуля огнестрельного оружия: быстрый снаряд, урон задаёт оружие при выстреле. Попадание считает сервер
    /// (EntityProjectile), после попадания в сущность или блок пуля исчезает.
    /// </summary>
    public class EntityBulletMvk : EntityProjectileMvk
    {
        public const string AliasBullet = "Bullet";

        /// <summary>
        /// Урон пули (задаёт оружие на сервере)
        /// </summary>
        public float BulletDamage = 10;

        protected override void _OnImpact(EntityLiving? entity, Vector3 pos, float hitY, EntityLiving? thrower)
        {
            entity?.Damage?.OnAttack((byte)EnumDamageSource.Piece, hitY, BulletDamage, thrower!);
            SetDead();
        }
    }
}
