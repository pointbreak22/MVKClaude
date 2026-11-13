#nullable enable
using System;
using Mvk.Engine.Util;
using Mvk.Rendering;

namespace Mvk.Game.Client.Renderer
{
    /// <summary>
    /// Текстуры неба Малювек 2 (Sun.png, Moon.png). В оригинале их загружал LoadingMvk
    /// и хранил RenderMvk (BindTextureSun, BindTextureMoon). Создаются один раз на устройство,
    /// небо мира пересоздаётся при каждом входе в мир.
    /// </summary>
    public sealed class SkyTexturesMvk : IDisposable
    {
        /// <summary>Солнце</summary>
        public readonly ITexture Sun;
        /// <summary>Луна, 8 фаз (4 × 2)</summary>
        public readonly ITexture Moon;

        public SkyTexturesMvk(IGraphicsDevice device)
        {
            Sun = _Load(device, "Sun");
            Moon = _Load(device, "Moon");
        }

        /// <summary>
        /// Загрузить картинку без мипмапов, как TextureMap.SetTexture оригинала
        /// (NEAREST, CLAMP_TO_BORDER; координаты не выходят за 0..1, поэтому край тот же)
        /// </summary>
        private static ITexture _Load(IGraphicsDevice device, string name)
        {
            BufferedImage image = BufferedFileImage.FileToBufferedImage(Options.PathTextures + name + ".png");
            ITexture texture = device.CreateTexture(
                TextureDescription.Texture2D(image.Width, image.Height, SamplerState.PointClamp));
            texture.SetData(image.Buffer, PixelFormat.Bgra8);
            return texture;
        }

        public void Dispose()
        {
            Sun.Dispose();
            Moon.Dispose();
        }
    }
}
