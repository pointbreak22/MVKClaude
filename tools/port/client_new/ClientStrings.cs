using System.Globalization;

namespace Mvk.Engine
{
    /// <summary>
    /// Строки клиента движка (выделено из Vge/Sr.cs: эти строки нужны только коду рендера)
    /// </summary>
    internal static class ClientStrings
    {
        internal const string ItIsImpossibleToCompareTwoObjects = "Невозможно сравнить два объекта";
        internal const string OutOfRangeArray = "Вне диапазона массива, количество {0}";

        public static string GetString(string name, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, name, args);
    }
}
