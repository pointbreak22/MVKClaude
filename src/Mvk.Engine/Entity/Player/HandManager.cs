using Mvk.Engine.Item.List;
#nullable enable
using System;
using System.Runtime.CompilerServices;
using Mvk.Engine.Games;
using Mvk.Engine.Item;
using Mvk.Engine.Network.Packets.Client;
using Mvk.Core.Util;
using Mvk.Engine.Util;
using Mvk.Engine.World.Block;
using Mvk.Core.Maths;

namespace Mvk.Engine.Entity.Player
{
    /// <summary>
    /// Объект менеджер действий рук игрока. Левой и правой клавишей мыши.
    /// В прошлом было объект ItemInWorldManager, что-то подобное, но там именно очередь ударов
    /// </summary>
    public class HandManager
    {
        /// <summary>
        /// Импульс отскока при атаке на XZ
        /// </summary>
        private static float _impulsAttackXZ = .9f;
        /// <summary>
        /// Импульс отскока при атаке на Y
        /// </summary>
        private static float _impulsAttackY = .18f;

        protected readonly PlayerClientOwner _player;
        /// <summary>
        /// Класс игры
        /// </summary>
        protected readonly GameBase _game;

        /// <summary>
        /// Пауза для активного действия (ЛКМ)
        /// </summary>
        private int _pauseAction;
        /// <summary>
        /// Имеется ли холостой выстрел для активного действия в промежутке, не первый!
        /// </summary>
        private bool _blankShotAction = false;
        /// <summary>
        /// Пауза для вспомогательное действие ПКМ
        /// </summary>
        private int _pauseSecond;
        /// <summary>
        /// Была ли остановка с отменой, флаг для вспомогательного действия
        /// </summary>
        private bool _flagAbort;

        /// <summary>
        /// Счётчик тактов от нажатия вспомогательное действия ПКМ
        /// </summary>
        private int _counterSecond;
        /// <summary>
        /// Активно ли действие ЛКМ
        /// </summary>
        private bool _action;
        /// <summary>
        /// Было ли активно действие ЛКМ в прошлом такте
        /// </summary>
        private bool _actionPrev;
        /// <summary>
        /// Активно ли вспомогательное действие ПКМ
        /// </summary>
        private bool _second;
        /// <summary>
        /// Было ли вспомогательное действие ПКМ в прошлом такте
        /// </summary>
        private bool _secondPrev;

        /// <summary>
        /// Прочность блока, сколько надо ударов для разрушения
        /// </summary>
        private int _hardness;
        /// <summary>
        /// Текущее значение разрушения
        /// </summary>
        private int _destroy;
        /// <summary>
        /// Координаты блока которого разрушаем
        /// </summary>
        private BlockPos _blockPos;
        /// <summary>
        /// Разрушаем ли мы блок
        /// </summary>
        private bool _isBlockDestroy;
        /// <summary>
        /// Флаг запуска вспомогательного предмета
        /// </summary>
        private bool _flagBeginUseSecond;


        public HandManager(GameBase game, PlayerClientOwner player)
        {
            _game = game;
            _player = player;
        }

        public HandManager(GameBase game, PlayerClientOwner player, bool left) : this(game, player) => _left = left;

        #region Две руки (выживание)

        /// <summary>
        /// Порог удержания в тактах: нажатие короче — клик, дольше — удержание
        /// </summary>
        public const int HoldTicks = 5;

        /// <summary>
        /// Чем рука действует предметом
        /// </summary>
        protected enum HandUse
        {
            /// <summary>Клик — удар, добыча или действие предмета; удержание — повтор по паузе</summary>
            Action,
            /// <summary>Клик — поставить (блок, жидкость); удержание — ставить повторно</summary>
            Place,
            /// <summary>Клик — удар или действие; удержание — вспомогательное действие (натяжение, перезарядка, еда)</summary>
            Charge,
            /// <summary>Нажатие — защита (щит) до отпускания</summary>
            Guard,
            /// <summary>Клик — основное действие (блок ставится, остальное бьёт); удержание — замах, отпускание — бросок</summary>
            Throw
        }

