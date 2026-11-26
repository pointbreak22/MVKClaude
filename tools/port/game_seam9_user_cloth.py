"""[ПОВЕДЕНИЕ] Одежда из скина пользователя (tools/Assets/skin_convert.py, слой LayerUserSkin): худи, штаны, обувь,
капюшон. Регистрируются последними (индексы прежних предметов не меняются), есть во вкладке одежды творческого
инвентаря."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Item\ItemsRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """            ItemsReg.RegisterItemClass("CannedFood", CannedFood = new ItemFood());""",
        """            ItemsReg.RegisterItemClass("CannedFood", CannedFood = new ItemFood());

            // [ПОВЕДЕНИЕ] Одежда из скина пользователя
            HoodieUser = ItemsReg.RegisterItemClothClass("HoodieUser");
            PantsUser = ItemsReg.RegisterItemClothClass("PantsUser");
            ShoesUser = ItemsReg.RegisterItemClothClass("ShoesUser");
            HoodUser = ItemsReg.RegisterItemClothClass("HoodUser");""", rel, 1)
t = rep(t, """        public static ItemCloth StrawHat { get; private set; }""", """        public static ItemCloth StrawHat { get; private set; }
        /// <summary>
        /// [ПОВЕДЕНИЕ] Одежда из скина пользователя: худи, штаны, обувь, капюшон
        /// </summary>
        public static ItemCloth HoodieUser { get; private set; }
        public static ItemCloth PantsUser { get; private set; }
        public static ItemCloth ShoesUser { get; private set; }
        public static ItemCloth HoodUser { get; private set; }""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsCreative.cs"
t = load(rel, GAME)
t = rep(t, """            tabCloth.Add(new ItemStack(ItemsRegMvk.BootsIron));""", """            tabCloth.Add(new ItemStack(ItemsRegMvk.BootsIron));
            tabCloth.Add(new ItemStack(ItemsRegMvk.HoodieUser));
            tabCloth.Add(new ItemStack(ItemsRegMvk.PantsUser));
            tabCloth.Add(new ItemStack(ItemsRegMvk.ShoesUser));
            tabCloth.Add(new ItemStack(ItemsRegMvk.HoodUser));""", rel, 1)
save(rel, t, GAME)
print("game_seam9_user_cloth ok")
