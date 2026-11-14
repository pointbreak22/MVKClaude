#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using Mvk.Core.Maths;
using Mvk.Core.Util;
using Mvk.Engine.Games;
using Mvk.Engine.Util;
using Mvk.Engine.Renderer.Shaders;
using Mvk.Engine.World.Environment;
using Mvk.Engine.Renderer.World.Entity;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.World
{
    /// <summary>
    /// Рендер мира на RHI. Перенос WorldRenderer оригинала: поток построения сеток, очереди,
    /// привязка сеток в кадре и проходы сплошных и альфа блоков.
    /// Небо и облака рисует SkyRender, его задаёт мод при создании настроек мира (WorldStarted).
    /// Сущности рисует EntitiesRenderer, частицы ParticlesRenderer, курсор CursorRender, тени ShadowMapping.
    /// </summary>
    public class WorldRenderer : IDisposable
    {
        /// <summary>Общие объекты рендера</summary>
        public readonly RenderContext Context;
        /// <summary>
        /// Сколько потоков строят сетки чанков: поток WorldRender и помощники планировщика.
        /// Хост может задать до создания рендера мира (Mvk.Desktop --mesh-workers). По умолчанию один:
        /// загрузка мира упирается в генерацию на сервере, а при 4 потоках сетка чанка дорожает с 3.8 до 5.7 мс
        /// (общая память и сборка мусора), загрузка не быстрее (docs/progress.md, этап 7.1)
        /// </summary>
        public static int MeshWorkers = 1;
        /// <summary>Построитель сеток чанков потока WorldRender (помощники берут свои из _meshers)</summary>
        public ChunkMesher Mesher => _meshers[0];
        /// <summary>Построители сеток, по одному на поток построения</summary>
        private readonly ChunkMesher[] _meshers;
        /// <summary>Планировщик потоков построения сеток (этап 7)</summary>
        private readonly JobScheduler _meshScheduler;
        /// <summary>Работа пакета сеток для планировщика</summary>
        private readonly MeshJob _meshJob;
        /// <summary>Карта света</summary>
        public readonly TextureLightMap LightMap;
        /// <summary>Шейдер блоков</summary>
        public readonly ShadersBlocks ShsBlocks;
        /// <summary>Текстуры атласа блоков</summary>
        public readonly BlockAtlasTextures Atlas;
        /// <summary>
        /// Рендер сущностей
        /// </summary>
        public readonly EntitiesRenderer Entities;
        /// <summary>
        /// Рендер частиц
        /// </summary>
        public readonly ParticlesRenderer Particles;
        /// <summary>
        /// Отрисовка интерфейса и линий (RenderMain оригинала)
        /// </summary>
        public readonly RenderMain Render;
        /// <summary>
        /// Курсор блока и сетка чанка
        /// </summary>
        private readonly CursorRender _cursorRender;
        /// <summary>
        /// Карта теней
        /// </summary>
        public readonly ShadowMapping Shadow;

        /// <summary>
        /// В оригинале GL окна, им создавались сетки; здесь отрисовка интерфейса и линий
        /// </summary>
        public RenderMain GetOpenGL() => Render;

        /// <summary>Обзор в блоках</summary>
        public int OverviewBlock = 32;
        /// <summary>Цвет неба</summary>
        public Vector3 ColorSky = new Vector3(0);
        /// <summary>Цвет тумана</summary>
        public Vector3 ColorFog = new Vector3(0);

        private readonly GameBase _game;
        /// <summary>Небо текущего мира</summary>
        private SkyRender? _skyRender;
        /// <summary>Флаг потока рендера чанков</summary>
        private volatile bool _flagRenderLoopRunning;
        /// <summary>Очередь чанков для рендера</summary>
        private readonly DoubleList<ChunkRender> _renderQueues = new DoubleList<ChunkRender>();
        /// <summary>Очередь чанков для рендера альфа</summary>
        private readonly DoubleList<ChunkRender> _renderAlphaQueues = new DoubleList<ChunkRender>();
        /// <summary>Массив чанков для прорисовки</summary>
        private readonly ArrayFast<ChunkRender> _arrayChunkRender;
        private readonly AutoResetEvent _waitHandler = new AutoResetEvent(true);
        private Thread? _thread;

        /// <summary>Желаемый размер партии рендера чанков</summary>
        private byte _desiredBatchSize = Ce.MinDesiredBatchSize;
        /// <summary>Время мс на рендер чанков</summary>
        private int _batchChunksTime;

        public WorldRenderer(GameBase game, RenderMain render, string shaderBlocksLow, string shaderEntityLow)
        {
            _game = game;
            Render = render;
            Context = render.Context;
            RenderContext context = Context;
            _arrayChunkRender = new ArrayFast<ChunkRender>(Ce.OverviewCircles.Length);
            _meshers = new ChunkMesher[MeshWorkers];
            for (int i = 0; i < _meshers.Length; i++) _meshers[i] = new ChunkMesher();
            _meshScheduler = new JobScheduler(MeshWorkers, "WorldRender");
            _meshJob = new MeshJob(this);
            LightMap = new TextureLightMap(context.Device);
            ShsBlocks = new ShadersBlocks(context, shaderBlocksLow, "BlocksHigh", "BlocksDepthMap");
            Shadow = new ShadowMapping(context.Device);
            Atlas = new BlockAtlasTextures(context.Device);
            Entities = new EntitiesRenderer(game, this, _arrayChunkRender, shaderEntityLow);
            Particles = new ParticlesRenderer(game, this);
            Entities.Gui = render;
            Particles.Gui = render;
            _cursorRender = new CursorRender(game.Player, this);
        }

        /// <summary>
        /// Изменён обзор чанков
        /// </summary>
        public void ModifyOverviewChunk()
        {
            _arrayChunkRender.Resize(Ce.OverviewCircles.Length);
            OverviewBlock = _game.Player.OverviewChunk * 16;
            _skyRender?.ModifyOverviewChunk(OverviewBlock);
        }

        /// <summary>
        /// Мир запущен, старт и смена мира: задать небо (прошлое освобождается)
        /// </summary>
        public void WorldStarted(SkyRender skyRender)
        {
            _skyRender?.Dispose();
            _skyRender = skyRender;
        }

        /// <summary>
        /// Перезапуск мира
        /// </summary>
        public void RespawnInWorld()
        {
            Particles.Clear();
            _cursorRender.SetHeightChunks(_game.World.ChunkPr.Settings.NumberBlocks);
        }

        /// <summary>
        /// Смена видимости курсора чанка
        /// </summary>
        public void ChunkCursorHiddenShow() => _cursorRender.ChunkCursorHiddenShow();

        /// <summary>
        /// Смена видимости хитбоксов сущностей
        /// </summary>
        public void HitboxEntitiesHiddenShow() => Entities.IsHitBox = !Entities.IsHitBox;

        /// <summary>
        /// Изменена опция теней (ModifyShadow оригинала)
        /// </summary>
        public void ModifyShadow()
        {
            Shadow.ReloadInit();
            Entities.ShsEntity.UpdateOptions();
            ShsBlocks.UpdateOptions();
        }

        /// <summary>
        /// Инициализация в рендере мира настроек мира
        /// </summary>
        public void WorldRenderInitSetting() => _skyRender?.InitSetting();

        #region Поток рендера

        /// <summary>
        /// Поток рендера чанков
        /// </summary>
        private void _RenderLoop()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            long timeBegin;
            int quantity;
            while (_flagRenderLoopRunning)
            {
                // Alpha
                if (_renderAlphaQueues.CountForward > 0)
                {
                    _RenderQueues(false, _renderAlphaQueues);
                }

                timeBegin = stopwatch.ElapsedMilliseconds;

                if (_renderQueues.CountForward > 0)
                {
                    quantity = _RenderQueues(true, _renderQueues);

                    _batchChunksTime = (int)(stopwatch.ElapsedMilliseconds - timeBegin);
                    _desiredBatchSize = Sundry.RecommendedQuantityBatch(_batchChunksTime,
                        quantity, _desiredBatchSize, Ce.MaxDesiredBatchSize, Ce.TickTime * 2);
                }
                // Ожидаем сигнала
                _waitHandler.WaitOne();
            }
        }

        /// <summary>
        /// Запуск рендера чанка из очередей в отдельном потоке
        /// </summary>
        private int _RenderQueues(bool isDense, DoubleList<ChunkRender> list)
        {
            list.Step();
            int count = list.CountBackward;
            // Пакет в массив, дальше чанки пакета строятся всеми потоками планировщика
            _meshJob.Prepare(count, isDense);
            for (int i = 0; i < count; i++)
            {
                _meshJob.Chunks[i] = list.GetNext();
            }
            _meshScheduler.ParallelFor(count, _meshJob);
            _meshJob.Clear(count);
            list.ClearBackward();

            return count;
        }

        /// <summary>
        /// Построение сеток пакета чанков на потоках планировщика: у каждого потока свой ChunkMesher
        /// </summary>
        private sealed class MeshJob : IParallelJob
        {
            /// <summary>Чанки пакета</summary>
            public ChunkRender[] Chunks = new ChunkRender[64];
            private readonly WorldRenderer _render;
            private bool _isDense;

            public MeshJob(WorldRenderer render) => _render = render;

            /// <summary>Подготовить массив под пакет (растёт только при первом большом пакете)</summary>
            public void Prepare(int count, bool isDense)
            {
                if (Chunks.Length < count) Chunks = new ChunkRender[Math.Max(count, Chunks.Length * 2)];
                _isDense = isDense;
            }

            /// <summary>Не держать ссылки на чанки после пакета</summary>
            public void Clear(int count) => Array.Clear(Chunks, 0, count);

            public void Execute(int index, int worker)
            {
                if (!_render._flagRenderLoopRunning) return;
                ChunkRender chunkRender = Chunks[index];
                chunkRender.SetMesher(_render._meshers[worker]);
                long statsBegin = ChunkStats.Begin();
                if (_isDense)
                {
                    chunkRender.Render();
                    ChunkStats.End(ChunkStats.Stage.Mesh, statsBegin);
                }
                else
                {
                    chunkRender.RenderAlpha();
                    ChunkStats.End(ChunkStats.Stage.MeshAlpha, statsBegin);
                }
                chunkRender.ClearBufferChunks();
            }
        }

        #endregion

        /// <summary>
        /// Запускается мир, возможно смена миров
        /// </summary>
        public void Starting()
        {
            if (_flagRenderLoopRunning) return;
            _flagRenderLoopRunning = true;
            _thread = new Thread(_RenderLoop) { Name = "WorldRender", IsBackground = true };
            _thread.Start();
        }

        /// <summary>
        /// Останавливаем мир, НЕ смена миров
        /// </summary>
        public void Stoping()
        {
            _flagRenderLoopRunning = false;
            _waitHandler.Set();
        }

        /// <summary>
        /// Игровой такт
        /// </summary>
        public void OnTick(float deltaTime)
        {
            if (_game.World?.Settings != null)
            {
                LightMap.Update(_game.World.Settings);
            }

            // Физика частичек, и их жизнь
            Particles.OnTick(deltaTime);

            // Считаем вектор, матрицы и прочее из календаря
            if (_game.World?.Settings != null)
            {
                IEnvironment environment = _game.World.Settings.Environment;
                Gi.ViewLightDir = environment.GetVectorLight();
                ColorSky = environment.GetColorSky();
                ColorFog = environment.GetColorFog();

                // Матрица карты теней от солнца (_GenVectorLightСalendar оригинала)
                Vector3 vr = Glm.Cross(new Vector3(0, 1, 0), Gi.ViewLightDir);
                Vector3 vu = Glm.Cross(Gi.ViewLightDir, vr);
                int sosm = ShadowMapping.SizeOrthShadowMap;
                Mat4 matrix = Glm.Ortho(-sosm, sosm, -sosm, sosm, -sosm * 4, sosm);
                matrix.Multiply(Glm.LookAt(Gi.ViewLightDir, new Vector3(0, 0, 0), vu));
                matrix.ConvArray(Gi.MatrixViewDepthMap);
            }
            _skyRender?.Update();
        }

        /// <summary>
        /// Готов ли мир к прорисовке
        /// </summary>
        public bool IsWorldReady => _game.World?.Settings != null && _game.Player != null && _skyRender != null;

        /// <summary>
        /// Записать кадр мира в список команд. Вызывать в потоке окна:
        /// здесь же сетки чанков загружаются на GPU.
        /// </summary>
        /// <param name="timeIndex">коэффициент времени от прошлого TPS клиента в диапазоне 0 .. 1</param>
        public void Draw(CommandList commands, float timeIndex)
        {
            commands.Clear(ColorFog.X, ColorFog.Y, ColorFog.Z, 1f);
            if (!IsWorldReady) return;

            _game.World!.Settings.Environment.UpdateFrame(timeIndex);
            // Обновить кадр основного игрока, камера и прочее
            _game.Player.UpdateFrame(timeIndex);
            // Биндим чанки вокселей и готовим массив чанков
            _BindChunkVoxel();

            // Текстуры и сетки сущностей создаются в первом кадре мира: к этому времени
            // мод зарегистрировал все сущности и предметы (в оригинале WorldRenderer.GameStarting)
            if (!Entities.IsStarted) Entities.GameStarting();
            Entities.UpdateMatrix(timeIndex);

            // Буфер теней есть, работаем дальше
            if (Shadow.RenderSceneBegin(commands))
            {
                // --- Начало сцены ТЕНЕЙ
                // Рисуем воксели сплошных и уникальных блоков
                _DrawVoxelDenseDepthMap(commands);
                // Сущности
                Entities.DrawDepthMap(commands, timeIndex);
                // Облака
                _skyRender!.DrawCloudsDepthMap(commands, timeIndex);
                // --- Конец сцены ТЕНЕЙ, обратно в окно
                Shadow.RenderSceneEnd(commands, Gi.Width, Gi.Height);
            }

            // Небо
            _skyRender!.DrawSky(commands, timeIndex);
            // Рисуем воксели сплошных и уникальных блоков
            _DrawVoxelDense(commands);
            // Сущности
            Entities.Draw(commands, timeIndex);
            // Рендер и прорисовка курсора если это необходимо
            _cursorRender.RenderDraw();
            // Прорисовка вид не с руки, а видим себя
            Entities.DrawOwner(commands, timeIndex);
            // Частички
            Particles.Draw(commands, timeIndex);
            // Прорисовка руки
            Entities.DrawOwnerEye(commands, timeIndex);
            // Облака и дождь
            _skyRender.DrawCloudsRain(commands, timeIndex);
            // Рисуем воксели альфа
            _DrawVoxelAlpha(commands);
        }

        /// <summary>
        /// Связывание чанков вокселей и подготовка массива чанков
        /// </summary>
        private void _BindChunkVoxel()
        {
            _arrayChunkRender.Clear();
            int count = _game.Player.FrustumCulling.Count;
            int batchCount = 0;
            int batchCountAlpha = 0;
            for (int i = 0; i < count; i++)
            {
                if (_game.Player.FrustumCulling[i] is ChunkRender chunkRender && chunkRender.IsChunkPresent)
                {
                    // Создание сеток на GPU если это необходимо
                    chunkRender.InitGL();
                    // Можем ли мы в этом кадре пополнять партию
                    if (batchCount < _desiredBatchSize)
                    {
                        if (chunkRender.IsModifiedRender && _renderQueues.CountBackward < 3)
                        {
                            // Проверяем занят ли чанк уже рендером
                            if (chunkRender.IsMeshDenseWait && chunkRender.IsMeshAlphaWait)
                            {
                                Debug.CountUpdateChunck++;
                                batchCount++;
                                chunkRender.StartRendering();
                                chunkRender.UpBufferChunks();
                                _renderQueues.Add(chunkRender);
                            }
                        }
                        else if (chunkRender.IsModifiedRenderAlpha && _renderAlphaQueues.CountBackward < 4)
                        {
                            if (chunkRender.IsMeshDenseWait && chunkRender.IsMeshAlphaWait)
                            {
                                Debug.CountUpdateChunckAlpha++;
                                batchCountAlpha++;
                                chunkRender.StartRenderingAlpha();
                                chunkRender.UpBufferChunks();
                                _renderAlphaQueues.Add(chunkRender);
                            }
                        }
                    }

                    // Занести буфер сплошных блоков если это требуется
                    if (chunkRender.IsMeshDenseBinding) chunkRender.BindBufferDense();
                    // Занести буфер альфа блоков если это требуется
                    if (chunkRender.IsMeshAlphaBinding) chunkRender.BindBufferAlpha();

                    _arrayChunkRender.Add(chunkRender);
                }
            }
            if (batchCount > 0 || batchCountAlpha > 0)
            {
                _waitHandler.Set();
            }
        }

        /// <summary>
        /// Рисуем воксели сплошных и уникальных блоков
        /// </summary>
        /// <summary>
        /// Рисуем воксели сплошных и уникальных блоков для карты глубины (ближние 45 чанков)
        /// </summary>
        private void _DrawVoxelDenseDepthMap(CommandList commands)
        {
            ShsBlocks.BeginDepthMap(commands, Atlas.Blurry, Atlas.Sharpness,
                _game.Player.PosFrameX, _game.Player.PosFrameY, _game.Player.PosFrameZ,
                (int)_game.World!.GetTickCounter(), _game.World.Settings.Environment.GetWind());

            int count = _arrayChunkRender.Count;
            if (count > ShadowMapping.CountChunkShadowMap) count = ShadowMapping.CountChunkShadowMap;
            ChunkRender chunkRender;
            for (int i = 0; i < count; i++)
            {
                chunkRender = _arrayChunkRender[i];
                if (chunkRender.NotNullMeshDense)
                {
                    ShsBlocks.UniformDataChunk(commands, chunkRender.CurrentChunkX << 4, chunkRender.CurrentChunkY << 4);
                    chunkRender.DrawDense(commands);
                }
            }
        }

        private void _DrawVoxelDense(CommandList commands)
        {
            ShsBlocks.Begin(commands, s => Debug.IsDrawVoxelLine ? s.DenseLine : s.Dense,
                Atlas.Blurry, Atlas.Sharpness, LightMap.Texture, Shadow.DepthTexture,
                _game.Player.PosFrameX, _game.Player.PosFrameY, _game.Player.PosFrameZ,
                (int)_game.World!.GetTickCounter(), _game.World.Settings.Environment.GetWind(),
                OverviewBlock, ColorFog, 5);

            int count = _arrayChunkRender.Count;
            ChunkRender chunkRender;
            for (int i = 0; i < count; i++)
            {
                chunkRender = _arrayChunkRender[i];
                if (chunkRender.NotNullMeshDense)
                {
                    ShsBlocks.UniformDataChunk(commands, chunkRender.CurrentChunkX << 4, chunkRender.CurrentChunkY << 4);
                    chunkRender.DrawDense(commands);
                }
            }
        }

        /// <summary>
        /// Прорисовка вокселей альфа цвета: от дальних чанков к ближним,
        /// ближние 9 со сдвигом глубины
        /// </summary>
        private void _DrawVoxelAlpha(CommandList commands)
        {
            ShsBlocks.Switch(commands, ShsBlocks.Alpha);

            bool flagAlpha = true;
            ChunkRender chunkRender;
            for (int i = _arrayChunkRender.Count - 1; i >= 0; i--)
            {
                if (flagAlpha && i < 9)
                {
                    flagAlpha = false;
                    ShsBlocks.Switch(commands, ShsBlocks.AlphaNear);
                }
                chunkRender = _arrayChunkRender[i];
                if (chunkRender.NotNullMeshAlpha)
                {
                    ShsBlocks.UniformDataChunk(commands, chunkRender.CurrentChunkX << 4, chunkRender.CurrentChunkY << 4);
                    chunkRender.DrawAlpha(commands);
                }
            }
        }

        public void Dispose()
        {
            Stoping();
            _thread?.Join(2000);
            _skyRender?.Dispose();
            _skyRender = null;
            _meshScheduler.Dispose();
            foreach (ChunkMesher mesher in _meshers) mesher.Dispose();
            LightMap.Dispose();
            ShsBlocks.Dispose();
            Entities.Dispose();
            Particles.Dispose();
            _cursorRender.Dispose();
            Shadow.Dispose();
            Atlas.Dispose();
        }

        public override string ToString() => "WorldRenderer dbs:" + _desiredBatchSize + "|" + _batchChunksTime + "mc";
    }
}