        /// <summary>
        /// Код анимации действия руки (наследник подставляет анимацию левой руки)
        /// </summary>
        protected virtual string _AnimationCode(string code) => code;

        /// <summary>
        /// Левая ли это рука
        /// </summary>
        protected readonly bool _left;
        /// <summary>
        /// Кнопка руки зажата
        /// </summary>
        private bool _hand;
        /// <summary>
        /// Кнопка руки была зажата в прошлом такте
        /// </summary>
        private bool _handPrev;
        /// <summary>
        /// Клик ждёт конца отката руки
        /// </summary>
        private bool _clickPending;
        /// <summary>
        /// Идёт удержание
        /// </summary>
        private bool _holding;
        /// <summary>
        /// Тактов от нажатия
        /// </summary>
        private int _handTicks;
        /// <summary>
        /// Чем рука действует в текущем нажатии
        /// </summary>
        private HandUse _use;

        /// <summary>
        /// Кнопка руки нажата или отпущена (выживание)
        /// </summary>
        public void SetHand(bool down) => _hand = down;

        /// <summary>
        /// Сколько тактов после действия руки раскрыт щит другой руки
        /// </summary>
        public const int ActOpenTicks = 8;
        /// <summary>
        /// Тактов, пока рука считается действующей
        /// </summary>
        private int _actTicks;

        /// <summary>
        /// Кнопка руки зажата (и рука не придерживает двуручный предмет другой)
        /// </summary>
        public bool HandDown => _hand && !_HandBusy();
        /// <summary>
        /// Тактов от нажатия (не больше порога удержания), 0 — кнопка отпущена
        /// </summary>
        public int HandTicks => _hand ? _handTicks : 0;
        /// <summary>
        /// Рука держит защиту (щит)
        /// </summary>
        public bool Guarding => _holding && _use == HandUse.Guard;
        /// <summary>
        /// Рука действует (удар, выстрел, установка, натяжение): щит другой руки на это время раскрыт
        /// </summary>
        public bool Acting => _actTicks > 0;
        /// <summary>
        /// Чем рука действует в текущем нажатии
        /// </summary>
        protected HandUse _Use => _use;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Удар руки сразу, без отката (добивающий удар): действие предмета этой руки
        /// </summary>
        public void Strike()
        {
            if (_player.IsSpectator() || _HandBusy()) return;
            _player.Inventory.HandLeft = _left;
            try
            {
                _actTicks = ActOpenTicks;
                _pauseAction = 0;
                _Action(true);
            }
            finally
            {
                _player.Inventory.HandLeft = false;
            }
        }

        /// <summary>
        /// Нажатие руки с защитой (щитом), до начала защиты
        /// </summary>
        protected virtual void _GuardPress() { }
        /// <summary>
        /// Наследник сам ведёт удержание (true — обычное удержание пропускается)
        /// </summary>
        protected virtual bool _HoldOverride() => false;
        /// <summary>
        /// Наследник запрещает клик (true — клик пропускается)
        /// </summary>
        protected virtual bool _ClickOverride() => false;
        /// <summary>
        /// Раскрывает ли удержание щит другой руки
        /// </summary>
        protected virtual bool _HoldOpensGuard() => true;
        /// <summary>
        /// Конец такта руки (выживание)
        /// </summary>
        protected virtual void _AfterHandTick() { }

        /// <summary>
        /// Две руки: в выживании; творческий режим по прежней схеме
        /// </summary>
        private bool _TwoHands() => !_player.CreativeMode;

        /// <summary>
        /// Чем рука действует предметом: блок ставится, остальное бьёт
        /// </summary>
        protected virtual HandUse _HandUse(ItemStack? stack)
            => stack?.Item.Throwable == true ? HandUse.Throw : stack?.Item is ItemBlock ? HandUse.Place : HandUse.Action;

        /// <summary>
        /// Рука занята двуручным предметом другой руки (InventoryBase.HandBusy)
        /// </summary>
        protected bool _HandBusy() => _player.Inventory.HandBusy(_left);

