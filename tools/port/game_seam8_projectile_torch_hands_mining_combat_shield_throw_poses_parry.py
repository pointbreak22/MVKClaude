"""[ПОВЕДЕНИЕ] Парирование щитом (parry.py): в окне парирования удар спереди не наносит урона, атакующий отброшен,
щит изнашивается."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\Damage\DamageLiving.cs"
t = load(rel, GAME)
t = rep(t, "                damage = _Block(blocker, damage);", "                damage = _Block(blocker, damage, entityAttack);", rel, 1)
t = rep(t, """        private static float _Block(PlayerServerMvk blocker, float damage)
        {
            InventoryPlayerMvk inventory = blocker.InvPlayer;
            int left = InventoryPlayerMvk.PocketCount;
            int right = inventory.GetCurrentIndex();""", """        private static float _Block(PlayerServerMvk blocker, float damage, EntityLiving attacker)
        {
            InventoryPlayerMvk inventory = blocker.InvPlayer;
            int left = InventoryPlayerMvk.PocketCount;
            int right = inventory.GetCurrentIndex();
            // [ПОВЕДЕНИЕ] Парирование: щит поднят нажатием только что — урона нет, атакующий отброшен
            if (blocker.IsParrying()
                && (inventory.GetStackInSlot(left)?.Item is ItemShield || inventory.GetStackInSlot(right)?.Item is ItemShield))
            {
                ItemWear.DamageSlot(blocker, inventory.GetStackInSlot(left)?.Item is ItemShield ? left : right, 1);
                float dx = attacker.PosX - blocker.PosX, dz = attacker.PosZ - blocker.PosZ;
                float d = System.MathF.Sqrt(dx * dx + dz * dz);
                if (d > .01f) attacker.SetPhysicsImpulse(dx / d * 1.2f, .35f, dz / d * 1.2f);
                return 0;
            }""", rel, 1)
save(rel, t, GAME)
print("game parry ok")
