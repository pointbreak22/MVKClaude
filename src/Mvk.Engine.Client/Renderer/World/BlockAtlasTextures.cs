using System;
using Mvk.Engine.Util;
using Mvk.Engine.World.Block;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.World
{
    /// <summary>
    /// Две текстуры атласа блоков и предметов на GPU, как GeneratingAtlas.EndImage оригинала:
    /// резкая без мипмапов и размытая со своими уровнями мипмапов (BufferedImage считает их сам,
    /// не давая прозрачным пикселям затемнять края).
    /// </summary>
    public sealed class BlockAtlasTextures : IDisposable
    {
        /// <summary>Атлас без мипмапов</summary>
        public readonly ITexture Sharpness;
        /// <summary>Атлас с мипмапами</summary>
        public readonly ITexture Blurry;

        /// <summary>
        /// Создать из готового атласа. Вызывать после инициализации блоков и предметов,
        /// когда BlocksReg.BlockItemAtlas заполнен.
        /// </summary>
        public BlockAtlasTextures(IGraphicsDevice device)
        {
            int size = BlocksReg.BlockItemAtlas.Size;
            byte[] buffer = BlocksReg.BlockItemAtlas.Buffer;

            // В оригинале CLAMP_TO_BORDER; координаты не выходят за атлас, край даёт тот же результат
            Sharpness = device.CreateTexture(TextureDescription.Texture2D(size, size, SamplerState.PointClamp));
            Sharpness.SetData(buffer, PixelFormat.Bgra8);

            BufferedImage image = new BufferedImage(size, size, buffer, 0, true);
            int count = image.CountMipMap();
            Blurry = device.CreateTexture(new TextureDescription(size, size, 1, count + 1,
                SamplerState.PointMipRepeat));
            Blurry.SetData(buffer, PixelFormat.Bgra8);
            for (int i = 0; i < count; i++)
            {
                Blurry.SetData(image.GetLevelMipMap(i).Buffer, PixelFormat.Bgra8, i + 1);
            }
        }

        public void Dispose()
        {
            Sharpness.Dispose();
            Blurry.Dispose();
        }
    }
}
