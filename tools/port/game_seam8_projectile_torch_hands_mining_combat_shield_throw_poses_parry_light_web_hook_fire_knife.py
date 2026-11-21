"""[ПОВЕДЕНИЕ] Метательное оружие (просьба пользователя 21.11): нож и копьё (game_new ItemKnife): клик бьёт, удержание
и отпускание — бросок по общей схеме броска (json). Регистрируются последними, в творческой вкладке инструментов.
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
print("game knife ok", [n for n, r in WEAPONS])
