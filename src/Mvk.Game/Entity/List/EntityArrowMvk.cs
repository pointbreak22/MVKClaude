#nullable enable
using Mvk.Core.Maths;
using Mvk.Engine.Entity;

namespace Mvk.Game.Entity.List
{
    /// <summary>
    /// Стрела: снаряд лука. Урон по сущности считает сервер (EntityProjectile: урон стрелы × сила), в блоке стрела
    /// застревает (без гравитации и движения) и остаётся предметом, который можно подобрать.
    /// </summary>
    public class EntityArrowMvk : EntityProjectileMvk
    {
        public const string AliasArrow = "Arrow";

        protected override void _OnImpact(EntityLiving? entity, Vector3 pos, float hitY, EntityLiving? thrower)
        {
            // [ПОВЕДЕНИЕ] Стрела врага не ранит других врагов (группы разбойников и зомби)
            if (!(thrower is EntityHostileMvk && entity is EntityHostileMvk)) base._OnImpact(entity, pos, hitY, thrower);
            if (entity == null)
            {
                // Застряла в блоке
                Physics.MotionX = Physics.MotionY = Physics.MotionZ = 0;
                Physics.SetGravity(0);
            }
        }
    }
}
