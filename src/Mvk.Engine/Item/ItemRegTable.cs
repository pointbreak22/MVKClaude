using Mvk.Core.Util;
using Mvk.Engine.Util;

namespace Mvk.Engine.Item
{
    /// <summary>
    /// Таблица блоков для регистрации
    /// </summary>
    public class ItemRegTable : RegTable<ItemBase>
    {
        /// <summary>
        /// Создать пустой предмет
        /// </summary>
        protected override ItemBase _CreateNull() => new ItemBase();
    }
}
