using Mvk.Game.Entity.AI.PathFinding;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.AI;

namespace Mvk.Game.Entity.AI
{
    /// <summary>
    /// Задача всплывать если в воде
    /// </summary>
    public class EntityAISwimming : EntityAIBase
    {
        /// <summary>
        /// Задача всплывать если в воде
        /// </summary>
        public EntityAISwimming(EntityMob entity) : base(entity, 4)
        {
            ((PathNavigateGround)entity.Navigator).CanSwim(true);
        }

        /// <summary>
        /// Возвращает значение, указывающее, следует ли начать выполнение
        /// </summary>
        public override bool ShouldExecute() => _entity.PresenceBlocks.IsInLiquid;

        /// <summary>
        /// Обновляет задачу
        /// </summary>
        public override void UpdateTask()
        {
            if (Rnd.NextFloat() < .8f)
            {
                _entity.MoveHelper.SetJumping();
            }
        }
    }
}
