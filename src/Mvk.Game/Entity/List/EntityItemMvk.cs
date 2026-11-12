using Mvk.Game.Entity.Damage;
using System.Runtime.CompilerServices;
using Mvk.Engine.Entity.List;
using Mvk.Engine.Entity.Physics;
using Mvk.Engine.World;

namespace Mvk.Game.Entity.List
{
    /// <summary>
    /// Сущность предмета
    /// </summary>
    public class EntityItemMvk : EntityItem
    {
        /// <summary>
        /// Инициализация физики
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void _InitPhysics(CollisionBase collision)
        {
            Physics = new PhysicsGround(collision, this, .5f);
            PresenceBlocks = new PresenceBlocksMvk(this);
            
        }

        /// <summary>
        /// Инициализация на сервере, после всех инициализаций
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void _InitServer()
        {
            Damage = new DamageItem(this);
        }
    }
}
