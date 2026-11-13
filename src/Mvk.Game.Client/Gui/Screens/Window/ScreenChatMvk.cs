// Перенос из Mvk2 (original/src/Mvk2) без изменения логики; код оригинала без аннотаций nullable
#nullable disable
using Mvk.Game.Client;
using Mvk.Game;
using Mvk.Game.Client.Renderer;
using System.Runtime.CompilerServices;

namespace Mvk.Engine.Gui.Screens
{
    /// <summary>
    /// Скрин окна чата
    /// </summary>
    public class ScreenChatMvk : ScreenChat
    {
        private readonly RenderMvk _renderMvk;

        public ScreenChatMvk(WindowMvk window) : base(window)
            => _renderMvk = window.GetRender();

        /// <summary>
        /// Название заголовка
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override string _GetTitle() => L.T("Chat");

        /// <summary>
        /// Запустить текстуру фона
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void _BindTextureBg() => _renderMvk.BindTextureChat();
    }
}
