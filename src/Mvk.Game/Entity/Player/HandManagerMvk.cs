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
