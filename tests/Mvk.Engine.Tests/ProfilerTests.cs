using System.Collections.Generic;
using System.IO;
using System.Threading;
using Mvk.Engine.Util;

namespace Mvk.Engine.Tests
{
    /// <summary>
    /// Имена секций профайлера собираются лениво (improvements R20), а текст в логе как в оригинале
    /// </summary>
    public class ProfilerTests
    {
        [Fact]
        public void LazySectionNamesLogSameText()
        {
            var log = new Logger(Path.Combine(Path.GetTempPath(), "MvkProfilerTest"), false);
            var lines = new List<string>();
            log.Loged += (_, e) => lines.Add(e.Text);
            var profiler = new Profiler(log, "S: ");

            profiler.StartSection("TickChunk ", 3, -4);
            Thread.Sleep(5);
            profiler.EndSection(0);
            profiler.StartSection("Ai ", "Chicken");
            Thread.Sleep(5);
            profiler.EndSection(0);
            profiler.StartSection("Plain");
            Thread.Sleep(5);
            profiler.EndSection(0);

            Assert.Equal(3, lines.Count);
            Assert.StartsWith("S: Долго! TickChunk 3:-4 заняло", lines[0]);
            Assert.StartsWith("S: Долго! Ai Chicken заняло", lines[1]);
            Assert.StartsWith("S: Долго! Plain заняло", lines[2]);
        }
    }
}
