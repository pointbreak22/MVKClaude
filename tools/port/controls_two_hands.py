"""[ПОВЕДЕНИЕ] Этап 8.1: управление двумя руками в выживании (docs/controls-survival-spec.md).

В выживании левая кнопка мыши — левая рука, правая — правая; у каждой руки свой HandManager со своим откатом,
клик (короче HoldTicks) и удержание. Творческий режим идёт прежним путём (кнопки «ломать» и «ставить»).
Пакеты действий (C03, C05, C07, C08) несут признак руки в свободном бите; сервер на время пакета включает
«руку действия» инвентаря (InventoryBase.HandLeft): текущий предмет, слот и расход берутся из левой руки,
поэтому все предметы работают в любой руке без правок. Левая рука не действует, если в правой двуручный предмет.
Новый пакет C0C: F — поменять руки, левый Shift + колесо — взять в левую руку предмет из кармана, Q — выбросить
предмет правой руки, Shift + Q — левой."""
import re
from portlib import load, save, rep, sub, add_using

FLAG_PROP = """
        /// <summary>
        /// Действие левой рукой (выживание, две руки)
        /// </summary>
        public bool Left { get; private set; }
"""


def hand_method(name):
    return f"""
        /// <summary>
        /// Задать руку действия
        /// </summary>
        public {name} Hand(bool left)
        {{
            Left = left;
            return this;
        }}
"""


# ---------- Пакеты: признак руки ----------
rel = r"Network\Packets\Client\PacketC03UseEntity.cs"
t = load(rel)
t = rep(t, "        public float HitY { get; private set; }\n", "        public float HitY { get; private set; }\n" + FLAG_PROP + hand_method("PacketC03UseEntity"), rel, 1)
t = rep(t, "            Action = (EnumAction)stream.Byte();", """            byte action = stream.Byte();
            Left = (action & 0x80) != 0;
            Action = (EnumAction)(action & 0x7F);""", rel, 1)
t = rep(t, "            stream.Byte((byte)Action);", "            stream.Byte((byte)((byte)Action | (Left ? 0x80 : 0)));", rel, 1)
save(rel, t)

rel = r"Network\Packets\Client\PacketC05UseItem.cs"
t = load(rel)
t = rep(t, "        public int Number { get; private set; }\n", "        public int Number { get; private set; }\n" + FLAG_PROP + hand_method("PacketC05UseItem"), rel, 1)
t = rep(t, "            IsSecond = stream.Bool();", """            byte flags = stream.Byte();
            IsSecond = (flags & 1) != 0;
            Left = (flags & 2) != 0;""", rel, 1)
t = rep(t, "            stream.Bool(IsSecond);", "            stream.Byte((byte)((IsSecond ? 1 : 0) | (Left ? 2 : 0)));", rel, 1)
save(rel, t)

rel = r"Network\Packets\Client\PacketC07PlayerDigging.cs"
t = load(rel)
t = rep(t, "        public byte Process { get; private set; }\n", "        public byte Process { get; private set; }\n" + FLAG_PROP + hand_method("PacketC07PlayerDigging"), rel, 1)
t = rep(t, "            Digging = (EnumDigging)stream.Byte();", """            byte digging = stream.Byte();
            Left = (digging & 0x80) != 0;
            Digging = (EnumDigging)(digging & 0x7F);""", rel, 1)
t = rep(t, "            stream.Byte((byte)Digging);", "            stream.Byte((byte)((byte)Digging | (Left ? 0x80 : 0)));", rel, 1)
save(rel, t)

rel = r"Network\Packets\Client\PacketC08PlayerBlockPlacement.cs"
t = load(rel)
t = rep(t, "        public bool Put { get; private set; }\n", "        public bool Put { get; private set; }\n" + FLAG_PROP + hand_method("PacketC08PlayerBlockPlacement"), rel, 1)
t = rep(t, "            Put = stream.Bool();", """            byte flags = stream.Byte();
            Put = (flags & 1) != 0;
            Left = (flags & 2) != 0;""", rel, 1)
t = rep(t, "            stream.Bool(Put);", "            stream.Byte((byte)((Put ? 1 : 0) | (Left ? 2 : 0)));", rel, 1)
save(rel, t)

rel = r"Network\PacketsInit.cs"
t = load(rel)
t = rep(t, "                case 0x0B: return new PacketC0BBlocking();", """                case 0x0B: return new PacketC0BBlocking();
                case 0x0C: return new PacketC0CHandControl();""", rel, 1)
save(rel, t)

