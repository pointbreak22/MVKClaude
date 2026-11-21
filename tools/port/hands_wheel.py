"""[ПОВЕДЕНИЕ] Колесо для левой руки (ответ пользователя 21.11): колесо по умолчанию листает правую руку, с Shift
или при зажатой кнопке левой руки — левую. Слот кармана, из которого левая рука взяла предмет, открыт HUD
(PlayerClientOwner.LeftScrollSlot), чтобы было видно, что левая перебирает карманы."""
from portlib import load, save, rep

rel = r"Entity\Player\PlayerClientOwner.cs"
t = load(rel)
t = rep(t, """        public bool IsTwoHands() => _handLeft != null && !CreativeMode && !IsSpectator();""", """        public bool IsTwoHands() => _handLeft != null && !CreativeMode && !IsSpectator();

        /// <summary>
        /// [ПОВЕДЕНИЕ] Кнопка левой руки зажата (колесо листает левую руку)
        /// </summary>
        public bool IsLeftHandDown() => _handLeft != null && _handLeft.HandDown;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Слот кармана, из которого левая рука последней взяла предмет колесом (-1 — нет), для HUD
        /// </summary>
        public int LeftScrollSlot => _leftScroll;""", rel, 1)
save(rel, t)

rel = r"Games\GameBase.cs"
t = load(rel)
t = rep(t, """                if (Key.KeyShift && Player.IsTwoHands()) Player.ScrollLeftHand(delta);""",
        """                if ((Key.KeyShift || Player.IsLeftHandDown()) && Player.IsTwoHands()) Player.ScrollLeftHand(delta);""", rel, 1)
save(rel, t)
print("hands_wheel ok")
