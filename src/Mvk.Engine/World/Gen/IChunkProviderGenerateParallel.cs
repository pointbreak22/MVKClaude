#nullable enable
namespace Mvk.Engine.World.Gen
{
    /// <summary>
    /// Генератор, рельеф которого можно считать несколькими потоками (режим EnumGenerationMode.Parallel)
    /// </summary>
    public interface IChunkProviderGenerateParallel : IChunkProviderGenerate
    {
        /// <summary>
        /// Генерация в режиме Parallel: рельеф каждого чанка зависит только от зерна и координат
        /// </summary>
        bool IsParallel { get; }

        /// <summary>
        /// Новый генератор того же мира со своими буферами и шумами для рабочего потока рельефа
        /// </summary>
        IChunkProviderGenerate CreateParallelWorker();
    }
}
