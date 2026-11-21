"""[ПОВЕДЕНИЕ] Добыча любым предметом (dig_any.py): инструменты Малювек по чужому материалу бьют как рука
(медленно), а не отказываются; свой материал — ×5 (DigBonus по умолчанию 5)."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

for name in ("ItemPickaxe", "ItemShovel", "ItemAxe", "ItemAxeStone"):
    rel = rf"Item\List\Tool\{name}.cs"
    t = load(rel, GAME)
    t = rep(t, "? Acceleration : 0;", "? Acceleration : (player.CanHitBlock(block) ? 1 : 0);", rel, 1)
    save(rel, t, GAME)

rel = r"Item\List\Tool\ItemAbTool.cs"
t = load(rel, GAME)
t = rep(t, "        public float DigBonus { get; protected set; } = 3;", "        public float DigBonus { get; protected set; } = 5;", rel, 1)
t = t.replace("Сила удара по своему материалу (json DigBonus)", "Сила удара по своему материалу (json DigBonus, по умолчанию ×5 к руке)")
save(rel, t, GAME)

# [ПОВЕДЕНИЕ] Пока рука держит натяжение, перезарядку или замах, анимация удара не играет (hands_click_release.py)
rel = r"Entity\Player\HandManagerMvk.cs"
t = load(rel, GAME)
t = rep(t, """        protected override bool _ActionAnimationRight(ItemStack itemStack, 
            MovingObjectPosition moving)
        {""", """        protected override bool _ActionAnimationRight(ItemStack itemStack, 
            MovingObjectPosition moving)
        {
            if (_HoldingCharge) return false;""", rel, 1)
save(rel, t, GAME)
print("game dig ok")
