// Перенос из Mvk2 (original/src/Mvk2) без изменения логики; код оригинала без аннотаций nullable
#nullable disable
using Mvk.Game.Client;
using Mvk.Game.Packets;
using System.Runtime.CompilerServices;
using Mvk.Engine.Network.Packets.Client;
using Mvk.Engine.Actions;

namespace Mvk.Game.Client.Gui.Screens
{
    /// <summary>
    /// Окно инвентаря для игры Малювеки 2
    /// </summary>
    public class ScreenInventory : ScreenStorage
    {
        public ScreenInventory(WindowMvk window) : base(window, 262)
        {
            _biasY = 27;
            _windowMvk.Game.TrancivePacket(new PacketC0EClickWindow((byte)EnumActionClickWindow.OpenInventory));
        }
            

        /// <summary>
        /// Название заголовка
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override string _GetTitle() => L.T("Inventory");

        public override void OnKeyDown(Keys keys)
        {
            base.OnKeyDown(keys);

            if (keys == Keys.E)
            {
                _Close();
            }
        }

        /// <summary>
        /// Запустить текстуру фона
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void _BindTextureBg() => _windowMvk.GetRender().BindTextureInventory();
    }
}
