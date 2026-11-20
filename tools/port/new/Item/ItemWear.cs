#nullable enable
using Mvk.Engine.Entity.Player;
using Mvk.Engine.World;

namespace Mvk.Engine.Item
{
    /// <summary>
    /// Износ предметов, который считает сервер (удар оружием, броня, щит, мотыга, стрелковое оружие): прочность,
    /// поломка со звуком и рассылка слота клиенту (InventoryPlayer.SetInventorySlotContents → SlotSetted).
    /// </summary>
    public static class ItemWear
    {
        /// <summary>
        /// Износить стак в слоте инвентаря игрока. В творческом режиме износа нет.
        /// </summary>
        /// <returns>true — предмет сломался</returns>
        public static bool DamageSlot(PlayerServer player, int slot, int damage)
        {
            if (player.CreativeMode || damage <= 0) return false;
            ItemStack stack = player.Inventory.GetStackInSlot(slot);
            if (stack == null || !stack.IsItemStackDamageable()) return false;
            if (stack.Wear(damage))
            {
                player.Inventory.SetInventorySlotContents(slot, null!);
                WorldServer world = player.GetWorldServer();
                world?.PlaySoundDistance(stack.Item.SampleBreak, player.GetPositionCenterVec(), 1,
                    world.Rnd.NextFloat() * .4f + .8f);
                return true;
            }
            player.Inventory.SetInventorySlotContents(slot, stack);
            return false;
        }

        /// <summary>
        /// Износить предмет в правой руке
        /// </summary>
        public static bool DamageHeld(PlayerServer player, int damage)
            => DamageSlot(player, player.Inventory.GetCurrentIndex(), damage);
    }
}
