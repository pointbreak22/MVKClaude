"""[ПОВЕДЕНИЕ] Огнестрел (в оригинале нет): одноручный мушкет (длинный кремнёвый пистолет, 1 ствол, пуля + порох)
и двуручная вертикальная двустволка (2 ствола подряд, пуля + гильза), общие пули калибра 12 (ItemAmmoPart: Bullet,
Powder, Shell), сущность «Bullet» (EntityBulletMvk). Выстрел, перезарядка, расход, отдача, износ — сервер (ItemGun).
Щит при двуручном оружии в правой руке не блокирует (проверка и на сервере)."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

ITEMS = [("Bullet", "ItemAmmoPart", "Пуля"), ("Powder", "ItemAmmoPart", "Порох"), ("Shell", "ItemAmmoPart", "Гильза"),
         ("Musket", "ItemGun", "Мушкет"), ("DoubleBarrel", "ItemGun", "Двустволка")]

rel = r"Item\ItemsRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public static ItemArrow Arrow { get; private set; }""", """        public static ItemArrow Arrow { get; private set; }
""" + "".join(f"""        /// <summary>
        /// {ru}
        /// </summary>
        public static {cls} {name} {{ get; private set; }}
""" for name, cls, ru in ITEMS).rstrip("\n"), rel, 1)
t = rep(t, """            ItemsReg.RegisterItemClass("Arrow", Arrow = new ItemArrow(), _Tools);""", """            ItemsReg.RegisterItemClass("Arrow", Arrow = new ItemArrow(), _Tools);
""" + "\n".join(f"""            ItemsReg.RegisterItemClass("{name}", {name} = new {cls}(), _Tools);""" for name, cls, _ in ITEMS), rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsCreative.cs"
t = load(rel, GAME)
t = rep(t, """            tabTools.Add(new ItemStack(ItemsRegMvk.Arrow, 64));""", """            tabTools.Add(new ItemStack(ItemsRegMvk.Arrow, 64));
            tabTools.Add(new ItemStack(ItemsRegMvk.Musket));
            tabTools.Add(new ItemStack(ItemsRegMvk.DoubleBarrel));
            tabTools.Add(new ItemStack(ItemsRegMvk.Bullet, 32));
            tabTools.Add(new ItemStack(ItemsRegMvk.Powder, 32));
            tabTools.Add(new ItemStack(ItemsRegMvk.Shell, 32));""", rel, 1)
save(rel, t, GAME)

rel = r"Entity\EntitiesRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """            EntitiesReg.RegisterEntityClass(EntityArrowMvk.AliasArrow, typeof(EntityArrowMvk), true);""",
"""            EntitiesReg.RegisterEntityClass(EntityArrowMvk.AliasArrow, typeof(EntityArrowMvk), true);
            EntitiesReg.RegisterEntityClass(EntityBulletMvk.AliasBullet, typeof(EntityBulletMvk), true);""", rel, 1)
save(rel, t, GAME)

rel = r"Entity\Damage\DamageLiving.cs"
t = load(rel, GAME)
t = rep(t, """            if (entityAttack != null && _entity is PlayerServerMvk blocker && blocker.IsBlocking""",
"""            if (entityAttack != null && _entity is PlayerServerMvk blocker && blocker.IsBlocking
                && blocker.InvPlayer.GetCurrentItem()?.Item.Two != true""", rel, 1)
save(rel, t, GAME)
print("game_seam8_projectile_sword_shield_bow_gun ok")
