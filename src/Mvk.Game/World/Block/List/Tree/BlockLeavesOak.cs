using Mvk.Engine.World.Block;

namespace Mvk.Game.World.Block.List
{
    /// <summary>
    /// Блок листвы дуба
    /// </summary>
    public class BlockLeavesOak : BlockLeaves
    {
        public BlockLeavesOak(MaterialBase material) : base(material) { }

        /// <summary>
        /// Дополнительная инициализация блока после инициализации предметов и корректировки id блоков
        /// </summary>
        public override void InitAfterItems()
        {
            _idLog = BlocksRegMvk.LogOak.IndexBlock;
            _idBranch = BlocksRegMvk.BranchOak.IndexBlock;
            _idFetus = BlocksRegMvk.FetusOak.IndexBlock;
        }
    }
}
