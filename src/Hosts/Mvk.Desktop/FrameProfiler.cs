using System;
using System.Diagnostics;

namespace Mvk.Desktop
{
    /// <summary>
    /// Замер аллокаций и времени кадра и такта в потоке окна (ключ --profile).
    /// Правило проекта: в горячих путях (такт, кадр, мешинг) аллокаций нет, этот замер их показывает.
    /// </summary>
    internal sealed class FrameProfiler
    {
        private long _updateBytes, _renderBytes, _updates, _frames;
        private double _updateMs, _renderMs;
        private long _totalStart;
        private int _gen0Start;
        private long _threadStart;
        private readonly Stopwatch _clock = new Stopwatch();
        private long _mark;

        /// <summary>Идёт ли замер</summary>
        public bool Running { get; private set; }

        public void Start()
        {
            Running = true;
            _totalStart = GC.GetTotalAllocatedBytes(false);
            _gen0Start = GC.CollectionCount(0);
            _threadStart = GC.GetAllocatedBytesForCurrentThread();
        }

        public void Begin()
        {
            _mark = GC.GetAllocatedBytesForCurrentThread();
            _clock.Restart();
        }

        public void EndUpdate()
        {
            _updateMs += _clock.Elapsed.TotalMilliseconds;
            _updateBytes += GC.GetAllocatedBytesForCurrentThread() - _mark;
            _updates++;
        }

        public void EndRender()
        {
            _renderMs += _clock.Elapsed.TotalMilliseconds;
            _renderBytes += GC.GetAllocatedBytesForCurrentThread() - _mark;
            _frames++;
        }

        public string Report()
        {
            long total = GC.GetTotalAllocatedBytes(false) - _totalStart;
            long thread = GC.GetAllocatedBytesForCurrentThread() - _threadStart;
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Профиль: кадров {0}, кадр {1:F2} мс и {2:F0} байт; вызовов Update {3}, Update {4:F2} мс и {5:F0} байт; " +
                "поток окна всего {8:F1} КБ, по процессу {6:F1} КБ, сборок Gen0 {7}",
                _frames, _renderMs / Math.Max(1, _frames), _renderBytes / (double)Math.Max(1, _frames),
                _updates, _updateMs / Math.Max(1, _updates), _updateBytes / (double)Math.Max(1, _updates),
                total / 1024.0, GC.CollectionCount(0) - _gen0Start, thread / 1024.0);
        }
    }
}
