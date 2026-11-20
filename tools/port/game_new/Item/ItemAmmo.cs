#nullable enable
using System;
using Mvk.Engine.Item;
using Mvk.Game.Entity.Inventory;
using Mvk.Game.Entity.List;

namespace Mvk.Game.Item
{
    /// <summary>
    /// Боеприпасы в инвентаре игрока (сервер): поиск по условию и расход с рассылкой слота клиенту.
    /// Порядок поиска: правая и левая рука, карманы, рюкзак (как у игроков привычно: сначала то, что под рукой).
    /// </summary>
    public static class ItemAmmo
    {
        private static int _SlotCount => InventoryPlayerMvk.PocketCount + InventoryPlayerMvk.ClothCount + InventoryPlayerMvk.BackpackCount;

        /// <summary>
        /// Слот первого подходящего стака или -1
        /// </summary>
        public static int Find(PlayerServerMvk player, Predicate<ItemBase> match)
        {
            InventoryPlayerMvk inventory = player.InvPlayer;
            int current = inventory.GetCurrentIndex();
            if (_Is(inventory.GetStackInSlot(current), match)) return current;
            if (_Is(inventory.GetStackInSlot(InventoryPlayerMvk.PocketCount), match)) return InventoryPlayerMvk.PocketCount;
            for (int slot = 0; slot < _SlotCount; slot++)
            {
                if (_Is(inventory.GetStackInSlot(slot), match)) return slot;
            }
            return -1;
        }

        /// <summary>
        /// Сколько всего подходящих предметов
        /// </summary>
        public static int Count(PlayerServerMvk player, Predicate<ItemBase> match)
        {
            int count = 0;
            for (int slot = 0; slot < _SlotCount; slot++)
            {
                ItemStack stack = player.InvPlayer.GetStackInSlot(slot);
                if (_Is(stack, match)) count += stack.Amount;
            }
            return count;
        }

        /// <summary>
        /// Израсходовать один предмет из слота; в творческом режиме расхода нет
        /// </summary>
        public static void ConsumeOne(PlayerServerMvk player, int slot)
        {
            if (player.CreativeMode) return;
            ItemStack stack = player.InvPlayer.GetStackInSlot(slot);
            if (stack == null) return;
            if (stack.Amount <= 1) player.InvPlayer.SetInventorySlotContents(slot, null!);
            else player.InvPlayer.SetInventorySlotContents(slot, stack.ReduceAmount(1));
        }

        private static bool _Is(ItemStack? stack, Predicate<ItemBase> match)
            => stack != null && stack.Amount > 0 && match(stack.Item);
    }
}
