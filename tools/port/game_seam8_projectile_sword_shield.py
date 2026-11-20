"""[ПОВЕДЕНИЕ] Щит (ItemShield, game_new): во второй (левой) руке. Клиент (HandManagerMvk) при зажатой второй кнопке,
если в левой руке щит, а правая свободна или держит оружие/инструмент, шлёт пакет C0B «блок»; при отпускании —
«конец блока». Сервер (DamageLiving): если игрок блокирует, щит в левой руке и атакующий спереди (≤ 90° от взгляда),
урон снижается на Protection щита, щит изнашивается на 1. Без атакующего (падение, огонь) щит не помогает."""
import os
from portlib import load, save, rep, ROOT
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
        /// Блок щитом отправлен серверу
        /// </summary>
        private bool _blocking;

        /// <summary>
        /// Можно ли блокировать: щит в левой руке, правая свободна или держит оружие или инструмент
        /// </summary>
        private bool _CanBlock()
        {
            if (_player.Inventory.GetCurrentLeftItem()?.Item is not ItemShield) return false;
            ItemStack right = _player.Inventory.GetCurrentItem();
            return right == null || (right.Item is ItemAbTool && !right.Item.Two);
        }

        protected override void _Second(bool begin)
        {
            if (_CanBlock())
            {
                if (!_blocking)
                {
                    _blocking = true;
                    _game.TrancivePacket(new PacketC0BBlocking(true));
                }
                return;
            }
            base._Second(begin);
        }

        protected override void _SecondEnd()
        {
            if (_blocking)
            {
                _blocking = false;
                _game.TrancivePacket(new PacketC0BBlocking(false));
                return;
            }
            base._SecondEnd();
        }""", rel, 1)
for u in ["using Mvk.Engine.Network.Packets.Client;", "using Mvk.Game.Item.List;"]:
    if u not in t:
        t = u + "\n" + t
save(rel, t, GAME)

rel = r"Entity\Damage\DamageLiving.cs"
t = load(rel, GAME)
t = rep(t, """        public override bool OnAttack(byte source, float hitY, float damage,
            EntityLiving entityAttack = null)
        {""", """        public override bool OnAttack(byte source, float hitY, float damage,
            EntityLiving entityAttack = null)
        {
            // Блок щитом (сервер): атакующий спереди, щит в левой руке
            if (entityAttack != null && _entity is PlayerServerMvk blocker && blocker.IsBlocking
                && blocker.InvPlayer.GetCurrentLeftItem()?.Item is ItemShield shield && _IsInFront(blocker, entityAttack))
            {
                damage *= 1f - shield.Protection;
                ItemWear.DamageSlot(blocker, InventoryPlayerMvk.PocketCount, 1);
            }""", rel, 1)
t = rep(t, """        /// <summary>
        /// Износ брони, принявшей удар""", """        /// <summary>
        /// Атакующий перед игроком: угол между взглядом и направлением на атакующего не больше 90°
        /// </summary>
        private static bool _IsInFront(EntityLiving player, EntityLiving attacker)
        {
            float dx = attacker.PosX - player.PosX, dz = attacker.PosZ - player.PosZ;
            // Взгляд игрока: yaw 0 — на -Z (как бросок предмета)
            float lookX = MathF.Sin(player.RotationYaw), lookZ = -MathF.Cos(player.RotationYaw);
            return dx * lookX + dz * lookZ >= 0;
        }

        /// <summary>
        /// Износ брони, принявшей удар""", rel, 1)
if "using Mvk.Game.Item.List;" not in t:
    t = "using Mvk.Game.Item.List;\n" + t
save(rel, t, GAME)

rel = r"Item\ItemsRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """        public static ItemAbTool HoeIron { get; private set; }""", """        public static ItemAbTool HoeIron { get; private set; }
        /// <summary>
        /// Деревянный щит с железной оковкой
        /// </summary>
        public static ItemShield Shield { get; private set; }""", rel, 1)
t = rep(t, """            ItemsReg.RegisterItemClass("HoeIron", HoeIron = new ItemHoe(), _Tools);""", """            ItemsReg.RegisterItemClass("HoeIron", HoeIron = new ItemHoe(), _Tools);
            ItemsReg.RegisterItemClass("Shield", Shield = new ItemShield(), _Tools);""", rel, 1)
save(rel, t, GAME)

rel = r"Item\ItemsCreative.cs"
t = load(rel, GAME)
t = rep(t, """            tabTools.Add(new ItemStack(ItemsRegMvk.HoeIron));""", """            tabTools.Add(new ItemStack(ItemsRegMvk.HoeIron));
            tabTools.Add(new ItemStack(ItemsRegMvk.Shield));""", rel, 1)
save(rel, t, GAME)
print("game_seam8_projectile_sword_shield ok")
