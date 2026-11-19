"""[ПОВЕДЕНИЕ] включаемый режим генерации Parallel (по умолчанию Original, мир 1:1 с оригиналом).
- Режим мира (EnumGenerationMode) хранится в сохранении игры (тег GenMode); новый мир берёт его из опции
  GenerationParallel, старые миры без тега остаются Original.
- Если генератор мира в режиме Parallel (IChunkProviderGenerateParallel.IsParallel), ChunkProviderServer считает
  рельеф пакета чанков пулом потоков (JobScheduler, Options.GenerationThreads, 0 — по числу ядер):
  у каждого потока свой генератор (CreateParallelWorker). Загрузка с диска до рельефа и ChunkPresent (декор)
  после него идут последовательно в порядке очереди."""
from portlib import load, save, rep, add_using

# Опции
rel = r"Util\Options.cs"
t = load(rel)
t = rep(t, """        public static bool BlockPlacementPreview = true;""", """        public static bool BlockPlacementPreview = true;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Новые миры генерируются в режиме Parallel (рельеф пулом потоков, мир не как в оригинале).
        /// Режим запоминается в сохранении мира, на старые миры опция не влияет
        /// </summary>
        public static bool GenerationParallel = false;
        /// <summary>
        /// Потоков рельефа в режиме Parallel, 0 — по числу ядер (JobScheduler.DefaultWorkers)
        /// </summary>
        public static int GenerationThreads = 0;""", rel, 1)
save(rel, t)

rel = r"Util\OptionsFile.cs"
t = load(rel)
t = rep(t, """                case "BlockPlacementPreview": Options.BlockPlacementPreview = value == "1"; return true;""",
"""                case "BlockPlacementPreview": Options.BlockPlacementPreview = value == "1"; return true;
                case "GenerationParallel": Options.GenerationParallel = value == "1"; return true;
                case "GenerationThreads": Options.GenerationThreads = int.Parse(value); return true;""", rel, 1)
save(rel, t)

# Режим в настройках игры и в сохранении
rel = r"Games\GameSettings.cs"
t = load(rel)
t = rep(t, """        public long Seed { get; private set; }""", """        public long Seed { get; private set; }
        /// <summary>
        /// Режим генерации мира, у загруженного мира из сохранения (без тега — Original)
        /// </summary>
        public EnumGenerationMode GenerationMode { get; private set; }""", rel, 1)
t = rep(t, """            Seed = seed;
        }""", """            Seed = seed;
            GenerationMode = Options.GenerationParallel ? EnumGenerationMode.Parallel : EnumGenerationMode.Original;
        }""", rel, 1)
t = rep(t, """                Seed = nbt.GetLong("Seed");""", """                Seed = nbt.GetLong("Seed");
                GenerationMode = (EnumGenerationMode)nbt.GetByte("GenMode");""", rel, 1)
t = add_using(t, "Mvk.Engine.World.Gen")
save(rel, t)

rel = r"Games\GameFile.cs"
t = load(rel)
t = rep(t, """                nbt.SetLong("Seed", gameSettings.Seed);""", """                nbt.SetLong("Seed", gameSettings.Seed);
                if (gameSettings.GenerationMode != EnumGenerationMode.Original)
                {
                    nbt.SetByte("GenMode", (byte)gameSettings.GenerationMode);
                }""", rel, 1)
t = add_using(t, "Mvk.Engine.World.Gen")
save(rel, t)

