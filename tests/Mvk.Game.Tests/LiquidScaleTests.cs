using Mvk.Game.Item;

namespace Mvk.Game.Tests
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] known-issues п. 2 и 3: согласованные шкалы ведра (1..7) и уровня блока жидкости (1..15)
    /// </summary>
    public class LiquidScaleTests
    {
        [Fact]
        public void SourceFillsEmptyBucketAndIsRemoved()
        {
            // met 0 — источник
            var (bucket, rest) = LiquidScale.Scoop(0, LiquidScale.LevelOfMet(0));
            Assert.Equal(LiquidScale.BucketFull, bucket);
            Assert.Equal(0, rest);
        }

        [Fact]
        public void FullBucketPoursSource()
        {
            Assert.Equal(0, LiquidScale.MetOfLevel(LiquidScale.LevelOfBucket(LiquidScale.BucketFull)));
            // Ведро из творческого инвентаря (Met 0) полное: выливает источник, как в оригинале
            Assert.Equal(LiquidScale.BucketFull, LiquidScale.AmountOfBucket(0));
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        [InlineData(7, 3)]
        [InlineData(8, 4)]
        [InlineData(12, 6)]
        [InlineData(14, 6)]
        public void FlowingLevelGivesProportionalAmount(int level, int amount)
        {
            var (bucket, rest) = LiquidScale.Scoop(0, level);
            Assert.Equal(amount, bucket);
            // В пустое ведро всё влезает: блок убирается
            Assert.Equal(0, rest);
        }

        [Fact]
        public void FlowingLiquidNeverMakesSource()
        {
            for (int level = 1; level < LiquidScale.LevelSource; level++)
            {
                int bucket = LiquidScale.Scoop(0, level).Bucket;
                Assert.True(bucket < LiquidScale.BucketFull, $"Уровень {level} дал полное ведро");
                Assert.True(LiquidScale.LevelOfBucket(bucket) < LiquidScale.LevelSource);
            }
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(3, 6)]
        [InlineData(6, 13)]
        public void PartialBucketPoursProportionalLevel(int amount, int level)
            => Assert.Equal(level, LiquidScale.LevelOfBucket(amount));

        [Fact]
        public void PartialBucketTakesOnlyWhatFitsRestStaysInBlock()
        {
            // В ведре 3 из 7, источник: влезает 4, в блоке остаётся уровень 15 - 9 = 6
            var (bucket, rest) = LiquidScale.Scoop(3, LiquidScale.LevelSource);
            Assert.Equal(LiquidScale.BucketFull, bucket);
            Assert.Equal(15 - LiquidScale.LevelOfBucket(4), rest);
            Assert.True(rest > 0);
        }

        [Fact]
        public void PartialBucketTakesWholeSmallBlock()
        {
            // В ведре 5, уровень 4 даёт 2: влезает всё, блок убирается
            var (bucket, rest) = LiquidScale.Scoop(5, 4);
            Assert.Equal(7, bucket);
            Assert.Equal(0, rest);
        }

        [Fact]
        public void FullBucketTakesNothing()
        {
            var (bucket, rest) = LiquidScale.Scoop(LiquidScale.BucketFull, 10);
            Assert.Equal(LiquidScale.BucketFull, bucket);
            Assert.Equal(10, rest);
        }

        [Fact]
        public void LevelsAndMetRoundTrip()
        {
            for (int met = 0; met < LiquidScale.LevelSource; met++)
            {
                Assert.Equal(met, LiquidScale.MetOfLevel(LiquidScale.LevelOfMet(met)));
            }
        }
    }
}
