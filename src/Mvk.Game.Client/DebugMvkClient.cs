using System.Diagnostics;
using Mvk.Core.Util;
using Mvk.Engine.Games;
using Mvk.Engine.World.Gen.Layer;
using Mvk.Game.World.Biome;
using Mvk.Game.World.Gen.Layer;

namespace Mvk.Game.Client
{
    /// <summary>
    /// Отладка Малювек на клиенте: перенос DebugMvk.ScreenFileBiomeArea оригинала (клавиша F6).
    /// Карта биомов слоя генерации острова в biomeLayer.png. В оригинале картинку собирал System.Drawing.Bitmap
    /// (только Windows), здесь пиксели RGBA пишет PngWriter: размер, цвета, прозрачное море и логи те же.
    /// </summary>
    public static class DebugMvkClient
    {
        /// <summary>
        /// Сохранить картинку биомов (seed 4, как в оригинале)
        /// </summary>
        public static void ScreenFileBiomeArea(GameBase game)
        {
            long seed = 4;
            // 0 - biome
            _FileArea(seed, 0, game);
        }

        private static void _FileArea(long seed, int id, GameBase game)
        {
            float timerFrequency = Stopwatch.Frequency / 1000;
            int width, height;
            width = height = 4096;
            int zoom = 4;
            GenLayer[] genLayer = GenLayerIsland.BeginLayerBiome(seed);
            int w = width / zoom;
            int h = height / zoom;
            // Bitmap оригинала: 32 бита ARGB, по умолчанию прозрачный чёрный
            byte[] rgba = new byte[w * h * 4];
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int x, z, key;
            int[] ar = genLayer[id].GetInts(width / -2, height / -2, width, height);
            long l = stopwatch.ElapsedTicks;
            game.Log.Log("GenBioms[{1}:{2}]: {0:0.00} ms",
                l / timerFrequency, width, height);
            EnumBiomeIsland biome;
            game.Log.Log("MansionInitLoading[{1}:{2}]: {0:0.00} ms",
               (stopwatch.ElapsedTicks - l) / timerFrequency, width, height);
            l = stopwatch.ElapsedTicks;
            for (x = 0; x < w; x++)
            {
                for (z = 0; z < h; z++)
                {
                    if (id == 0)
                    {
                        biome = (EnumBiomeIsland)ar[z * width * zoom + x * zoom];
                        if (biome != EnumBiomeIsland.Sea)
                        {
                            _SetPixel(rgba, w, x, z, ConvertBiome(biome));
                        }
                    }
                    else
                    {
                        key = ar[z * width * zoom + x * zoom];
                        if (key != 0)
                        {
                            _SetPixel(rgba, w, x, z, _ConvertBiomeHeight(key));
                        }
                    }
                }
            }
            game.Log.Log("SetPixelGenBioms[{1}:{2}]: {0:0.00} ms",
                (stopwatch.ElapsedTicks - l) / timerFrequency, width, height);
            PngWriter.Write("biomeLayer" + (id == 0 ? "" : "H") + ".png", w, h, rgba, true);
        }

        private static void _SetPixel(byte[] rgba, int width, int x, int z, uint argb)
        {
            int i = (z * width + x) * 4;
            rgba[i] = (byte)(argb >> 16);
            rgba[i + 1] = (byte)(argb >> 8);
            rgba[i + 2] = (byte)argb;
            rgba[i + 3] = (byte)(argb >> 24);
        }

        private static uint _ConvertBiomeHeight(int index)
        {
            uint rgb = (uint)(byte)(255 - index * 2);
            return 0xFF000000 | rgb << 16 | rgb << 8 | rgb;
        }

        /// <summary>
        /// Цвет биома ARGB: значения именованных цветов System.Drawing.Color, которые брал оригинал
        /// </summary>
        public static uint ConvertBiome(EnumBiomeIsland biome)
        {
            switch (biome)
            {
                case EnumBiomeIsland.Sea: return 0xFF0000FF; // Blue, море
                case EnumBiomeIsland.River: return 0xFF0000CD; // MediumBlue, река
                case EnumBiomeIsland.Plain: return 0xFF90EE90; // LightGreen, равнина
                case EnumBiomeIsland.Desert: return 0xFFFFFF00; // Yellow, пустыня
                case EnumBiomeIsland.Beach: return 0xFFFFFFFF; // White, пляж
                case EnumBiomeIsland.MixedForest: return 0xFF008000; // Green, смешанный лес
                case EnumBiomeIsland.ConiferousForest: return 0xFF006400; // DarkGreen, хвойный лес
                case EnumBiomeIsland.BirchForest: return 0xFF228B22; // ForestGreen, берёзовый лес
                case EnumBiomeIsland.Tropics: return 0xFFFFA500; // Orange, тропики
                case EnumBiomeIsland.Swamp: return 0xFF5F9EA0; // CadetBlue, болото
                case EnumBiomeIsland.Mountains: return 0xFFD3D3D3; // LightGray, горы
                case EnumBiomeIsland.MountainsDesert: return 0xFFA52A2A; // Brown, горы в пустыне
            }
            return 0xFF000000; // Black
        }
    }
}
