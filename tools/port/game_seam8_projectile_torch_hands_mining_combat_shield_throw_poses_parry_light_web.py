"""[ПОВЕДЕНИЕ] Паутина (в оригинале нет): блок «Cobweb» крестом, без столкновений, сильно замедляет (FactorSlow),
ломается рукой, мечом втрое быстрее (ItemSword.DigStrength). Регистрируется последней (id прежних блоков не
сдвигаются), предмет — в творческой вкладке блоков-предметов. Текстуры свои (assets/port-assets.txt)."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"World\Block\BlocksRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public static BlockBase Torch { get; private set; }""", """        public static BlockBase Torch { get; private set; }
        /// <summary>
        /// Паутина
        /// </summary>
        public static BlockBase Cobweb { get; private set; }""", rel, 1)
t = rep(t, """            Mvk.Engine.World.HandLight.Block = torchLight;
""", """            Mvk.Engine.World.HandLight.Block = torchLight;
            Cobweb = BlocksReg.RegisterBlockClass("Cobweb", materials.Rope);
""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public static ItemBase Torch { get; private set; }""", """        public static ItemBase Torch { get; private set; }
        /// <summary>
        /// Паутина
        /// </summary>
        public static ItemBase Cobweb { get; private set; }""", rel, 1)
t = rep(t, """            ItemsReg.RegisterItemClass("Torch", Torch = new ItemBlock(BlocksRegMvk.Torch), _ItemsForBlock);""",
        """            ItemsReg.RegisterItemClass("Torch", Torch = new ItemBlock(BlocksRegMvk.Torch), _ItemsForBlock);
            ItemsReg.RegisterItemClass("Cobweb", Cobweb = new ItemBlock(BlocksRegMvk.Cobweb), _ItemsForBlock);""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsCreative.cs"
t = load(rel, GAME)
t = rep(t, """            tabItemsForBlock.Add(new ItemStack(ItemsRegMvk.Torch));""", """            tabItemsForBlock.Add(new ItemStack(ItemsRegMvk.Torch));
            tabItemsForBlock.Add(new ItemStack(ItemsRegMvk.Cobweb));""", rel, 1)
save(rel, t, GAME)
print("game web ok")
