using System;
using System.Diagnostics;
using System.Globalization;
using Mvk.Engine.Util;

namespace Mvk.Desktop
{
    /// <summary>
    /// Замер этапа 7 (ключ --bench): загрузка мира и кадр на неподвижной сцене.
    /// 1) Загрузка: от входа в мир до момента, когда сетки чанков перестали строиться 3 секунды подряд;
    ///    каждую секунду в журнал число чанков по этапам (ChunkStats) и кадров.
    /// 2) Кадр: 5 секунд на готовом мире, среднее время кадра в потоке окна и число кадров в секунду.
    /// 3) Перестройка всех сеток на готовом мире и свёртка вершин (MeshDigest): одинакова, если сетки совпали.
    /// В конце отчёт ChunkStats (время рельефа, декора, света, сеток на чанк).
    /// </summary>
    internal sealed class BenchRun
    {
        private const double _StableSeconds = 3;
        private const double _FrameSeconds = 5;

        private readonly double _limitSeconds;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private double _nextLog = 1;
        private long _framesSecond;
        private long _meshPrev = -1;
        private double _meshChanged;
        private double _loadedAt = -1;
        private double _frameStart;
        private long _frames;
        private double _frameMs;
        private readonly long _allocStart = GC.GetTotalAllocatedBytes(false);
        private readonly int _gen0Start = GC.CollectionCount(0);
        private readonly int _gen1Start = GC.CollectionCount(1);
        private readonly int _gen2Start = GC.CollectionCount(2);
        private long _allocLoad;
        private int _gen0Load, _gen1Load, _gen2Load;
        private readonly Action _rerenderAll;
        /// <summary>Идёт фаза перестройки всех сеток (свёртка сеток на готовом мире)</summary>
        private bool _remeshing;
        private double _remeshStart;
        private long _remeshMeshStart;

        /// <summary>Замер закончен, отчёт напечатан</summary>
        public bool Done { get; private set; }

        /// <param name="limitSeconds">Наибольшая длительность замера</param>
        /// <param name="rerenderAll">Пометить все чанки вокруг игрока к перестройке сеток</param>
        public BenchRun(double limitSeconds, Action rerenderAll)
        {
            _limitSeconds = limitSeconds;
            _rerenderAll = rerenderAll;
            ChunkStats.Reset();
            Console.WriteLine("Замер: вход в мир");
        }

        /// <summary>
        /// Кадр нарисован за frameMs миллисекунд потока окна
        /// </summary>
        public void Frame(double frameMs)
        {
            if (Done) return;
            double t = _clock.Elapsed.TotalSeconds;
            _framesSecond++;
            long mesh = ChunkStats.Count(ChunkStats.Stage.Mesh) + ChunkStats.Count(ChunkStats.Stage.MeshAlpha);
            if (mesh != _meshPrev)
            {
                _meshPrev = mesh;
                _meshChanged = t;
            }

            if (_loadedAt < 0)
            {
                if (t >= _nextLog)
                {
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0,4:F0} с: рельеф {1}, декор {2}, свет {3}/{4}, сетки {5}+{6}, кадров {7}",
                        t, ChunkStats.Count(ChunkStats.Stage.Relief), ChunkStats.Count(ChunkStats.Stage.Decoration),
                        ChunkStats.Count(ChunkStats.Stage.HeightMap), ChunkStats.Count(ChunkStats.Stage.SideLight),
                        ChunkStats.Count(ChunkStats.Stage.Mesh), ChunkStats.Count(ChunkStats.Stage.MeshAlpha),
                        _framesSecond));
                    _framesSecond = 0;
                    _nextLog += 1;
                }
                if (mesh > 0 && t - _meshChanged >= _StableSeconds || t >= _limitSeconds)
                {
                    _loadedAt = _meshChanged;
                    _frameStart = t;
                    _allocLoad = GC.GetTotalAllocatedBytes(false) - _allocStart;
                    _gen0Load = GC.CollectionCount(0) - _gen0Start;
                    _gen1Load = GC.CollectionCount(1) - _gen1Start;
                    _gen2Load = GC.CollectionCount(2) - _gen2Start;
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "Мир загружен за {0:F2} с (последняя сетка), чанков с рельефом {1}, сеток {2}",
                        _loadedAt, ChunkStats.Count(ChunkStats.Stage.Relief), mesh));
                }
                return;
            }

            if (_remeshing)
            {
                // 3) Все сетки перестроены на готовом мире: свёртка одинакова, если сетки совпали байт в байт
                if (mesh > _remeshMeshStart && t - _meshChanged >= _StableSeconds || t - _remeshStart >= _limitSeconds)
                {
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "Перестройка всех сеток: {0} сеток за {1:F2} с", mesh - _remeshMeshStart, _meshChanged - _remeshStart));
                    Console.WriteLine("Свёртка сеток: " + Mvk.Engine.Renderer.World.MeshDigest.Report());
                    Done = true;
                }
                return;
            }

            _frames++;
            _frameMs += frameMs;
            if (t - _frameStart >= _FrameSeconds)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "Кадр: {0:F3} мс в потоке окна, {1:F1} кадров/с",
                    _frameMs / _frames, _frames / (t - _frameStart)));
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "Память при загрузке: выделено {0:F1} МБ, сборок Gen0 {1}, Gen1 {2}, Gen2 {3}",
                    _allocLoad / 1048576.0, _gen0Load, _gen1Load, _gen2Load));
                Console.WriteLine("Этапы: " + ChunkStats.Report());
                // Перестроить все сетки на готовом мире (свет и соседи уже на месте) и посчитать свёртку
                Mvk.Engine.Renderer.World.MeshDigest.Clear();
                _remeshing = true;
                _remeshStart = t;
                _remeshMeshStart = mesh;
                _meshChanged = t;
                _rerenderAll();
            }
        }
    }
}
