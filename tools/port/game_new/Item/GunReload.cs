#nullable enable
using Mvk.Engine.Item;
using Mvk.Game.Entity.Inventory;
using Mvk.Game.Entity.List;
using Mvk.Game.Item.List;

namespace Mvk.Game.Item
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Перезарядка оружия игрока по тактам сервера: оружие в любом слоте кармана и в левой руке само
    /// заряжает по стволу за ReloadTicks, пока есть пуля и заряд. Время хранится за самим стаком оружия: при переносе
    /// оружия в другой слот (в левую руку и обратно) отсчёт продолжается, новый стак (выстрел, заряд ствола) начинает
    /// его заново. Двуручное оружие при закрытом щите не перезаряжается (отсчёт стоит). Пока идёт перезарядка
    /// и пауза после выстрела, оружие не стреляет. Клиент показывает откат в HUD по тем же правилам (HudMvk).
    /// </summary>
    public sealed class GunReload
    {
        /// <summary>
        /// Слоты с перезарядкой: карманы и левая рука
        /// </summary>
        public const int SlotCount = InventoryPlayerMvk.PocketCount + 1;

        private readonly ItemStack?[] _stacks = new ItemStack?[SlotCount];
        private readonly int[] _ticks = new int[SlotCount];
        private readonly int[] _pause = new int[SlotCount];
        private readonly ItemStack?[] _stacksPrev = new ItemStack?[SlotCount];
        private readonly int[] _ticksPrev = new int[SlotCount];
        private readonly int[] _pausePrev = new int[SlotCount];

        /// <summary>
        /// Такт игрока на сервере
        /// </summary>
        public void Update(PlayerServerMvk player)
        {
            InventoryPlayerMvk inventory = player.InvPlayer;
            _stacks.CopyTo(_stacksPrev, 0);
            _ticks.CopyTo(_ticksPrev, 0);
            _pause.CopyTo(_pausePrev, 0);
            for (int slot = 0; slot < SlotCount; slot++)
            {
                ItemStack? stack = inventory.GetStackInSlot(slot);
                if (stack != _stacksPrev[slot])
                {
                    // Стак переехал из другого слота: отсчёт идёт за ним
                    int from = System.Array.IndexOf(_stacksPrev, stack);
                    _ticks[slot] = stack != null && from >= 0 ? _ticksPrev[from] : 0;
                    _pause[slot] = stack != null && from >= 0 ? _pausePrev[from] : 0;
                    _stacks[slot] = stack;
                }
                if (_pause[slot] > 0) _pause[slot]--;
                if (stack?.Item is not ItemGun gun || !gun.IsReloading(stack, inventory, player.CreativeMode))
                {
                    _ticks[slot] = 0;
                    continue;
                }
                if (gun.Two && player.IsBlocking) continue;
                if (++_ticks[slot] < gun.ReloadTicks) continue;
                _ticks[slot] = 0;
                if (gun.LoadBarrel(player, slot)) _stacks[slot] = inventory.GetStackInSlot(slot);
            }
        }

        /// <summary>
        /// Можно ли выстрелить оружием слота: не идёт перезарядка и пауза после выстрела
        /// (пауза с запасом в такт на задержку сети)
        /// </summary>
        public bool CanShoot(int slot, ItemStack stack, InventoryPlayerMvk inventory, bool creative)
            => stack.Item is ItemGun gun && !gun.IsReloading(stack, inventory, creative)
            && (slot >= SlotCount || _stacks[slot] != stack || _pause[slot] <= 1);

        /// <summary>
        /// Выстрел оружием слота: пауза после выстрела за новым стаком
        /// </summary>
        public void Shot(int slot, ItemStack? stack, int pause)
        {
            if (slot >= SlotCount) return;
            _stacks[slot] = stack;
            _ticks[slot] = 0;
            _pause[slot] = pause;
        }
    }
}
