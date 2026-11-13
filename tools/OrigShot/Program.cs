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
    /// --look yaw,pitch (градусы), --calm (снимать при ветре около нуля), --size ширина высота (внешний размер окна до DPI, по умолчанию 1280x720).
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
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--screenshot" && i + 1 < args.Length) shot.Screenshot = Path.GetFullPath(args[++i], startDir);
                else if (args[i] == "--shot-after" && i + 1 < args.Length) shot.ShotAfter = double.Parse(args[++i], CultureInfo.InvariantCulture);
                else if (args[i] == "--slot" && i + 1 < args.Length) shot.Slot = int.Parse(args[++i]);
                else if (args[i] == "--seed" && i + 1 < args.Length) shot.Seed = long.Parse(args[++i]);
                else if (args[i] == "--cmd" && i + 1 < args.Length) shot.Commands.Add(args[++i]);
                else if (args[i] == "--calm") shot.Calm = true;
                else if (args[i] == "--look" && i + 1 < args.Length)
                {
                    string[] parts = args[++i].Split(',');
                    // Градусы, как у Mvk.Desktop
                    shot.Look = new[] { float.Parse(parts[0], CultureInfo.InvariantCulture) * MathF.PI / 180f,
                        float.Parse(parts[1], CultureInfo.InvariantCulture) * MathF.PI / 180f };
                }
                else if (args[i] == "--size" && i + 2 < args.Length) shot.SetSize(int.Parse(args[++i]), int.Parse(args[++i]));
            }
            if (shot.Screenshot == null)
            {
                Console.Error.WriteLine("Нужен --screenshot файл.png");
                return 2;
            }
            // Мир с нуля: старый слот удаляем, иначе игрок окажется там, где его сохранили
            string slotPath = Path.Combine(AppContext.BaseDirectory, "Games");
            if (Directory.Exists(slotPath)) Directory.Delete(slotPath, true);
            Window.Run(shot);
            return shot.Saved ? 0 : 1;
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
        /// <summary>Снимать при ветре около нуля, как --calm у Mvk.Desktop</summary>
        public bool Calm;

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

        protected override void OnTick()
        {
            base.OnTick();
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
            if (Look != null && _sinceStart != null && Game != null)
            {
                var p = Game.Player;
                p.RotationYaw = p.RotationPrevYaw = p.RotationFrameYaw = Look[0];
                p.RotationPitch = p.RotationPrevPitch = p.RotationFramePitch = Look[1];
            }
            base.OnOpenGlDraw();
            if (_sinceStart != null && !Saved && _sinceStart.Elapsed.TotalSeconds >= ShotAfter
                && (!Calm || (Game.World.GetTickCounter() & 0x7F) == 32))
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
