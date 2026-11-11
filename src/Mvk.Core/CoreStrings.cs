using System.Globalization;

namespace Mvk.Core
{
    /// <summary>
    /// Тексты ошибок Core (перенесены из Vge/SR.cs без изменения текста)
    /// </summary>
    internal static class CoreStrings
    {
        internal const string AddingInappropriateTagTypesToList = "Добавление несоответствующих типов тегов в список тегов";
        internal const string EmptyStringIsNotAllowed = "Пустая строка не разрешена";
        internal const string IndexOutOfBoundsToSetTagInTagList = "Индекс выходит за пределы, чтобы установить тег в списке тегов";
        internal const string InvalidEndJsonReadTag = "Некорректный тег чтения Json, отсутствует завершение {0} файла: {1}";
        internal const string InvalidJsonReadTag = "Некорректный тег чтения Json, символ {0} файла: {1}";
        internal const string ItIsImpossibleToCompareTwoObjects = "Невозможно сравнить два объекта";
        internal const string OutOfRangeArray = "Вне диапазона массива, количество {0}";
        internal const string TheValueMustBeGreaterThanZero = "Значение должно быть больше нуля";

        public static string GetString(string name, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, name, args);
    }
}
