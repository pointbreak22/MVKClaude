"""[ПОВЕДЕНИЕ] Ответы пользователя 21.11: Ctrl + Q — выбросить весь стак правой руки, Ctrl + Shift + Q — левой;
G — отдать один предмет правой руки игроку, на которого смотришь (до 4 блоков), Shift + G — из левой.
Перекладывание и проверку расстояния делает сервер."""
from portlib import load, save, rep, add_using

rel = r"Actions\Keyboard.cs"
t = load(rel)
t = rep(t, "        public bool KeyShift { get; private set; }", """        public bool KeyShift { get; private set; }
        /// <summary>
        /// [ПОВЕДЕНИЕ] Зажат Ctrl (модификатор: выбросить весь стак)
        /// </summary>
        public bool KeyCtrl { get; private set; }""", rel, 1)
t = rep(t, "                else if (keys == Keys.ShiftKey) KeyShift = true;", """                else if (keys == Keys.ShiftKey) KeyShift = true;
                else if (keys == Keys.ControlKey) KeyCtrl = true;""", rel, 1)
t = rep(t, "            if (keys == Keys.ShiftKey) KeyShift = false;", """            if (keys == Keys.ShiftKey) KeyShift = false;
            if (keys == Keys.ControlKey) KeyCtrl = false;""", rel, 1)
save(rel, t)

rel = r"Util\Options.cs"
t = load(rel)
t = rep(t, "        public static int ControlDropItem = (int)Keys.Q;", """        public static int ControlDropItem = (int)Keys.Q;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Отдать предмет игроку, на которого смотришь (с Shift — из левой руки)
        /// </summary>
        public static int ControlGiveItem = (int)Keys.G;""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerClientOwner.cs"
t = load(rel)
t = rep(t, """                    _game.TrancivePacket(new PacketC0CHandControl(PacketC0CHandControl.EnumAction.Drop, 0, left));""",
        """                    _game.TrancivePacket(new PacketC0CHandControl(PacketC0CHandControl.EnumAction.Drop,
                        (byte)(_game.Key.KeyCtrl ? 1 : 0), left));""", rel, 1)
t = rep(t, """            else if (Options.ControlDropItem == index)""", """            else if (Options.ControlGiveItem == index)
            {
                if (down && IsTwoHands() && MovingObject.IsEntity() && MovingObject.Entity is PlayerBase target)
                {
                    _game.TrancivePacket(PacketC0CHandControl.Give(target.Id, IsKeyShift()));
                }
            }
            else if (Options.ControlDropItem == index)""", rel, 1)
save(rel, t)

rel = r"Entity\Inventory\InventoryPlayer.cs"
t = load(rel)
t = rep(t, """                    if (stack.Amount > 1)
                    {
                        SetInventorySlotContents(index, stack.Copy().ReduceAmount(1));
                        stack = stack.Copy(1);
                    }""", """                    if (stack.Amount > 1 && packet.Slot == 0)
                    {
                        SetInventorySlotContents(index, stack.Copy().ReduceAmount(1));
                        stack = stack.Copy(1);
                    }""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """            if (packet.Action == PacketC0CHandControl.EnumAction.Ignite)""", """            if (packet.Action == PacketC0CHandControl.EnumAction.Give)
            {
                _GiveItem(packet.Target, packet.Left);
                return;
            }
            if (packet.Action == PacketC0CHandControl.EnumAction.Ignite)""", rel, 1)
t = rep(t, """        private bool _IsReachable(BlockPos blockPos)""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Отдать один предмет руки игроку рядом (до 4 блоков); не влезло — остаётся у себя
        /// </summary>
        private void _GiveItem(int target, bool left)
        {
            if (IsSpectator() || CreativeMode) return;
            if (GetWorldServer().LoadedEntityList.Get(target) is not PlayerServer receiver || receiver == this
                || receiver.IsDead) return;
            float dx = receiver.PosX - PosX, dy = receiver.PosY - PosY, dz = receiver.PosZ - PosZ;
            if (dx * dx + dy * dy + dz * dz > 16) return;
            Inventory.HandLeft = left;
            try
            {
                ItemStack stack = Inventory.GetCurrentItem();
                if (stack == null) return;
                ItemStack one = stack.Copy(1);
                if (receiver.Inventory.AddItemStackToInventory(one)) Inventory.DecrCurrentItem(1);
            }
            finally
            {
                Inventory.HandLeft = false;
            }
        }

        private bool _IsReachable(BlockPos blockPos)""", rel, 1)
save(rel, t)
print("hands_drop_give ok")
