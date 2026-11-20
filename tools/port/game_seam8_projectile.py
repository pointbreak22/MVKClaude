"""[ПОВЕДЕНИЕ] Снаряд в игре: сущность «Projectile» (EntityProjectileMvk, рисуется как предмет) и бросок камня
снарядом. В оригинале камень вылетал обычным выброшенным предметом и никого не ранил; теперь попадание по живой
сущности считает сервер (EntityProjectile), урон — урон предмета на силу броска."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\EntitiesRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """            EntitiesReg.RegisterEntityClass("Item", typeof(EntityItemMvk), true);""",
"""            EntitiesReg.RegisterEntityClass("Item", typeof(EntityItemMvk), true);
            // Снаряд: брошенный предмет, попадание считает сервер
            EntitiesReg.RegisterEntityClass(EntityProjectile.Alias, typeof(EntityProjectileMvk), true);""", rel, 1)
if "using Mvk.Engine.Entity.List;" not in t:
    t = "using Mvk.Engine.Entity.List;\n" + t
save(rel, t, GAME)

rel = r"Item\List\ItemPiece.cs"
t = load(rel, GAME)
t = rep(t, """        public override void OnUseAction(ItemStack stack, PlayerServer player)
        {
            player.DropItem(stack.Copy(1), true, true);""", """        public override void OnUseAction(ItemStack stack, PlayerServer player)
        {
            // Быстрый бросок снарядом (в оригинале выброшенный предмет)
            EntityProjectile.Throw(player, stack.Copy(1), 1);""", rel, 1)
t = rep(t, """                player.DropItem(stack.Copy(1), true, number > 1);""", """                // Бросок с замахом: сила от времени удержания (в оригинале выброшенный предмет)
                if (number > 1) EntityProjectile.Throw(player, stack.Copy(1), .5f + number * .125f);
                else player.DropItem(stack.Copy(1), true, false);""", rel, 1)
if "using Mvk.Engine.Entity.List;" not in t:
    t = "using Mvk.Engine.Entity.List;\n" + t
save(rel, t, GAME)
print("game_seam8_projectile ok")
