#nullable enable
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;

namespace Mvk.Game.Item.List.Tool
{
    /// <summary>
    /// Меч: оружие ближнего боя. Урон по сущности из json (Damage), износ при ударе считает сервер (ItemWear),
    /// блоки ломает только те, что ломаются рукой, и медленно.
    /// </summary>
    public class ItemSword : ItemAbTool
    {
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
