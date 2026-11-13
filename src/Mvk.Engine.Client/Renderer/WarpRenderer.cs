#nullable enable
using System;
using Mvk.Engine.Games;

namespace Mvk.Engine.Renderer
{
    /// <summary>
    /// База объектов рендера клиента (HUD и его части), как WarpRenderer оригинала.
    /// В оригинале Render и GL брались из окна через игру; здесь из хоста клиента.
    /// </summary>
    public abstract class WarpRenderer : IDisposable
    {
        protected readonly GameBase _game;

        public WarpRenderer(GameBase game) => _game = game;

        /// <summary>Хост клиента с рендером</summary>
        public GameClientHost Host => (GameClientHost)_game.Host;

        /// <summary>Отрисовка интерфейса (в оригинале GetOpenGL отдавал GL, им создавались сетки)</summary>
        public RenderMain GetOpenGL() => Host.Render;

        /// <summary>Отрисовка интерфейса</summary>
        public RenderMain Render => Host.Render;

        public virtual void Draw(float timeIndex) { }

        public virtual void OnTick(float deltaTime) { }

        public virtual void Dispose() { }
    }
}
