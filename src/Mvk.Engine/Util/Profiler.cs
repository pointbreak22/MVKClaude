using Mvk.Core.Util;
using System;
using System.Diagnostics;

namespace Mvk.Engine.Util
{
    /// <summary>
    /// Объект сыщик, замеряющий и фиксирующий медленный код
    /// </summary>
    public class Profiler
    {

#if DEBUG
        private const int _stepTime = 10;
        public const int StepTime = 25;
#else
        private const int _stepTime = 50;
        public const int StepTime = 60;
#endif
        /// <summary>
        /// Объект лога
        /// </summary>
        public readonly Logger Log;
        /// <summary>
        /// Получает частоту таймера в виде количества тактов в милисекунду
        /// </summary>
        private static readonly long _timerFrequency = Stopwatch.Frequency / 1000;
        /// <summary>
        /// Оглавление перед сообщением
        /// </summary>
        private readonly string _prefix;

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private string _profilingSection;
        /// <summary>
        /// Части имени секции, строка собирается только для лога:
        /// 0 только имя, 1 имя и строка, 2 имя и два числа через двоеточие
        /// </summary>
        private int _sectionParts;
        private string _sectionText;
        private int _sectionX, _sectionY;
        private bool _profilingEnabled = false;

        public Profiler(Logger log, string prefix = "")
        {
            Log = log;
            _prefix = prefix;
            _stopwatch.Start();
        }

        /// <summary>
        /// Начало секции
        /// </summary>
        /// <param name="name">Название секции</param>
        public void StartSection(string name)
        {
            _profilingSection = name;
            _sectionParts = 0;
            _profilingEnabled = true;
            _stopwatch.Restart();
        }

        /// <summary>
        /// Начало секции с именем name + text, строка не собирается без записи в лог
        /// </summary>
        public void StartSection(string name, string text)
        {
            StartSection(name);
            _sectionParts = 1;
            _sectionText = text;
        }

        /// <summary>
        /// Начало секции с именем name + x + ":" + y, строка не собирается без записи в лог
        /// </summary>
        public void StartSection(string name, int x, int y)
        {
            StartSection(name);
            _sectionParts = 2;
            _sectionX = x;
            _sectionY = y;
        }

        /// <summary>
        /// Полное имя секции для лога
        /// </summary>
        private string _SectionName() => _sectionParts switch
        {
            1 => _profilingSection + _sectionText,
            2 => _profilingSection + _sectionX + ":" + _sectionY,
            _ => _profilingSection
        };

        /// <summary>
        /// Закрыть проверку по времени
        /// </summary>
        public void EndSection(int stepTime = _stepTime)
        {
            if (_profilingEnabled)
            {
                _profilingEnabled = false;
                long time = _stopwatch.ElapsedMilliseconds;
                if (time > stepTime)
                {
                    Log.Log(Srl.SomethingIsTooLong, _prefix, _SectionName(), time);
                }
            }
        }

        /// <summary>
        /// Закрыть проверку по времени и указать обязательно время в мс 0.00
        /// </summary>
        public void EndSectionLog()
        {
            if (_profilingEnabled)
            {
                _profilingEnabled = false;
                Log.Log(Srl.EndSectionTime, _prefix,
                    _SectionName(), _stopwatch.ElapsedTicks / (float)_timerFrequency);
            }
        }

        /// <summary>
        /// Закрыть секцию и тут же открыть
        /// </summary>
        /// <param name="name">Название секции</param>
        public void EndStartSection(string name, int stepTime = _stepTime)
        {
            EndSection(stepTime);
            StartSection(name);
        }


        // ====== Static Console =======


        private static readonly Stopwatch _stopwatchStatic = new Stopwatch();

        public static void StartConsole()
        {
            _stopwatchStatic.Restart();
        }

        public static void StopConsole(string name)
        {
            _stopwatchStatic.Stop();
            Console.WriteLine(name + " " + _stopwatchStatic.ElapsedTicks / (float)_timerFrequency);
        }

    }
}
