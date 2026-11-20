using Mvk.Game.Item.List;
using Mvk.Engine.Network.Packets.Client;
using Mvk.Game.Entity.List;
using Mvk.Game.Item.List.Tool;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Games;
using Mvk.Engine.Item;
using Mvk.Core.Util;
using Mvk.Engine.Util;

namespace Mvk.Game.Entity.Player
{
    /// <summary>
    /// Объект менеджер действий рук игрока. Левой и правой клавишей мыши.
    /// В прошлом было объект ItemInWorldManager, что-то подобное, но там именно очередь ударов
    /// </summary>
    public class HandManagerMvk : HandManager
    {
        private readonly PlayerClientOwnerMvk _playerMvk;

        public HandManagerMvk(GameBase game, PlayerClientOwnerMvk player)
            : base(game, player)
        {
            _playerMvk = player;
        }

        /// <summary>
        /// Менеджер руки: left — левая рука (выживание, две руки)
        /// </summary>
        public HandManagerMvk(GameBase game, PlayerClientOwnerMvk player, bool left)
            : base(game, player, left)
        {
            _playerMvk = player;
        }

        /// <summary>
        /// Чем рука действует предметом Малювек
        /// </summary>
        protected override HandUse _HandUse(ItemStack stack)
        {
            ItemBase item = stack?.Item;
            if (item is ItemShield) return HandUse.Guard;
            if (item is ItemBow || item is ItemGun || item is ItemDynamite || item is ItemPiece || item is ItemFood)
            {
                return HandUse.Charge;
            }
            if (item is ItemBucket || item is ItemSpawn) return HandUse.Place;
            return base._HandUse(stack);
        }

        /// <summary>
        /// Блок щитом отправлен серверу
        /// </summary>
        private bool _blocking;

        /// <summary>
        /// Можно ли блокировать: щит в левой руке, правая свободна или держит оружие или инструмент
        /// </summary>
        private bool _CanBlock()
        {
            if (_player.Inventory.GetCurrentLeftItem()?.Item is not ItemShield) return false;
            ItemStack right = _player.Inventory.GetCurrentRightItem();
            return right == null || (right.Item is ItemAbTool && !right.Item.Two);
        }

        protected override void _Second(bool begin)
        {
            if (_CanBlock())
            {
                if (!_blocking)
                {
                    _blocking = true;
                    _game.TrancivePacket(new PacketC0BBlocking(true));
                }
                return;
            }
            base._Second(begin);
        }

        protected override void _SecondEnd()
        {
            if (_blocking)
            {
                _blocking = false;
                _game.TrancivePacket(new PacketC0BBlocking(false));
                return;
            }
            base._SecondEnd();
        }

        /// <summary>
        /// Действие анимации правой руки
        /// </summary>
        /// <param name="itemStack">Предмет которым делаю действие</param>
        /// <param name="moving">Выбранный объект</param>
        protected override bool _ActionAnimationRight(ItemStack itemStack, 
            MovingObjectPosition moving)
        {
            if (itemStack == null)
            {
               // return false;
                _player.Render.SetAnimationCodeAdd("AttackRight");
            }
            else
            {
                if (itemStack.Item is ItemAxe)
                {
                    ItemStack itemStackLeft = _playerMvk.InvPlayer.GetCurrentLeftItem();
                    if (itemStackLeft != null
                        && itemStackLeft.Item is ItemAxe)
                    {
                        _player.Render.SetAnimationCodeAdd("ActionAxes");
                        return true;
                    }
                }

                if (itemStack.Item.Action != "")
                {
                    _player.Render.SetAnimationCodeAdd(itemStack.Item.Action);
                    return true;
                }

                _player.Render.SetAnimationCodeAdd("ActionAxe");
            }
            return true;
        }
    }
}
