"""[ПОВЕДЕНИЕ] Добыча любым предметом и рукой (просьба пользователя 21.11): в выживании (две руки) рукой и любым
предметом можно бить любой ломаемый блок — дерево, землю, камень — медленно (ударов = твёрдость); свой инструмент
по своему материалу бьёт ×5 (DigBonus). Не ломаются: жидкости и блоки без твёрдости, которым нужен инструмент
(коренная порода). PlayerBase.CanHitBlock; клиент (OnAction, пустая рука) и сервер (DigAcceleration) согласованы."""
from portlib import load, save, rep

rel = r"Entity\Player\PlayerBase.cs"
t = load(rel)
t = rep(t, """        public bool CanDestroyedBlock(BlockBase block)
            => CreativeMode || block.Material.RequiresNoTool;""", """        public bool CanDestroyedBlock(BlockBase block)
            => CreativeMode || block.Material.RequiresNoTool;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Можно ли бить блок рукой или любым предметом (медленно): ломается рукой, или имеет твёрдость
        /// и не жидкость (коренная порода без твёрдости не ломается)
        /// </summary>
        public bool CanHitBlock(BlockBase block)
            => CanDestroyedBlock(block) || (block.Hardness > 0 && !block.Material.Liquid);""", rel, 1)
save(rel, t)

rel = r"Item\ItemBase.cs"
t = load(rel)
t = rep(t, """        public virtual float DigAcceleration(BlockBase block, PlayerBase player)
            => player.CanDestroyedBlock(block) ? 1 : 0;""", """        public virtual float DigAcceleration(BlockBase block, PlayerBase player)
            => player.CanHitBlock(block) ? 1 : 0;""", rel, 1)
i = t.index("        public virtual ResultHandAction OnAction(bool begin, ItemStack stack, PlayerClientOwner player)")
j = t.index("if (moving.IsBlock() && player.CanDestroyedBlock(moving.Block.GetBlock()))", i)
t = t[:j] + "if (moving.IsBlock() && (player.CanDestroyedBlock(moving.Block.GetBlock())\n                || (player.IsTwoHands() && player.CanHitBlock(moving.Block.GetBlock()))))" + t[j + len("if (moving.IsBlock() && player.CanDestroyedBlock(moving.Block.GetBlock()))"):]
save(rel, t)

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """                : (CanDestroyedBlock(block) ? 1 : 0);""", """                : (CanHitBlock(block) ? 1 : 0);""", rel, 1)
save(rel, t)

rel = r"Entity\Player\HandManager.cs"
t = load(rel)
t = rep(t, """                    if (_player.CanDestroyedBlock(moving.Block.GetBlock()))""",
        """                    if (_player.CanDestroyedBlock(moving.Block.GetBlock())
                        || (_TwoHands() && _player.CanHitBlock(moving.Block.GetBlock())))""", rel, 1)
save(rel, t)
print("dig_any ok")
