"""[ПОВЕДЕНИЕ] Этап 8.4: щит в любой руке, раскрытие щита на время действия другой руки, удар щитом с разгона,
«крест» двумя мечами (game_new/Entity/Player/HandManagerMvkHands.cs, controls_shield_hands.py).
Сервер: блок щитом из любой руки (левая первой) или двумя мечами (CrossProtection меча, по умолчанию половина
защиты щита), износ щита или обоих мечей."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\Player\HandManagerMvk.cs"
t = load(rel, GAME)
t = rep(t, "    public class HandManagerMvk : HandManager", "    public partial class HandManagerMvk : HandManager", rel, 1)
t = rep(t, """        private bool _CanBlock() => _player.Inventory.GetCurrentLeftItem()?.Item is ItemShield;

        protected override void _Second(bool begin)
        {""", """        private bool _CanBlock() => _player.Inventory.GetCurrentLeftItem()?.Item is ItemShield;

        protected override void _Second(bool begin)
        {
            if (_player.IsTwoHands())
            {
                // Две руки: защиту щитом ведёт такт рук (_AfterHandTick)
                if (_player.Inventory.GetCurrentItem()?.Item is ItemShield) return;
                base._Second(begin);
                return;
            }""", rel, 1)
t = rep(t, """        protected override void _SecondEnd()
        {""", """        protected override void _SecondEnd()
        {
            if (_player.IsTwoHands())
            {
                if (_player.Inventory.GetCurrentItem()?.Item is ItemShield) return;
                base._SecondEnd();
                return;
            }""", rel, 1)
save(rel, t, GAME)

rel = r"Entity\List\PlayerClientOwnerMvk.cs"
t = load(rel, GAME)
t = rep(t, """            _handLeft = new HandManagerMvk(game, this, true);
""", """            _handLeft = new HandManagerMvk(game, this, true);
            ((HandManagerMvk)_handManager).Other = (HandManagerMvk)_handLeft;
            ((HandManagerMvk)_handLeft).Other = (HandManagerMvk)_handManager;
""", rel, 1)
t = rep(t, """        public InventoryPlayerMvk InvPlayer { get; private set; }
""", """        public InventoryPlayerMvk InvPlayer { get; private set; }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Блок (щит или «крест» мечами) отправлен серверу (две руки)
        /// </summary>
        public bool BlockingSent;
""", rel, 1)
save(rel, t, GAME)

rel = r"Entity\Damage\DamageLiving.cs"
t = load(rel, GAME)
t = rep(t, """            // Блок щитом (сервер): атакующий спереди, щит в левой руке
            if (entityAttack != null && _entity is PlayerServerMvk blocker && blocker.IsBlocking
                && blocker.InvPlayer.GetCurrentLeftItem()?.Item is ItemShield shield && _IsInFront(blocker, entityAttack))
            {
                damage *= 1f - shield.Protection;
                ItemWear.DamageSlot(blocker, InventoryPlayerMvk.PocketCount, 1);
            }""", """            // Блок (сервер): атакующий спереди, щит в любой руке или два меча «крестом»
            if (entityAttack != null && _entity is PlayerServerMvk blocker && blocker.IsBlocking
                && _IsInFront(blocker, entityAttack))
            {
                damage = _Block(blocker, damage);
            }""", rel, 1)
i = t.index("        private static bool _IsInFront(")
j = t.rfind("        /// <summary>", 0, i)
t = t[:j] + """        /// <summary>
        /// [ПОВЕДЕНИЕ] Урон после блока: щит в левой, иначе в правой руке снимает свою долю (Protection) и изнашивается;
        /// два меча снимают CrossProtection и изнашиваются оба
        /// </summary>
        private static float _Block(PlayerServerMvk blocker, float damage)
        {
            InventoryPlayerMvk inventory = blocker.InvPlayer;
            int left = InventoryPlayerMvk.PocketCount;
            int right = inventory.GetCurrentIndex();
            if (inventory.GetStackInSlot(left)?.Item is ItemShield shieldLeft)
            {
                ItemWear.DamageSlot(blocker, left, 1);
                return damage * (1f - shieldLeft.Protection);
            }
            if (inventory.GetStackInSlot(right)?.Item is ItemShield shieldRight)
            {
                ItemWear.DamageSlot(blocker, right, 1);
                return damage * (1f - shieldRight.Protection);
            }
            if (inventory.GetStackInSlot(left)?.Item is ItemSword swordLeft
                && inventory.GetStackInSlot(right)?.Item is ItemSword swordRight)
            {
                ItemWear.DamageSlot(blocker, left, 1);
                ItemWear.DamageSlot(blocker, right, 1);
                return damage * (1f - System.Math.Min(swordLeft.CrossProtection, swordRight.CrossProtection));
            }
            return damage;
        }

""" + t[j:]
if "using Mvk.Game.Item.List.Tool;" not in t:
    t = "using Mvk.Game.Item.List.Tool;\n" + t
save(rel, t, GAME)
print("game shield ok")
