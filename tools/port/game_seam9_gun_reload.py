"""[ПОВЕДЕНИЕ] Перезарядка оружия по тактам сервера (GunReload): такт игрока Малювек ведёт отсчёт по оружию
в карманах и левой руке; время хранится за стаком оружия."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\List\PlayerServerMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public InventoryPlayerMvk InvPlayer { get; private set; }
""", """        public InventoryPlayerMvk InvPlayer { get; private set; }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Перезарядка оружия по тактам сервера
        /// </summary>
        public readonly Item.GunReload Guns = new Item.GunReload();

        /// <summary>
        /// [ПОВЕДЕНИЕ] Такт игрока: после основного — перезарядка оружия
        /// </summary>
        public override void Update()
        {
            base.Update();
            Guns.Update(this);
        }
""", rel, 1)
save(rel, t, GAME)
print("game_seam9_gun_reload ok")