        /// <summary>
        /// Такт руки: клик сразу при нажатии (если откат руки прошёл, иначе как только пройдёт),
        /// с порога HoldTicks — удержание, отпускание завершает удержание
        /// </summary>
        private void _UpdateHand()
        {
            if (_actTicks > 0) _actTicks--;
            bool holdAction = _holding && _use == HandUse.Action;
            bool holdSecond = _holding && _use != HandUse.Action;
            if (!holdAction && _pauseAction > 0) _pauseAction--;
            if (!holdSecond && _pauseSecond > 0) _pauseSecond--;

            bool hand = _hand && !_HandBusy();
            if (hand)
            {
                if (!_handPrev)
                {
                    _handTicks = 0;
                    _holding = false;
                    _use = _HandUse(_player.Inventory.GetCurrentItem());
                    if (_use == HandUse.Guard)
                    {
                        _clickPending = false;
                        _holding = true;
                        _GuardPress();
                        _HoldBegin();
                    }
                    else
                    {
                        _clickPending = true;
                    }
                }
                else if (_holding)
                {
                    _HoldTick();
                }
                else if (++_handTicks >= HoldTicks && !_clickPending)
                {
                    _holding = true;
                    _HoldBegin();
                }
            }
            else if (_handPrev)
            {
                if (_holding) _HoldEnd();
                _holding = false;
                _ActionEnd();
            }

            if (_clickPending && _pauseAction <= 0 && _pauseSecond <= 0)
            {
                _clickPending = false;
                _HandClick();
            }
            _handPrev = hand;
            _AfterHandTick();
        }

        /// <summary>
        /// Клик рукой: поставить, активировать блок пустой рукой, иначе действие предмета (удар, добыча, выстрел)
        /// </summary>
        private void _HandClick()
        {
            if (_player.IsSpectator() || _ClickOverride()) return;
            _actTicks = ActOpenTicks;
            if (_use == HandUse.Place || (_use == HandUse.Throw && _player.Inventory.GetCurrentItem()?.Item is ItemBlock))
            {
                _counterSecond = 0;
                _flagAbort = false;
                _Second(true);
                return;
            }
            MovingObjectPosition moving = _player.MovingObject;
            if (_player.Inventory.GetCurrentItem() == null && !_player.IsKeyShift() && moving.IsBlock()
                && moving.Block.GetBlock().OnBlockActivated(_player, moving.BlockPosition, moving.Side, moving.Facing))
            {
                // Активация блока (дверь, кнопка) кликом пустой руки
                _game.TrancivePacket(new PacketC08PlayerBlockPlacement(moving.BlockPosition,
                    moving.Side, moving.Facing, false, false));
                _pauseAction = 8;
                return;
            }
            if (moving.IsEntity() && moving.Entity is not EntityLiving)
            {
                // [ПОВЕДЕНИЕ] Поднять лежащий предмет этой рукой
                _game.TrancivePacket(new PacketC03UseEntity(moving.Entity!.Id, PacketC03UseEntity.EnumAction.Interact).Hand(_left));
                _pauseAction = 4;
                return;
            }
            _Action(true);
        }

        /// <summary>
        /// Начало удержания
        /// </summary>
        private void _HoldBegin()
        {
            if (_HoldOverride()) return;
            if (_use != HandUse.Guard && _HoldOpensGuard()) _actTicks = ActOpenTicks;
            if (_use == HandUse.Action)
            {
                _Action(false);
            }
            else if (_use == HandUse.Place)
            {
                _Second(false);
            }
            else if (_use == HandUse.Throw)
            {
                _flagAbort = false;
                _game.TrancivePacket(new PacketC0CHandControl(PacketC0CHandControl.EnumAction.ThrowBegin, 0, _left));
                _player.Render.SetAnimationCodeAdd(_AnimationCode("ActionAxe"));
            }
            else
            {
                _counterSecond = 0;
                _flagAbort = false;
                _Second(true);
                _counterSecond++;
            }
        }

        /// <summary>
        /// Такт удержания: повтор действия или вспомогательное действие
        /// </summary>
        private void _HoldTick()
        {
            if (_HoldOverride()) return;
            if (_use != HandUse.Guard && _HoldOpensGuard()) _actTicks = ActOpenTicks;
            if (_use == HandUse.Action)
            {
                _Action(false);
            }
            else if (_use != HandUse.Throw)
            {
                _Second(false);
                _counterSecond++;
            }
        }