# Пакет чанков: рельеф пулом потоков
rel = r"World\Chunk\ChunkProviderServer.cs"
t = load(rel)
t = rep(t, """        public ChunkProviderServer(WorldServer world) : base(world)
        {
            _worldServer = world;
            Settings.SetHeightChunks(world.Settings.NumberChunkSections);
            ChunkGenerate = world.Settings.ChunkGenerate;
        }""", """        /// <summary>
        /// Пул потоков рельефа в режиме генерации Parallel, иначе null
        /// </summary>
        private readonly JobScheduler _reliefScheduler;
        /// <summary>
        /// Рельеф пакета: генератор на поток и чанки пакета
        /// </summary>
        private readonly ReliefJob _reliefJob;
        /// <summary>
        /// Чанки пакета, которым нужен ChunkPresent (не были готовы до загрузки)
        /// </summary>
        private ChunkServer[] _presentChunks = new ChunkServer[64];

        public ChunkProviderServer(WorldServer world) : base(world)
        {
            _worldServer = world;
            Settings.SetHeightChunks(world.Settings.NumberChunkSections);
            ChunkGenerate = world.Settings.ChunkGenerate;
            if (ChunkGenerate is IChunkProviderGenerateParallel parallel && parallel.IsParallel)
            {
                _reliefScheduler = new JobScheduler(Options.GenerationThreads > 0
                    ? Options.GenerationThreads : JobScheduler.DefaultWorkers, "Relief");
                _reliefJob = new ReliefJob(parallel, _reliefScheduler.WorkerCount);
            }
        }

        /// <summary>
        /// Остановить потоки рельефа (мир выгружен)
        /// </summary>
        public void StopRelief() => _reliefScheduler?.Dispose();

        /// <summary>
        /// Рельеф пакета чанков на потоках планировщика: у каждого потока свой генератор
        /// </summary>
        private sealed class ReliefJob : IParallelJob
        {
            private readonly IChunkProviderGenerate[] _workers;
            public ChunkServer[] Chunks = new ChunkServer[64];

            public ReliefJob(IChunkProviderGenerateParallel main, int count)
            {
                _workers = new IChunkProviderGenerate[count];
                _workers[0] = main;
                for (int i = 1; i < count; i++) _workers[i] = main.CreateParallelWorker();
            }

            public void Execute(int index, int worker) => _workers[worker].Relief(Chunks[index]);
        }

        /// <summary>
        /// Режим Parallel: загрузка с диска последовательно, рельеф пулом потоков, затем ChunkPresent
        /// в порядке очереди
        /// </summary>
        private void _LoadOrGenParallel(int count)
        {
            if (_presentChunks.Length < count)
            {
                _presentChunks = new ChunkServer[count * 2];
                _reliefJob.Chunks = new ChunkServer[count * 2];
            }
            int present = 0, relief = 0;
            ChunkServer chunk;
            for (int i = 0; i < count; i++)
            {
                chunk = _loadingChunks[i];
                if (chunk.IsChunkPresent) continue;
                _presentChunks[present++] = chunk;
                _LoadFile(chunk);
                if (!chunk.IsChunkPresent) _reliefJob.Chunks[relief++] = chunk;
            }
            long statsBegin = ChunkStats.Begin();
            _reliefScheduler.ParallelFor(relief, _reliefJob);
            ChunkStats.End(ChunkStats.Stage.Relief, statsBegin);
            for (int i = 0; i < present; i++)
            {
                _presentChunks[i].ChunkPresent();
                _presentChunks[i] = null;
            }
            for (int i = 0; i < relief; i++) _reliefJob.Chunks[i] = null;
        }""", rel, 1)
t = rep(t, """                for (i = 0; i < count; i++)
                {
                    chunk = _loadingChunks[i];
                    _LoadOrGen(chunk);
                }""", """                if (_reliefScheduler != null) _LoadOrGenParallel(count);
                else
                {
                    for (i = 0; i < count; i++)
                    {
                        chunk = _loadingChunks[i];
                        _LoadOrGen(chunk);
                    }
                }""", rel, 1)
t = rep(t, """            if (!chunk.IsChunkPresent)
            {
                // Пробуем загрузить с файла
                try""", """            if (!chunk.IsChunkPresent)
            {
                _LoadFile(chunk);
                if (!chunk.IsChunkPresent)
                {
                    // Начинаем генерацию рельефа
                    long statsBegin = ChunkStats.Begin();
                    ChunkGenerate.Relief(chunk);
                    ChunkStats.End(ChunkStats.Stage.Relief, statsBegin);
                }
                // Готова начальная генерация или загрузка
                chunk.ChunkPresent();
            }
        }

        /// <summary>
        /// Пробуем загрузить чанк с файла
        /// </summary>
        private void _LoadFile(ChunkServer chunk)
        {
            {
                try""", rel, 1)
t = rep(t, """                    _worldServer.SetLog(Srl.CouldNotReadChunkInRegion,
                        ex.Message, ex.StackTrace, chunk.ToPosition(), chunk.ToRegion());
                }
                if (!chunk.IsChunkPresent)
                {
                    // Начинаем генерацию рельефа
                    long statsBegin = ChunkStats.Begin();
                    ChunkGenerate.Relief(chunk);
                    ChunkStats.End(ChunkStats.Stage.Relief, statsBegin);
                }
                // Готова начальная генерация или загрузка
                chunk.ChunkPresent();
            }
        }""", """                    _worldServer.SetLog(Srl.CouldNotReadChunkInRegion,
                        ex.Message, ex.StackTrace, chunk.ToPosition(), chunk.ToRegion());
                }
            }
        }""", rel, 1)
# Тело _LoadFile без лишнего блока (перенесено из _LoadOrGen): убрать скобки и сдвинуть на уровень
i = t.index("        private void _LoadFile(ChunkServer chunk)")
j = t.index("\n        }\n", i) + len("\n        }\n")
lines = t[i:j].split("\n")
assert lines[2] == "            {" and lines[-3] == "            }", (lines[2], lines[-3])
lines = lines[:2] + [l[4:] for l in lines[3:-3]] + lines[-2:]
t = t[:i] + "\n".join(lines) + t[j:]
t = add_using(t, "Mvk.Core.Util")
t = add_using(t, "Mvk.Engine.World.Gen")
save(rel, t)

rel = r"World\WorldServer.cs"
t = load(rel)
t = rep(t, """            IsRuning = false;
            Wait?.Stoping();""", """            IsRuning = false;
            Wait?.Stoping();
            ChunkPrServ.StopRelief();""", rel, 1)
save(rel, t)
print("gen_parallel ok")
