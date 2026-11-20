"""[ПОВЕДЕНИЕ] Этап 8.2: инструменты Малювек сообщают серверу, какие блоки добывают и с каким ускорением
(ItemBase.DigAcceleration, mining_server.py) — та же логика, что их OnAction на клиенте. Два топора больше не ускоряют
друг друга вдвое: в двух руках каждый бьёт сам."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

ON_ACTION = "        public override ResultHandAction OnAction(bool begin, ItemStack stack, PlayerClientOwner player)"


def dig(materials):
    cond = " || ".join(f"block.Material.IndexMaterial == (int)EnumMaterial.{m}" for m in materials)
    return f"""        /// <summary>
        /// Ускорение добычи блока (сервер): свои материалы и блоки, которые ломаются рукой
        /// </summary>
        public override float DigAcceleration(BlockBase block, PlayerBase player)
            => player.CanDestroyedBlock(block) || {cond} ? Acceleration : 0;

"""


for name, materials in (("ItemPickaxe", ["Solid"]), ("ItemShovel", ["Loose"]),
                        ("ItemAxe", ["Wood", "Door", "Branch"]),
                        ("ItemAxeStone", ["Wood", "Door", "Branch", "Solid"])):
    rel = rf"Item\List\Tool\{name}.cs"
    t = load(rel, GAME)
    t = rep(t, ON_ACTION, dig(materials) + ON_ACTION, rel, 1)
    if "using Mvk.Engine.Entity.Player;" not in t:
        t = "using Mvk.Engine.Entity.Player;\n" + t
    save(rel, t, GAME)
print("game mining ok")
