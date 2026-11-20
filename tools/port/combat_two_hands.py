"""[ПОВЕДЕНИЕ] Этап 8.3: сила удара по блоку и крит (docs/controls-survival-spec.md).

- ItemBase.DigStrength: сила одного удара по блоку (рука и чужой материал — 1, инструмент по своему материалу —
  множитель из json, ItemAbTool.DigBonus). Сервер копит удары этой силы (BlockHits).
- ItemBase.CritMultiplier: крит удара по сущности в прыжке (не на земле) или на бегу; по умолчанию ×1.5
  (в оригинале в воздухе ×1.1), у топора из json ×2.
- Сервер не принимает удары по сущности одной рукой чаще, чем раз в 4 такта (защита от частых пакетов)."""
from portlib import load, save, rep, add_using

rel = r"Item\ItemBase.cs"
t = load(rel)
t = rep(t, """        public virtual float DigAcceleration(BlockBase block, PlayerBase player)
            => player.CanDestroyedBlock(block) ? 1 : 0;
""", """        public virtual float DigAcceleration(BlockBase block, PlayerBase player)
            => player.CanDestroyedBlock(block) ? 1 : 0;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Сила одного удара по блоку (сервер): рука и чужой материал — 1, инструмент по своему — больше
        /// </summary>
        public virtual float DigStrength(BlockBase block, PlayerBase player) => 1;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Множитель крита удара по сущности в прыжке или на бегу
        /// </summary>
        public virtual float CritMultiplier => 1.5f;
""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """            if (block.Hardness <= 0 || CreativeMode || worldServer.BlockHits.Hit(worldServer, pos, need, 1))""",
        """            float strength = stack != null ? stack.Item.DigStrength(block, this) : 1;
            if (block.Hardness <= 0 || CreativeMode || worldServer.BlockHits.Hit(worldServer, pos, need, strength))""", rel, 1)
t = rep(t, """                        if (PresenceBlocks.IsInLiquid) damage *= .75f;
                        else if (!OnGround) damage *= 1.1f;""", """                        if (PresenceBlocks.IsInLiquid) damage *= .75f;
                        // [ПОВЕДЕНИЕ] Крит в прыжке или на бегу (в оригинале в воздухе ×1.1)
                        else if (!OnGround || IsSprinting()) damage *= stack != null ? stack.Item.CritMultiplier : 1.5f;""", rel, 1)
t = rep(t, """                else if (packet.Action == PacketC03UseEntity.EnumAction.Attack)
                {
                    if (entity.Damage != null)""", """                else if (packet.Action == PacketC03UseEntity.EnumAction.Attack)
                {
                    // [ПОВЕДЕНИЕ] Не чаще раза в 4 такта на руку
                    uint tick = _server.TickCounterGlobal;
                    int hand = packet.Left ? 1 : 0;
                    if (tick - _attackTick[hand] < 4) return;
                    _attackTick[hand] = tick;
                    if (entity.Damage != null)""", rel, 1)
t = rep(t, """        public virtual void PacketUseEntity(PacketC03UseEntity packet)""", """        /// <summary>
        /// Такт сервера последнего удара по сущности правой и левой рукой
        /// </summary>
        private readonly uint[] _attackTick = new uint[] { uint.MaxValue - 100, uint.MaxValue - 100 };

        public virtual void PacketUseEntity(PacketC03UseEntity packet)""", rel, 1)
save(rel, t)

# Двуручность по уточнению пользователя: двуручный предмет занимает вторую руку (она придерживает и сама
# не действует), щит на предплечье (Forearm) кисть не занимает; два двуручных сразу держать нельзя
rel = r"Item\ItemBase.cs"
t = load(rel)
t = rep(t, "                        else if (json.IsKey(Cti.Two)) Two = json.GetBool();",
        """                        else if (json.IsKey(Cti.Two)) Two = json.GetBool();
                        else if (json.IsKey("Forearm")) Forearm = json.GetBool();""", rel, 1)
t = rep(t, "        public bool Two { get; protected set; } = false;", """        public bool Two { get; protected set; } = false;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Носится на предплечье (щит): кисть руки свободна, двуручный предмет в другой руке её не занимает
        /// </summary>
        public bool Forearm { get; protected set; } = false;""", rel, 1)
save(rel, t)

rel = r"Entity\Inventory\InventoryBase.cs"
t = load(rel)
t = rep(t, """        public virtual ItemStack GetCurrentRightItem() => GetCurrentItem();
""", """        public virtual ItemStack GetCurrentRightItem() => GetCurrentItem();

        /// <summary>
        /// [ПОВЕДЕНИЕ] Рука занята: в другой руке двуручный предмет (эта рука его придерживает), если в этой руке
        /// не предмет на предплечье (щит)
        /// </summary>
        public bool HandBusy(bool left)
        {
            ItemStack self = left ? GetCurrentLeftItem() : GetCurrentRightItem();
            ItemStack other = left ? GetCurrentRightItem() : GetCurrentLeftItem();
            return other?.Item.Two == true && self?.Item.Forearm != true;
        }
""", rel, 1)
save(rel, t)

rel = r"Entity\Player\HandManager.cs"
t = load(rel)
t = rep(t, """        /// <summary>
        /// Рука занята: левая при двуручном предмете в правой
        /// </summary>
        protected bool _HandBusy() => _left && _player.Inventory.GetCurrentRightItem()?.Item.Two == true;""",
        """        /// <summary>
        /// Рука занята двуручным предметом другой руки (InventoryBase.HandBusy)
        /// </summary>
        protected bool _HandBusy() => _player.Inventory.HandBusy(_left);""", rel, 1)
save(rel, t)

rel = r"Network\ProcessServerPackets.cs"
t = load(rel)
t = rep(t, """        /// Включить руку действия игрока на время пакета. Левая рука не действует, если в правой двуручный предмет
        /// </summary>
        private static bool _HandBegin(PlayerServer player, bool left)
        {
            if (left && player.Inventory.GetCurrentRightItem()?.Item.Two == true) return false;""", """        /// Включить руку действия игрока на время пакета. Рука не действует, если придерживает двуручный предмет другой
        /// </summary>
        private static bool _HandBegin(PlayerServer player, bool left)
        {
            if (player.Inventory.HandBusy(left)) return false;""", rel, 1)
save(rel, t)

rel = r"Entity\Inventory\InventoryPlayer.cs"
t = load(rel)
t = rep(t, """                    if (packet.Slot < LimitPocket && packet.Slot != _currentIndex) _SwapSlots(packet.Slot, leftIndex);""",
        """                    if (packet.Slot < LimitPocket && packet.Slot != _currentIndex
                        && !(_items[packet.Slot]?.Item.Two == true && _items[_currentIndex]?.Item.Two == true))
                    {
                        _SwapSlots(packet.Slot, leftIndex);
                    }""", rel, 1)
t = rep(t, """        /// <summary>
        /// Поменять стаки двух слотов местами с рассылкой
        /// </summary>""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Два двуручных предмета в руках держать нельзя: предмет левой руки уходит в инвентарь
        /// (что не влезло — выпадает)
        /// </summary>
        public void ServerCheckTwoHands()
        {
            int leftIndex = _pocketCount;
            if (_pocketCount == 0 || leftIndex >= _allCount) return;
            ItemStack left = _items[leftIndex];
            if (left?.Item.Two != true || _items[_currentIndex]?.Item.Two != true) return;
            SetInventorySlotContents(leftIndex, null!);
            if (!AddItemStackToInventory(left) && left.Amount > 0) _playerServer?.DropItem(left, true, false);
        }

        /// <summary>
        /// Поменять стаки двух слотов местами с рассылкой
        /// </summary>""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """            if (Inventory.SetCurrentIndex(packet.SlotId))
            {
                MarkPlayerActive();""", """            if (Inventory.SetCurrentIndex(packet.SlotId))
            {
                (Inventory as InventoryPlayer)?.ServerCheckTwoHands();
                MarkPlayerActive();""", rel, 1)
t = add_using(t, "Mvk.Engine.Entity.Inventory")
save(rel, t)
print("combat_two_hands ok")
