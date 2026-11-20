"""[ПОВЕДЕНИЕ] Единая схема броска в выживании (docs/controls-survival-spec.md, пожелание пользователя 20.11).

Бросаемый предмет описывается json: ThrowMax (сила полного замаха; 0 — не бросается), ThrowMin (сила без замаха),
ThrowCharge (тактов до полного замаха), ThrowEntity (псевдоним снаряда, по умолчанию простой снаряд-предмет).
Рука: клик — основное действие предмета (блок ставится, остальное бьёт), удержание — замах, отпускание — бросок.
Клиент сообщает начало и конец замаха (C0C ThrowBegin, ThrowEnd); время замаха по своим тактам, силу, снаряд
и расход предмета считает сервер (ItemBase.OnThrow, по умолчанию EntityProjectile.Throw). Творческий режим
бросает прежним путём предметов."""
from portlib import load, save, rep, add_using

rel = r"Network\Packets\Client\PacketC0CHandControl.cs"
t = load(rel, r"E:\Projects\Games\MvkClaude\tools\port\new")
assert "ThrowBegin" in t, "PacketC0CHandControl: нет ThrowBegin в new/"

rel = r"Item\ItemBase.cs"
t = load(rel)
t = rep(t, """                        else if (json.IsKey("Knockback")) Knockback = json.GetFloat();""",
        """                        else if (json.IsKey("Knockback")) Knockback = json.GetFloat();
                        else if (json.IsKey("ThrowMin")) ThrowMin = json.GetFloat();
                        else if (json.IsKey("ThrowMax")) ThrowMax = json.GetFloat();
                        else if (json.IsKey("ThrowCharge")) ThrowCharge = Math.Max(1, json.GetInt());
                        else if (json.IsKey("ThrowEntity")) ThrowEntity = json.GetString();""", rel, 1)
t = rep(t, """        public float Knockback { get; protected set; } = 1;""", """        public float Knockback { get; protected set; } = 1;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Сила броска без замаха (json ThrowMin)
        /// </summary>
        public float ThrowMin { get; protected set; } = 0;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Сила броска с полным замахом (json ThrowMax); 0 — предмет не бросается
        /// </summary>
        public float ThrowMax { get; protected set; } = 0;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Тактов до полного замаха (json ThrowCharge)
        /// </summary>
        public int ThrowCharge { get; protected set; } = 20;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Псевдоним снаряда (json ThrowEntity), пусто — простой снаряд-предмет
        /// </summary>
        public string ThrowEntity { get; protected set; } = "";
        /// <summary>
        /// [ПОВЕДЕНИЕ] Предмет можно бросить с замахом
        /// </summary>
        public bool Throwable => ThrowMax > 0;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Бросок одного предмета силой power (сервер); true — брошен (расход делает вызывающий)
        /// </summary>
        public virtual bool OnThrow(ItemStack stack, PlayerServer player, float power)
            => EntityProjectile.Throw(player, stack.Copy(1), power,
                ThrowEntity != "" ? ThrowEntity : EntityProjectile.Alias) != null;""", rel, 1)