        /// <summary>
        /// Конец удержания: выстрел из лука, бросок, конец защиты
        /// </summary>
        private void _HoldEnd()
        {
            if (_use == HandUse.Charge || _use == HandUse.Guard) _SecondEnd();
            else if (_use == HandUse.Throw && !_flagAbort)
            {
                _game.TrancivePacket(new PacketC0CHandControl(PacketC0CHandControl.EnumAction.ThrowEnd, 0, _left));
                _player.Render.SetAnimationCodeAdd(_AnimationCode("ActionAxe"));
            }
        }

        #endregion


        public void SetStop()
        {
            _hand = false;
            _action = false;
            _second = false;
            _flagAbort = true;
        }

        /// <summary>
        /// Изменить состояние активного действия ЛКМ
        /// </summary>
        public void SetAction(bool action) => _action = action;

        /// <summary>
        /// Изменить состояние вспомогательного действия ПКМ
        /// </summary>
        public void SetSecond(bool action) => _second = action;
        
        /// <summary>
        /// В такте: рука действия инвентаря на время такта своя (левая или правая)
        /// </summary>
        public void Update()
        {
            _player.Inventory.HandLeft = _left;
            try
            {
                if (_TwoHands()) _UpdateHand();
                else _UpdateOriginal();
            }
            finally
            {
                _player.Inventory.HandLeft = false;
            }
        }

        /// <summary>
        /// Такт прежней схемы (творческий режим): кнопка «ломать» и «ставить» правой рукой
        /// </summary>
        private void _UpdateOriginal()
        {
            if (_action)
            {
                _Action(!_actionPrev);
            }
            else if (_actionPrev)
            {
                _ActionEnd();
            }
            if (_second)
            {
                if (!_secondPrev)
                {
                    _counterSecond = 0;
                    _flagAbort = false;
                }
                _Second(!_secondPrev);
            }
            else if (_secondPrev)
            {
                _SecondEnd();
                _secondPrev = _second;
            }
            _actionPrev = _action;
            _counterSecond++;
        }

        /// <summary>
        /// Действие анимации правой руки
        /// </summary>
        /// <param name="itemStack">Предмет которым делаю действие</param>
        /// <param name="moving">Выбранный объект</param>
        protected virtual bool _ActionAnimationRight(ItemStack itemStack, 
            MovingObjectPosition moving) => false;

        /// <summary>
        /// Отправить анимацию на сервер
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _SendPlayerAnimation()
        {
            _game.TrancivePacket(new PacketC0APlayerAnimation(
                _player.Render.AnimationCodeAdd,
                PacketC0APlayerAnimation.EnumAction.CodeAdd,
                _player.Render.SpeedAnimationCodeAdd));
        }

