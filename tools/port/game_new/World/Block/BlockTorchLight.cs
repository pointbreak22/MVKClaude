#nullable enable
using Mvk.Core.Util;
using Mvk.Engine.World;
using Mvk.Engine.World.Block;
using Mvk.Engine.World.Block.List;
using Mvk.Engine.World.Chunk;

namespace Mvk.Game.World.Block
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Невидимый свет факела в руке (HandLight): как воздух (не рисуется, не выбирается, заменяется,
    /// без столкновений), но светит на 14. Ставит и убирает сервер; без хозяина гаснет по случайному такту.
    /// </summary>
    public class BlockTorchLight : BlockAir
    {
        public BlockTorchLight(MaterialBase material) : base(material)
        {
            LightValue = 14;
            NeedsRandomTick = true;
        }

        public override void RandomTick(WorldServer world, ChunkServer chunk, BlockPos blockPos, BlockState blockState, Rand random)
        {
            if (!HandLight.IsActive(world, blockPos)) world.SetBlockState(blockPos, new BlockState(0), BlockFlag.None);
        }
    }
}
