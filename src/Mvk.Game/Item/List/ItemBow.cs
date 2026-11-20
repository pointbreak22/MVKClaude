#nullable enable
using Mvk.Engine.Entity.List;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;
using Mvk.Game.Entity.List;

namespace Mvk.Game.Item.List
{
    /// <summary>
    /// Лук (двуручный, json Two): натяжение удержанием второй кнопки до 20 тактов, выстрел при отпускании.
    /// Клиент сообщает только силу натяжения; стрелу из инвентаря, выстрел, попадание и износ лука считает сервер.
    /// </summary>
    public class ItemBow : ItemBase
    {
        /// <summary>
        /// Такты полного натяжения
        /// </summary>
        public const int FullDraw = 20;

        public override ResultHandSecond OnSecond(bool begin, ItemStack stack, PlayerClientOwner player, int counter)
        {
            if (counter > FullDraw) counter = FullDraw;
            return new ResultHandSecond(5, counter);
        }

        public override int OnSecondEnd(ItemStack stack, PlayerClientOwner player, bool abort, int counter)
        {
            if (abort) return 255;
            if (counter > FullDraw) counter = FullDraw;
            return 100 + counter;
        }

        public override void OnUseSecond(ItemStack stack, PlayerServer player, int number)
        {
            if (number < 100 || number == 255 || player is not PlayerServerMvk mvk) return;
            int draw = number - 100;
            if (draw < 3) return; // не натянул
            int slot = ItemAmmo.Find(mvk, item => item is ItemArrow);
            if (slot < 0) return;
            ItemStack arrow = mvk.InvPlayer.GetStackInSlot(slot).Copy(1);
            float power = .4f + 2.1f * draw / FullDraw;
            if (EntityProjectile.Throw(player, arrow, power, EntityArrowMvk.AliasArrow) == null) return;
            ItemAmmo.ConsumeOne(mvk, slot);
            ItemWear.DamageHeld(player, 1);
        }
    }
}