# ---------- Инвентарь: рука действия ----------
rel = r"Entity\Inventory\InventoryBase.cs"
t = load(rel)
t = rep(t, """        /// <summary>
        /// Получить текущий стак левой руки
        /// </summary>""", """        /// <summary>
        /// Рука действия: пока true, «текущий» предмет, его слот, замена и расход относятся к левой руке.
        /// Включается на время действия левой рукой (пакет на сервере, такт левой руки на клиенте)
        /// </summary>
        public bool HandLeft { get; set; }

        /// <summary>
        /// Стак правой руки без учёта руки действия
        /// </summary>
        public virtual ItemStack GetCurrentRightItem() => GetCurrentItem();

        /// <summary>
        /// Получить текущий стак левой руки
        /// </summary>""", rel, 1)
save(rel, t)

rel = r"Entity\Inventory\InventoryList.cs"
t = load(rel)
t = rep(t, "        public override byte GetCurrentIndex() => _currentIndex;",
        "        public override byte GetCurrentIndex() => HandLeft && _pocketCount > 0 ? _pocketCount : _currentIndex;", rel, 1)
t = rep(t, "        public override ItemStack GetCurrentItem() => (_pocketCount > 0 ? _items[_currentIndex] : null)!;",
        """        public override ItemStack GetCurrentItem() => (_pocketCount > 0 ? _items[GetCurrentIndex()] : null)!;

        /// <summary>
        /// Стак правой руки без учёта руки действия
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ItemStack GetCurrentRightItem() => (_pocketCount > 0 ? _items[_currentIndex] : null)!;""", rel, 1)
t = rep(t, """        public override void SetCurrentItem(ItemStack stack)
        {
            if (_pocketCount > 0)""", """        public override void SetCurrentItem(ItemStack stack)
        {
            if (HandLeft && _pocketCount > 0)
            {
                // Рука действия левая: слот левой руки с рассылкой
                SetInventorySlotContents(_pocketCount, stack);
                return;
            }
            if (_pocketCount > 0)""", rel, 1)
i = t.index("        public override void DecrCurrentItem(byte amount)")
j = t.index("\n        }\n", i)
body = t[i:j].replace("_items[_currentIndex]", "_items[GetCurrentIndex()]")
t = t[:i] + body + t[j:]
save(rel, t)

rel = r"Entity\Inventory\InventoryPlayer.cs"
t = load(rel)
t = rep(t, "        #region Server\n", """        #region Server

        /// <summary>
        /// [ПОВЕДЕНИЕ] Управление руками (сервер): поменять руки, взять в левую руку предмет из кармана,
        /// выбросить один предмет из руки
        /// </summary>
        public void ServerHandControl(PacketC0CHandControl packet)
        {
            int leftIndex = _pocketCount;
            if (_pocketCount == 0 || leftIndex >= _allCount) return;
            switch (packet.Action)
            {
                case PacketC0CHandControl.EnumAction.Swap:
                    _SwapSlots(_currentIndex, leftIndex);
                    break;
                case PacketC0CHandControl.EnumAction.LeftFromSlot:
                    if (packet.Slot < LimitPocket && packet.Slot != _currentIndex) _SwapSlots(packet.Slot, leftIndex);
                    break;
                case PacketC0CHandControl.EnumAction.Drop:
                    int index = packet.Left ? leftIndex : _currentIndex;
                    ItemStack stack = _items[index];
                    if (stack == null) return;
                    if (stack.Amount > 1)
                    {
                        SetInventorySlotContents(index, stack.Copy().ReduceAmount(1));
                        stack = stack.Copy(1);
                    }
                    else
                    {
                        SetInventorySlotContents(index, null!);
                    }
                    _playerServer?.DropItem(stack, true, true);
                    break;
            }
        }

        /// <summary>
        /// Поменять стаки двух слотов местами с рассылкой
        /// </summary>
        private void _SwapSlots(int a, int b)
        {
            ItemStack stackA = _items[a];
            ItemStack stackB = _items[b];
            SetInventorySlotContents(a, stackB);
            SetInventorySlotContents(b, stackA);
        }
""", rel, 1)
t = add_using(t, "Mvk.Engine.Network.Packets.Client")
save(rel, t)

# ---------- Сервер: рука действия на время пакета ----------
rel = r"Network\ProcessServerPackets.cs"
t = load(rel)
t = rep(t, "                    case 0x0B: _Handle0BBlocking(sp.Side, (PacketC0BBlocking)sp.Packet); break;",
        """                    case 0x0B: _Handle0BBlocking(sp.Side, (PacketC0BBlocking)sp.Packet); break;
                    case 0x0C: _Handle0CHandControl(sp.Side, (PacketC0CHandControl)sp.Packet); break;""", rel, 1)
