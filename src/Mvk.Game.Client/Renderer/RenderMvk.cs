using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mvk.Engine.Renderer;
using Mvk.Engine.Renderer.Font;
using Mvk.Engine.Util;

namespace Mvk.Game.Client.Renderer
{
    /// <summary>
    /// Отрисовка интерфейса Малювек: два дополнительных шрифта и текстуры окон, HUD и инвентаря.
    /// Перенос RenderMvk оригинала, картинки загружает LoadingMvk. Солнце и луну грузит SkyTexturesMvk
    /// (им нужна только текстура RHI), поэтому их картинки здесь не нужны.
    /// </summary>
    public class RenderMvk : RenderMain
    {
        /// <summary>Мелкий шрифт</summary>
        public FontBase? FontSmall { get; private set; }
        /// <summary>Крупный шрифт</summary>
        public FontBase? FontLarge { get; private set; }

        private uint _windowBig, _windowSmall, _chat, _hud, _inventory, _conteinerStorage, _itemMet;

        public RenderMvk(RenderContext context) : base(context) { }

        public override void UpdateSizeInterface()
        {
            base.UpdateSizeInterface();
            FontSmall?.UpdateSizeInterface();
            FontLarge?.UpdateSizeInterface();
        }

        #region Texture

        public void CreateTextureFontSmall(BufferedImage buffered) => FontSmall = new FontBase(buffered, 1, this);

        public void CreateTextureFontLarge(BufferedImage buffered) => FontLarge = new FontBase(buffered, 2, this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindTextureWindowBig() => Texture.BindTexture(_windowBig);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindTextureWindowSmall() => Texture.BindTexture(_windowSmall);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindTextureChat() => Texture.BindTexture(_chat);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindTextureHud() => Texture.BindTexture(_hud);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindTextureInventory() => Texture.BindTexture(_inventory);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindTextureConteinerStorage() => Texture.BindTexture(_conteinerStorage);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindTextureItemMet() => Texture.BindTexture(_itemMet);

        #endregion

        public override void AtFinishLoading(Dictionary<string, BufferedImage> buffereds)
        {
            base.AtFinishLoading(buffereds);
            if (FontSmall != null && buffereds.TryGetValue(nameof(FontSmall), out BufferedImage? small))
            {
                FontSmall.CreateMesh(Texture.SetTexture(small));
            }
            if (FontLarge != null && buffereds.TryGetValue(nameof(FontLarge), out BufferedImage? large))
            {
                FontLarge.CreateMesh(Texture.SetTexture(large));
            }
            _windowBig = _Set(buffereds, "WindowBig");
            _windowSmall = _Set(buffereds, "WindowSmall");
            _chat = _Set(buffereds, "Chat");
            _hud = _Set(buffereds, "Hud");
            _inventory = _Set(buffereds, "Inventory");
            _conteinerStorage = _Set(buffereds, "ConteinerStorage");
            _itemMet = _Set(buffereds, "ItemMet");
        }

        private uint _Set(Dictionary<string, BufferedImage> buffereds, string name)
            => buffereds.TryGetValue(name, out BufferedImage? image) ? Texture.SetTexture(image) : 0;
    }

    /// <summary>
    /// Ключи текстур Малювек (TextureIndexMvk.cs оригинала), порядок загрузки LoadingMvk
    /// </summary>
    public enum EnumTextureMvk
    {
        FontSmall,
        FontLarge,
        WindowBig,
        WindowSmall,
        Chat,
        Hud,
        Inventory,
        ConteinerStorage,
        Sun,
        Moon,
        ItemMet
    }
}
