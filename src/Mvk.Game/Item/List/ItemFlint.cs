#nullable enable
using Mvk.Core.Maths;
using Mvk.Core.Util;
using Mvk.Engine.Audio;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;
using Mvk.Engine.World;
using Mvk.Engine.World.Block;
using Mvk.Game.World.Block;

namespace Mvk.Game.Item.List
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Огниво (по идее ItemFlintAndSteel старого проекта автора Mvk): клик по блоку высекает искру —
    /// с шансом 1 из IgniteChance на стороне блока загорается огонь (BlockFireMvk), иначе только искра и звук;
    /// огниво изнашивается. Другой рукой с огнивом можно поджечь динамит (как факелом). Всё решает сервер.
    /// </summary>
    public class ItemFlint : ItemBase
    {
        /// <summary>
        /// Шанс зажечь огонь (1 из N)
        /// </summary>
        public const int IgniteChance = 3;

        public override ResultHandAction OnAction(bool begin, ItemStack stack, PlayerClientOwner player)
        {
            if (player.MovingObject.IsBlock()) return new ResultHandAction(10, false);
            return base.OnAction(begin, stack, player);
        }

        public override bool OnItemOnBlockPlacement(ItemStack stack, PlayerBase player, BlockPos blockPos, Pole side,
            Vector3 facing, bool flagReplaceable, float playerYaw)
        {
            if (player.GetWorld() is not WorldServer world || player is not PlayerServer server) return true;
            if (!world.GetBlockState(blockPos).GetBlock().IsReplaceable) blockPos = blockPos.Offset(side);
            Vector3 at = blockPos.ToVector3Center();
            world.PlaySoundDistance(AudioIndexs.GetKey("Click"), at, .8f, 1.8f + world.Rnd.NextFloat() * .3f);
            ItemWear.DamageHeld(server, 1);
            if (!world.GetBlockState(blockPos).IsAir() || !BlockFireMvk.CanBurnAt(world, blockPos)) return true;
            if (world.Rnd.Next(IgniteChance) != 0) return true;
            world.SetBlockState(blockPos, new BlockState(BlocksRegMvk.Fire.IndexBlock), BlockFlag.cNotify);
            return true;
        }
    }
}
