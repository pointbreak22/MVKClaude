using Mvk.Engine.World.Block;

namespace Mvk.Game.World.Block.List
{
    /// <summary>
    /// Блок листвы хаойных
    /// </summary>
    public class BlockLeavesConifer : BlockLeaves
    {
        public BlockLeavesConifer(MaterialBase material) : base(material) { }

        /// <summary>
        /// Дополнительная инициализация блока после инициализации предметов и корректировки id блоков
        /// </summary>
        public override void InitAfterItems()
        {
            _idLog = BlocksRegMvk.LogConifer.IndexBlock;
            _idBranch = BlocksRegMvk.BranchConifer.IndexBlock;
            _idFetus = BlocksRegMvk.FetusConifer.IndexBlock;
        }
    }
}
