"""[ПОВЕДЕНИЕ] Крюк с верёвкой (ответ пользователя 21.11): предмет «Hook» бросается по общей схеме броска
(удержание и отпускание, json ThrowMax и ThrowEntity), снаряд EntityHookMvk цепляется и свешивает верёвку.
Снаряд и предмет регистрируются последними. Значок и модель свои (assets/port-assets.txt)."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\EntitiesRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """            EntitiesReg.RegisterEntityClass(EntityTorchMvk.AliasTorch, typeof(EntityTorchMvk), true);""",
        """            EntitiesReg.RegisterEntityClass(EntityTorchMvk.AliasTorch, typeof(EntityTorchMvk), true);
            EntitiesReg.RegisterEntityClass(EntityHookMvk.AliasHook, typeof(EntityHookMvk), true);""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public static ItemBase Cobweb { get; private set; }""", """        public static ItemBase Cobweb { get; private set; }
        /// <summary>
        /// Крюк с верёвкой
        /// </summary>
        public static ItemBase Hook { get; private set; }""", rel, 1)
t = rep(t, """            ItemsReg.RegisterItemClass("Cobweb", Cobweb = new ItemBlock(BlocksRegMvk.Cobweb), _ItemsForBlock);""",
        """            ItemsReg.RegisterItemClass("Cobweb", Cobweb = new ItemBlock(BlocksRegMvk.Cobweb), _ItemsForBlock);
            ItemsReg.RegisterItemClass("Hook", Hook = new ItemBase(), _ItemsForBlock);""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsCreative.cs"
t = load(rel, GAME)
t = rep(t, """            tabItemsForBlock.Add(new ItemStack(ItemsRegMvk.Cobweb));""", """            tabItemsForBlock.Add(new ItemStack(ItemsRegMvk.Cobweb));
            tabItemsForBlock.Add(new ItemStack(ItemsRegMvk.Hook));""", rel, 1)
save(rel, t, GAME)
print("game hook ok")
