"""[ПОВЕДЕНИЕ] Слот лица (просьба пользователя 26.11): девятый слот одежды Face (маска, очки). Рюкзак сдвигается на одну
ячейку; старые сохранения без признака InvFace переносятся (ячейки рюкзака +1). Маска и очки из скина пользователя
регистрируются последними."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\Inventory\InventoryPlayerMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public const byte ClothCount = 8;""", """        public const byte ClothCount = 9;""", rel, 1)
t = rep(t, """        public override ItemStack GetCurrentLeftItem() """, """        /// <summary>
        /// [ПОВЕДЕНИЕ] Сохранение с признаком слота лица
        /// </summary>
        public override void WriteToNBT(Mvk.Core.Nbt.TagCompound nbt)
        {
            base.WriteToNBT(nbt);
            nbt.SetBool("InvFace", true);
        }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Старое сохранение (до слота лица): ячейки рюкзака сдвигаются на одну
        /// </summary>
        public override void ReadFromNBT(Mvk.Core.Nbt.TagCompound nbt)
        {
            if (nbt.HasKey("InvFace"))
            {
                base.ReadFromNBT(nbt);
                return;
            }
            _Clear();
            int oldBackpack = PocketCount + ClothCount - 1;
            foreach (Slot slot in Mvk.Engine.Nbt.NBTTools.ItemStacksReadFromNBT(nbt, "Inventory"))
            {
                SetStackInSlot(slot.SlotId >= oldBackpack ? slot.SlotId + 1 : slot.SlotId, slot.Stack);
            }
        }

        public override ItemStack GetCurrentLeftItem() """, rel, 1)
save(rel, t, GAME)

rel = r"Item\EnumCloth.cs"
t = load(rel, GAME)
t = rep(t, """        Shoes = 8
""", """        Shoes = 8,
        /// <summary>
        /// [ПОВЕДЕНИЕ] Лицо (маска, очки)
        /// </summary>
        Face = 9
""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """            HoodUser = ItemsReg.RegisterItemClothClass("HoodUser");""", """            HoodUser = ItemsReg.RegisterItemClothClass("HoodUser");
            MaskUser = ItemsReg.RegisterItemClothClass("MaskUser");
            GlassesUser = ItemsReg.RegisterItemClothClass("GlassesUser");
            BackpackUser = ItemsReg.RegisterItemClothClass("BackpackUser");""", rel, 1)
t = rep(t, """        public static ItemCloth HoodUser { get; private set; }""", """        public static ItemCloth HoodUser { get; private set; }
        /// <summary>
        /// [ПОВЕДЕНИЕ] Маска и очки из скина пользователя (слот лица)
        /// </summary>
        public static ItemCloth MaskUser { get; private set; }
        public static ItemCloth GlassesUser { get; private set; }
        /// <summary>
        /// [ПОВЕДЕНИЕ] Рюкзак в стиле скина пользователя, 35 ячеек (весь рюкзак)
        /// </summary>
        public static ItemCloth BackpackUser { get; private set; }""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsCreative.cs"
t = load(rel, GAME)
t = rep(t, """            tabCloth.Add(new ItemStack(ItemsRegMvk.HoodUser));""", """            tabCloth.Add(new ItemStack(ItemsRegMvk.HoodUser));
            tabCloth.Add(new ItemStack(ItemsRegMvk.MaskUser));
            tabCloth.Add(new ItemStack(ItemsRegMvk.GlassesUser));
            tabCloth.Add(new ItemStack(ItemsRegMvk.BackpackUser));""", rel, 1)
save(rel, t, GAME)
print("game_seam9_user_face ok")
