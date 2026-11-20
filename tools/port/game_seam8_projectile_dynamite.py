"""[ПОВЕДЕНИЕ] Динамит (нового предмета в оригинале нет): предмет ItemDynamite, сущность «Dynamite» (EntityDynamiteMvk),
взрыв на сервере (ExplosionMvk). По идее ItemUniExplosives/EntityExplosives/Explosion старого проекта автора Mvk,
код свой, спрайт — своя заглушка. Новый предмет в конце таблицы регистрации: старые миры получают его новым id."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Item\ItemsRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public static ItemBase PieceStone { get; private set; }""", """        public static ItemBase PieceStone { get; private set; }
        /// <summary>
        /// Динамит: бросается с замахом и взрывается при попадании
        /// </summary>
        public static ItemDynamite Dynamite { get; private set; }""", rel, 1)
t = rep(t, """            ItemsReg.RegisterItemClass("PieceStone", PieceStone = new ItemPiece());""",
"""            ItemsReg.RegisterItemClass("PieceStone", PieceStone = new ItemPiece());
            ItemsReg.RegisterItemClass("Dynamite", Dynamite = new ItemDynamite());""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsCreative.cs"
t = load(rel, GAME)
t = rep(t, """            tabTools.Add(new ItemStack(ItemsRegMvk.PickaxeRed));""", """            tabTools.Add(new ItemStack(ItemsRegMvk.PickaxeRed));
            tabTools.Add(new ItemStack(ItemsRegMvk.Dynamite));""", rel, 1)
save(rel, t, GAME)

rel = r"Entity\EntitiesRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """            EntitiesReg.RegisterEntityClass(EntityProjectile.Alias, typeof(EntityProjectileMvk), true);""",
"""            EntitiesReg.RegisterEntityClass(EntityProjectile.Alias, typeof(EntityProjectileMvk), true);
            // Динамит: снаряд, который взрывается при попадании
            EntitiesReg.RegisterEntityClass(EntityDynamiteMvk.AliasDynamite, typeof(EntityDynamiteMvk), true);""", rel, 1)
save(rel, t, GAME)
print("game_seam8_projectile_dynamite ok")
