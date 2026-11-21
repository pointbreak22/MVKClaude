"""[ПОВЕДЕНИЕ] Враждебные мобы порта (EntityHostileMvk): зомби и разбойник регистрируются после курицы
(индексы прежних сущностей не меняются)."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\EntitiesRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """            EntitiesReg.RegisterEntityClass("Chicken", typeof(EntityChicken));""", """            EntitiesReg.RegisterEntityClass("Chicken", typeof(EntityChicken));
            // [ПОВЕДЕНИЕ] Враждебные мобы порта
            EntitiesReg.RegisterEntityClass(EntityZombieMvk.AliasZombie, typeof(EntityZombieMvk));
            EntitiesReg.RegisterEntityClass(EntityBanditMvk.AliasBandit, typeof(EntityBanditMvk));""", rel, 1)
t = rep(t, """                if (alias == "Chicken") ChickenId = i;""", """                if (alias == "Chicken") ChickenId = i;
                else if (alias == EntityZombieMvk.AliasZombie) ZombieId = i;
                else if (alias == EntityBanditMvk.AliasBandit) BanditId = i;""", rel, 1)
t = rep(t, """        public static ushort ChickenId { get; private set; }""", """        public static ushort ChickenId { get; private set; }
        /// <summary>
        /// [ПОВЕДЕНИЕ] Индексы враждебных мобов
        /// </summary>
        public static ushort ZombieId { get; private set; }
        public static ushort BanditId { get; private set; }""", rel, 1)
save(rel, t, GAME)
print("game_seam9_mobs ok")
