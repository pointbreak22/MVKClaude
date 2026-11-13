using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Mvk.Platform;
using Mvk.Platform.Audio;
using Mvk.Platform.Input;
using Mvk.Rendering;
using Mvk.Rendering.OpenGL;
using Silk.NET.Input;
using Silk.NET.Maths;
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
            bool testScene = false;
            int slot = 0;
            long seed = 2;
            double shotAfter = 8;
            List<string> commands = new List<string>();
            float[]? look = null;
            int view = 0;
            bool profile = false;
            int[]? size = null;
            int[]? resize = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--assets" && i + 1 < args.Length) assets = args[++i];
                else if (args[i] == "--screenshot" && i + 1 < args.Length) screenshot = Path.GetFullPath(args[++i]);
                else if (args[i] == "--frame" && i + 1 < args.Length) shotFrame = int.Parse(args[++i]);
                else if (args[i] == "--wireframe") wireframe = true;
                else if (args[i] == "--test-scene") testScene = true;
                else if (args[i] == "--slot" && i + 1 < args.Length) slot = int.Parse(args[++i]);
                else if (args[i] == "--seed" && i + 1 < args.Length) seed = long.Parse(args[++i]);
                else if (args[i] == "--cmd" && i + 1 < args.Length) commands.Add(args[++i]);
                else if (args[i] == "--size" && i + 1 < args.Length) size = _Size(args[++i]);
                else if (args[i] == "--resize" && i + 1 < args.Length) resize = _Size(args[++i]);
                else if (args[i] == "--profile") profile = true;
                else if (args[i] == "--view" && i + 1 < args.Length) view = int.Parse(args[++i]);
                else if (args[i] == "--look" && i + 1 < args.Length)
                {
                    // Поворот и наклон в градусах через запятую
                    string[] parts = args[++i].Split(',');
                    look = new float[] { _Deg(parts[0]), _Deg(parts[1]) };
                }
                else if (args[i] == "--shot-after" && i + 1 < args.Length) shotAfter = double.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
            }
            assets ??= FindAssets();
            if (assets == null || !Directory.Exists(assets))
            {
                Console.Error.WriteLine("Не найдена папка Assets. Укажите её: --assets <путь>");
                return 2;
            }
            if (!testScene) return RunGame(assets, screenshot, shotAfter, slot, seed, wireframe, commands, look, view, size, resize, profile);

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
        /// Локальная игра: остров в слоте, обзор мышью (щелчок по окну), WASD, пробел, F3+клавиши отладки.
        /// С --screenshot ждёт входа в мир и ещё shotAfter секунд построения сеток, сохраняет кадр и выходит.
        /// --cmd отправляет команды чата после входа в мир (и включает отладочную /summon), --look задаёт
        /// взгляд, --view переключает камеру как F5, --size задаёт размер окна при старте, --resize меняет
        /// его на половине ожидания снимка, --profile печатает аллокации и время кадра и такта
        /// во второй половине ожидания (для снимков и замеров без человека).
        /// </summary>
        private static int RunGame(string assets, string? screenshot, double shotAfter, int slot, long seed, bool wireframe,
            List<string> commands, float[]? look, int view, int[]? size, int[]? resize, bool profile)
        {
            using IWindow window = WindowFactory.CreateOpenGL("Малювекi 2 - MvkClaude",
                size?[0] ?? 1280, size?[1] ?? 720);
            IGraphicsDevice? device = null;
            GameScene? scene = null;
            IInputContext? input = null;
            Stopwatch? sinceStart = null;
            FrameProfiler? profiler = profile ? new FrameProfiler() : null;
            int result = 0;

            window.Load += () =>
            {
                device = new OpenGLDevice(GL.GetApi(window));
                Console.WriteLine(device.Description);
                Mvk.Engine.Debug.IsDrawVoxelLine = wireframe;
                scene = new GameScene(device, assets, slot, seed, window.FramebufferSize.X, window.FramebufferSize.Y, commands.Count > 0);

                input = window.CreateInput();
                foreach (IKeyboard k in input.Keyboards)
                {
                    k.KeyDown += (_, key, _) => scene.KeyDown(key);
                    k.KeyUp += (_, key, _) => scene.KeyUp(key);
                    k.KeyChar += (_, c) => scene.KeyChar(c);
                }
                foreach (IMouse m in input.Mice)
                {
                    m.MouseDown += (_, b) => scene.MouseDown(b);
                    m.MouseUp += (_, b) => scene.MouseUp(b);
                    m.MouseMove += (_, p) => scene.MouseMove(p);
                    m.Scroll += (_, w) => scene.MouseWheel(w.Y);
                }
                scene.MouseLookChanged += look =>
                {
                    foreach (IMouse m in input.Mice)
                    {
                        m.Cursor.CursorMode = look ? CursorMode.Raw : CursorMode.Normal;
                        scene.ResetMouse(m.Position);
                    }
                };
            };

            window.FramebufferResize += fb => scene?.Resized(fb.X, fb.Y);

            window.Update += _ =>
            {
                if (scene == null) return;
                profiler?.Begin();
                scene.Update();
                if (profiler is { Running: true }) profiler.EndUpdate();
                if (scene.Error != null)
                {
                    Console.Error.WriteLine("Ошибка игры: " + scene.Error);
                    result = 1;
                    window.Close();
                }
                else if (scene.Stopped != null)
                {
                    Console.WriteLine("Игра остановлена: " + scene.Stopped);
                    window.Close();
                }
            };

            window.Render += _ =>
            {
                if (device == null || scene == null) return;
                int w = window.FramebufferSize.X;
                int h = window.FramebufferSize.Y;
                // Свёрнутое окно не рисуем
                if (w <= 0 || h <= 0) return;
                profiler?.Begin();
                scene.Render(w, h);
                if (profiler is { Running: true }) profiler.EndRender();
                if (profiler is { Running: false } && sinceStart != null && sinceStart.Elapsed.TotalSeconds >= shotAfter / 2)
                {
                    profiler.Start();
                }
                if (scene.Started && sinceStart == null)
                {
                    sinceStart = Stopwatch.StartNew();
                    foreach (string command in commands) scene.SendCommand(command);
                    scene.ViewCameraNext(view);
                }
                if (look != null && screenshot != null && scene.Started) scene.Look(look[0], look[1]);
                if (resize != null && sinceStart != null && sinceStart.Elapsed.TotalSeconds >= shotAfter / 2)
                {
                    // Ресайз в середине ожидания: игрок уже стоит, камера сама не обновляется
                    window.Size = new Vector2D<int>(resize[0], resize[1]);
                    resize = null;
                }
                if (screenshot != null && sinceStart != null)
                {
                    if (sinceStart.Elapsed.TotalSeconds >= shotAfter)
                    {
                        byte[] pixels = new byte[w * h * 4];
                        device.ReadPixels(0, 0, w, h, pixels);
                        PngWriter.Write(screenshot, w, h, pixels);
                        Console.WriteLine($"Снимок {w}x{h}: {screenshot}");
                        if (profiler != null) Console.WriteLine(profiler.Report());
                        window.Close();
                    }
                }
            };

            window.Closing += () =>
            {
                scene?.Dispose();
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

        private static int[] _Size(string value)
        {
            string[] parts = value.Split('x', 'X');
            return new[] { int.Parse(parts[0]), int.Parse(parts[1]) };
        }

        private static float _Deg(string value)
            => float.Parse(value, System.Globalization.CultureInfo.InvariantCulture) * MathF.PI / 180f;
    }
}
