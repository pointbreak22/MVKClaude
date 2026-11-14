using System;
using System.Threading;

namespace Mvk.Core.Util
{
    /// <summary>
    /// Работа для <see cref="JobScheduler.ParallelFor"/>: выполнить элемент index в рабочем потоке worker
    /// (0 — поток, который вызвал ParallelFor). Номер потока нужен, чтобы у каждого потока были свои буферы.
    /// </summary>
    public interface IParallelJob
    {
        void Execute(int index, int worker);
    }

    /// <summary>
    /// Планировщик задач этапа 7: постоянные рабочие потоки и параллельный цикл по элементам пакета.
    /// Вызов ParallelFor не выделяет память: потоки создаются один раз, элементы раздаются счётчиком
    /// Interlocked, вызывающий поток работает наравне с остальными и ждёт их в конце.
    /// Одновременно выполняется один ParallelFor (у планировщика один владелец).
    /// </summary>
    public sealed class JobScheduler : IDisposable
    {
        /// <summary>
        /// Потоков на пакет по умолчанию: два ядра оставляем потоку окна и потоку сервера, не больше 4
        /// </summary>
        public static int DefaultWorkers => Math.Clamp(Environment.ProcessorCount - 2, 1, 4);

        /// <summary>Сколько потоков работает над пакетом, вместе с вызывающим</summary>
        public int WorkerCount { get; }

        private readonly Thread[] _threads;
        private readonly SemaphoreSlim _wake = new SemaphoreSlim(0);
        /// <summary>Последний закончивший помощник отпускает вызывающий поток (объект блокировки создан сразу)</summary>
        private readonly SemaphoreSlim _helpersDone = new SemaphoreSlim(0);
        private IParallelJob? _job;
        private int _count;
        private int _next;
        private int _helpersRunning;
        private Exception? _error;
        private volatile bool _stop;

        /// <param name="workerCount">Потоков на пакет вместе с вызывающим, не меньше 1</param>
        /// <param name="name">Имя рабочих потоков (видно в отладчике и профайлере)</param>
        public JobScheduler(int workerCount, string name)
        {
            WorkerCount = Math.Max(1, workerCount);
            _threads = new Thread[WorkerCount - 1];
            for (int i = 0; i < _threads.Length; i++)
            {
                int worker = i + 1;
                _threads[i] = new Thread(() => _HelperLoop(worker))
                {
                    Name = name + " " + worker,
                    IsBackground = true
                };
                _threads[i].Start();
            }
        }

        /// <summary>
        /// Выполнить job.Execute для каждого index от 0 до count - 1 на всех потоках планировщика.
        /// Возвращает управление, когда все элементы выполнены; первое исключение работы пробрасывается.
        /// </summary>
        public void ParallelFor(int count, IParallelJob job)
        {
            if (count <= 0) return;
            if (_threads.Length == 0 || count == 1)
            {
                for (int i = 0; i < count; i++) job.Execute(i, 0);
                return;
            }
            _job = job;
            _count = count;
            _next = -1;
            _error = null;
            _helpersRunning = _threads.Length;
            _wake.Release(_threads.Length);
            _Drain(0);
            _helpersDone.Wait();
            _job = null;
            if (_error != null)
            {
                Exception error = _error;
                _error = null;
                throw new AggregateException(error);
            }
        }

        private void _HelperLoop(int worker)
        {
            while (true)
            {
                _wake.Wait();
                if (_stop) return;
                _Drain(worker);
                if (Interlocked.Decrement(ref _helpersRunning) == 0) _helpersDone.Release();
            }
        }

        private void _Drain(int worker)
        {
            IParallelJob? job = _job;
            if (job == null) return;
            int index;
            while ((index = Interlocked.Increment(ref _next)) < _count)
            {
                try
                {
                    job.Execute(index, worker);
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref _error, ex, null);
                }
            }
        }

        public void Dispose()
        {
            _stop = true;
            if (_threads.Length > 0) _wake.Release(_threads.Length);
            foreach (Thread thread in _threads) thread.Join(1000);
            _wake.Dispose();
            _helpersDone.Dispose();
        }
    }
}
