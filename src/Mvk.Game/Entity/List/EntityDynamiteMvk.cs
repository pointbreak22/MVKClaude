#nullable enable
using Mvk.Core.Maths;
using Mvk.Engine.Entity;
using Mvk.Game.World;

namespace Mvk.Game.Entity.List
{
    /// <summary>
    /// Брошенный динамит: взрывается при первом попадании в блок, сущность или землю (EntityExplosives старого Mvk).
    /// Попадание и взрыв считает сервер.
    /// </summary>
    public class EntityDynamiteMvk : EntityProjectileMvk
    {
        /// <summary>
        /// Псевдоним сущности
        /// </summary>
        public const string AliasDynamite = "Dynamite";
        /// <summary>
        /// Сила взрыва
        /// </summary>
        public const float Strength = 4;
        /// <summary>
        /// Радиус взрыва в блоках
        /// </summary>
        public const float Radius = 4;

        protected override void _OnImpact(EntityLiving? entity, Vector3 pos, float hitY, EntityLiving? thrower)
        {
            SetDead();
            ExplosionMvk.Explode(_worldServer, pos, Strength, Radius, thrower);
        }
    }
}
