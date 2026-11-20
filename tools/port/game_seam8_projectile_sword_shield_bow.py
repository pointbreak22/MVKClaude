"""[ПОВЕДЕНИЕ] Лук и стрелы (в оригинале нет): ItemBow (двуручный, натяжение удержанием), ItemArrow (боеприпас),
сущность «Arrow» (EntityArrowMvk: застревает в блоке). Выстрел, расход стрел, попадание и износ лука — сервер."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Item\ItemsRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public static ItemShield Shield { get; private set; }""", """        public static ItemShield Shield { get; private set; }
        /// <summary>
        /// Лук
        /// </summary>
        public static ItemBow Bow { get; private set; }
        /// <summary>
        /// Стрела
        /// </summary>
        public static ItemArrow Arrow { get; private set; }""", rel, 1)
t = rep(t, """            ItemsReg.RegisterItemClass("Shield", Shield = new ItemShield(), _Tools);""", """            ItemsReg.RegisterItemClass("Shield", Shield = new ItemShield(), _Tools);
            ItemsReg.RegisterItemClass("Bow", Bow = new ItemBow(), _Tools);
            ItemsReg.RegisterItemClass("Arrow", Arrow = new ItemArrow(), _Tools);""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsCreative.cs"
t = load(rel, GAME)
t = rep(t, """            tabTools.Add(new ItemStack(ItemsRegMvk.Shield));""", """            tabTools.Add(new ItemStack(ItemsRegMvk.Shield));
            tabTools.Add(new ItemStack(ItemsRegMvk.Bow));
            tabTools.Add(new ItemStack(ItemsRegMvk.Arrow, 64));""", rel, 1)
save(rel, t, GAME)

rel = r"Entity\EntitiesRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """            EntitiesReg.RegisterEntityClass(EntityDynamiteMvk.AliasDynamite, typeof(EntityDynamiteMvk), true);""",
"""            EntitiesReg.RegisterEntityClass(EntityDynamiteMvk.AliasDynamite, typeof(EntityDynamiteMvk), true);
            EntitiesReg.RegisterEntityClass(EntityArrowMvk.AliasArrow, typeof(EntityArrowMvk), true);""", rel, 1)
save(rel, t, GAME)
print("game_seam8_projectile_sword_shield_bow ok")