for method, call in (("_Handle03UseEntity(SocketSide? socketSide, PacketC03UseEntity packet)", "PacketUseEntity"),
                     ("_Handle05UseItem(SocketSide? socketSide, PacketC05UseItem packet)", "PacketUseItem"),
                     ("_Handle07PlayerDigging(SocketSide? socketSide, PacketC07PlayerDigging packet)", "PacketPlayerDigging"),
                     ("_Handle08PlayerBlockPlacement(SocketSide? socketSide, PacketC08PlayerBlockPlacement packet)", "PacketPlayerBlockPlacement")):
    t = rep(t, f"""        private void {method}
            => _server.Players.FindPlayerBySocket(socketSide!)?.{call}(packet);""", f"""        private void {method}
        {{
            PlayerServer? player = _server.Players.FindPlayerBySocket(socketSide!);
            if (player == null || !_HandBegin(player, packet.Left)) return;
            try
            {{
                player.{call}(packet);
            }}
            finally
            {{
                player.Inventory.HandLeft = false;
            }}
        }}""", rel, 1)
t = rep(t, """        /// <summary>
        /// Блок щитом начат или закончен
        /// </summary>""", """        /// <summary>
        /// Включить руку действия игрока на время пакета. Левая рука не действует, если в правой двуручный предмет
        /// </summary>
        private static bool _HandBegin(PlayerServer player, bool left)
        {
            if (left && player.Inventory.GetCurrentRightItem()?.Item.Two == true) return false;
            player.Inventory.HandLeft = left;
            return true;
        }

        /// <summary>
        /// Управление руками: поменять, взять в левую из кармана, выбросить
        /// </summary>
        private void _Handle0CHandControl(SocketSide? socketSide, PacketC0CHandControl packet)
        {
            if (_server.Players.FindPlayerBySocket(socketSide!)?.Inventory is InventoryPlayer inventory)
            {
                inventory.ServerHandControl(packet);
            }
        }

        /// <summary>
        /// Блок щитом начат или закончен
        /// </summary>""", rel, 1)
t = add_using(t, "Mvk.Engine.Entity.Inventory")
save(rel, t)

# ---------- Клиент: менеджер руки ----------
rel = r"Entity\Player\HandManager.cs"
t = load(rel)
# Пакеты действий несут руку
t = sub(t, r"_game\.TrancivePacket\((new PacketC0[3578]\w+\([^;]*?\))\);", r"_game.TrancivePacket(\1.Hand(_left));", rel)
t = rep(t, "            DamageItemTool(1);", "            DamageItemTool(1, _left);", rel, 1)
t = rep(t, """        public void SetStop()
        {
            _action = false;""", """        public void SetStop()
        {
            _hand = false;
            _action = false;""", rel, 1)
t = rep(t, """        /// <summary>
        /// В такте
        /// </summary>
        public void Update()
        {""", """        /// <summary>
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
        {""", rel, 1)
t = rep(t, """        public HandManager(GameBase game, PlayerClientOwner player)
        {
            _game = game;
            _player = player;
        }
""", """        public HandManager(GameBase game, PlayerClientOwner player)
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
            Guard
        }

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
        /// Две руки: в выживании; творческий режим по прежней схеме
        /// </summary>
        private bool _TwoHands() => !_player.CreativeMode;

        /// <summary>
        /// Чем рука действует предметом: блок ставится, остальное бьёт
        /// </summary>
        protected virtual HandUse _HandUse(ItemStack? stack)
            => stack?.Item is ItemBlock ? HandUse.Place : HandUse.Action;

        /// <summary>
        /// Рука занята: левая при двуручном предмете в правой
        /// </summary>
        protected bool _HandBusy() => _left && _player.Inventory.GetCurrentRightItem()?.Item.Two == true;

        /// <summary>
        /// Такт руки: клик сразу при нажатии (если откат руки прошёл, иначе как только пройдёт),
        /// с порога HoldTicks — удержание, отпускание завершает удержание
        /// </summary>
        private void _UpdateHand()
        {
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
        }

        /// <summary>
        /// Клик рукой: поставить, активировать блок пустой рукой, иначе действие предмета (удар, добыча, выстрел)
        /// </summary>
        private void _HandClick()
        {
            if (_player.IsSpectator()) return;
            if (_use == HandUse.Place)
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
            _Action(true);
        }

        /// <summary>
        /// Начало удержания
        /// </summary>
        private void _HoldBegin()
        {
            if (_use == HandUse.Action)
            {
                _Action(false);
            }
            else if (_use == HandUse.Place)
            {
                _Second(false);
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
            if (_use == HandUse.Action)
            {
                _Action(false);
            }
            else
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
        }

        #endregion
""", rel, 1)
t = add_using(t, "Mvk.Engine.Item.List")
save(rel, t)

