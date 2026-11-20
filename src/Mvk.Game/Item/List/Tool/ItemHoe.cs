#nullable enable
using Mvk.Core.Maths;
using Mvk.Core.Util;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;
using Mvk.Engine.Util;
using Mvk.Engine.World;
using Mvk.Engine.World.Block;
using Mvk.Game.World.Block;

namespace Mvk.Game.Item.List.Tool
{
    /// <summary>
    /// Мотыга: копает рыхлое как лопата, второе действие по дёрну снимает его — дёрн становится перегноем, дёрн на
    /// суглинке суглинком (блока пашни в игре нет). Замену блока и износ считает сервер.
    /// </summary>
    public class ItemHoe : ItemShovel
    {
        public override ResultHandSecond OnSecond(bool begin, ItemStack stack, PlayerClientOwner player, int counter)
        {
            ResultHandSecond result = _CheckEntitySecond(player);
            if (result.Action != ResultHandSecond.ActionType.None) return result;
            MovingObjectPosition moving = player.MovingObject;
            if (moving.IsBlock() && _Tilled(moving.Block.GetBlock()) != null)
            {
                return new ResultHandSecond(ResultHandSecond.ActionType.BlockPlacement, 8);
            }
            return new ResultHandSecond(ResultHandSecond.ActionType.None);
        }

        public override bool OnItemOnBlockPlacement(ItemStack stack, PlayerBase player,
            BlockPos blockPos, Pole side, Vector3 facing, bool flagReplaceable, float playerYaw)
        {
            WorldBase world = player.GetWorld();
            if (world.IsRemote || world is not WorldServer worldServer || player is not PlayerServer playerServer) return false;
            BlockBase? tilled = _Tilled(worldServer.GetBlockState(blockPos).GetBlock());
            if (tilled == null) return false;
            worldServer.SetBlockState(blockPos, new BlockState(tilled.IndexBlock), BlockFlag.cParticle);
            ItemWear.DamageHeld(playerServer, 1);
            return true;
        }

        /// <summary>
        /// Блок после вспашки или null, если этот блок не пашется
        /// </summary>
        private static BlockBase? _Tilled(BlockBase block)
        {
            if (block == BlocksRegMvk.Turf) return BlocksRegMvk.Humus;
            if (block == BlocksRegMvk.TurfLoam) return BlocksRegMvk.Loam;
            return null;
        }
    }
}
