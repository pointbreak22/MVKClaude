#nullable enable
using Mvk.Core.Maths;
using Mvk.Engine.Item;
using Mvk.Engine.Network.Packets.Client;
using Mvk.Engine.Util;
using Mvk.Game.Item;
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
        /// <summary>
        /// Рука держала защиту в прошлом такте (нажатие щита — начало парирования)
        /// </summary>
        private bool _guardPrev;
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
        protected override bool _HoldOverride() => _Cross() || _GunGuarded() || _HeavyWaits() || _Thrust();

        /// <summary>
        /// Выпад этого нажатия уже сделан
        /// </summary>
        private bool _thrusted;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Копьё (ThrustSpeed в json) без приседа: удержание — один колющий выпад за нажатие, урон от скорости
        /// игрока считает сервер (PacketC03UseEntity.Thrust)
        /// </summary>
        private bool _Thrust()
        {
            if (_Use != HandUse.Action || !(_player.Inventory.GetCurrentItem()?.Item.ThrustSpeed > 0)) return false;
            if (_thrusted) return true;
            _thrusted = true;
            MovingObjectPosition moving = _player.MovingObject;
            _player.Render.SetAnimationCodeAdd(_AnimationCode("AttackRight"));
            if (moving.IsEntity() && moving.Entity is Mvk.Engine.Entity.EntityLiving)
            {
                Vector3 vec = (moving.Entity!.GetPositionVec() - _player.GetPositionVec()).Normalize();
                _game.TrancivePacket(new PacketC03UseEntity(moving.Entity.Id, moving.RayHit.Y,
                    vec.X * .9f, vec.Y * .18f, vec.Z * .9f).Hand(_left).AsThrust());
            }
            return true;
        }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Вторая рука занята, пока другая действует тяжёлым двуручным (придерживает его)
        /// </summary>
        protected override bool _HandBusy() => Other != null && Other.Acting && _ItemOf(Other)?.Item.Heavy == true;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Тяжёлое двуручное не действует, пока другая рука занята своим действием (её нужно освободить)
        /// </summary>
        private bool _HeavyWaits() => Other != null && Other.Acting && _ItemOf(this)?.Item.Heavy == true;

        /// <summary>
        /// Двуручное оружие (двустволка) при закрытом щите другой руки не стреляет и не перезаряжается
        /// </summary>
        private bool _GunGuarded()
            => Other != null && Other.Guarding && _ItemOf(this)?.Item is ItemGun gun && gun.Two;

        protected override bool _ClickOverride() => _GunGuarded() || _HeavyWaits() || _Ignite();

        /// <summary>
        /// [ПОВЕДЕНИЕ] Клик рукой с факелом или огнивом, когда в другой руке динамит: поджечь его (вместо установки факела)
        /// </summary>
        private bool _Ignite()
        {
            if (Other == null || (_ItemOf(this)?.Item != ItemsRegMvk.Torch && _ItemOf(this)?.Item != ItemsRegMvk.Flint)
                || _ItemOf(Other)?.Item is not ItemDynamite) return false;
            _game.TrancivePacket(new PacketC0CHandControl(PacketC0CHandControl.EnumAction.Ignite, 0, Other._left));
            _player.Render.SetAnimationCodeAdd(_AnimationCode("ActionAxe"));
            return true;
        }

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
            // [ПОВЕДЕНИЕ] Щит на предплечье, кисть свободна: нажатие по двери или кнопке открывает её
            if (!_player.IsKeyShift() && moving.IsBlock() && moving.Block.GetBlock().OnBlockActivated(
                _player, moving.BlockPosition, moving.Side, moving.Facing))
            {
                _game.TrancivePacket(new PacketC08PlayerBlockPlacement(moving.BlockPosition,
                    moving.Side, moving.Facing, false, false));
                return;
            }
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
        /// [ПОВЕДЕНИЕ] Клик рукой активирует дверь и кнопку любым предметом, кроме стреляющего оружия (инструменты
        /// тоже открывают; удержание — добыча). Рука со щитом открывает нажатием (_GuardPress)
        /// </summary>
        protected override bool _CanActivate(ItemStack? stack)
            => stack == null || !(stack.Item is ItemGun || stack.Item is ItemBow);

        /// <summary>
        /// Конец такта левой руки: блок держится, если щит одной руки зажат и другая рука не действует, или «крест»
        /// </summary>
        protected override void _AfterHandTick()
        {
            if (!HandDown) _thrusted = false;
            if (Other != null && !Other.HandDown) Other._thrusted = false;
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
            // [ПОВЕДЕНИЕ] Щит поднят нажатием кнопки (не возврат после действия другой руки) — окно парирования
            bool press = (Guarding && !_guardPrev) || (Other.Guarding && !Other._guardPrev);
            _guardPrev = Guarding;
            Other._guardPrev = Other.Guarding;
            // [ПОВЕДЕНИЕ] Поза руки своего игрока (блок щитом, натяжение лука) — сразу на клиенте, не дожидаясь сервера
            bool pose = want || (_HoldingCharge && _ItemOf(this)?.Item is ItemBow) || (Other._HoldingCharge && _ItemOf(Other)?.Item is ItemBow);
            if (_player.IsHandPose() != pose) _player.SetHandPose(pose);
            if (want != _playerMvk.BlockingSent || (want && press))
            {
                _playerMvk.BlockingSent = want;
                _game.TrancivePacket(new PacketC0BBlocking(want, want && press));
            }
        }
    }
}
