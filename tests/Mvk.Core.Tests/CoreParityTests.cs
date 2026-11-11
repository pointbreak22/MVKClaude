using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoldenGen;
using Mvk.Core.Nbt;
using Mvk.Core.Util;

namespace Mvk.Core.Tests
{
    /// <summary>
    /// Rand, NBT и Json из Mvk.Core должны давать те же байты, что старый Vge
    /// (эталон Golden/core.csv снят утилитой tools/GoldenGen в режиме core)
    /// </summary>
    public class CoreParityTests
    {
        public static IEnumerable<object[]> Names()
            => _LoadGolden().Keys.Select(k => new object[] { k });

        [Theory]
        [MemberData(nameof(Names))]
        public void SameAsOriginal(string name)
        {
            Dictionary<string, string> actual = CoreGolden.Compute().ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal(_LoadGolden()[name], actual[name]);
        }

        [Fact]
        public void AllCovered()
        {
            Assert.Equal(_LoadGolden().Keys.OrderBy(k => k), CoreGolden.Compute().Select(p => p.Key).OrderBy(k => k));
        }

        [Fact]
        public void TruncatedNbtThrows()
        {
            TagCompound nbt = new TagCompound();
            nbt.SetByteArray("a", new byte[] { 1, 2, 3, 4 });
            NBTStream stream = new NBTStream();
            nbt.Write(stream);
            byte[] bytes = stream.ToArray();
            NBTStream cut = new NBTStream();
            cut.Write(bytes, 0, bytes.Length - 2);
            cut.Position = 0;
            Assert.Throws<EndOfStreamException>(() => new TagCompound().Read(cut));
        }

        [Fact]
        public void ListFastContainsStructWithoutBoxing()
        {
            ListFast<Mvk.Core.Maths.Vector3i> list = new ListFast<Mvk.Core.Maths.Vector3i>(2);
            list.Add(new Mvk.Core.Maths.Vector3i(1, 2, 3));
            list.Add(new Mvk.Core.Maths.Vector3i(4, 5, 6));
            list.Add(new Mvk.Core.Maths.Vector3i(7, 8, 9));
            Assert.True(list.Contains(new Mvk.Core.Maths.Vector3i(7, 8, 9)));
            Assert.False(list.Contains(new Mvk.Core.Maths.Vector3i(0, 0, 0)));
            Assert.Equal(3, list.Count);
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("   ", true)]
        [InlineData("  # comment", true)]
        [InlineData("{ \"a\": 1 }", false)]
        public void ChekComment(string line, bool expected)
        {
            Assert.Equal(expected, Sundry.ChekComment(line));
        }

        private static Dictionary<string, string> _LoadGolden()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Golden", "core.csv");
            return File.ReadAllLines(path)
                .Where(l => l.Length > 0 && l[0] != '#')
                .Select(l => l.Split(';'))
                .ToDictionary(p => p[0], p => p[1]);
        }
    }
}
