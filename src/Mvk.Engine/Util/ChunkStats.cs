using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Mvk.Engine.Util
{
    /// <summary>
    /// Время этапов жизни чанка для замеров этапа 7 (Mvk.Desktop --bench): загрузка с диска, рельеф, декор,
    /// карта высот с небесным светом, боковой свет, сетки клиента. Счётчики общие для всех потоков,
    /// запись без блокировок и без аллокаций (Interlocked), на поведение не влияет.
    /// </summary>
    public static class ChunkStats
    {
        /// <summary>Этап жизни чанка</summary>
        public enum Stage
        {
            /// <summary>Чтение с диска</summary>
            Load,
            /// <summary>Генерация рельефа</summary>
            Relief,
            /// <summary>Декор 3x3</summary>
            Decoration,
            /// <summary>Карта высот и вертикальный небесный свет</summary>
            HeightMap,
            /// <summary>Боковой небесный и блочный свет</summary>
            SideLight,
            /// <summary>Сетка сплошных блоков на клиенте</summary>
            Mesh,
            /// <summary>Сетка альфа блоков на клиенте</summary>
            MeshAlpha
        }

        private const int _Count = 7;
        private static readonly long[] _ticks = new long[_Count];
        private static readonly long[] _counts = new long[_Count];

        /// <summary>Начало замера: метка времени</summary>
        public static long Begin() => Stopwatch.GetTimestamp();

        /// <summary>Конец замера этапа</summary>
        public static void End(Stage stage, long begin)
        {
            Interlocked.Add(ref _ticks[(int)stage], Stopwatch.GetTimestamp() - begin);
            Interlocked.Increment(ref _counts[(int)stage]);
        }

        /// <summary>Сколько раз выполнен этап</summary>
        public static long Count(Stage stage) => Interlocked.Read(ref _counts[(int)stage]);

        /// <summary>Сколько миллисекунд занял этап всего</summary>
        public static double TotalMs(Stage stage)
            => Interlocked.Read(ref _ticks[(int)stage]) * 1000.0 / Stopwatch.Frequency;

        /// <summary>Обнулить счётчики</summary>
        public static void Reset()
        {
            for (int i = 0; i < _Count; i++)
            {
                Interlocked.Exchange(ref _ticks[i], 0);
                Interlocked.Exchange(ref _counts[i], 0);
            }
        }

        /// <summary>
        /// Отчёт: этап, число, всего мс, среднее мс
        /// </summary>
        public static string Report()
        {
            var sb = new StringBuilder();
            foreach (Stage stage in Enum.GetValues<Stage>())
            {
                long count = Count(stage);
                double ms = TotalMs(stage);
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1} шт, {2:F0} мс, {3:F3} мс/шт; ",
                    stage, count, ms, count > 0 ? ms / count : 0);
            }
            return sb.ToString();
        }
    }
}
