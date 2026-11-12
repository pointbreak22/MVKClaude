using Mvk.Game.World.Biome;
using Mvk.Engine.World.Chunk;
using Mvk.Core.Maths;

namespace Mvk.Game
{
    /// <summary>
    /// Отладочный класс для Малювек
    /// </summary>
    public class DebugMvk
    {


        /// <summary>
        /// Для определения параметров блока чанк и локальные координаты блока
        /// </summary>
        public static string ToBlockInfo(ChunkBase chunk, Vector3i pos)
        {
            if (pos.Y > chunk.Settings.NumberBlocks) return "";
            ChunkStorage storage = chunk.StorageArrays[pos.Y >> 4];
            int index = (pos.Y & 15) << 8 | pos.Z << 4 | pos.X;
            return string.Format("[{2}] b{0} s{1} {3}",
                storage.Light[index] >> 4, storage.Light[index] & 15, pos,
                    "Biom: " + (EnumBiomeIsland)chunk.Biome[pos.Z << 4 | pos.X]);
        }
    }
}