        /// <summary>
        /// Такт действия
        /// </summary>
        protected virtual void _Action(bool begin)
        {
            if (_player.IsSpectator()) return;

            if (begin)
            {
                _pauseAction = 0;
                _blankShotAction = false;
            }

            if (_pauseAction == 0)
            {
                MovingObjectPosition moving = _player.MovingObject;
                // Стак предмета в руке
                ItemStack itemStack = _player.Inventory.GetCurrentItem();

                bool isAnimationPrev = false;
                bool isAnimation = false;

                if (!_blankShotAction)
                {
                    isAnimationPrev = true;
                    if (_ActionAnimationRight(itemStack, moving))
                    {
                        // Отправить анимацию
                        _SendPlayerAnimation();
                    }
                }

                if (itemStack != null)
                {
                    ResultHandAction resultAction = itemStack.Item.OnAction(begin, itemStack, _player);
                    if (resultAction.Action == ResultHandAction.ActionType.DestroyBlock)
                    {
                        // Разрушение блока с предметом
                        //Console.WriteLine("BlockDiggingItem");
                        _BlockDigging(moving.BlockPosition, resultAction.Acceleration);
                        isAnimation = true;
                    }
                    else if (resultAction.Action == ResultHandAction.ActionType.AttackEntity)
                    {
                        // Атака на сущность, с предметом
                        _Attack(moving);
                        isAnimation = true;
                    }
                    else if (resultAction.Action == ResultHandAction.ActionType.ItemOnBlock)
                    {
                        // Размещение блока (Взаимодействие предмета на выбранный блок)
                        //Console.WriteLine("BlockPlacement " + resultAction.Pause);
                        _game.TrancivePacket(new PacketC08PlayerBlockPlacement(moving.BlockPosition,
                                moving.Side, moving.Facing, resultAction.Replaceable, true).Hand(_left));
                        isAnimation = true;
                    }
                    else if (resultAction.Action == ResultHandAction.ActionType.UseItem)
                    {
                        // Взаимодействия предмета, ЛКМ без блока
                        //Console.WriteLine("UseItem");
                        _game.TrancivePacket(new PacketC05UseItem().Hand(_left));
                        isAnimation = true;
                    }
                    else
                    {
                        // Нет действий с предметом
                        //Console.WriteLine("Нет действий с предметом");
                        _blankShotAction = true;
                    }
                    _pauseAction = resultAction.Pause;
                    //Console.Write(_pauseAction + " ");
                }
                else if (moving.IsBlock())
                {
                    if (_player.CanDestroyedBlock(moving.Block.GetBlock()))
                    {
                        // Разрушение блока без предмета
                        _BlockDigging(moving.BlockPosition);
                        //Console.WriteLine("BlockDigging");
                        _pauseAction = 6;
                    }
                    else
                    {
                        //Console.WriteLine("Нет действий без предмета [block]");
                        _pauseAction = -1; // Но тут если не begin нет анимации
                        _blankShotAction = true;
                    }
                }
                else if (moving.IsEntity())
                {
                    // Атака на сущность, без предмета
                    _Attack(moving);
                    _pauseAction = -1;
                }
                else
                {
                    // Нет действий без предмета
                    //Console.WriteLine("Нет действий без предмета [null]");
                    _pauseAction = -1;
                    _blankShotAction = true;
                }

                if (!isAnimationPrev && isAnimation)
                {
                    if (_ActionAnimationRight(itemStack!, moving))
                    {
                        // Отправить анимацию
                        _SendPlayerAnimation();
                    }
                }
                //Console.WriteLine(_pauseAction);
            }
            else if (_pauseAction > 0)
            {
                _pauseAction--;
            }

            if (_pauseAction == -1)
            {
                _pauseAction = _blankShotAction ? 0 : 10;
            }
            //Console.WriteLine(_pauseAction);
        }

        /// <summary>
        /// Атака сущности
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _Attack(MovingObjectPosition moving)
        {
            //Console.WriteLine("Attack");
            Vector3 vec = (moving.Entity!.GetPositionVec() - _player.GetPositionVec()).Normalize();
            _game.TrancivePacket(new PacketC03UseEntity(moving.Entity.Id, moving.RayHit.Y,
                vec.X * _impulsAttackXZ, vec.Y * _impulsAttackY, vec.Z * _impulsAttackXZ).Hand(_left));
        }

        /// <summary>
        /// Конец действия
        /// </summary>
        protected virtual void _ActionEnd() { }

