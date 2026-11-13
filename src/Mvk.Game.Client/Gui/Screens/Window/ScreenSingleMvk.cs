// Перенос из Mvk2 (original/src/Mvk2) без изменения логики; код оригинала без аннотаций nullable
#nullable disable
using Mvk.Game.Client;
using Mvk.Game;
using Mvk.Game.Client.Renderer;
using System.Runtime.CompilerServices;

namespace Mvk.Engine.Gui.Screens
{
    /// <summary>
    /// Экран выбора одиночной игры
    /// </summary>
    public class ScreenSingleMvk : ScreenSingle
    {
        private readonly RenderMvk _renderMvk;

        public ScreenSingleMvk(WindowMvk window) : base(window)
            => _renderMvk = window.GetRender();

        /// <summary>
        /// Запустить текстуру фона
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void _BindTextureBg() => _renderMvk.BindTextureWindowBig();
    }
}
