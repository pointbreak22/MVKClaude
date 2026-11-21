#nullable enable
using System.Collections.Generic;
using Mvk.Core.Maths;
using Mvk.Core.Util;
using Mvk.Engine.Entity;
using Mvk.Engine.Util;
using Mvk.Engine.World;
using Mvk.Engine.World.Block;
using Mvk.Engine.World.Chunk;

namespace Mvk.Game.World.Block
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Огонь (в порте свой, по идее BlockFire старого проекта автора Mvk, упрощённо): блок без столкновений
    /// со светом 15. Держится на твёрдом блоке снизу или рядом с горючим, иначе гаснет. Каждые 1–2 с стареет (возраст
    /// в met), перекидывается на соседние горючие блоки (дерево, ветки, листва, растения, дверь, верёвка: соседний
    /// горючий блок сгорает и становится огнём) и жжёт живых внутри клетки. Догорев (возраст MaxAge), гаснет.
    /// Вода заменяет огонь, как любой заменяемый блок. Всё решает сервер.
    /// </summary>
    public class BlockFireMvk : BlockBase
    {
        /// <summary>
        /// Возраст, после которого огонь гаснет
        /// </summary>
        public const int MaxAge = 10;
        /// <summary>
        /// Шанс перекинуться на каждый горючий сосед за такт огня (1 из N)
        /// </summary>
        private const int _SpreadChance = 3;

        private static readonly List<EntityBase> _entities = new List<EntityBase>();

        public BlockFireMvk(MaterialBase material) : base(material) { }

        /// <summary>
        /// Горит ли блок
        /// </summary>
        public static bool IsBurnable(BlockBase block)
        {
            int m = block.Material.IndexMaterial;
            return m == (int)EnumMaterial.Wood || m == (int)EnumMaterial.Branch || m == (int)EnumMaterial.Leaves
                || m == (int)EnumMaterial.Plant || m == (int)EnumMaterial.Door || m == (int)EnumMaterial.Rope;
        }

        /// <summary>
        /// Может ли огонь быть в клетке: твёрдый блок снизу или горючий рядом
        /// </summary>
        public static bool CanBurnAt(WorldBase world, BlockPos pos)
        {
            if (world.GetBlockState(pos.OffsetDown()).GetBlock().FullBlock) return true;
            return _Burnable(world, pos.OffsetDown()) || _Burnable(world, pos.OffsetUp()) || _Burnable(world, pos.OffsetEast())
                || _Burnable(world, pos.OffsetWest()) || _Burnable(world, pos.OffsetNorth()) || _Burnable(world, pos.OffsetSouth());
        }

        private static bool _Burnable(WorldBase world, BlockPos pos)
        {
            BlockState state = world.GetBlockState(pos);
            return !state.IsAir() && IsBurnable(state.GetBlock());
        }

        public override void OnBlockAdded(WorldServer world, ChunkServer chunk, BlockPos blockPos, BlockState state)
            => world.SetBlockTick(blockPos, (uint)(20 + world.Rnd.Next(20)));

        public override void UpdateTick(WorldServer world, ChunkServer chunk, BlockPos blockPos, BlockState blockState, Rand random)
        {
            int age = blockState.Met & 15;
            if (!CanBurnAt(world, blockPos) || age >= MaxAge)
            {
                world.SetBlockToAir(blockPos);
                return;
            }
            // Перекинуться на горючих соседей: сосед сгорает и становится огнём
            _Spread(world, blockPos.OffsetUp(), random);
            _Spread(world, blockPos.OffsetDown(), random);
            _Spread(world, blockPos.OffsetEast(), random);
            _Spread(world, blockPos.OffsetWest(), random);
            _Spread(world, blockPos.OffsetNorth(), random);
            _Spread(world, blockPos.OffsetSouth(), random);
            _BurnEntities(world, blockPos);
            world.SetBlockState(blockPos, new BlockState(IndexBlock | (age + 1) << 12), BlockFlag.None);
            world.SetBlockTick(blockPos, (uint)(20 + random.Next(20)));
        }

        private void _Spread(WorldServer world, BlockPos pos, Rand random)
        {
            if (random.Next(_SpreadChance) != 0 || !_Burnable(world, pos)) return;
            world.SetBlockState(pos, new BlockState(IndexBlock), BlockFlag.cNotify);
        }

        /// <summary>
        /// Жечь живых в клетке огня
        /// </summary>
        private static void _BurnEntities(WorldServer world, BlockPos pos)
        {
            AxisAlignedBB aabb = new AxisAlignedBB(pos.X, pos.Y, pos.Z, pos.X + 1, pos.Y + 1, pos.Z + 1);
            world.Collision.EntityBoundingBoxesFromSector(aabb, -1);
            _entities.Clear();
            for (int i = 0; i < world.Collision.ListEntity.Count; i++) _entities.Add(world.Collision.ListEntity[i]);
            world.Collision.ListEntity.Clear();
            foreach (EntityBase entity in _entities)
            {
                if (entity is EntityLiving living && !living.IsDead)
                {
                    living.Damage?.OnAttack((byte)EnumDamageSource.InFire, -1, 1, null!);
                }
            }
            _entities.Clear();
        }
    }
}
