#nullable enable
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;
using Mvk.Engine.World.Block;

namespace Mvk.Game.Item.List.Tool
{
    /// <summary>
    /// Меч: оружие ближнего боя. Урон по сущности из json (Damage), износ при ударе считает сервер (ItemWear),
    /// блоки ломает только те, что ломаются рукой, и медленно.
    /// </summary>
    public class ItemSword : ItemAbTool
    {
        /// <summary>
        /// Ускорение добычи блока (сервер): только блоки, которые ломаются рукой
        /// </summary>
        public override float DigAcceleration(BlockBase block, PlayerBase player)
            => player.CanDestroyedBlock(block) ? Acceleration : 0;

        public override ResultHandAction OnAction(bool begin, ItemStack stack, PlayerClientOwner player)
        {
            if (player.MovingObject.IsBlock() && player.CanDestroyedBlock(player.MovingObject.Block.GetBlock()))
            {
                // Мечом блоки ломаются медленнее руки
                return new ResultHandAction(Pause, Acceleration);
            }
            return base.OnAction(begin, stack, player);
        }
    }
}
