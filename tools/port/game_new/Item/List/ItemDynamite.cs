#nullable enable
using Mvk.Core.Maths;
using Mvk.Engine.Audio;
using Mvk.Engine.Entity.List;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;
using Mvk.Engine.World;
using Mvk.Game.Entity.List;
using Mvk.Game.World;

namespace Mvk.Game.Item.List
{
    /// <summary>
    /// Динамит: бросок с замахом (держать кнопку второго действия), взрыв при попадании (EntityDynamiteMvk).
    /// По идее ItemUniExplosives старого проекта автора Mvk. Бросок и взрыв считает сервер.
    /// [ПОВЕДЕНИЕ] В выживании клик рукой с факелом, когда в другой руке динамит, поджигает его: фитиль FuseTicks
    /// (3 с). Брошенный подожжённым взрывается, когда догорит фитиль; не успел бросить — взрыв в руке.
    /// </summary>
    public class ItemDynamite : ItemBase
    {
        /// <summary>
        /// [ПОВЕДЕНИЕ] Фитиль подожжённого динамита, тактов
        /// </summary>
        public const int FuseTicks = 60;

        public override bool OnIgnite(ItemStack stack, PlayerServer player, ItemStack lighter, bool left)
        {
            if (lighter.Item != ItemsRegMvk.Torch || player.FuseEnd != 0) return false;
            WorldServer world = player.GetWorldServer();
            player.FuseEnd = world.Server.TickCounterGlobal + FuseTicks;
            player.FuseLeft = left;
            world.PlaySoundDistance(AudioIndexs.GetKey("Click"), player.GetPositionCenterVec(), .8f, 1.6f);
            return true;
        }

        public override void OnFuseEnd(ItemStack stack, PlayerServer player)
        {
            if (stack.Item != this) return;
            if (!player.CreativeMode) player.Inventory.DecrCurrentItem(1);
            ExplosionMvk.Explode(player.GetWorldServer(),
                new Vector3(player.PosX, player.PosY + player.GetEyeHeight() * .7f, player.PosZ),
                EntityDynamiteMvk.Strength, EntityDynamiteMvk.Radius, player);
        }

        /// <summary>
        /// Бросок: подожжённый в этой руке летит с остатком фитиля, иначе взрыв при попадании
        /// </summary>
        public override bool OnThrow(ItemStack stack, PlayerServer player, float power)
        {
            EntityProjectile? shot = EntityProjectile.Throw(player, stack.Copy(1), power, EntityDynamiteMvk.AliasDynamite);
            if (shot == null) return false;
            uint tick = player.GetWorldServer().Server.TickCounterGlobal;
            if (player.FuseEnd != 0 && player.FuseLeft == player.Inventory.HandLeft && shot is EntityDynamiteMvk dynamite)
            {
                dynamite.FuseTicks = (int)System.Math.Max(1, player.FuseEnd - tick);
                dynamite.Igniter = player;
                player.FuseEnd = 0;
            }
            return true;
        }

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