        /// <summary>
        /// Такт вспомогательного действия 
        /// </summary>
        protected virtual void _Second(bool begin)
        {
            if (_player.IsSpectator()) return;

            if (begin)
            {
                _pauseSecond = 0;
                _secondPrev = true;
            }

            if (_pauseSecond == 0)
            {
                _flagBeginUseSecond = false;
                MovingObjectPosition moving = _player.MovingObject;

                if (!_player.IsKeyShift() && moving.IsBlock() && moving.Block.GetBlock().OnBlockActivated(
                    _player, moving.BlockPosition, moving.Side, moving.Facing))
                {
                    // Активация блока
                    _game.TrancivePacket(new PacketC08PlayerBlockPlacement(moving.BlockPosition,
                                    moving.Side, moving.Facing, false, false).Hand(_left));
                    _pauseSecond = -1;
                }
                else
                {
                    // Стак предмета в руке
                    ItemStack itemStack = _player.Inventory.GetCurrentItem();

                    if (itemStack != null)
                    {
                        ResultHandSecond resultSecond = itemStack.Item.OnSecond(begin,
                            itemStack, _player, _counterSecond);
                        if (resultSecond.Action == ResultHandSecond.ActionType.BlockPlacement)
                        {
                            // Размещение блока (Взаимодействие предмета на выбранный блок)
                            //Console.WriteLine("BlockPlacement " + resultSecond.Pause);
                            _game.TrancivePacket(new PacketC08PlayerBlockPlacement(
                                resultSecond.IsLiquid ? moving.BlockLiquidPosition : moving.BlockPosition,
                                    moving.Side, moving.Facing, resultSecond.Replaceable, true).Hand(_left));
                            if (_ActionAnimationRight(itemStack, moving))
                            {
                                // Отправить анимацию
                                _SendPlayerAnimation();
                            }
                            _pauseSecond = resultSecond.Pause;
                        }
                        else if (resultSecond.Action == ResultHandSecond.ActionType.InteractEntity)
                        {
                            // Взаимодействие на сущность, с предметом
                            //Console.WriteLine("InteractItem");
                            _game.TrancivePacket(new PacketC03UseEntity(moving.Entity!.Id,
                                PacketC03UseEntity.EnumAction.Interact).Hand(_left));
                            if (_ActionAnimationRight(itemStack, moving))
                            {
                                // Отправить анимацию
                                _SendPlayerAnimation();
                            }
                            _pauseSecond = resultSecond.Pause;
                        }
                        else if (resultSecond.Action == ResultHandSecond.ActionType.UseItem)
                        {
                            // Взаимодействия предмета, ПКМ без блока
                            _flagBeginUseSecond = true;
                             //Console.WriteLine("UseItem");
                            _game.TrancivePacket(new PacketC05UseItem(resultSecond.Number).Hand(_left));
                            if (_ActionAnimationRight(itemStack, moving))
                            {
                                // Отправить анимацию
                                _SendPlayerAnimation();
                            }
                            _pauseSecond = resultSecond.Pause;
                        }
                        else
                        {
                            // Нет вспомогательного действий с предметом
                            //Console.WriteLine("Нет вспомогательного действий с предметом");
                            if (begin) _secondPrev = false;
                        }
                        //Console.WriteLine(_pauseSecond);
                    }
                    else if (moving.IsBlock())
                    {
                        //OnBlockActivated
                        // Клик ПКМ на блок без предмета
                        //Console.WriteLine("Click RightMouse");
                        _pauseSecond = 8;
                    }
                    else if (moving.IsEntity())
                    {
                        // Взаимодействие на сущность, без предмета
                        //Console.WriteLine("Interact");
                        _game.TrancivePacket(new PacketC03UseEntity(moving.Entity!.Id,
                            PacketC03UseEntity.EnumAction.Interact).Hand(_left));
                        _pauseSecond = -1;
                    }
                    else
                    {
                        // Нет вспомогательного действий без предмета
                        //Console.WriteLine("Нет вспомогательного действий без предмета");
                        _pauseSecond = -1;
                    }
                }
            }
            else if (_pauseSecond > 0)
            {
                _pauseSecond--;
            }
        }

        /// <summary>
        /// Конец вспомогательного действия
        /// </summary>
        protected virtual void _SecondEnd()
        {
            if (_player.IsSpectator()) return;
            if (_flagBeginUseSecond)
            {
                ItemStack itemStack = _player.Inventory.GetCurrentItem();
                if (itemStack != null)
                {
                    int number = itemStack.Item.OnSecondEnd(itemStack, _player,
                        _flagAbort, _counterSecond);
                    if (number >= 0)
                    {
                        _game.TrancivePacket(new PacketC05UseItem(number).Hand(_left));
                    }
                }
            }
        }

