// Перенос из Mvk2 (original/src/Mvk2) без изменения логики; код оригинала без аннотаций nullable
#nullable disable
using Mvk.Game.Client;
using Mvk.Game.Client.Renderer;
using System.Runtime.CompilerServices;
using Mvk.Engine.Gui.Screens;

namespace Mvk.Game.Client.Gui.Screens
{
    /// <summary>
    /// Экран настроек
    /// </summary>
    public class ScreenOptionsMvk : ScreenOptions
    {
        private readonly RenderMvk _renderMvk;

        public ScreenOptionsMvk(WindowMvk window, ScreenBase parent, bool inGame) 
            : base(window, parent, inGame)
            => _renderMvk = window.GetRender();

        /// <summary>
        /// Запустить текстуру фона
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void _BindTextureBg() => _renderMvk.BindTextureWindowBig();
    }
}
