// Перенос из Mvk2 (original/src/Mvk2) без изменения логики; код оригинала без аннотаций nullable
#nullable disable
using Mvk.Engine.Util;
using Mvk.Game.Client;
using Mvk.Game.Client.Realms;
using Mvk.Engine.Gui.Screens;
using Mvk.Engine.Renderer;
using Mvk.Engine.Renderer.Mesh;

namespace Mvk.Game.Client.Gui.Screens
{
    /// <summary>
    /// Заставка
    /// </summary>
    public class ScreenSplashMvk : ScreenSplash
    {
        private readonly MeshGuiColor _meshLogo;
        
        public ScreenSplashMvk(WindowMvk window) : base(window)
        {
            _meshLogo = new MeshGuiColor(window.GetOpenGL());
        }

        /// <summary>
        /// Объвление объекта загрузки
        /// </summary>
        protected override void _LoadingCreate() => _loading = new LoadingMvk((WindowMvk)window);

        /// <summary>
        /// Начальный рендер
        /// </summary>
        protected override void _RenderBegin()
        {
            int w = Gi.Width / 2;
            int h = (Gi.Height - 480 * _si) / 2;
            int w2 = 256 * _si;
            int h2 = 384 * _si;
            _meshLogo.Reload(RenderFigure.Rectangle(w - w2, h, w + w2, h + h2, 0, 0, 1, .75f));
        }

        /// <summary>
        /// Логотип
        /// </summary>
        protected override void _DrawLogo() => _meshLogo.Draw();
    }
}
