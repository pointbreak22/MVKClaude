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
        internal const string EntityClothingLayersObjectIsMissing = "Отсутствует объект слоёв одежды [{0}] у сущности [{1}].";
        internal const string TheSymbolIsNotInTheList = "Символ [{0}] отсутствует в перечне";
        internal const string ClosingTheApplication = "Закрытие приложения";
        internal const string TheUserStoppedTheGame = "Пользователь остановил игру";
        internal const string BufferItemIsMissing = "Отсутствует буфер сетки, предмет {0}";

        public static string GetString(string name, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, name, args);
    }
}
