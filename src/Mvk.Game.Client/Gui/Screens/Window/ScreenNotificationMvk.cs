// Перенос из Mvk2 (original/src/Mvk2) без изменения логики; код оригинала без аннотаций nullable
#nullable disable
using Mvk.Game.Client;
using Mvk.Game.Client.Renderer;
using System.Runtime.CompilerServices;
using Mvk.Engine.Gui.Screens;

namespace Mvk.Game.Client.Gui.Screens
{
    /// <summary>
    /// Экран оповещения, ошибки
    /// </summary>
    public class ScreenNotificationMvk : ScreenNotification
    {
        private readonly RenderMvk _renderMvk;

        public ScreenNotificationMvk(WindowMvk window, string notification) 
            : base(window, notification) => _renderMvk = window.GetRender();

        /// <summary>
        /// Запустить текстуру фона
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void _BindTextureBg() => _renderMvk.BindTextureWindowSmall();
    }
}
