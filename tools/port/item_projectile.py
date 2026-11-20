"""[ПОВЕДЕНИЕ] Снаряд (EntityProjectile, new/Entity/List): источники урона «кусочек» (снаряд) и «взрыв» —
они были заготовлены в оригинале закомментированными (Piece, ExplosionSource)."""
from portlib import load, save, rep

rel = r"Entity\EnumDamageSource.cs"
t = load(rel)
t = rep(t, """        Player = 6,
""", """        Player = 6,
        /// <summary>
        /// Кусочек: брошенный снаряд (EntityProjectile)
        /// </summary>
        Piece = 7,
        /// <summary>
        /// Источник взрыва
        /// </summary>
        ExplosionSource = 8,
""", rel, 1)
t = rep(t, """        ///// <summary>
        ///// Источник взрыва
        ///// </summary>
        //ExplosionSource,
        ///// <summary>
        ///// Кусочек
        ///// </summary>
        //Piece,
""", "", rel, 1)
save(rel, t)
print("item_projectile ok")
