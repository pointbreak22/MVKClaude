"""[ПОВЕДЕНИЕ] Единая схема броска (throw_hands.py): в выживании бросаемые предметы (json ThrowMax) — клик основное
действие, удержание замах, отпускание бросок. Брошенный факел (EntityTorchMvk) ставит факел-блок, где упал.
Снаряд регистрируется после прежних снарядов."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\EntitiesRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """            EntitiesReg.RegisterEntityClass(EntityBulletMvk.AliasBullet, typeof(EntityBulletMvk), true);""",
        """            EntitiesReg.RegisterEntityClass(EntityBulletMvk.AliasBullet, typeof(EntityBulletMvk), true);
            EntitiesReg.RegisterEntityClass(EntityTorchMvk.AliasTorch, typeof(EntityTorchMvk), true);""", rel, 1)
save(rel, t, GAME)

rel = r"Entity\Player\HandManagerMvk.cs"
t = load(rel, GAME)
t = rep(t, """            if (item is ItemShield) return HandUse.Guard;""", """            if (item is ItemShield) return HandUse.Guard;
            if (item != null && item.Throwable) return HandUse.Throw;""", rel, 1)
save(rel, t, GAME)
print("game throw ok")
