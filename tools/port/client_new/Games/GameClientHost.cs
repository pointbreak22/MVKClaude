#nullable enable
using Mvk.Engine.Renderer;
using Mvk.Engine.Renderer.World;
using Mvk.Engine.World;
using Mvk.Engine.World.Chunk;
using Mvk.Rendering;

namespace Mvk.Engine.Games
{
    /// <summary>
    /// Хост клиента с рендером мира на RHI. Пока без экранов, HUD, частиц и звука:
    /// от хоста без окна отличается чанками с сетками и рендером мира.
    /// </summary>
    /// <remarks>
    /// В оригинале WorldRenderer создавался вместе с окном, до игры. Здесь он создаётся при первом
    /// обращении: WorldClient сообщает о старте мира (OnWorldStarting) раньше, чем GameBase зовёт OnGameStarting.
    /// Создавать его нужно в потоке окна (GameStarting вызывается оттуда).
    /// </remarks>
    public class GameClientHost : HeadlessGameClientHost
    {
        /// <summary>Общие объекты рендера</summary>
        public readonly RenderContext Context;
        /// <summary>Рендер мира, создаётся при запуске игры</summary>
        public WorldRenderer? WorldRender { get; private set; }

        private readonly string _shaderBlocksLow;

        public GameClientHost(GameModServer modServer, IGraphicsDevice device, string shaderBlocksLow)
            : base(modServer)
        {
            Context = new RenderContext(device);
            _shaderBlocksLow = shaderBlocksLow;
        }

        private WorldRenderer _Render() => WorldRender ??= new WorldRenderer(Game, Context, _shaderBlocksLow);

        public override ChunkClient CreateChunk(WorldClient world, int chunkPosX, int chunkPosY)
            => new ChunkRender(world, _Render(), chunkPosX, chunkPosY);

        /// <summary>
        /// Окно показывает или прячет курсор (игра включает и выключает обзор мышью)
        /// </summary>
        public System.Action<bool>? CursorShowHandler { get; set; }

        public override void CursorShow(bool show) => CursorShowHandler?.Invoke(show);

        public override void OnGameStarting() => _Render();
        public override void OnWorldStarting() => _Render().Starting();
        public override void OnTick(float deltaTime) => WorldRender?.OnTick(deltaTime);
        public override void OnWorldStoping() => WorldRender?.Stoping();
        public override void OnModifyOverviewChunk() => WorldRender?.ModifyOverviewChunk();
        public override string ToDebugString() => WorldRender?.ToString() ?? "";

        public override void DisposeGame()
        {
            WorldRender?.Dispose();
            WorldRender = null;
        }

        /// <summary>
        /// Записать кадр мира
        /// </summary>
        public void Draw(CommandList commands, float timeIndex)
        {
            if (WorldRender != null) WorldRender.Draw(commands, timeIndex);
            else commands.Clear(0, 0, 0, 1);
        }
    }
}
