using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Mvk2;
using Vge;
using Vge.Games;
using Vge.Network.Packets.Client;
using WinGL;

namespace OrigShot
{
    /// <summary>
    /// Снимок кадра оригинального клиента для сравнения с портом (этап 6).
    /// Ключи как у Mvk.Desktop: --screenshot файл, --shot-after сек, --slot, --seed, --cmd "/time n",
    /// --look yaw,pitch (градусы), --calm (снимать при ветре около нуля), --phase N (фаза ветра такта), --size ширина высота (внешний размер окна до DPI, по умолчанию 1280x720).
    /// В консоль пишется размер клиентской области: с ним запускается порт (--size), чтобы кадры совпали по пикселям.
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            // Рабочая папка до создания окна: WindowMain читает options.ini и пути к ресурсам в конструкторе
            string startDir = Environment.CurrentDirectory;
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            var shot = new WindowShot();
            string script = null, trace = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--screenshot" && i + 1 < args.Length) shot.Screenshot = Path.GetFullPath(args[++i], startDir);
                else if (args[i] == "--shot-after" && i + 1 < args.Length) shot.ShotAfter = double.Parse(args[++i], CultureInfo.InvariantCulture);
                else if (args[i] == "--slot" && i + 1 < args.Length) shot.Slot = int.Parse(args[++i]);
                else if (args[i] == "--seed" && i + 1 < args.Length) shot.Seed = long.Parse(args[++i]);
                else if (args[i] == "--cmd" && i + 1 < args.Length) shot.Commands.Add(args[++i]);
                else if (args[i] == "--calm") shot.Phase = 32;
                else if (args[i] == "--script" && i + 1 < args.Length) script = args[++i];
                else if (args[i] == "--trace" && i + 1 < args.Length) trace = Path.GetFullPath(args[++i], startDir);
                else if (args[i] == "--phase" && i + 1 < args.Length) shot.Phase = int.Parse(args[++i]) & 0x7F;
                else if (args[i] == "--look" && i + 1 < args.Length)
                {
                    string[] parts = args[++i].Split(',');
                    // Градусы, как у Mvk.Desktop
                    shot.Look = new[] { float.Parse(parts[0], CultureInfo.InvariantCulture) * MathF.PI / 180f,
                        float.Parse(parts[1], CultureInfo.InvariantCulture) * MathF.PI / 180f };
                }
                else if (args[i] == "--size" && i + 2 < args.Length) shot.SetSize(int.Parse(args[++i]), int.Parse(args[++i]));
            }
            if (script != null) shot.Script = new Mvk.Desktop.MechanicsScript(script, trace ?? Path.Combine(startDir, "trace.csv"));
            if (shot.Screenshot == null && shot.Script == null)
            {
                Console.Error.WriteLine("Нужен --screenshot файл.png или --script");
                return 2;
            }
            // Мир с нуля: старый слот удаляем, иначе игрок окажется там, где его сохранили
            string slotPath = Path.Combine(AppContext.BaseDirectory, "Games");
            if (Directory.Exists(slotPath)) Directory.Delete(slotPath, true);
            Window.Run(shot);
            // Потоки оригинала (звук) могут пережить окно: процесс завершаем явно
            Environment.Exit(shot.Saved ? 0 : 1);
            return 0;
        }
    }

    /// <summary>
    /// Оригинальный WindowMvk, который сам запускает одиночную игру и сохраняет кадр
    /// </summary>
    public class WindowShot : WindowMvk
    {
        public string Screenshot;
        public double ShotAfter = 8;
        public int Slot;
        public long Seed = 2;
        public readonly List<string> Commands = new List<string>();
        public float[] Look;
        public bool Saved;
        /// <summary>Фаза ветра такта снимка (--calm = 32, ветер около нуля) или -1, как у Mvk.Desktop</summary>
        public int Phase = -1;
        /// <summary>Сценарий механики (--script, --trace), как у Mvk.Desktop</summary>
        public Mvk.Desktop.MechanicsScript Script;
        private bool _scriptRunning;

        private Stopwatch _sinceStart;
        private bool _runRequested;
        private bool _commandsSent;

        [DllImport("opengl32.dll")]
        private static extern IntPtr wglGetCurrentDC();
        [DllImport("gdi32.dll")]
        private static extern int GetPixelFormat(IntPtr hdc);
        [DllImport("gdi32.dll")]
        private static extern int DescribePixelFormat(IntPtr hdc, int format, uint size, byte[] pfd);

        /// <summary>Бит в буфере глубины окна (оригинал просит 16 в PixelFormatDescriptor)</summary>
        private static int _DepthBits()
        {
            IntPtr hdc = wglGetCurrentDC();
            byte[] pfd = new byte[40];
            if (DescribePixelFormat(hdc, GetPixelFormat(hdc), 40, pfd) == 0) return -1;
            // PIXELFORMATDESCRIPTOR.cDepthBits
            return pfd[23];
        }

        [DllImport("opengl32.dll")]
        private static extern void glReadPixels(int x, int y, int width, int height, uint format, uint type, byte[] pixels);

        /// <summary>Сущности мира клиента для следа механики</summary>
        private static string _Entities(Vge.World.WorldClient world)
        {
            var list = new List<(int, float, float, float)>();
            for (int i = 0; i < world.LoadedEntityList.Count; i++)
            {
                var e = world.LoadedEntityList.GetAt(i);
                list.Add((e.Id, e.PosX, e.PosY, e.PosZ));
            }
            return Mvk.Desktop.MechanicsScript.EntitiesDigest(list);
        }

        /// <summary>Мод сервера с отладочной командой /summon, как у Mvk.Desktop</summary>
        public override Vge.Games.GameModServer CreateGameModServer() => new GameModServerShot();

        public void SetSize(int width, int height)
        {
            _width = width;
            _height = height;
        }

        protected override void OnResized(int width, int height)
        {
            base.OnResized(width, height);
            Console.WriteLine($"OnResized {width}x{height}");
        }

        /// <summary>
        /// Сценарий механики идёт как в окне с фокусом: окно снимков запускается в фоне, а оригинал
        /// при потере фокуса выключает вид от первого лица и отпускает действия руки
        /// </summary>
        protected override void OnActivate(bool active)
        {
            if (Script != null) return;
            base.OnActivate(active);
        }

        protected override void OnTick()
        {
            if (_scriptRunning && !Script.Finished && Game != null)
            {
                Script.BeforeTick((name, down) =>
                {
                    if (name.StartsWith('/'))
                    {
                        Game.TrancivePacket(new PacketC14Message(name, Game.Player.MovingObject));
                        return;
                    }
                    if (name.StartsWith("Mouse"))
                    {
                        var button = name == "MouseLeft" ? WinGL.Actions.MouseButton.Left : WinGL.Actions.MouseButton.Right;
                        if (down) OnMouseDown(button, Gi.Width / 2, Gi.Height / 2);
                        else OnMouseUp(button, Gi.Width / 2, Gi.Height / 2);
                        return;
                    }
                    var key = Enum.Parse<WinGL.Actions.Keys>(name);
                    if (down) OnKeyDown(key);
                    else OnKeyUp(key);
                });
            }
            base.OnTick();
            if (_scriptRunning && !Script.Finished && Game != null)
            {
                var pl = Game.Player;
                var mo = pl.MovingObject;
                Script.AfterTick(pl.PosX, pl.PosY, pl.PosZ,
                    pl.Physics.MotionX, pl.Physics.MotionY, pl.Physics.MotionZ, pl.OnGround,
                    mo.IsBlock() ? $"{mo.BlockPosition.X},{mo.BlockPosition.Y},{mo.BlockPosition.Z}:{mo.Block.Id}" : "-",
                    _Entities(Game.World));
            }
            // Как кнопки «Одиночная» и «Играть»: новый мир в слоте с заданным зерном
            if (Ready && Game == null && !_runRequested)
            {
                _runRequested = true;
                GameLocalRun(new GameSettings(Slot, Seed));
            }
        }

        protected override void OnOpenGlDraw()
        {
            if (Game != null && Game.FlagGameStarted && _sinceStart == null)
            {
                _sinceStart = Stopwatch.StartNew();
            }
            // Команды через секунду: сразу после FlagGameStarted сервер оригинала иногда их теряет
            if (!_commandsSent && _sinceStart != null && _sinceStart.Elapsed.TotalSeconds >= 1)
            {
                _commandsSent = true;
                foreach (string command in Commands)
                {
                    Game.TrancivePacket(new PacketC14Message(command, Game.Player.MovingObject));
                }
            }
            if (Script != null && !_scriptRunning && _sinceStart != null && _sinceStart.Elapsed.TotalSeconds >= ShotAfter
                && (Phase < 0 || (Game.World.GetTickCounter() & 0x7F) == Phase))
            {
                _scriptRunning = true;
            }
            if (Look != null && _sinceStart != null && Game != null)
            {
                var p = Game.Player;
                p.RotationYaw = p.RotationPrevYaw = p.RotationFrameYaw = Look[0];
                p.RotationPitch = p.RotationPrevPitch = p.RotationFramePitch = Look[1];
            }
            base.OnOpenGlDraw();
            if (Script != null && Script.Finished && Screenshot == null && !Saved)
            {
                Console.WriteLine("Сценарий механики закончен");
                Saved = true;
                Exit();
                return;
            }
            if (Screenshot != null && _sinceStart != null && !Saved && (Script != null ? Script.Finished
                : _sinceStart.Elapsed.TotalSeconds >= ShotAfter && (Phase < 0 || (Game.World.GetTickCounter() & 0x7F) == Phase)))
            {
                int w = Gi.Width;
                int h = Gi.Height;
                byte[] pixels = new byte[w * h * 4];
                glReadPixels(0, 0, w, h, 0x1908, 0x1401, pixels);
                // OpenGL отдаёт строки снизу вверх, PNG пишется сверху вниз
                byte[] flipped = new byte[pixels.Length];
                int stride = w * 4;
                for (int y = 0; y < h; y++)
                {
                    Buffer.BlockCopy(pixels, (h - 1 - y) * stride, flipped, y * stride, stride);
                }
                Mvk.Core.Util.PngWriter.Write(Screenshot, w, h, flipped);
                Console.WriteLine($"Снимок оригинала {w}x{h}: {Screenshot}");
                Console.WriteLine($"Буфер глубины: {_DepthBits()} бит");
                var pl = Game.Player;
                Console.WriteLine(FormattableString.Invariant($"Игрок {pl.PosX:0.###} {pl.PosY:0.###} {pl.PosZ:0.###} yaw {pl.RotationFrameYaw:0.###} pitch {pl.RotationFramePitch:0.###}"));
                File.WriteAllText(Screenshot + ".size", $"{w} {h}");
                Saved = true;
                Exit();
            }
        }
    }
}
