using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoldenGen;

namespace Mvk.Core.Tests
{
    /// <summary>
    /// Новая математика Mvk.Core.Maths должна давать побитно те же результаты, что старый WinGL.Util
    /// (эталон Golden/math.csv снят утилитой tools/GoldenGen в режиме math)
    /// </summary>
    public class MathParityTests
    {
        public static IEnumerable<object[]> Names()
            => _LoadGolden().Keys.Select(k => new object[] { k });

        [Theory]
        [MemberData(nameof(Names))]
        public void SameAsOriginal(string name)
        {
            Dictionary<string, string> golden = _LoadGolden();
            Dictionary<string, string> actual = MathGolden.Compute().ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal(golden[name], actual[name]);
        }

        [Fact]
        public void AllFunctionsCovered()
        {
            Assert.Equal(_LoadGolden().Keys.OrderBy(k => k), MathGolden.Compute().Select(p => p.Key).OrderBy(k => k));
        }

        private static Dictionary<string, string> _LoadGolden()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Golden", "math.csv");
            return File.ReadAllLines(path)
                .Where(l => l.Length > 0 && l[0] != '#')
                .Select(l => l.Split(';'))
                .ToDictionary(p => p[0], p => p[1]);
        }
    }

    public class VectorTests
    {
        [Fact]
        public void EqualityAndHash()
        {
            Vector3i a = new Vector3i(1, 2, 3);
            Vector3i b = new Vector3i(1, 2, 3);
            Assert.True(a == b);
            Assert.True(a.Equals((object)b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.NotEqual(new Vector3i(1, 2, 3).GetHashCode(), new Vector3i(2, 1, 3).GetHashCode());
        }

        [Theory]
        [InlineData(-0.5f, -1)]
        [InlineData(-1f, -1)]
        [InlineData(0.99f, 0)]
        [InlineData(-1.01f, -2)]
        public void FloorNegative(float value, int expected)
        {
            Assert.Equal(expected, Mth.Floor(value));
        }

        [Fact]
        public void SinTableReadyWithoutInit()
        {
            // В оригинале без Glm.Initialized() здесь был бы 0
            Assert.True(Glm.Sin(Glm.Pi / 2f) > 0.99f);
        }
    }
}
