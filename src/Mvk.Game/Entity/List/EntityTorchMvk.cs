#nullable enable
using Mvk.Core.Maths;
using Mvk.Engine.Entity;
using Mvk.Engine.World.Block;
using Mvk.Game.World.Block;

namespace Mvk.Game.Entity.List
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Брошенный факел: при попадании в блок или землю ставит факел-блок там, где остановился (если место
    /// свободно), иначе остаётся лежать предметом. По мобу бьёт как камень. Всё решает сервер.
    /// </summary>
    public class EntityTorchMvk : EntityProjectileMvk
    {
        public const string AliasTorch = "TorchThrown";

        protected override void _OnImpact(EntityLiving? entity, Vector3 pos, float hitY, EntityLiving? thrower)
        {
            if (entity != null)
            {
                base._OnImpact(entity, pos, hitY, thrower);
                return;
            }
            BlockPos blockPos = new BlockPos(PosX, PosY + .1f, PosZ);
            BlockState state = _worldServer.GetBlockState(blockPos);
            if (!state.IsAir() && !state.GetBlock().IsReplaceable) return;
            _worldServer.SetBlockState(blockPos, new BlockState(BlocksRegMvk.Torch.IndexBlock), BlockFlag.cNotify | BlockFlag.Sound);
            SetDead();
        }
    }
}
