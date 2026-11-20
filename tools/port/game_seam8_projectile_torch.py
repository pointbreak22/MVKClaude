"""[ПОВЕДЕНИЕ] Факел (в оригинале нет): блок «Torch» — тонкий столбик со светом 14, сквозь него проходят, материал
верёвки (ломается рукой); предмет ставит его как обычный блок. Блок регистрируется последним, чтобы id прежних блоков
новых миров не сдвигались (эталон генерации island.csv). Огня и распространения нет; опору факел не проверяет.
Модели и текстуры свои (assets/port-assets.txt)."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"World\Block\BlocksRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public static BlockRope Rope { get; private set; }""", """        public static BlockRope Rope { get; private set; }
        /// <summary>
        /// Факел: светящийся тонкий столбик
        /// </summary>
        public static BlockBase Torch { get; private set; }""", rel, 1)
t = rep(t, """            BlocksReg.RegisterBlockClass("Rope", Rope = new BlockRope(materials.Rope));
""", """            BlocksReg.RegisterBlockClass("Rope", Rope = new BlockRope(materials.Rope));
            // Новые блоки порта — только в конце регистрации
            Torch = BlocksReg.RegisterBlockClass("Torch", materials.Rope);
""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public static ItemRope Rope { get; private set; }""", """        public static ItemRope Rope { get; private set; }
        /// <summary>
        /// Факел
        /// </summary>
        public static ItemBase Torch { get; private set; }""", rel, 1)
t = rep(t, """            ItemsReg.RegisterItemClass("Rope", Rope = new ItemRope(BlocksRegMvk.Rope), _ItemsForBlock);""",
"""            ItemsReg.RegisterItemClass("Rope", Rope = new ItemRope(BlocksRegMvk.Rope), _ItemsForBlock);
            ItemsReg.RegisterItemClass("Torch", Torch = new ItemBlock(BlocksRegMvk.Torch), _ItemsForBlock);""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsCreative.cs"
t = load(rel, GAME)
t = rep(t, """            tabItemsForBlock.Add(new ItemStack(ItemsRegMvk.Rope));""", """            tabItemsForBlock.Add(new ItemStack(ItemsRegMvk.Rope));
            tabItemsForBlock.Add(new ItemStack(ItemsRegMvk.Torch));""", rel, 1)
save(rel, t, GAME)
print("game_seam8_projectile_torch ok")
