using Mvk.Core.Util;
using Mvk.Engine.Util;
using Mvk.Engine.World.Block.List;

namespace Mvk.Engine.World.Block
{
    /// <summary>
    /// Таблица блоков для регистрации
    /// </summary>
    public class BlockRegTable : RegTable<BlockBase>
    {
        protected override BlockBase _CreateNull() => new BlockNull();
    }
}
