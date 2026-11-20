#nullable enable
using Mvk.Engine.Entity.List;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;
using Mvk.Game.Entity.List;

namespace Mvk.Game.Item.List
{
    /// <summary>
    /// Динамит: бросок с замахом (держать кнопку второго действия), взрыв при попадании (EntityDynamiteMvk).
    /// По идее ItemUniExplosives старого проекта автора Mvk. Бросок и взрыв считает сервер.
    /// </summary>
    public class ItemDynamite : ItemBase
    {
        public override ResultHandSecond OnSecond(bool begin, ItemStack stack, PlayerClientOwner player, int counter)
        {
            ResultHandSecond result = _CheckEntitySecond(player);
            if (result.Action == ResultHandSecond.ActionType.None || player.IsKeyShift())
            {
                if (counter > 20) counter = 20;
                return new ResultHandSecond(3, counter / 4);
            }
            return result;
        }

        public override int OnSecondEnd(ItemStack stack, PlayerClientOwner player, bool abort, int counter)
        {
            if (abort) return 255;
            if (counter > 20) counter = 20;
            return counter / 4 + 100;
        }

        public override void OnUseSecond(ItemStack stack, PlayerServer player, int number)
        {
            if (number < 100 || number == 255) return;
            number -= 100;
            if (EntityProjectile.Throw(player, stack.Copy(1), .5f + number * .125f, EntityDynamiteMvk.AliasDynamite) != null
                && !player.CreativeMode)
            {
                player.Inventory.DecrCurrentItem(1);
            }
        }
    }
}
