"""[ПОВЕДЕНИЕ] Меч и мотыга (в оригинале нет): ItemSword (урон из json, износ при ударе на сервере), ItemHoe (копает
рыхлое, второе действие снимает дёрн на сервере). Предметы регистрируются после прежних инструментов, спрайты свои."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Item\ItemsRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public static ItemAbTool PickaxeRed { get; private set; }""", """        public static ItemAbTool PickaxeRed { get; private set; }
        /// <summary>
        /// Железный меч
        /// </summary>
        public static ItemAbTool SwordIron { get; private set; }
        /// <summary>
        /// Железная мотыга
        /// </summary>
        public static ItemAbTool HoeIron { get; private set; }""", rel, 1)
t = rep(t, """            ItemsReg.RegisterItemClass("PickaxeRed", PickaxeRed = new ItemPickaxe(), _Tools);""", """            ItemsReg.RegisterItemClass("PickaxeRed", PickaxeRed = new ItemPickaxe(), _Tools);
            ItemsReg.RegisterItemClass("SwordIron", SwordIron = new ItemSword(), _Tools);
            ItemsReg.RegisterItemClass("HoeIron", HoeIron = new ItemHoe(), _Tools);""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsCreative.cs"
t = load(rel, GAME)
t = rep(t, """            tabTools.Add(new ItemStack(ItemsRegMvk.PickaxeRed));""", """            tabTools.Add(new ItemStack(ItemsRegMvk.PickaxeRed));
            tabTools.Add(new ItemStack(ItemsRegMvk.SwordIron));
            tabTools.Add(new ItemStack(ItemsRegMvk.HoeIron));""", rel, 1)
save(rel, t, GAME)
print("game_seam8_projectile_sword ok")
