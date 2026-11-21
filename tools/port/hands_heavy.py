"""[ПОВЕДЕНИЕ] Двуручность по ответу пользователя 21.11 (docs/controls-survival-spec.md):
- «Тяжёлый» предмет (json Heavy: двустволка, позже молот, двойной сундук) — два тяжёлых в руках держать нельзя
  (вместо прежнего «два двуручных»): лук и двустволку держать вместе можно.
- Вторая рука занята только во время действия тяжёлым двуручным другой руки (решает клиент по анимации,
  HandManager._HandBusy у мода); в остальное время она свободна, держит и использует свой предмет.
  Сервер руку статически не запрещает (InventoryBase.HandBusy).
- Тяжёлое в руке: ход медленнее (×0.8), бег не ускоряет."""
from portlib import load, save, rep

rel = r"Item\ItemBase.cs"
t = load(rel)
t = rep(t, """                        else if (json.IsKey("Forearm")) Forearm = json.GetBool();""",
        """                        else if (json.IsKey("Forearm")) Forearm = json.GetBool();
                        else if (json.IsKey("Heavy")) Heavy = json.GetBool();""", rel, 1)
t = rep(t, """        public bool Forearm { get; protected set; } = false;""", """        public bool Forearm { get; protected set; } = false;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Тяжёлый двуручный (json Heavy): два в руках держать нельзя, при действии им вторая рука занята,
        /// ход медленнее
        /// </summary>
        public bool Heavy { get; protected set; } = false;""", rel, 1)
save(rel, t)

rel = r"Entity\Inventory\InventoryBase.cs"
t = load(rel)
t = rep(t, """        /// <summary>
        /// [ПОВЕДЕНИЕ] Рука занята: в другой руке двуручный предмет (эта рука его придерживает), если в этой руке
        /// не предмет на предплечье (щит)
        /// </summary>
        public bool HandBusy(bool left)
        {
            ItemStack self = left ? GetCurrentLeftItem() : GetCurrentRightItem();
            ItemStack other = left ? GetCurrentRightItem() : GetCurrentLeftItem();
            return other?.Item.Two == true && self?.Item.Forearm != true;
        }""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Рука занята статически: никогда. Вторая рука занята только на время действия тяжёлым двуручным
        /// другой руки, это решает клиент по анимации (HandManager._HandBusy у мода)
        /// </summary>
        public bool HandBusy(bool left) => false;

        /// <summary>
        /// [ПОВЕДЕНИЕ] В руках тяжёлый предмет (ход медленнее)
        /// </summary>
        public bool IsHeavyLoad() => GetCurrentRightItem()?.Item.Heavy == true || GetCurrentLeftItem()?.Item.Heavy == true;""", rel, 1)
save(rel, t)

rel = r"Entity\Inventory\InventoryPlayer.cs"
t = load(rel)
t = rep(t, "&& !(_items[packet.Slot]?.Item.Two == true && _items[_currentIndex]?.Item.Two == true))",
        "&& !(_items[packet.Slot]?.Item.Heavy == true && _items[_currentIndex]?.Item.Heavy == true))", rel, 1)
t = rep(t, "if (left?.Item.Two != true || _items[_currentIndex]?.Item.Two != true) return;",
        "if (left?.Item.Heavy != true || _items[_currentIndex]?.Item.Heavy != true) return;", rel, 1)
t = rep(t, """                    return stack == null || stack.Item.Forearm || stack.Item.Two != true
                        || _items[_currentIndex]?.Item.Two != true;""", """                    return stack == null || stack.Item.Heavy != true || _items[_currentIndex]?.Item.Heavy != true;""", rel, 1)
t = t.replace("Два двуручных предмета в руках держать нельзя", "Два тяжёлых предмета в руках держать нельзя")
save(rel, t)

rel = r"Entity\Player\HandManager.cs"
t = load(rel)
t = rep(t, "        protected bool _HandBusy() => _player.Inventory.HandBusy(_left);",
        "        protected virtual bool _HandBusy() => _player.Inventory.HandBusy(_left);", rel, 1)
save(rel, t)

rel = r"Entity\Physics\PhysicsGroundLiving.cs"
t = load(rel)
t = rep(t, """            bool isSprinting = Movement.Sprinting && Movement.Forward && !isSneakingPrev;""",
        """            bool isSprinting = Movement.Sprinting && Movement.Forward && !isSneakingPrev && !_HeavyLoad();""", rel, 1)
t = rep(t, """            float speed = Entity.Speed * Movement.MoveSpeed;
            if (Movement.Sprinting)
            {
                // Если ускорение
                speed += speed * Cp.SprintSpeed;
            }""", """            float speed = Entity.Speed * Movement.MoveSpeed;
            bool heavy = _HeavyLoad();
            if (Movement.Sprinting && !heavy)
            {
                // Если ускорение
                speed += speed * Cp.SprintSpeed;
            }
            // [ПОВЕДЕНИЕ] Тяжёлое в руках: ход медленнее
            if (heavy) speed *= .8f;""", rel, 1)
t = rep(t, """            float acceleration = Cp.AirborneAcceleration;
            if (Movement.Sprinting)""", """            float acceleration = Cp.AirborneAcceleration;
            if (Movement.Sprinting && !_HeavyLoad())""", rel, 1)
t = rep(t, """        protected override float _LivingUpdateSpeed()""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] В руках тяжёлый предмет
        /// </summary>
        private bool _HeavyLoad() => _entityLiving.Inventory != null && _entityLiving.Inventory.IsHeavyLoad();

        protected override float _LivingUpdateSpeed()""", rel, 1)
save(rel, t)
print("hands_heavy ok")
