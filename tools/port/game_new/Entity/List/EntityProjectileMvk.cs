#nullable enable
using Mvk.Engine.Entity.List;
using Mvk.Engine.Entity.Physics;
using Mvk.Engine.World;

namespace Mvk.Game.Entity.List
{
    /// <summary>
    /// Снаряд Малювек: физика и наличие блоков как у предмета (EntityItemMvk), попадание считает сервер (EntityProjectile)
    /// </summary>
    public class EntityProjectileMvk : EntityProjectile
    {
        protected override void _InitPhysics(CollisionBase collision)
        {
            Physics = new PhysicsGround(collision, this, .5f);
            PresenceBlocks = new PresenceBlocksMvk(this);
        }
    }
}