t = add_using(t, "Mvk.Engine.Entity.List")
save(rel, t)

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """        private bool _IsReachable(BlockPos blockPos)""", """        /// <summary>
        /// Такт сервера начала замаха правой и левой рукой (0 — замаха нет)
        /// </summary>
        private readonly uint[] _throwStart = new uint[2];

        /// <summary>
        /// [ПОВЕДЕНИЕ] Управление руками: перекладывание и выброс — инвентарь, замах и бросок — здесь
        /// </summary>
        public void PacketHandControl(PacketC0CHandControl packet)
        {
            int hand = packet.Left ? 1 : 0;
            uint tick = _server.TickCounterGlobal;
            if (packet.Action == PacketC0CHandControl.EnumAction.ThrowBegin)
            {
                _throwStart[hand] = tick == 0 ? 1 : tick;
                return;
            }
            if (packet.Action == PacketC0CHandControl.EnumAction.ThrowEnd)
            {
                uint start = _throwStart[hand];
                _throwStart[hand] = 0;
                if (start == 0 || IsSpectator() || Inventory.HandBusy(packet.Left)) return;
                Inventory.HandLeft = packet.Left;
                try
                {
                    ItemStack stack = Inventory.GetCurrentItem();
                    if (stack == null || !stack.Item.Throwable) return;
                    float charge = Math.Min(1f, (tick - start) / (float)stack.Item.ThrowCharge);
                    float power = stack.Item.ThrowMin + (stack.Item.ThrowMax - stack.Item.ThrowMin) * charge;
                    if (stack.Item.OnThrow(stack, this, power) && !CreativeMode) Inventory.DecrCurrentItem(1);
                }
                finally
                {
                    Inventory.HandLeft = false;
                }
                return;
            }
            (Inventory as InventoryPlayer)?.ServerHandControl(packet);
        }

        private bool _IsReachable(BlockPos blockPos)""", rel, 1)
save(rel, t)

rel = r"Network\ProcessServerPackets.cs"
t = load(rel)
t = rep(t, """            if (_server.Players.FindPlayerBySocket(socketSide!)?.Inventory is InventoryPlayer inventory)
            {
                inventory.ServerHandControl(packet);
            }""", """            _server.Players.FindPlayerBySocket(socketSide!)?.PacketHandControl(packet);""", rel, 1)
save(rel, t)

rel = r"Entity\Player\HandManager.cs"
t = load(rel)
t = rep(t, """            /// <summary>Нажатие — защита (щит) до отпускания</summary>
            Guard
        }""", """            /// <summary>Нажатие — защита (щит) до отпускания</summary>
            Guard,
            /// <summary>Клик — основное действие (блок ставится, остальное бьёт); удержание — замах, отпускание — бросок</summary>
            Throw
        }""", rel, 1)
t = rep(t, """        protected virtual HandUse _HandUse(ItemStack? stack)
            => stack?.Item is ItemBlock ? HandUse.Place : HandUse.Action;""", """        protected virtual HandUse _HandUse(ItemStack? stack)
            => stack?.Item.Throwable == true ? HandUse.Throw : stack?.Item is ItemBlock ? HandUse.Place : HandUse.Action;""", rel, 1)
# Клик броском: блок ставится, остальное — действие
t = rep(t, """            _actTicks = ActOpenTicks;
            if (_use == HandUse.Place)""", """            _actTicks = ActOpenTicks;
            if (_use == HandUse.Place || (_use == HandUse.Throw && _player.Inventory.GetCurrentItem()?.Item is ItemBlock))""", rel, 1)
# Удержание броска: замах
t = rep(t, """            if (_use == HandUse.Action)
            {
                _Action(false);
            }
            else if (_use == HandUse.Place)
            {
                _Second(false);
            }
            else
            {
                _counterSecond = 0;""", """            if (_use == HandUse.Action)
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
                _player.Render.SetAnimationCodeAdd("ActionAxe");
            }
            else
            {
                _counterSecond = 0;""", rel, 1)
t = rep(t, """            if (_use == HandUse.Action)
            {
                _Action(false);
            }
            else
            {
                _Second(false);
                _counterSecond++;
            }""", """            if (_use == HandUse.Action)
            {
                _Action(false);
            }
            else if (_use != HandUse.Throw)
            {
                _Second(false);
                _counterSecond++;
            }""", rel, 1)
t = rep(t, """            if (_use == HandUse.Charge || _use == HandUse.Guard) _SecondEnd();""", """            if (_use == HandUse.Charge || _use == HandUse.Guard) _SecondEnd();
            else if (_use == HandUse.Throw && !_flagAbort)
            {
                _game.TrancivePacket(new PacketC0CHandControl(PacketC0CHandControl.EnumAction.ThrowEnd, 0, _left));
                _player.Render.SetAnimationCodeAdd("ActionAxe");
            }""", rel, 1)
save(rel, t)
print("throw_hands ok")
