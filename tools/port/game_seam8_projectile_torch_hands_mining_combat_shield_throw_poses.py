"""[ПОВЕДЕНИЕ] Этап 8.8: анимации левой руки. Действия руки в HandManagerMvk идут через _AnimationCode: левая рука
в выживании играет зеркальные анимации (AttackLeft, ActionAxeLeft в Humanoid.bbmodel и Player.json)."""
import os, re
from portlib import load, save, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\Player\HandManagerMvk.cs"
t = load(rel, GAME)
t, n = re.subn(r"_player\.Render\.SetAnimationCodeAdd\(([^;]*?)\);", r"_player.Render.SetAnimationCodeAdd(_AnimationCode(\1));", t)
if n < 4:
    raise SystemExit(f"FAIL {rel}: анимаций {n}")
save(rel, t, GAME)
print("game poses ok", n)
