// Перенос из Mvk2 (original/src/Mvk2) без изменения логики; код оригинала без аннотаций nullable
#nullable disable
using Mvk.Game.Client;
using Mvk.Engine.Gui.Screens;

namespace Mvk.Game.Client.Gui.Screens
{
    public class ScreenMainMenuMvk : ScreenMainMenu
    {
        public ScreenMainMenuMvk(WindowMvk window) : base(window)
        {
            _buttonSingle.SetFont(window.GetRender().FontLarge);
            _buttonMultiplayer.SetFont(window.GetRender().FontLarge);
            _buttonOptions.SetFont(window.GetRender().FontLarge);
            _buttonExit.SetFont(window.GetRender().FontLarge);
        }
    }
}
