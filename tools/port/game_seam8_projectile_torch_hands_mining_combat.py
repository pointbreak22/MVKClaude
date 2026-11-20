"""[ПОВЕДЕНИЕ] Этап 8.3: инструменты Малювек — сила удара по своему материалу (DigBonus, по умолчанию ×3: кирка камень
и руда, лопата рыхлое, топор дерево), крит (Crit, по умолчанию ×1.5, у топоров ×2 в json) и пауза удара по сущности
(AttackPause: меч 10, топор 16 тактов в json). Значения баланса в json предметов (docs/controls-survival-spec.md)."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Item\List\Tool\ItemAbTool.cs"
t = load(rel, GAME)
t = rep(t, """        public float Acceleration { get; protected set; }
""", """        public float Acceleration { get; protected set; }
        /// <summary>
        /// [ПОВЕДЕНИЕ] Сила удара по своему материалу (json DigBonus)
        /// </summary>
        public float DigBonus { get; protected set; } = 3;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Крит удара по сущности в прыжке или на бегу (json Crit)
        /// </summary>
        public float Crit { get; protected set; } = 1.5f;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Пауза между ударами по сущности при удержании, тактов (json AttackPause; 0 — по умолчанию 10)
        /// </summary>
        public int AttackPause { get; protected set; }
""", rel, 1)
t = rep(t, """                        else if(json.IsKey(Cti.Acceleration)) Acceleration = json.GetFloat();""",
        """                        else if(json.IsKey(Cti.Acceleration)) Acceleration = json.GetFloat();
                        else if (json.IsKey("DigBonus")) DigBonus = json.GetFloat();
                        else if (json.IsKey("Crit")) Crit = json.GetFloat();
                        else if (json.IsKey("AttackPause")) AttackPause = json.GetInt();""", rel, 1)
t = rep(t, """        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float GetDamageToAttack() => _damage;""", """        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float GetDamageToAttack() => _damage;

        public override float CritMultiplier => Crit;

        /// <summary>
        /// Удар по сущности с паузой инструмента (AttackPause), остальное как у предмета
        /// </summary>
        public override ResultHandAction OnAction(bool begin, ItemStack stack, PlayerClientOwner player)
        {
            if (AttackPause > 0 && player.MovingObject.IsEntity())
            {
                return new ResultHandAction(ResultHandAction.ActionType.AttackEntity, AttackPause);
            }
            return base.OnAction(begin, stack, player);
        }""", rel, 1)
if "using Mvk.Engine.Entity.Player;" not in t:
    t = "using Mvk.Engine.Entity.Player;\n" + t
save(rel, t, GAME)

for name, materials in (("ItemPickaxe", ["Solid"]), ("ItemShovel", ["Loose"]),
                        ("ItemAxe", ["Wood", "Door", "Branch"]),
                        ("ItemAxeStone", ["Wood", "Door", "Branch", "Solid"])):
    rel = rf"Item\List\Tool\{name}.cs"
    t = load(rel, GAME)
    cond = " || ".join(f"block.Material.IndexMaterial == (int)EnumMaterial.{m}" for m in materials)
    anchor = "        public override float DigAcceleration(BlockBase block, PlayerBase player)"
    t = rep(t, anchor, f"""        public override float DigStrength(BlockBase block, PlayerBase player)
            => {cond} ? DigBonus : 1;

        /// <summary>
        /// Ускорение добычи блока (сервер): свои материалы и блоки, которые ломаются рукой
        /// </summary>
{anchor}""", rel, 1)
    # Описание нового метода: заменить doc-комментарий, оказавшийся над DigStrength
    t = rep(t, """        /// <summary>
        /// Ускорение добычи блока (сервер): свои материалы и блоки, которые ломаются рукой
        /// </summary>
        public override float DigStrength""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Сила удара по блоку (сервер): по своему материалу — DigBonus
        /// </summary>
        public override float DigStrength""", rel, 1)
    save(rel, t, GAME)

# Щит на предплечье: блок при любом предмете в другой руке (лук, оружие, блок)
rel = r"Entity\Player\HandManagerMvk.cs"
t = load(rel, GAME)
t = rep(t, """        /// <summary>
        /// Можно ли блокировать: щит в левой руке, правая свободна или держит оружие или инструмент
        /// </summary>
        private bool _CanBlock()
        {
            if (_player.Inventory.GetCurrentLeftItem()?.Item is not ItemShield) return false;
            ItemStack right = _player.Inventory.GetCurrentRightItem();
            return right == null || (right.Item is ItemAbTool && !right.Item.Two);
        }""", """        /// <summary>
        /// Можно ли блокировать: щит в левой руке; он на предплечье, кисть свободна, предмет другой руки любой
        /// </summary>
        private bool _CanBlock() => _player.Inventory.GetCurrentLeftItem()?.Item is ItemShield;""", rel, 1)
save(rel, t, GAME)

rel = r"Entity\Damage\DamageLiving.cs"
t = load(rel, GAME)
t = rep(t, """                && blocker.InvPlayer.GetCurrentItem()?.Item.Two != true
""", "", rel, 1)
save(rel, t, GAME)
print("game combat ok")
