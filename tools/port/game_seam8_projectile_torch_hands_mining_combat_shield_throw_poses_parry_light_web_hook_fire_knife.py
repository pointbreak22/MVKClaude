"""[ПОВЕДЕНИЕ] Метательное оружие (просьба пользователя 21.11): нож и копьё (game_new ItemKnife). Нож: клик бьёт,
удержание и отпускание — бросок (json). Копьё: клик бьёт, удержание — колющий выпад (урон от скорости, json ThrustSpeed),
присед + удержание и отпускание — бросок. Регистрируются последними, в творческой вкладке инструментов.
Значки свои (assets/port-assets.txt)."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

WEAPONS = [("Knife", "Нож")]
if os.path.exists(os.path.join(ROOT, r"assets\Mvk2Items\Tools\Spear.json")):
    WEAPONS.append(("Spear", "Копьё"))

rel = r"Item\ItemsRegMvk.cs"
t = load(rel, GAME)
props = "".join(f"""
        /// <summary>
        /// {ru}
        /// </summary>
        public static ItemAbTool {name} {{ get; private set; }}""" for name, ru in WEAPONS)
t = rep(t, """        public static ItemBase Flint { get; private set; }""", """        public static ItemBase Flint { get; private set; }""" + props, rel, 1)
regs = "".join(f"""
            ItemsReg.RegisterItemClass("{name}", {name} = new ItemKnife(), _Tools);""" for name, ru in WEAPONS)
t = rep(t, """            ItemsReg.RegisterItemClass("Flint", Flint = new ItemFlint(), _ItemsForBlock);""",
        """            ItemsReg.RegisterItemClass("Flint", Flint = new ItemFlint(), _ItemsForBlock);""" + regs, rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsCreative.cs"
t = load(rel, GAME)
tabs = "".join(f"""
            tabTools.Add(new ItemStack(ItemsRegMvk.{name}));""" for name, ru in WEAPONS)
t = rep(t, """            tabTools.Add(new ItemStack(ItemsRegMvk.SwordIron));""", """            tabTools.Add(new ItemStack(ItemsRegMvk.SwordIron));""" + tabs, rel, 1)
save(rel, t, GAME)

# Копьё: бросок только с приседом (Alt), без приседа удержание — колющий выпад (HandManagerMvkHands._Thrust)
rel = r"Entity\Player\HandManagerMvk.cs"
t = load(rel, GAME)
t = rep(t, """            if (item != null && item.Throwable) return HandUse.Throw;""",
        """            if (item != null && item.Throwable) return item.ThrustSpeed <= 0 || _player.IsSneaking() ? HandUse.Throw : HandUse.Action;""", rel, 1)
save(rel, t, GAME)
print("game knife ok", [n for n, r in WEAPONS])
