#nullable enable
using System;
using Mvk.Engine.Actions;
using Mvk.Engine.Renderer;

namespace Mvk.Engine
{
    /// <summary>
    /// База объектов окна (экраны, запуск экранов), как Warp оригинала.
    /// Поле gl в оригинале было GL окна; здесь отрисовка интерфейса (ClearColor и создание сеток).
    /// </summary>
    public abstract class Warp : IDisposable
    {
        protected readonly WindowMain window;
        protected readonly WarpGl gl;

        public Warp(WindowMain window)
        {
            this.window = window;
            gl = new WarpGl(window);
        }

        public void OptionsSave() => window.OptionsSave();

        /// <summary>Отрисовка интерфейса (в оригинале GL, им создавались сетки)</summary>
        public RenderMain GetOpenGL() => window.Render;

        public RenderMain Render => window.Render;

        #region Draw

        public virtual void Draw(float timeIndex) { }

        #endregion

        #region Tick

        public virtual void OnTick(float deltaTime) { }

        #endregion

        #region OnMouse

        public virtual void OnMouseMove(int x, int y) { }
        public virtual void OnMouseDown(MouseButton button, int x, int y) { }
        public virtual void OnMouseUp(MouseButton button, int x, int y) { }
        public virtual void OnMouseWheel(int delta, int x, int y) { }

        #endregion

        #region OnKey

        public virtual void OnKeyDown(Keys keys) { }
        public virtual void OnKeyUp(Keys keys) { }
        public virtual void OnKeyPress(char key) { }

        #endregion

        public virtual void Dispose() { }
    }

    /// <summary>
    /// То немногое из GL, что вызывают экраны напрямую: цвет очистки кадра
    /// </summary>
    public readonly struct WarpGl
    {
        private readonly WindowMain _window;
        public WarpGl(WindowMain window) => _window = window;
        public void ClearColor(float r, float g, float b, float a) => _window.ClearColor(r, g, b, a);

        /// <summary>
        /// Сетки виджетов создаются от gl (new MeshGuiColor(gl)): отдаём отрисовку интерфейса окна
        /// </summary>
        public static implicit operator RenderMain(WarpGl gl) => gl._window.Render;
    }
}
