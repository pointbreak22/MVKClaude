using Mvk.Core.Util;
using Mvk.Engine.Util;
using Mvk.Engine.World.Chunk;

namespace Mvk.Engine.World.Gen
{
    /// <summary>
    /// Интерфейс генерации особенностей, не выходящего за чанк
    /// </summary>
    public interface IFeatureGeneratorColumn
    {
        /// <summary>
        /// Декорация блока или столба не выходящего за чанк
        /// </summary>
        void DecorationsColumn(ChunkServer chunkSpawn, Rand rand);
    }

    /// <summary>
    /// Интерфейс генерации особенностей, которые могу выйти за 1 чанк
    /// </summary>
    public interface IFeatureGeneratorArea
    {
        /// <summary>
        /// Декорация областей которые могу выйти за 1 чанк
        /// </summary>
        void DecorationsArea(ChunkServer chunkSpawn, Rand rand, int biasX, int biasZ);
    }
}
