"""[ПОВЕДЕНИЕ] Режим генерации Parallel для острова (включаемый, по умолчанию Original, см. gen_parallel.py).
В режиме Parallel генератор в начале рельефа засевает свой Rand от зерна и координат чанка: трава рельефа
(BiomeIsland._GenTallGrass) больше не зависит от декора предыдущих чанков (improvements R24), и рельеф можно
считать несколькими генераторами в разных потоках. В режиме Original код рельефа не меняется."""
import os
from portlib import load, save, rep, add_using, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"World\Gen\ChunkProviderGenerateIsland.cs"
t = load(rel, GAME)
t = rep(t, "    public class ChunkProviderGenerateIsland : IChunkProviderGenerate, IGenTree",
        "    public class ChunkProviderGenerateIsland : IChunkProviderGenerateParallel, IGenTree", rel, 1)
t = rep(t, """        public ChunkProviderGenerateIsland(byte numberChunkSections, long seed)
        {
            Seed = seed;""", """        /// <summary>
        /// Режим генерации Parallel: Rand рельефа засевается от зерна и координат чанка
        /// </summary>
        public bool IsParallel { get; }

        /// <param name="parallel">Режим генерации Parallel (EnumGenerationMode.Parallel)</param>
        public ChunkProviderGenerateIsland(byte numberChunkSections, long seed, bool parallel = false)
        {
            IsParallel = parallel;
            Seed = seed;""", rel, 1)
t = rep(t, """                ChunkPrimer.Clear();

                // Шум для бедрока""", """                ChunkPrimer.Clear();
                if (IsParallel)
                {
                    // Рельеф чанка зависит только от зерна и координат, а не от прошлых чанков этого генератора
                    Rnd.SetSeed(Seed ^ (chunk.CurrentChunkX * 341873128712L + chunk.CurrentChunkY * 132897987541L));
                }

                // Шум для бедрока""", rel, 1)
t = rep(t, """        public void Decoration(ChunkProviderServer provider, ChunkServer chunk)""", """        /// <summary>
        /// Генератор того же мира со своими буферами для рабочего потока рельефа
        /// </summary>
        public IChunkProviderGenerate CreateParallelWorker()
            => new ChunkProviderGenerateIsland(_numberChunkSections, Seed, IsParallel);

        public void Decoration(ChunkProviderServer provider, ChunkServer chunk)""", rel, 1)
save(rel, t, GAME)

rel = r"World\WorldSettingsIsland.cs"
t = load(rel, GAME)
t = rep(t, """            ChunkGenerate = new ChunkProviderGenerateIsland(NumberChunkSections, server.Settings.Seed);""",
        """            ChunkGenerate = new ChunkProviderGenerateIsland(NumberChunkSections, server.Settings.Seed,
                server.Settings.GenerationMode == EnumGenerationMode.Parallel);""", rel, 1)
t = add_using(t, "Mvk.Engine.World.Gen")
save(rel, t, GAME)
print("game_seam8_parallel ok")
