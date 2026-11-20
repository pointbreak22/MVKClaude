"""[ПОВЕДЕНИЕ] Решения по умолчанию (docs/controls-survival-spec.md): Q не выбрасывает в творческом режиме (клиент
и сервер); слот левой руки в окне инвентаря принимает любой предмет (кроме второго двуручного); клик рукой
активирует дверь и кнопку любым предметом, кроме оружия и инструментов (решает мод, HandManager._CanActivate)."""
from portlib import load, save, rep

rel = r"Entity\Player\PlayerClientOwner.cs"
t = load(rel)
t = rep(t, """            else if (Options.ControlDropItem == index)
            {
                if (down && !IsSpectator())""", """            else if (Options.ControlDropItem == index)
            {
                if (down && !IsSpectator() && !CreativeMode)""", rel, 1)
save(rel, t)

rel = r"Entity\Inventory\InventoryPlayer.cs"
t = load(rel)
t = rep(t, """                case PacketC0CHandControl.EnumAction.Drop:
                    int index""", """                case PacketC0CHandControl.EnumAction.Drop:
                    if (_playerServer == null || _playerServer.CreativeMode) return;
                    int index""", rel, 1)
t = rep(t, """            if (slotIn < 100)
            {
                // Для одежды свои правила""", """            if (slotIn < 100)
            {
                // [ПОВЕДЕНИЕ] Левая рука: любой предмет, кроме второго двуручного
                if (slotIn == _pocketCount && _pocketCount > 0)
                {
                    return stack == null || stack.Item.Forearm || stack.Item.Two != true
                        || _items[_currentIndex]?.Item.Two != true;
                }
                // Для одежды свои правила""", rel, 1)
save(rel, t)

rel = r"Entity\Player\HandManager.cs"
t = load(rel)
t = rep(t, """            if (_player.Inventory.GetCurrentItem() == null && !_player.IsKeyShift() && moving.IsBlock()""",
        """            if (_CanActivate(_player.Inventory.GetCurrentItem()) && !_player.IsKeyShift() && moving.IsBlock()""", rel, 1)
t = rep(t, """        /// <summary>
        /// Нажатие руки с защитой (щитом), до начала защиты
        /// </summary>""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Активирует ли клик рукой с этим предметом блок (дверь, кнопку); по умолчанию пустая рука
        /// </summary>
        protected virtual bool _CanActivate(ItemStack? stack) => stack == null;

        /// <summary>
        /// Нажатие руки с защитой (щитом), до начала защиты
        /// </summary>""", rel, 1)
save(rel, t)
print("hands_defaults ok")
