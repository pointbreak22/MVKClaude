"""[ПОВЕДЕНИЕ] Этап 8.4: точки расширения менеджера руки для щита и «креста» (docs/controls-survival-spec.md).

Менеджер руки сообщает наружу своё состояние (кнопка зажата, удержание, защита, «действует» — последние
ActOpenTicks тактов после клика или во время удержания), даёт наследнику перехватить нажатие защиты (_GuardPress,
удар щитом), удержание (_HoldOverride, «крест» мечами), решить, раскрывает ли удержание щит другой руки
(_HoldOpensGuard: перезарядка мушкета не раскрывает), и действовать в конце такта (_AfterHandTick).
Сервер: у предмета сила отталкивания (Knockback в json, щит), удар предметом на предплечье (щитом) только на бегу."""
from portlib import load, save, rep

rel = r"Entity\Player\HandManager.cs"
t = load(rel)
t = rep(t, """        /// <summary>
        /// Кнопка руки нажата или отпущена (выживание)
        /// </summary>
        public void SetHand(bool down) => _hand = down;""", """        /// <summary>
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
        protected virtual void _AfterHandTick() { }""", rel, 1)
t = rep(t, """            bool holdAction = _holding && _use == HandUse.Action;""", """            if (_actTicks > 0) _actTicks--;
            bool holdAction = _holding && _use == HandUse.Action;""", rel, 1)
t = rep(t, """                        _clickPending = false;
                        _holding = true;
                        _HoldBegin();""", """                        _clickPending = false;
                        _holding = true;
                        _GuardPress();
                        _HoldBegin();""", rel, 1)
t = rep(t, """                _clickPending = false;
                _HandClick();
            }
            _handPrev = hand;""", """                _clickPending = false;
                _HandClick();
            }
            _handPrev = hand;
            _AfterHandTick();""", rel, 1)
t = rep(t, """            if (_player.IsSpectator()) return;
            if (_use == HandUse.Place)""", """            if (_player.IsSpectator() || _ClickOverride()) return;
            _actTicks = ActOpenTicks;
            if (_use == HandUse.Place)""", rel, 1)
t = rep(t, """        private void _HoldBegin()
        {""", """        private void _HoldBegin()
        {
            if (_HoldOverride()) return;
            if (_use != HandUse.Guard && _HoldOpensGuard()) _actTicks = ActOpenTicks;""", rel, 1)
t = rep(t, """        private void _HoldTick()
        {""", """        private void _HoldTick()
        {
            if (_HoldOverride()) return;
            if (_use != HandUse.Guard && _HoldOpensGuard()) _actTicks = ActOpenTicks;""", rel, 1)
save(rel, t)

rel = r"Item\ItemBase.cs"
t = load(rel)
t = rep(t, """                        else if (json.IsKey("Forearm")) Forearm = json.GetBool();""",
        """                        else if (json.IsKey("Forearm")) Forearm = json.GetBool();
                        else if (json.IsKey("Knockback")) Knockback = json.GetFloat();""", rel, 1)
t = rep(t, """        public bool Forearm { get; protected set; } = false;""", """        public bool Forearm { get; protected set; } = false;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Множитель отталкивания при ударе предметом (щит бьёт слабо, но отталкивает сильно)
        /// </summary>
        public float Knockback { get; protected set; } = 1;""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """                    // [ПОВЕДЕНИЕ] Не чаще раза в 4 такта на руку
                    uint tick = _server.TickCounterGlobal;""", """                    // [ПОВЕДЕНИЕ] Удар предметом на предплечье (щитом) только с разгона
                    if (Inventory.GetCurrentItem()?.Item.Forearm == true && !IsSprinting()) return;
                    // [ПОВЕДЕНИЕ] Не чаще раза в 4 такта на руку
                    uint tick = _server.TickCounterGlobal;""", rel, 1)
t = rep(t, """                            // Если урон сработал добавляем импулс
                            entity.SetPhysicsImpulse(packet.X, packet.Y, packet.Z);""", """                            // Если урон сработал добавляем импулс; [ПОВЕДЕНИЕ] толчок ограничен, сила по предмету
                            float knockback = stack != null ? stack.Item.Knockback : 1;
                            entity.SetPhysicsImpulse(Math.Clamp(packet.X, -1, 1) * knockback,
                                Math.Clamp(packet.Y, -.3f, .3f) * knockback, Math.Clamp(packet.Z, -1, 1) * knockback);""", rel, 1)
save(rel, t)
print("controls_shield_hands ok")
