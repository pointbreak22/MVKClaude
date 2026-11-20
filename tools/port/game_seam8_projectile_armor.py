"""[ПОВЕДЕНИЕ] Броня: шлем, нагрудник, поножи и ботинки (железные) — одежда со слотами шапки, куртки, штанов и обуви
и защитой Protection (шов движка item_armor.py). Сервер снижает урон по части тела, в которую пришёлся удар
(DamageLiving._GetDamageFromBody: «2. Защита от брони» оригинала была только в комментарии); удар по всему телу —
средняя защита четырёх частей, удар без урона по одежде (hitY = -2) — без защиты. Внешний вид на модели игрока
пока заглушка: слои существующей одежды. В старом проекте автора Mvk у брони были только слоты."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

ARMOR = [("HelmetIron", "Шлем"), ("ChestplateIron", "Нагрудник"), ("LeggingsIron", "Поножи"), ("BootsIron", "Ботинки")]

rel = r"Entity\Damage\DamageLiving.cs"
t = load(rel, GAME)
t = rep(t, """            Console.Write(" " + enumBody);
""", """            // Защита надетой брони по части тела (сервер)
            damage *= 1f - _Protection(enumBody);
            Console.Write(" " + enumBody);
""", rel, 1)
t = rep(t, """        protected override float _GetDamageFromBody(byte source, float hitY, float damage)""", """        /// <summary>
        /// Защита одежды игрока по части тела: голова — шапка, тело — куртка, ноги — штаны, стопы — обувь,
        /// всё тело — средняя защита четырёх частей
        /// </summary>
        private float _Protection(EnumBodyDamage body)
        {
            if (_entity is not PlayerServerMvk player) return 0;
            InventoryPlayerMvk inventory = player.InvPlayer;
            return body switch
            {
                EnumBodyDamage.Head => _Cloth(inventory, EnumCloth.Hat),
                EnumBodyDamage.Body => _Cloth(inventory, EnumCloth.Jacket),
                EnumBodyDamage.Legs => _Cloth(inventory, EnumCloth.Pants),
                EnumBodyDamage.Feet => _Cloth(inventory, EnumCloth.Shoes),
                EnumBodyDamage.All => (_Cloth(inventory, EnumCloth.Hat) + _Cloth(inventory, EnumCloth.Jacket)
                    + _Cloth(inventory, EnumCloth.Pants) + _Cloth(inventory, EnumCloth.Shoes)) / 4f,
                _ => 0
            };
        }

        /// <summary>
        /// Защита вещи в слоте одежды (слот одежды = карманы + ключ части тела - 1)
        /// </summary>
        private static float _Cloth(InventoryPlayerMvk inventory, EnumCloth part)
            => inventory.GetStackInSlot(InventoryPlayerMvk.PocketCount + (int)part - 1)?.Item is ItemCloth cloth
                ? cloth.Protection : 0;

        protected override float _GetDamageFromBody(byte source, float hitY, float damage)""", rel, 1)
for u in ["using Mvk.Engine.Item.List;", "using Mvk.Game.Entity.Inventory;", "using Mvk.Game.Entity.List;", "using Mvk.Game.Item;"]:
    if u not in t:
        t = u + "\n" + t
save(rel, t, GAME)

rel = r"Item\ItemsRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public static ItemCloth CamouflageJacket { get; private set; }""", """        public static ItemCloth CamouflageJacket { get; private set; }
""" + "".join(f"""        /// <summary>
        /// {ru} железный: броня, снижает урон по своей части тела
        /// </summary>
        public static ItemCloth {name} {{ get; private set; }}
""" for name, ru in ARMOR).rstrip("\n"), rel, 1)
t = rep(t, """            CamouflageJacket = ItemsReg.RegisterItemClothClass("CamouflageJacket");""", """            CamouflageJacket = ItemsReg.RegisterItemClothClass("CamouflageJacket");
""" + "\n".join(f"""            {name} = ItemsReg.RegisterItemClothClass("{name}");""" for name, _ in ARMOR), rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsCreative.cs"
t = load(rel, GAME)
t = rep(t, """            tabCloth.Add(new ItemStack(ItemsRegMvk.CamouflageJacket));""", """            tabCloth.Add(new ItemStack(ItemsRegMvk.CamouflageJacket));
""" + "\n".join(f"""            tabCloth.Add(new ItemStack(ItemsRegMvk.{name}));""" for name, _ in ARMOR), rel, 1)
save(rel, t, GAME)
print("game_seam8_projectile_armor ok")
