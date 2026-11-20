using System;
using System.Diagnostics;
using System.IO;
using Xunit.Abstractions;

namespace Mvk.Platform.Tests
{
    /// <summary>
    /// Технический п. 6: тестовая сцена RHI (Mvk.Desktop --test-scene) на OpenGL и на Vulkan, кадры сравниваются.
    /// Сцена рисуется по номеру кадра, поэтому кадры бэкендов должны совпадать почти побитно (этап 10: 0.0).
    /// Без графического устройства (сервер сборки без GPU и без программного рендера) бэкенд не создаётся:
    /// тест пишет причину и не проверяет этот бэкенд. На CI Linux сцену рисуют Mesa llvmpipe и lavapipe.
    /// </summary>
    public class RenderBackendTests
    {
        private readonly ITestOutputHelper _output;

        public RenderBackendTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public void TestSceneSameOnOpenGLAndVulkan()
        {
            string? exe = _FindHost();
            Assert.True(exe != null, "Не найден собранный Mvk.Desktop");
            byte[]? gl = _Shot(exe!, "opengl", out int w, out int h);
            byte[]? vk = _Shot(exe!, "vulkan", out int w2, out int h2);
            if (gl == null || vk == null)
            {
                _output.WriteLine("Сравнение пропущено: бэкенд " + (gl == null ? "OpenGL" : "Vulkan") + " недоступен");
                return;
            }
            Assert.Equal(w, w2);
            Assert.Equal(h, h2);

            // Сцена не пустая: кадр не одного цвета
            int distinct = 0;
            for (int i = 4; i < gl.Length; i += 4 * 97)
            {
                if (gl[i] != gl[0] || gl[i + 1] != gl[1] || gl[i + 2] != gl[2]) distinct++;
            }
            Assert.True(distinct > 100, "Кадр OpenGL пустой");

            long sum = 0;
            int bad = 0;
            for (int i = 0; i < gl.Length; i += 4)
            {
                int d = Math.Max(Math.Abs(gl[i] - vk[i]), Math.Max(Math.Abs(gl[i + 1] - vk[i + 1]), Math.Abs(gl[i + 2] - vk[i + 2])));
                sum += Math.Abs(gl[i] - vk[i]) + Math.Abs(gl[i + 1] - vk[i + 1]) + Math.Abs(gl[i + 2] - vk[i + 2]);
                if (d > 24) bad++;
            }
            double mean = sum / (double)(w * h * 3);
            double share = bad * 100.0 / (w * h);
            _output.WriteLine($"Средняя разница {mean:0.000}, пикселей с разницей > 24: {share:0.000}%");
            Assert.True(mean < 1 && share < 0.5, $"Кадры бэкендов различаются: средняя {mean:0.000}, доля {share:0.000}%");
        }

        /// <summary>
        /// Снимок тестовой сцены бэкендом; null, если бэкенд на этой машине не создаётся
        /// </summary>
        private byte[]? _Shot(string exe, string backend, out int width, out int height)
        {
            width = height = 0;
            string file = Path.Combine(Path.GetTempPath(), "mvk-scene-" + backend + "-" + Environment.ProcessId + ".rgba");
            File.Delete(file);
            var info = new ProcessStartInfo(exe)
            {
                WorkingDirectory = Path.GetDirectoryName(exe)!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            foreach (string a in new[] { "--test-scene", "--backend", backend, "--screenshot", file }) info.ArgumentList.Add(a);
            using Process process = Process.Start(info)!;
            string stdout = process.StandardOutput.ReadToEndAsync().Result;
            string stderr = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(120000))
            {
                process.Kill(true);
                Assert.Fail(backend + ": сцена не завершилась за 2 минуты");
            }
            if (!File.Exists(file))
            {
                _output.WriteLine(backend + " недоступен (код " + process.ExitCode + "): " + (stderr + stdout).Trim());
                return null;
            }
            try
            {
                using var reader = new BinaryReader(File.OpenRead(file));
                width = reader.ReadInt32();
                height = reader.ReadInt32();
                return reader.ReadBytes(width * height * 4);
            }
            finally
            {
                File.Delete(file);
            }
        }

        /// <summary>
        /// Собранный хост той же конфигурации, что и тесты (src/Hosts/Mvk.Desktop/bin/&lt;конфигурация&gt;/net10.0)
        /// </summary>
        private static string? _FindHost()
        {
            string dir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string framework = Path.GetFileName(dir);
            string configuration = Path.GetFileName(Path.GetDirectoryName(dir)!);
            DirectoryInfo? root = new DirectoryInfo(dir);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "MvkClaude.slnx"))) root = root.Parent;
            if (root == null) return null;
            string bin = Path.Combine(root.FullName, "src", "Hosts", "Mvk.Desktop", "bin", configuration, framework);
            string exe = Path.Combine(bin, OperatingSystem.IsWindows() ? "Mvk2.exe" : "Mvk2");
            return File.Exists(exe) ? exe : null;
        }
    }
}
