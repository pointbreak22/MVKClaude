#nullable enable
using System.Collections.Generic;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;
using Mvk.Engine.World.Block;

namespace Mvk.Engine.World
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Свет предмета в руке (факел): освещение движка считается по блокам, поэтому сервер ставит в клетку
    /// головы игрока невидимый светящийся блок (BlockId задаёт мод при регистрации) и переносит его при движении.
    /// Действующие клетки учитываются здесь; блок без хозяина сам гаснет по случайному такту (остался после выхода
    /// игрока или перезапуска).
    /// </summary>
    public static class HandLight
    {
        /// <summary>
        /// Невидимый светящийся блок (задаёт мод; null — мод свет в руке не поддерживает)
        /// </summary>
        public static BlockBase? Block;

        /// <summary>
        /// Id светящегося блока (0 — нет)
        /// </summary>
        public static int BlockId => Block?.IndexBlock ?? 0;

        private static readonly HashSet<(byte, int, int, int)> _active = new HashSet<(byte, int, int, int)>();

        /// <summary>
        /// Поддерживает ли эту клетку игрок
        /// </summary>
        public static bool IsActive(WorldServer world, BlockPos pos)
        {
            lock (_active) return _active.Contains((world.IdWorld, pos.X, pos.Y, pos.Z));
        }

        /// <summary>
        /// Такт игрока: перенести, поставить или убрать свет; pos — текущая клетка света игрока или null
        /// </summary>
        public static void Update(PlayerServer player, ref BlockPos? pos)
        {
            WorldServer world = player.GetWorldServer();
            if (BlockId == 0 || world == null) return;
            ItemStack right = player.Inventory.GetCurrentRightItem();
            ItemStack left = player.Inventory.GetCurrentLeftItem();
            bool lit = !player.IsSpectator() && !player.IsDead
                && ((right != null && right.Item.HandLight > 0) || (left != null && left.Item.HandLight > 0));
            BlockPos? target = null;
            if (lit)
            {
                BlockPos head = new BlockPos(player.PosX, player.PosY + player.GetEyeHeight(), player.PosZ);
                BlockState state = world.GetBlockState(head);
                if (state.IsAir() || state.Id == BlockId) target = head;
                else
                {
                    // Голова в блоке (проход, жидкость): свет в клетке ног, если она свободна
                    BlockPos feet = new BlockPos(player.PosX, player.PosY + .1f, player.PosZ);
                    BlockState feetState = world.GetBlockState(feet);
                    if (feetState.IsAir() || feetState.Id == BlockId) target = feet;
                }
            }
            if (pos is BlockPos old && (target is not BlockPos t || !t.Equals(old)))
            {
                _Off(world, old);
                pos = null;
            }
            if (target is BlockPos tp && world.GetBlockState(tp).Id != BlockId)
            {
                world.SetBlockState(tp, new BlockState(BlockId), BlockFlag.None);
                lock (_active) _active.Add((world.IdWorld, tp.X, tp.Y, tp.Z));
            }
            if (target is BlockPos tp2) pos = tp2;
        }

        /// <summary>
        /// Убрать свет игрока (выход, смерть)
        /// </summary>
        public static void Off(PlayerServer player, ref BlockPos? pos)
        {
            WorldServer world = player.GetWorldServer();
            if (pos is BlockPos old && world != null) _Off(world, old);
            pos = null;
        }

        private static void _Off(WorldServer world, BlockPos pos)
        {
            lock (_active) _active.Remove((world.IdWorld, pos.X, pos.Y, pos.Z));
            if (world.GetBlockState(pos).Id == BlockId) world.SetBlockState(pos, new BlockState(0), BlockFlag.None);
        }
    }
}
