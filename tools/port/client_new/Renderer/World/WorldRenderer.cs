#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using Mvk.Core.Maths;
using Mvk.Core.Util;
using Mvk.Engine.Games;
using Mvk.Engine.Renderer.Shaders;
using Mvk.Engine.World.Environment;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.World
{
    /// <summary>
    /// Рендер мира на RHI. Перенос WorldRenderer оригинала: поток построения сеток, очереди,
    /// привязка сеток в кадре и проходы сплошных и альфа блоков.
    /// Небо и облака рисует SkyRender, его задаёт мод при создании настроек мира (WorldStarted).
    /// Пока без сущностей, частиц, курсора и теней (следующие шаги этапа 5).
    /// </summary>
    public class WorldRenderer : IDisposable
    {
        /// <summary>Общие объекты рендера</summary>
        public readonly RenderContext Context;
        /// <summary>Построитель сеток чанков (пользуется поток построения сеток)</summary>
        public readonly ChunkMesher Mesher = new ChunkMesher();
        /// <summary>Карта света</summary>
        public readonly TextureLightMap LightMap;
        /// <summary>Шейдер блоков</summary>
        public readonly ShadersBlocks ShsBlocks;
        /// <summary>Текстуры атласа блоков</summary>
        public readonly BlockAtlasTextures Atlas;

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

        public WorldRenderer(GameBase game, RenderContext context, string shaderBlocksLow)
        {
            _game = game;
            Context = context;
            _arrayChunkRender = new ArrayFast<ChunkRender>(Ce.OverviewCircles.Length);
            LightMap = new TextureLightMap(context.Device);
            ShsBlocks = new ShadersBlocks(context, shaderBlocksLow);
            Atlas = new BlockAtlasTextures(context.Device);
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
            int count, i;
            ChunkRender chunkRender;
            list.Step();
            count = list.CountBackward;
            for (i = 0; i < count; i++)
            {
                chunkRender = list.GetNext();
                if (isDense)
                {
                    chunkRender.Render();
                }
                else
                {
                    chunkRender.RenderAlpha();
                }
                chunkRender.ClearBufferChunks();
                if (!_flagRenderLoopRunning) break;
            }
            list.ClearBackward();

            return count;
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
                IEnvironment environment = _game.World.Settings.Environment;
                Gi.ViewLightDir = environment.GetVectorLight();
                ColorSky = environment.GetColorSky();
                ColorFog = environment.GetColorFog();
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

            // Небо
            _skyRender!.DrawSky(commands, timeIndex);
            // Рисуем воксели сплошных и уникальных блоков
            _DrawVoxelDense(commands);
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
        private void _DrawVoxelDense(CommandList commands)
        {
            ShsBlocks.Begin(commands, Debug.IsDrawVoxelLine ? ShsBlocks.DenseLine : ShsBlocks.Dense,
                Atlas.Blurry, Atlas.Sharpness, LightMap.Texture,
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
            Mesher.Dispose();
            LightMap.Dispose();
            ShsBlocks.Dispose();
            Atlas.Dispose();
        }

        public override string ToString() => "WorldRenderer dbs:" + _desiredBatchSize + "|" + _batchChunksTime + "mc";
    }
}
