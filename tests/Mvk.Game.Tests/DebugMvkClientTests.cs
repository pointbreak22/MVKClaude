using System.Drawing;
using Mvk.Game.Client;
using Mvk.Game.World.Biome;

namespace Mvk.Game.Tests
{
    /// <summary>
    /// F6 (карта биомов): цвета те же, что именованные цвета System.Drawing оригинала
    /// </summary>
    public class DebugMvkClientTests
    {
        [Theory]
        [InlineData(EnumBiomeIsland.Sea, "Blue")]
        [InlineData(EnumBiomeIsland.River, "MediumBlue")]
        [InlineData(EnumBiomeIsland.Plain, "LightGreen")]
        [InlineData(EnumBiomeIsland.Desert, "Yellow")]
        [InlineData(EnumBiomeIsland.Beach, "White")]
        [InlineData(EnumBiomeIsland.MixedForest, "Green")]
        [InlineData(EnumBiomeIsland.ConiferousForest, "DarkGreen")]
        [InlineData(EnumBiomeIsland.BirchForest, "ForestGreen")]
        [InlineData(EnumBiomeIsland.Tropics, "Orange")]
        [InlineData(EnumBiomeIsland.Swamp, "CadetBlue")]
        [InlineData(EnumBiomeIsland.Mountains, "LightGray")]
        [InlineData(EnumBiomeIsland.MountainsDesert, "Brown")]
        public void BiomeColorsAsSystemDrawing(EnumBiomeIsland biome, string colorName)
            => Assert.Equal((uint)Color.FromName(colorName).ToArgb(), DebugMvkClient.ConvertBiome(biome));
    }
}
