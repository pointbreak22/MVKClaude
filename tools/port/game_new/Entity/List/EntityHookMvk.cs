#nullable enable
using System;
using Mvk.Core.Maths;
using Mvk.Core.Util;
using Mvk.Engine.Entity;
using Mvk.Engine.Item;
using Mvk.Engine.World.Block;
using Mvk.Game.Item;
using Mvk.Game.World.Block;
using Mvk.Game.World.Block.List;

namespace Mvk.Game.Entity.List
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Брошенный крюк с верёвкой: при попадании в боковую грань или потолок блока цепляется, и от него
    /// вниз свешивается верёвка (блок «Верёвка», по ней лезут), длиной в число верёвок у бросившего (не больше
    /// MaxLength, в творческом MaxLength), верёвки тратятся. Крюк возвращается бросившему. На верх блока крюк не
    /// цепляется и остаётся лежать предметом. Всё решает сервер.
    /// </summary>
    public class EntityHookMvk : EntityProjectileMvk
    {
        public const string AliasHook = "HookThrown";

        /// <summary>
        /// Наибольшая длина верёвки в блоках
        /// </summary>
        public const int MaxLength = 24;

        protected override void _OnImpact(EntityLiving? entity, Vector3 pos, float hitY, EntityLiving? thrower)
        {
            if (entity != null || thrower is not PlayerServerMvk player) return;
            // Клетка крюка — воздух; опора — соседний полный блок: сначала по направлению полёта, затем по бокам и сверху
            BlockPos air = new BlockPos(PosX, PosY + .1f, PosZ);
            if (!_worldServer.GetBlockState(air).IsAir()) return;
            float mx = Physics.MotionX, mz = Physics.MotionZ;
            Pole[] order = Math.Abs(mx) > Math.Abs(mz)
                ? new[] { mx > 0 ? Pole.East : Pole.West, mz > 0 ? Pole.South : Pole.North, mz > 0 ? Pole.North : Pole.South, mx > 0 ? Pole.West : Pole.East, Pole.Up }
                : new[] { mz > 0 ? Pole.South : Pole.North, mx > 0 ? Pole.East : Pole.West, mx > 0 ? Pole.West : Pole.East, mz > 0 ? Pole.North : Pole.South, Pole.Up };
            Pole side = Pole.All;
            foreach (Pole toward in order)
            {
                BlockPos solid = toward switch
                {
                    Pole.East => air.OffsetEast(),
                    Pole.West => air.OffsetWest(),
                    Pole.South => air.OffsetSouth(),
                    Pole.North => air.OffsetNorth(),
                    _ => air.OffsetUp()
                };
                if (!_worldServer.GetBlockState(solid).GetBlock().FullBlock) continue;
                // Сторона опоры, обращённая к крюку
                side = toward switch
                {
                    Pole.East => Pole.West,
                    Pole.West => Pole.East,
                    Pole.South => Pole.North,
                    Pole.North => Pole.South,
                    _ => Pole.Down
                };
                break;
            }
            if (side == Pole.All) return;

            BlockRope rope = BlocksRegMvk.Rope;
            BlockState state = rope.OnBlockPlaced(_worldServer, air, new BlockState(), side, new Vector3(.5f), player.RotationYaw);
            if (!rope.CanBlockStay(_worldServer, null!, air, state.Met)) return;
            int count = player.CreativeMode ? MaxLength
                : Math.Min(MaxLength, ItemAmmo.Count(player, item => item == ItemsRegMvk.Rope));
            if (count == 0) return;
            int placed = 0;
            BlockPos at = air;
            while (placed < count && at.Y > 2 && _worldServer.GetBlockState(at).IsAir())
            {
                if (!_worldServer.SetBlockState(at, state, BlockFlag.cParticle)) break;
                placed++;
                at.Y--;
            }
            if (placed == 0) return;
            if (!player.CreativeMode)
            {
                for (int i = 0; i < placed; i++)
                {
                    int slot = ItemAmmo.Find(player, item => item == ItemsRegMvk.Rope);
                    if (slot < 0) break;
                    ItemAmmo.ConsumeOne(player, slot);
                }
            }
            // Крюк сматывается обратно бросившему
            ItemStack hook = GetEntityItemStack();
            SetDead();
            if (hook != null && !player.Inventory.AddItemStackToInventory(hook.Copy())) player.DropItem(hook.Copy(), true, false);
        }
    }
}