# ---------- Клиент: игрок ----------
rel = r"Util\Options.cs"
t = load(rel)
t = rep(t, "        public static int ControlHandSecond = (int)MouseButton.Right << 20;", """        public static int ControlHandSecond = (int)MouseButton.Right << 20;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Поменять предметы рук местами (выживание)
        /// </summary>
        public static int ControlSwapHands = (int)Keys.F;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Выбросить предмет из правой руки, с Shift — из левой
        /// </summary>
        public static int ControlDropItem = (int)Keys.Q;""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerClientOwner.cs"
t = load(rel)
t = rep(t, "        protected HandManager _handManager = null!;\n", """        protected HandManager _handManager = null!;
        /// <summary>
        /// Менеджер левой руки (выживание, две руки)
        /// </summary>
        protected HandManager? _handLeft;
        /// <summary>
        /// Последний слот кармана, взятый в левую руку колесом
        /// </summary>
        private int _leftScroll = -1;
""", rel, 1)
t = rep(t, """            else if (Options.ControlHandAction == index)
            {
                _handManager.SetAction(down);
            }
            else if (Options.ControlHandSecond == index)
            {
                _handManager.SetSecond(down);
            }""", """            else if (Options.ControlHandAction == index)
            {
                // [ПОВЕДЕНИЕ] В выживании левая кнопка — левая рука
                if (IsTwoHands()) _handLeft!.SetHand(down);
                else _handManager.SetAction(down);
            }
            else if (Options.ControlHandSecond == index)
            {
                // [ПОВЕДЕНИЕ] В выживании правая кнопка — правая рука
                if (IsTwoHands()) _handManager.SetHand(down);
                else _handManager.SetSecond(down);
            }
            else if (Options.ControlSwapHands == index)
            {
                if (down && IsTwoHands())
                {
                    _handManager.SetStop();
                    _handLeft!.SetStop();
                    _game.TrancivePacket(new PacketC0CHandControl(PacketC0CHandControl.EnumAction.Swap));
                }
            }
            else if (Options.ControlDropItem == index)
            {
                if (down && !IsSpectator())
                {
                    bool left = IsKeyShift() && IsTwoHands();
                    if (left) _handLeft!.SetStop();
                    else _handManager.SetStop();
                    _game.TrancivePacket(new PacketC0CHandControl(PacketC0CHandControl.EnumAction.Drop, 0, left));
                }
            }""", rel, 1)
t = rep(t, """        /// <summary>
        /// Двойной клик пробела
        /// </summary>""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Две руки: выживание (не творческий и не наблюдатель), есть менеджер левой руки
        /// </summary>
        public bool IsTwoHands() => _handLeft != null && !CreativeMode && !IsSpectator();

        /// <summary>
        /// [ПОВЕДЕНИЕ] Левый Shift + колесо: взять в левую руку предмет следующего (или предыдущего) слота кармана,
        /// прежний предмет левой руки ложится на его место. Слот правой руки пропускается
        /// </summary>
        public void ScrollLeftHand(int delta)
        {
            if (Inventory is not InventoryPlayer inventory || inventory.LimitPocket < 2) return;
            int limit = inventory.LimitPocket;
            int right = inventory.GetCurrentIndex();
            int slot = _leftScroll < 0 || _leftScroll >= limit ? right : _leftScroll;
            for (int i = 0; i < limit; i++)
            {
                slot = delta > 0 ? (slot + limit - 1) % limit : (slot + 1) % limit;
                if (slot != right) break;
            }
            if (slot == right) return;
            _leftScroll = slot;
            _handLeft?.SetStop();
            _game.TrancivePacket(new PacketC0CHandControl(PacketC0CHandControl.EnumAction.LeftFromSlot, (byte)slot));
        }

        /// <summary>
        /// Двойной клик пробела
        /// </summary>""", rel, 1)
t = rep(t, "            Movement.SetStop();\n            _handManager.SetStop();\n",
        "            Movement.SetStop();\n            _handManager.SetStop();\n            _handLeft?.SetStop();\n", rel, 1)
t = rep(t, "            _handManager.Update();\n", "            _handManager.Update();\n            _handLeft?.Update();\n", rel, 1)
t = rep(t, "            CreativeMode = packet.CreativeMode;\n", """            if (CreativeMode != packet.CreativeMode)
            {
                // Смена схемы управления рук: остановить обе
                _handManager.SetStop();
                _handLeft?.SetStop();
            }
            CreativeMode = packet.CreativeMode;
""", rel, 1)
t = add_using(t, "Mvk.Engine.Entity.Inventory")
save(rel, t)

rel = r"Games\GameBase.cs"
t = load(rel)
t = rep(t, """                if (delta > 0) Player.Inventory.SlotLess();
                else if (delta < 0) Player.Inventory.SlotMore();""", """                // [ПОВЕДЕНИЕ] Shift + колесо в выживании листает предмет левой руки
                if (Key.KeyShift && Player.IsTwoHands()) Player.ScrollLeftHand(delta);
                else if (delta > 0) Player.Inventory.SlotLess();
                else if (delta < 0) Player.Inventory.SlotMore();""", rel, 1)
save(rel, t)
print("controls_two_hands ok")
