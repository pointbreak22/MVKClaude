using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Mvk.Core.Util;
using Mvk.Engine;
using Mvk.Engine.Games;
using Mvk.Game.Client;
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
            bool menu = false;
            bool sound = false;
            List<string> presses = new List<string>();
            bool holdLeft = false;
            bool fx = false;
            int phase = -1;
            WorldMouseLayout mouseLayout = DesktopApp.DefaultMouseLayout;
            string? screen = null;
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
                else if (args[i] == "--menu") menu = true;
                else if (args[i] == "--sound") sound = true;
                else if (args[i] == "--press" && i + 1 < args.Length) presses.Add(args[++i]);
                else if (args[i] == "--hold-left") holdLeft = true;
                else if (args[i] == "--fx") fx = true;
                else if (args[i] == "--calm") phase = 32;
                else if (args[i] == "--phase" && i + 1 < args.Length) phase = int.Parse(args[++i]) & 0x7F;
                else if (args[i] == "--mouse" && i + 1 < args.Length)
                    mouseLayout = args[++i] == "original" ? WorldMouseLayout.Original : WorldMouseLayout.Swapped;
                else if (args[i] == "--screen" && i + 1 < args.Length) screen = args[++i];
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
            if (!testScene) return RunGame(assets, screenshot, shotAfter, slot, seed, wireframe, commands, look, view, size, resize, profile, menu, screen, sound, presses, holdLeft, fx, mouseLayout, phase);

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
        /// Игра с меню (WindowMvk): заставка, главное меню, одиночная игра. Без --screenshot играет человек.
        /// С --screenshot после загрузки сразу запускается игра в слоте (--menu: снимок главного меню),
        /// ждёт входа в мир и ещё shotAfter секунд построения сеток, сохраняет кадр и выходит.
        /// --screen chat|inventory|creative|storage|pause открывает экран клавишей на половине ожидания.
        /// Снимки идут без звука (быстрее загрузка), --sound включает его. --press F3+G нажимает комбинацию
        /// клавиш (имена Keys оригинала) на половине ожидания, --hold-left зажимает левую кнопку мыши,
        /// --calm снимает кадр при ветре около нуля (для сравнения с оригиналом, tools/OrigShot),
        /// --phase N в такт с фазой ветра N (0..127, 0 наибольший ветер).
        /// --fx спавнит плоские частицы перед игроком. --mouse original возвращает кнопки мыши в мире как в оригинале
        /// (левая ломает, правая ставит); по умолчанию наоборот.
        /// --cmd отправляет команды чата после входа в мир (и включает отладочную /summon), --look задаёт
        /// взгляд, --view переключает камеру как F5, --size задаёт размер окна при старте, --resize меняет
        /// его на половине ожидания снимка, --profile печатает аллокации и время кадра и такта
        /// во второй половине ожидания (для снимков и замеров без человека).
        /// </summary>
        private static int RunGame(string assets, string? screenshot, double shotAfter, int slot, long seed, bool wireframe,
            List<string> commands, float[]? look, int view, int[]? size, int[]? resize, bool profile,
            bool menu, string? screen, bool sound, List<string> presses, bool holdLeft, bool fx,
            WorldMouseLayout mouseLayout, int phase)
        {
            using IWindow window = WindowFactory.CreateOpenGL("Малювекi 2 - MvkClaude",
                size?[0] ?? 1280, size?[1] ?? 720);
            IGraphicsDevice? device = null;
            DesktopApp? app = null;
            Stopwatch? sinceStart = null;
            FrameProfiler? profiler = profile ? new FrameProfiler() : null;
            bool screenOpened = false;
            int result = 0;

            window.Load += () =>
            {
                GL gl = GL.GetApi(window);
                device = new OpenGLDevice(gl);
                Console.WriteLine(device.Description);
                gl.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, (FramebufferAttachment)0x1801,
                    FramebufferAttachmentParameterName.DepthSize, out int depthBits);
                Console.WriteLine("Буфер глубины: " + depthBits + " бит");
                Mvk.Engine.Debug.IsDrawVoxelLine = wireframe;
                MvkClient.SetAssets(assets);
                app = new DesktopApp(window, device, commands.Count > 0, screenshot == null || sound, mouseLayout);
            };

            window.FramebufferResize += fb => app?.App.OnResized(fb.X, fb.Y);

            window.Update += _ =>
            {
                if (app == null) return;
                profiler?.Begin();
                app.Update();
                if (profiler is { Running: true }) profiler.EndUpdate();
                if (app.Error != null) result = 1;
                // Снимок без человека: после загрузки сразу одиночная игра (как кнопки «Одиночная» и «Играть»)
                if (screenshot != null && !menu && app.App.Ready && app.App.Game == null && sinceStart == null)
                {
                    app.App.GameLocalRun(new GameSettings(slot, seed));
                }
            };

            window.Render += _ =>
            {
                if (device == null || app == null) return;
                int w = window.FramebufferSize.X;
                int h = window.FramebufferSize.Y;
                // Свёрнутое окно не рисуем
                if (w <= 0 || h <= 0) return;
                profiler?.Begin();
                app.Render(w, h);
                if (profiler is { Running: true }) profiler.EndRender();
                if (profiler is { Running: false } && sinceStart != null && sinceStart.Elapsed.TotalSeconds >= shotAfter / 2)
                {
                    profiler.Start();
                }
                bool started = menu ? app.App.Ready : app.App.GameHost?.Started == true;
                if (started && sinceStart == null)
                {
                    sinceStart = Stopwatch.StartNew();
                    foreach (string command in commands) app.SendCommand(command);
                    app.ViewCameraNext(view);
                }
                if (look != null && screenshot != null && sinceStart != null && !menu) app.Look(look[0], look[1]);
                if (sinceStart != null && sinceStart.Elapsed.TotalSeconds >= shotAfter / 2)
                {
                    if (resize != null)
                    {
                        // Ресайз в середине ожидания: игрок уже стоит, камера сама не обновляется
                        window.Size = new Vector2D<int>(resize[0], resize[1]);
                        resize = null;
                    }
                    if (fx)
                    {
                        // Плоские частицы перед игроком: дым (спрайт) и капли (квад), как спавнит клиентский мир
                        fx = false;
                        app.SpawnFlatParticles();
                    }
                    if (holdLeft)
                    {
                        // Зажать левую кнопку (ломать блок под прицелом) до снимка
                        holdLeft = false;
                        // Первый щелчок, как у человека, только включает обзор мышью (GameBase.OnMouseDown)
                        app.App.OnMouseDown(Mvk.Engine.Actions.MouseButton.Left, Gi.Width / 2, Gi.Height / 2);
                        app.App.OnMouseUp(Mvk.Engine.Actions.MouseButton.Left, Gi.Width / 2, Gi.Height / 2);
                        app.App.OnMouseDown(Mvk.Engine.Actions.MouseButton.Left, Gi.Width / 2, Gi.Height / 2);
                    }
                    if (presses.Count > 0)
                    {
                        // Нажать комбинации клавиш, как человек: F3+G держим F3 и жмём G
                        foreach (string press in presses)
                        {
                            string[] parts = press.Split('+');
                            var keys = new Mvk.Engine.Actions.Keys[parts.Length];
                            for (int k = 0; k < parts.Length; k++) keys[k] = Enum.Parse<Mvk.Engine.Actions.Keys>(parts[k]);
                            foreach (var key in keys) app.App.OnKeyDown(key);
                            for (int k = keys.Length - 1; k >= 0; k--) app.App.OnKeyUp(keys[k]);
                        }
                        presses.Clear();
                    }
                    if (screen != null && !screenOpened)
                    {
                        // Открыть экран клавишей, как человек (T чат, E инвентарь, Escape пауза, C креатив)
                        screenOpened = true;
                        Mvk.Engine.Actions.Keys key = screen switch
                        {
                            "chat" => Mvk.Engine.Actions.Keys.T,
                            "inventory" => Mvk.Engine.Actions.Keys.E,
                            "creative" => Mvk.Engine.Actions.Keys.C,
                            "storage" => Mvk.Engine.Actions.Keys.R,
                            _ => Mvk.Engine.Actions.Keys.Escape
                        };
                        app.App.OnKeyDown(key);
                        app.App.OnKeyUp(key);
                    }
                }
                if (screenshot != null && sinceStart != null && sinceStart.Elapsed.TotalSeconds >= shotAfter
                    && (phase < 0 || _AtPhase(app.App.Game, phase)))
                {
                    // Отчёт до снимка: буфер пикселей и PNG не относятся к кадру игры
                    if (profiler != null) Console.WriteLine(profiler.Report());
                    byte[] pixels = new byte[w * h * 4];
                    device.ReadPixels(0, 0, w, h, pixels);
                    PngWriter.Write(screenshot, w, h, pixels);
                    Console.WriteLine($"Снимок {w}x{h}: {screenshot}");
                    if (app.App.Game != null)
                    {
                        var pl = app.App.Game.Player;
                        Console.WriteLine(FormattableString.Invariant($"Игрок {pl.PosX:0.###} {pl.PosY:0.###} {pl.PosZ:0.###} yaw {pl.RotationFrameYaw:0.###} pitch {pl.RotationFramePitch:0.###}"));
                    }
                    if (app.Audio != null) Console.WriteLine("Звук (звучит/источников): " + app.App.debug.Audio);
                    if (app.App.GameHost?.WorldRender != null) Console.WriteLine("Частицы: " + app.App.GameHost.WorldRender.Particles);
                    window.Close();
                    screenshot = null;
                }
            };

            window.Closing += () =>
            {
                app?.Dispose();
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

        /// <summary>
        /// Такт с фазой ветра phase (--phase, --calm = 32). Ветер Environment32: cos((такт &amp; 0x7F) * .049) * .16,
        /// у фазы 32 около нуля (волна воды не сдвигает вершины), у 0 наибольший.
        /// </summary>
        private static bool _AtPhase(Mvk.Engine.Games.GameBase? game, int phase)
            => game?.World == null || (game.World.GetTickCounter() & 0x7F) == phase;

        private static int[] _Size(string value)
        {
            string[] parts = value.Split('x', 'X');
            return new[] { int.Parse(parts[0]), int.Parse(parts[1]) };
        }

        private static float _Deg(string value)
            => float.Parse(value, System.Globalization.CultureInfo.InvariantCulture) * MathF.PI / 180f;
    }
}
