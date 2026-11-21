"""[ПОВЕДЕНИЕ] Огонь и огниво (ответ пользователя 21.11; по идее BlockFire и ItemFlintAndSteel старого проекта Mvk,
код свой): блок «Fire» (game_new BlockFireMvk) регистрируется последним, предмет «Flint» (огниво, ItemFlint).
Огниво поджигает и динамит из другой руки. Текстуры и значок свои (assets/port-assets.txt)."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"World\Block\BlocksRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public static BlockBase Cobweb { get; private set; }""", """        public static BlockBase Cobweb { get; private set; }
        /// <summary>
        /// Огонь
        /// </summary>
        public static BlockFireMvk Fire { get; private set; }""", rel, 1)
t = rep(t, """            Cobweb = BlocksReg.RegisterBlockClass("Cobweb", materials.Rope);
""", """            Cobweb = BlocksReg.RegisterBlockClass("Cobweb", materials.Rope);
            BlocksReg.RegisterBlockClass("Fire", Fire = new BlockFireMvk(materials.Plant));
""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public static ItemBase Hook { get; private set; }""", """        public static ItemBase Hook { get; private set; }
        /// <summary>
        /// Огниво
        /// </summary>
        public static ItemBase Flint { get; private set; }""", rel, 1)
t = rep(t, """            ItemsReg.RegisterItemClass("Hook", Hook = new ItemBase(), _ItemsForBlock);""",
        """            ItemsReg.RegisterItemClass("Hook", Hook = new ItemBase(), _ItemsForBlock);
            ItemsReg.RegisterItemClass("Flint", Flint = new ItemFlint(), _ItemsForBlock);""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsCreative.cs"
t = load(rel, GAME)
t = rep(t, """            tabItemsForBlock.Add(new ItemStack(ItemsRegMvk.Hook));""", """            tabItemsForBlock.Add(new ItemStack(ItemsRegMvk.Hook));
            tabItemsForBlock.Add(new ItemStack(ItemsRegMvk.Flint));""", rel, 1)
save(rel, t, GAME)
print("game fire ok")
