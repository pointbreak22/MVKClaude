using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoldenGen;

namespace Mvk.Platform.Tests
{
    /// <summary>
    /// PcmSample.DecodeOgg даёт те же байты PCM16, что AudioSample.LoadOgg оригинала, для всех звуков ассетов
    /// </summary>
    public class AudioParityTests
    {
        [Fact]
        public void AllSoundsSameAsOriginal()
        {
            Dictionary<string, string> golden = _LoadGolden();
            var actual = AudioGolden.Compute(_AssetsDir()).ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal(golden.Keys.OrderBy(k => k, StringComparer.Ordinal),
                actual.Keys.OrderBy(k => k, StringComparer.Ordinal));
            var diff = golden.Where(p => actual[p.Key] != p.Value)
                .Select(p => p.Key + ": ждали " + p.Value + ", получили " + actual[p.Key]).ToList();
            Assert.True(diff.Count == 0, string.Join("\n", diff));
        }

        /// <summary>
        /// Ассеты игры (assets/, байты как в res/Assets оригинала)
        /// </summary>
        private static string _AssetsDir()
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MvkClaude.slnx"))) dir = dir.Parent;
            Assert.NotNull(dir);
            string assets = Path.Combine(dir.FullName, "assets");
            Assert.True(Directory.Exists(assets), "Нет ассетов оригинала: " + assets);
            return assets;
        }

        private static Dictionary<string, string> _LoadGolden()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Golden", "audio.csv");
            return File.ReadAllLines(path)
                .Where(l => l.Length > 0 && l[0] != '#')
                .Select(l => l.Split(';'))
                .ToDictionary(p => p[0], p => p[1]);
        }
    }
}
