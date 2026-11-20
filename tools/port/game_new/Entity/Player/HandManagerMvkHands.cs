#nullable enable
using Mvk.Core.Maths;
using Mvk.Engine.Item;
using Mvk.Engine.Network.Packets.Client;
using Mvk.Engine.Util;
using Mvk.Game.Item.List;
using Mvk.Game.Item.List.Tool;

namespace Mvk.Game.Entity.Player
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Две руки Малювек (выживание): щит в любой руке, раскрытие щита на время действия другой руки,
    /// удар щитом с разгона, «крест» двумя мечами и добивающий удар после него. Состояние блока серверу (пакет C0B) шлёт менеджер левой руки:
    /// его такт идёт последним, он видит обе руки. Работает ли блок и чем (щит или два меча), решает сервер
    /// (DamageLiving).
    /// </summary>
    public partial class HandManagerMvk
    {
        /// <summary>
        /// Менеджер другой руки
        /// </summary>
        public HandManagerMvk? Other;

        /// <summary>
        /// «Крест» был в прошлом такте; тактов, пока после него ждём отпускания обеих кнопок
        /// </summary>
        private bool _crossPrev;
        private int _crossEndTicks;

        /// <summary>
        /// Предмет руки менеджера без учёта руки действия
        /// </summary>
        private ItemStack? _ItemOf(HandManagerMvk hand)
            => hand._left ? _player.Inventory.GetCurrentLeftItem() : _player.Inventory.GetCurrentRightItem();

        /// <summary>
        /// «Крест»: в обеих руках мечи и обе кнопки зажаты дольше порога удержания
        /// </summary>
        private bool _Cross()
            => Other != null && HandTicks >= HoldTicks && Other.HandTicks >= HoldTicks
            && _ItemOf(this)?.Item is ItemSword && _ItemOf(Other)?.Item is ItemSword;

        /// <summary>
        /// При «кресте» мечи не бьют, а держат блок
        /// </summary>
        protected override bool _HoldOverride() => _Cross() || _GunGuarded();

        /// <summary>
        /// Двуручное оружие (двустволка) при закрытом щите другой руки не стреляет и не перезаряжается
        /// </summary>
        private bool _GunGuarded()
            => Other != null && Other.Guarding && _ItemOf(this)?.Item is ItemGun gun && gun.Two;

        protected override bool _ClickOverride() => _GunGuarded();

        /// <summary>
        /// Перезарядка оружия щит не раскрывает (одной рукой, при закрытом щите)
        /// </summary>
        protected override bool _HoldOpensGuard()
            => !(_Use == HandUse.Charge && _player.Inventory.GetCurrentItem()?.Item is ItemGun);

        /// <summary>
        /// Разгон (бег, в воздухе, скорость быстрее шага); окончательно решает сервер (PlayerServer.IsMomentum)
        /// </summary>
        private bool _Momentum()
        {
            float mx = _player.Physics.MotionX, mz = _player.Physics.MotionZ;
            return _player.IsSprinting() || !_player.OnGround || mx * mx + mz * mz > .09f || _player.Physics.MotionY < -.3f;
        }

        /// <summary>
        /// Удар щитом с разгона: малый урон, сильное отталкивание (сервер проверяет бег, Knockback щита)
        /// </summary>
        protected override void _GuardPress()
        {
            MovingObjectPosition moving = _player.MovingObject;
            if (!_Momentum() || !moving.IsEntity()) return;
            Vector3 vec = (moving.Entity!.GetPositionVec() - _player.GetPositionVec()).Normalize();
            _game.TrancivePacket(new PacketC03UseEntity(moving.Entity.Id, moving.RayHit.Y,
                vec.X * .9f, vec.Y * .18f, vec.Z * .9f).Hand(_left));
            _player.Render.SetAnimationCodeAdd(_AnimationCode("ActionAxe"));
        }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Анимация действия левой руки (две руки): зеркальные удар и взмах; двуручные анимации общие
        /// </summary>
        protected override string _AnimationCode(string code)
            => !_left || !_player.IsTwoHands() ? code : code switch
            {
                "AttackRight" => "AttackLeft",
                "ActionAxe" => "ActionAxeLeft",
                _ => code
            };

        /// <summary>
        /// Конец такта левой руки: блок держится, если щит одной руки зажат и другая рука не действует, или «крест»
        /// </summary>
        protected override void _AfterHandTick()
        {
            if (!_left || Other == null) return;
            bool cross = _Cross();
            // [ПОВЕДЕНИЕ] Добивающий удар: «крест» разомкнут отпусканием обеих кнопок (в пределах 4 тактов) — удар обоими мечами
            if (_crossPrev && !cross) _crossEndTicks = 4;
            else if (_crossEndTicks > 0) _crossEndTicks--;
            _crossPrev = cross;
            if (_crossEndTicks > 0 && !HandDown && !Other.HandDown)
            {
                _crossEndTicks = 0;
                Strike();
                Other.Strike();
            }
            bool want = (Guarding && !Other.Acting) || (Other.Guarding && !Acting) || cross;
            if (want != _playerMvk.BlockingSent)
            {
                _playerMvk.BlockingSent = want;
                _game.TrancivePacket(new PacketC0BBlocking(want));
            }
        }
    }
}
