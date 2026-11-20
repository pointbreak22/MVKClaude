"""[ПОВЕДЕНИЕ] Этап 8.1: две руки в выживании (controls_two_hands.py). Игрок Малювек получает менеджер левой руки;
HandManagerMvk решает, чем рука действует предметом: щит — защита, лук, оружие, динамит, кусок, еда — удержание
(натяжение, перезарядка, бросок, еда), ведро, жидкость и яйцо призыва — поставить, остальное — удар и добыча.
Блок щитом смотрит предмет правой руки без учёта руки действия."""
import os
from portlib import load, save, rep, add_using, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\Player\HandManagerMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public HandManagerMvk(GameBase game, PlayerClientOwnerMvk player)
            : base(game, player)
        {
            _playerMvk = player;
        }""", """        public HandManagerMvk(GameBase game, PlayerClientOwnerMvk player)
            : base(game, player)
        {
            _playerMvk = player;
        }

        /// <summary>
        /// Менеджер руки: left — левая рука (выживание, две руки)
        /// </summary>
        public HandManagerMvk(GameBase game, PlayerClientOwnerMvk player, bool left)
            : base(game, player, left)
        {
            _playerMvk = player;
        }

        /// <summary>
        /// Чем рука действует предметом Малювек
        /// </summary>
        protected override HandUse _HandUse(ItemStack stack)
        {
            ItemBase item = stack?.Item;
            if (item is ItemShield) return HandUse.Guard;
            if (item is ItemBow || item is ItemGun || item is ItemDynamite || item is ItemPiece || item is ItemFood)
            {
                return HandUse.Charge;
            }
            if (item is ItemBucket || item is ItemSpawn) return HandUse.Place;
            return base._HandUse(stack);
        }""", rel, 1)
t = rep(t, "            ItemStack right = _player.Inventory.GetCurrentItem();", "            ItemStack right = _player.Inventory.GetCurrentRightItem();", rel, 1)
save(rel, t, GAME)

rel = r"Entity\List\PlayerClientOwnerMvk.cs"
t = load(rel, GAME)
t = rep(t, "            _handManager = new HandManagerMvk(game, this);\n", """            _handManager = new HandManagerMvk(game, this);
            _handLeft = new HandManagerMvk(game, this, true);
""", rel, 1)
save(rel, t, GAME)
print("game hands ok")
