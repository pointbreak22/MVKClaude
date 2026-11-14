"""[ПОВЕДЕНИЕ] Для согласованного ведра (docs/known-issues.md п. 2): после набора в неполное ведро остаток
жидкости остаётся в блоке, и блоку нужен такт жидкости, чтобы он растекался и высыхал как обычно.
Открывает тик жидкости для кода мода (раньше был только внутри BlockLiquid)."""
from portlib import load, save, rep

rel = r"World\Block\List\BlockLiquid.cs"
t = load(rel)
t = rep(t, """        /// <summary>
        /// Задать тик блоку жидкости
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _SetTickBlock(WorldServer world, BlockPos pos)""", """        /// <summary>
        /// Запустить такт жидкости в блоке (жидкость или доп. жидкость в другом блоке)
        /// </summary>
        public void ScheduleTick(WorldServer world, BlockPos pos) => _SetTickBlock(world, pos);

        /// <summary>
        /// Задать тик блоку жидкости
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _SetTickBlock(WorldServer world, BlockPos pos)""", rel, 1)
save(rel, t)
print("fix_liquid_tick ok")
