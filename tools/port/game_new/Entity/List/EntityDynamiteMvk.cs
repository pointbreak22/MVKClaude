#nullable enable
using Mvk.Core.Maths;
using Mvk.Engine.Entity;
using Mvk.Game.World;

namespace Mvk.Game.Entity.List
{
    /// <summary>
    /// Брошенный динамит: взрывается при первом попадании в блок, сущность или землю (EntityExplosives старого Mvk).
    /// [ПОВЕДЕНИЕ] Подожжённый (FuseTicks > 0) не взрывается от удара, а лежит или катится и взрывается, когда
    /// догорит фитиль. Попадание и взрыв считает сервер.
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

        /// <summary>
        /// [ПОВЕДЕНИЕ] Тактов до взрыва подожжённого динамита (0 — взрыв при попадании)
        /// </summary>
        public int FuseTicks;

        /// <summary>
        /// Кто бросил подожжённый динамит (для выпадения блоков)
        /// </summary>
        public EntityLiving? Igniter;

        public override void Update()
        {
            base.Update();
            if (FuseTicks > 0 && !IsDead && --FuseTicks == 0)
            {
                SetDead();
                ExplosionMvk.Explode(_worldServer, new Vector3(PosX, PosY + .2f, PosZ), Strength, Radius, Igniter);
            }
        }

        protected override void _OnImpact(EntityLiving? entity, Vector3 pos, float hitY, EntityLiving? thrower)
        {
            if (FuseTicks > 0) return;
            SetDead();
            ExplosionMvk.Explode(_worldServer, pos, Strength, Radius, thrower);
        }
    }
}
