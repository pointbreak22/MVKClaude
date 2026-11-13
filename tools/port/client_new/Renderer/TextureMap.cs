#nullable enable
using System.Collections.Generic;
using Mvk.Engine.Util;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer
{
    /// <summary>
    /// Текстуры GUI по номеру, как TextureMap оригинала: код GUI хранит uint и вызывает BindTexture(index).
    /// Номер 0 означает «нет текстуры». Привязка записывается в список команд кадра RenderMain.
    /// </summary>
    public sealed class TextureMap : System.IDisposable
    {
        /// <summary>Как TextureMap.SetTexture оригинала без мипмапов: NEAREST и CLAMP_TO_BORDER</summary>
        private static readonly SamplerState _sampler = new SamplerState(
            TextureFilter.Nearest, TextureFilter.Nearest, MipFilter.None, TextureWrap.ClampToBorder);

        private readonly RenderMain _render;
        private readonly Dictionary<uint, ITexture> _textures = new Dictionary<uint, ITexture>();
        private uint _next = 1;

        public TextureMap(RenderMain render) => _render = render;

        /// <summary>
        /// Загрузить картинку в новую текстуру или заменить существующую с номером index.
        /// Картинки GUI без мипмапов (мипмапы есть только у атласа блоков, он грузится отдельно).
        /// </summary>
        public uint SetTexture(BufferedImage image, uint index = 0)
        {
            if (index != 0 && _textures.TryGetValue(index, out ITexture? old))
            {
                old.Dispose();
            }
            else
            {
                index = _next++;
            }
            ITexture texture = _render.Context.Device.CreateTexture(
                TextureDescription.Texture2D(image.Width, image.Height, _sampler));
            texture.SetData(image.Buffer, PixelFormat.Bgra8);
            _textures[index] = texture;
            return index;
        }

        /// <summary>
        /// Текстура по номеру или null
        /// </summary>
        public ITexture? Get(uint index) => _textures.TryGetValue(index, out ITexture? t) ? t : null;

        /// <summary>
        /// Привязать текстуру к слоту (в оригинале glActiveTexture + glBindTexture)
        /// </summary>
        public void BindTexture(uint index, uint texture = 0)
        {
            if (_textures.TryGetValue(index, out ITexture? t)) _render.Commands.SetTexture((int)texture, t);
        }

        /// <summary>
        /// Удалить текстуру
        /// </summary>
        public void DeleteTexture(uint index)
        {
            if (_textures.Remove(index, out ITexture? t)) t.Dispose();
        }

        public void Dispose()
        {
            foreach (ITexture t in _textures.Values) t.Dispose();
            _textures.Clear();
        }
    }
}
