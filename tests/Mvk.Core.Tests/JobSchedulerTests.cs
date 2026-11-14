using System;
using System.Threading;
using Mvk.Core.Util;

namespace Mvk.Core.Tests
{
    /// <summary>
    /// Планировщик задач этапа 7: каждый элемент выполняется ровно один раз, номера потоков в пределах,
    /// исключение доходит до вызывающего, повторный вызов без аллокаций в вызывающем потоке
    /// </summary>
    public class JobSchedulerTests
    {
        private sealed class CountJob : IParallelJob
        {
            public readonly int[] Hits;
            public readonly int[] Workers;
            public int MaxWorker;
            public int ThrowAt = -1;

            public CountJob(int count)
            {
                Hits = new int[count];
                Workers = new int[count];
            }

            public void Execute(int index, int worker)
            {
                if (index == ThrowAt) throw new InvalidOperationException("ошибка " + index);
                Interlocked.Increment(ref Hits[index]);
                Workers[index] = worker;
                int max;
                while ((max = MaxWorker) < worker && Interlocked.CompareExchange(ref MaxWorker, worker, max) != max) { }
            }
        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(4, 1)]
        [InlineData(4, 1000)]
        public void EveryIndexOnce(int workers, int count)
        {
            using var scheduler = new JobScheduler(workers, "Тест");
            for (int round = 0; round < 20; round++)
            {
                var job = new CountJob(count);
                scheduler.ParallelFor(count, job);
                Assert.All(job.Hits, h => Assert.Equal(1, h));
                Assert.InRange(job.MaxWorker, 0, workers - 1);
            }
        }

        [Fact]
        public void ExceptionReachesCaller()
        {
            using var scheduler = new JobScheduler(3, "Тест");
            var job = new CountJob(200) { ThrowAt = 77 };
            var error = Assert.Throws<AggregateException>(() => scheduler.ParallelFor(200, job));
            Assert.IsType<InvalidOperationException>(error.InnerException);
            // Остальные элементы выполнены, планировщик работает дальше
            Assert.Equal(199, job.Hits.Sum());
            var next = new CountJob(50);
            scheduler.ParallelFor(50, next);
            Assert.All(next.Hits, h => Assert.Equal(1, h));
        }

        [Fact]
        public void NoAllocationPerCall()
        {
            using var scheduler = new JobScheduler(4, "Тест");
            var job = new CountJob(256);
            // Прогрев: события синхронизации лениво создают объект ядра при первом настоящем ожидании
            for (int i = 0; i < 300; i++) scheduler.ParallelFor(256, job);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) scheduler.ParallelFor(256, job);
            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }
    }

    internal static class ArraySum
    {
        public static int Sum(this int[] values)
        {
            int sum = 0;
            foreach (int v in values) sum += v;
            return sum;
        }
    }
}
