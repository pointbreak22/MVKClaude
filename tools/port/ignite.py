"""[ПОВЕДЕНИЕ] Поджог предмета предметом другой руки (динамит факелом): клиент шлёт C0C Ignite, сервер спрашивает
предмет (ItemBase.OnIgnite) и запоминает фитиль игрока (FuseEnd, рука); по концу фитиля предмет в той руке получает
OnFuseEnd (динамит взрывается в руке), бросок до конца фитиля уносит его с собой (ItemDynamite)."""
from portlib import load, save, rep

rel = r"Item\ItemBase.cs"
t = load(rel)
t = rep(t, """        public bool Throwable => ThrowMax > 0;""", """        public bool Throwable => ThrowMax > 0;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Поджечь предмет (сервер) предметом другой руки lighter; true — подожжён
        /// </summary>
        public virtual bool OnIgnite(ItemStack stack, PlayerServer player, ItemStack lighter, bool left) => false;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Фитиль догорел, предмет ещё в руке (сервер; рука действия — та, что держала)
        /// </summary>
        public virtual void OnFuseEnd(ItemStack stack, PlayerServer player) { }""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """            if (packet.Action == PacketC0CHandControl.EnumAction.ThrowBegin)""", """            if (packet.Action == PacketC0CHandControl.EnumAction.Ignite)
            {
                if (IsSpectator() || Inventory.HandBusy(packet.Left)) return;
                ItemStack target = packet.Left ? Inventory.GetCurrentLeftItem() : Inventory.GetCurrentRightItem();
                ItemStack lighter = packet.Left ? Inventory.GetCurrentRightItem() : Inventory.GetCurrentLeftItem();
                if (target != null && lighter != null) target.Item.OnIgnite(target, this, lighter, packet.Left);
                return;
            }
            if (packet.Action == PacketC0CHandControl.EnumAction.ThrowBegin)""", rel, 1)
t = rep(t, """        private bool _IsReachable(BlockPos blockPos)""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Такт сервера конца фитиля подожжённого предмета в руке (0 — нет) и рука
        /// </summary>
        public uint FuseEnd;
        public bool FuseLeft;

        /// <summary>
        /// Фитиль догорел: предмет в той руке получает OnFuseEnd
        /// </summary>
        private void _UpdateFuse()
        {
            if (FuseEnd == 0 || _server.TickCounterGlobal < FuseEnd) return;
            FuseEnd = 0;
            Inventory.HandLeft = FuseLeft;
            try
            {
                ItemStack stack = Inventory.GetCurrentItem();
                stack?.Item.OnFuseEnd(stack, this);
            }
            finally
            {
                Inventory.HandLeft = false;
            }
        }

        private bool _IsReachable(BlockPos blockPos)""", rel, 1)
t = rep(t, """            // Обновление урона
            Damage?.Update();""", """            // [ПОВЕДЕНИЕ] Фитиль в руке
            _UpdateFuse();

            // Обновление урона
            Damage?.Update();""", rel, 1)
save(rel, t)
print("ignite ok")