        /// <summary>
        /// Разрушение блока
        /// </summary>
        private void _BlockDigging(BlockPos blockPos, float power = 1)
        {
            if (_TwoHands())
            {
                _BlockHit(blockPos);
                return;
            }
            bool begin;

            BlockState blockState = _game.World.GetBlockState(blockPos);
            BlockBase block = blockState.GetBlock();
            BlockPos blockPosShift = block.ShiftPosition(blockPos, blockState);

            if (!_isBlockDestroy || !blockPosShift.Equals(_blockPos))
            {
                begin = true;
                _blockPos = blockPosShift;
                _isBlockDestroy = true;
            }
            else
            {
                // Если блок тот же самый, то отменяем началку, удары могут быть кликами
                begin = false;
            }

            if (!blockPosShift.Equals(blockPos))
            {
                blockState = _game.World.GetBlockState(_blockPos);
                block = blockState.GetBlock();
            }

            if (block.Hardness > 0 && !_player.CreativeMode)
            {
                if (begin)
                {
                    _hardness = (int)(block.Hardness * power);
                }

                // Определяем текущее фиксированное состояние разрушения блока, для старта и если кто-то тоже ломает
                int destroyFix = _game.World.GetBlockDestroy(_blockPos);
                // Получить предположительное значение если разрушать
                int destroy = destroyFix * _hardness / 8;

                if (begin || destroy > _destroy)
                {
                    // Если первый клик или значение больше, .т.е. разрушили больше, мы меняем, кто-то помогает
                    _destroy = destroy;
                }
                _destroy++;

                if (_destroy >= _hardness)
                {
                    // Разрушили
                    _BlockDestroyed(block);
                }
                else
                {
                    // Разрушаем, расчитываем значение для фиксации разрушения
                    destroy = _destroy * 8 / _hardness;
                    if (destroy != destroyFix)
                    {
                        _game.World.SetBlockDestroy(_blockPos, (byte)destroy);
                        _game.TrancivePacket(new PacketC07PlayerDigging(_blockPos, (byte)destroy).Hand(_left));
                    }
                    _game.World.PlaySound(block.Material.SampleBreak(_game.World.Rnd),
                        _blockPos.ToVector3Center(), .5f, .9f + _game.World.Rnd.NextFloat() * .2f);

                    _game.World.ParticleDiggingBlock(block, _player.MovingObject.RayHit, 5, true);
                    //if (block.IsParticle)
                    //{
                        //_game.World.SpawnParticle((ushort)EnumParticles.Smoke, 5, _player.MovingObject.RayHit,
                        //    new Vector3(.25f), 1, block.IndexBlock);
                    //}
                }
            }
            else
            {
                // Разрушили
                _BlockDestroyed(block);
            }

            // Инструмент ламаем
            DamageItemTool(1, _left);
        }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Удар по блоку (две руки): прогресс, разрушение и износ считает сервер, звук и частицы сразу
        /// </summary>
        private void _BlockHit(BlockPos blockPos)
        {
            BlockBase block = _game.World.GetBlockState(blockPos).GetBlock();
            _game.TrancivePacket(PacketC07PlayerDigging.Hit(blockPos).Hand(_left));
            _game.World.PlaySound(block.Material.SampleBreak(_game.World.Rnd),
                blockPos.ToVector3Center(), .5f, .9f + _game.World.Rnd.NextFloat() * .2f);
            _game.World.ParticleDiggingBlock(block, _player.MovingObject.RayHit, 5, true);
        }

        /// <summary>
        /// Разрушили выбранный блок
        /// </summary>
        private void _BlockDestroyed(BlockBase block)
        {
            _game.TrancivePacket(new PacketC07PlayerDigging(_blockPos).Hand(_left));
            _game.World.SetBlockToAir(_blockPos);
            _isBlockDestroy = false;
        }

        /// <summary>
        /// Наносим урон предмету (инструменту)
        /// </summary>
        /// <param name="damage">Сила урона</param>
        /// <param name="isLeft">Левая ли рука</param>
        public void DamageItemTool(int damage, bool isLeft = false)
        {
            if (!_player.CreativeMode)
            {
                ItemStack stack = isLeft ? _player.Inventory.GetCurrentLeftItem()
                    : _player.Inventory.GetCurrentItem(); 

                if (stack != null)
                {
                    damage = stack.DamageItemTool(_player, damage, isLeft);
                    if (damage > 0)
                    {
                        _game.TrancivePacket(new PacketC06DamageItem((byte)damage, isLeft));
                    }
                }
            }
        }
    }
}
