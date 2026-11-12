using System;
using System.IO;
using Mvk.Platform;
using Mvk.Platform.Audio;
using Mvk.Platform.Input;
using Mvk.Rendering;
using Mvk.Rendering.OpenGL;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Mvk.Desktop
{
    /// <summary>
    /// Точка входа для ПК.
    /// Этап 4: тестовая сцена RHI (кубы с текстурами блоков). Игра появится здесь на этапе 5.
    /// </summary>
    /// <remarks>
    /// Параметры:
    ///   --assets путь       папка Assets (по умолчанию ищется рядом с программой и в original/res)
    ///   --screenshot файл   нарисовать кадр, сохранить PNG и выйти (проверка рендера без человека)
    ///   --frame N           номер кадра для снимка при 60 кадрах в секунду (по умолчанию 90)
    ///   --wireframe         начать в каркасном режиме
    /// Управление: A/D или стрелки вращают камеру, F3 каркас, пробел пауза, ЛКМ звук, Esc выход.
    /// </remarks>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string? assets = null;
            string? screenshot = null;
            int shotFrame = 90;
            bool wireframe = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--assets" && i + 1 < args.Length) assets = args[++i];
                else if (args[i] == "--screenshot" && i + 1 < args.Length) screenshot = Path.GetFullPath(args[++i]);
                else if (args[i] == "--frame" && i + 1 < args.Length) shotFrame = int.Parse(args[++i]);
                else if (args[i] == "--wireframe") wireframe = true;
            }
            assets ??= FindAssets();
            if (assets == null || !Directory.Exists(assets))
            {
                Console.Error.WriteLine("Не найдена папка Assets. Укажите её: --assets <путь>");
                return 2;
            }

            using IWindow window = WindowFactory.CreateOpenGL("Малювекi 2 - MvkClaude", 1280, 720);
            IGraphicsDevice? device = null;
            TestScene? scene = null;
            InputDevice? input = null;
            AudioDevice? audio = null;
            int clickSound = -1;
            InputAction exit = default, left = default, right = default, wire = default, pause = default, click = default;
            float time = 0;
            int frame = 0;
            bool paused = false;
            int result = 0;

            window.Load += () =>
            {
                device = new OpenGLDevice(GL.GetApi(window));
                Console.WriteLine(device.Description);
                scene = new TestScene(device, assets) { Wireframe = wireframe };

                input = new InputDevice(window);
                InputActionMap map = input.Actions;
                exit = map.Register("Exit", InputBinding.Of(Key.Escape));
                left = map.Register("CameraLeft", InputBinding.Of(Key.A), InputBinding.Of(Key.Left));
                right = map.Register("CameraRight", InputBinding.Of(Key.D), InputBinding.Of(Key.Right));
                wire = map.Register("Wireframe", InputBinding.Of(Key.F3));
                pause = map.Register("Pause", InputBinding.Of(Key.Space));
                click = map.Register("Click", InputBinding.Of(MouseButton.Left));

                if (screenshot == null)
                {
                    try
                    {
                        audio = new AudioDevice();
                        clickSound = audio.AddSample(PcmSample.DecodeOgg(Path.Combine(assets, "Mvk2Sounds", "Click.ogg")));
                    }
                    catch (Exception e)
                    {
                        // Без звуковой карты сцена всё равно должна работать
                        Console.Error.WriteLine("Звук отключён: " + e.Message);
                        audio = null;
                    }
                }
            };

            window.FocusChanged += focused =>
            {
                if (!focused) input?.Actions.ReleaseAll();
            };

            window.Update += delta =>
            {
                if (input == null || scene == null) return;
                InputActionMap map = input.Actions;
                if (map.WasPressed(exit)) window.Close();
                if (map.WasPressed(wire)) scene.Wireframe = !scene.Wireframe;
                if (map.WasPressed(pause)) paused = !paused;
                if (map.WasPressed(click)) audio?.Play(clickSound, 0, 0, 0, 1f, 1f);
                if (map.IsDown(left)) scene.CameraYaw -= (float)delta * 1.5f;
                if (map.IsDown(right)) scene.CameraYaw += (float)delta * 1.5f;
                audio?.Tick();
                input.EndFrame();
                if (screenshot == null && !paused) time += (float)delta;
            };

            window.Render += _ =>
            {
                if (device == null || scene == null) return;
                int w = window.FramebufferSize.X;
                int h = window.FramebufferSize.Y;
                if (screenshot != null)
                {
                    // Снимок не зависит от скорости машины: время задаётся номером кадра
                    scene.Render(w, h, shotFrame / 60f);
                    if (++frame >= 3)
                    {
                        byte[] pixels = new byte[w * h * 4];
                        device.ReadPixels(0, 0, w, h, pixels);
                        PngWriter.Write(screenshot, w, h, pixels);
                        Console.WriteLine($"Снимок {w}x{h}: {screenshot}");
                        window.Close();
                    }
                    return;
                }
                scene.Render(w, h, time);
            };

            window.Closing += () =>
            {
                scene?.Dispose();
                audio?.Dispose();
                input?.Dispose();
                device?.Dispose();
            };

            try
            {
                window.Run();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                result = 1;
            }
            return result;
        }

        /// <summary>
        /// Найти Assets: рядом с программой, иначе в original/res (пока ассеты не перенесены, этап 5)
        /// </summary>
        private static string? FindAssets()
        {
            string local = Path.Combine(AppContext.BaseDirectory, "Assets");
            if (Directory.Exists(local)) return local;
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "original", "res", "Assets");
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            return null;
        }
    }
}
