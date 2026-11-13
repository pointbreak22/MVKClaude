using System;
using System.Diagnostics;
using System.Numerics;
using Mvk.Engine.Actions;
using Mvk.Engine.Games;
using Mvk.Engine.Network.Packets.Client;
using Mvk.Engine.Util;
using Mvk.Game.Client;
using Mvk.Game.Client.Renderer;
using Mvk.Game.Games;
using Mvk.Game.World;
using Mvk.Rendering;
using Silk.NET.Input;
using EngineKeys = Mvk.Engine.Actions.Keys;
using EngineMouse = Mvk.Engine.Actions.MouseButton;
using SilkKey = Silk.NET.Input.Key;
using SilkMouse = Silk.NET.Input.MouseButton;

namespace Mvk.Desktop
{
    /// <summary>
    /// Локальная игра в окне: сервер и клиент в одном процессе, как WindowMain.GameLocalRun оригинала.
    /// Такт игры 20 раз в секунду, кадр рисуется с долей времени до следующего такта.
    /// Пока без меню и HUD: игра запускается сразу в слоте.
    /// </summary>
    internal sealed class GameScene : IDisposable
    {
        /// <summary>Длительность такта в мс (Ce.TickTime оригинала)</summary>
        private const float TickMs = 50f;

        private readonly IGraphicsDevice _device;
        private readonly GameClientHost _host;
        private readonly GameLocal _game;
        private readonly CommandList _commands = new CommandList();
        /// <summary>Текстуры неба острова</summary>
        private readonly SkyTexturesMvk _skyTextures;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private double _lastMs;
        private double _accumulatorMs;
        private Vector2 _mouse;
        private bool _mouseLook;

        /// <summary>Ошибка игры или null</summary>
        public Exception? Error { get; private set; }
        /// <summary>Игра остановилась, текст причины</summary>
        public string? Stopped { get; private set; }
        /// <summary>Игрок вошёл в мир</summary>
        public bool Started => _host.Started;
        /// <summary>Окно должно прятать курсор и ловить мышь</summary>
        public event Action<bool>? MouseLookChanged;

        public GameScene(IGraphicsDevice device, string assets, int slot, long seed, int width, int height)
        {
            _device = device;
            MvkClient.Initialize(assets, false);
            Gi.Width = width;
            Gi.Height = height;

            _host = new GameClientHost(new GameModServerMvk(), device, MvkClient.ShaderBlocksLow);
            _skyTextures = new SkyTexturesMvk(device);
            _host.SkyFactory = MvkClient.SkyFactory(_skyTextures);
            _host.CursorShowHandler = show =>
            {
                _mouseLook = !show;
                MouseLookChanged?.Invoke(_mouseLook);
            };
            _game = new GameLocal(_host, new GameSettings(slot, seed), new AllWorldsMvk(), new GameModClientMvk());
            _game.Error += (_, e) => Error = e.Exception;
            _game.Stoped += (_, e) => Stopped = e.Notification;
            _game.GameStarting();
        }

        /// <summary>
        /// Прокрутить такты игры по реальному времени
        /// </summary>
        public void Update()
        {
            double now = _clock.Elapsed.TotalMilliseconds;
            _accumulatorMs += now - _lastMs;
            _lastMs = now;
            // Если машина не успевает, не копим такты бесконечно
            if (_accumulatorMs > TickMs * 10) _accumulatorMs = TickMs * 10;
            while (_accumulatorMs >= TickMs)
            {
                _accumulatorMs -= TickMs;
                _game.OnTick(TickMs);
            }
        }

        /// <summary>
        /// Нарисовать кадр мира
        /// </summary>
        public void Render(int width, int height)
        {
            Gi.Width = width;
            Gi.Height = height;
            _commands.Reset();
            _commands.SetViewport(0, 0, width, height);
            _host.Draw(_commands, (float)(_accumulatorMs / TickMs));
            _device.Submit(_commands);
        }

        /// <summary>
        /// Отправить серверу команду чата (например "/time n" или "/weather rain"), как ScreenChat оригинала
        /// </summary>
        public void SendCommand(string message)
            => _game.TrancivePacket(new PacketC14Message(message, _game.Player.MovingObject));

        /// <summary>
        /// Задать направление взгляда игрока в радианах (для снимков без человека)
        /// </summary>
        public void Look(float yaw, float pitch)
        {
            _game.Player.RotationYaw = _game.Player.RotationPrevYaw = _game.Player.RotationFrameYaw = yaw;
            _game.Player.RotationPitch = _game.Player.RotationPrevPitch = _game.Player.RotationFramePitch = pitch;
        }

        #region Ввод

        public void KeyDown(SilkKey key)
        {
            EngineKeys k = MapKey(key);
            if (k != EngineKeys.None) _game.OnKeyDown(k);
        }

        public void KeyUp(SilkKey key)
        {
            EngineKeys k = MapKey(key);
            if (k != EngineKeys.None) _game.OnKeyUp(k);
        }

        public void KeyChar(char c) => _game.OnKeyPress(c);

        public void MouseDown(SilkMouse button)
            => _game.OnMouseDown(MapMouse(button), (int)_mouse.X, (int)_mouse.Y);

        public void MouseUp(SilkMouse button)
            => _game.OnMouseUp(MapMouse(button), (int)_mouse.X, (int)_mouse.Y);

        public void MouseWheel(float delta)
            => _game.OnMouseWheel(Math.Sign(delta), (int)_mouse.X, (int)_mouse.Y);

        /// <summary>
        /// Движение мыши. При обзоре мышью игра ждёт координаты относительно центра окна
        /// (в оригинале курсор после каждого движения возвращался в центр).
        /// </summary>
        public void MouseMove(Vector2 position)
        {
            Vector2 delta = position - _mouse;
            _mouse = position;
            if (_mouseLook)
            {
                _game.OnMouseMove(Gi.Width / 2 + (int)delta.X, Gi.Height / 2 + (int)delta.Y);
            }
            else
            {
                _game.OnMouseMove((int)position.X, (int)position.Y);
            }
        }

        /// <summary>
        /// Начальная позиция мыши, чтобы первое движение не дало рывок
        /// </summary>
        public void ResetMouse(Vector2 position) => _mouse = position;

        private static EngineMouse MapMouse(SilkMouse button) => button switch
        {
            SilkMouse.Left => EngineMouse.Left,
            SilkMouse.Right => EngineMouse.Right,
            SilkMouse.Middle => EngineMouse.Middle,
            _ => EngineMouse.None
        };

        /// <summary>
        /// Клавиша Silk в код клавиши Windows, которые ждёт игра (Keys оригинала из WinForms)
        /// </summary>
        public static EngineKeys MapKey(SilkKey key)
        {
            if (key >= SilkKey.A && key <= SilkKey.Z) return (EngineKeys)(int)key; // 65..90 совпадают
            if (key >= SilkKey.Number0 && key <= SilkKey.Number9) return (EngineKeys)(int)key; // 48..57
            if (key >= SilkKey.F1 && key <= SilkKey.F24) return EngineKeys.F1 + (key - SilkKey.F1);
            if (key >= SilkKey.Keypad0 && key <= SilkKey.Keypad9) return EngineKeys.NumPad0 + (key - SilkKey.Keypad0);
            return key switch
            {
                SilkKey.Space => EngineKeys.Space,
                SilkKey.Escape => EngineKeys.Escape,
                SilkKey.Enter or SilkKey.KeypadEnter => EngineKeys.Enter,
                SilkKey.Tab => EngineKeys.Tab,
                SilkKey.Backspace => EngineKeys.Back,
                SilkKey.Insert => EngineKeys.Insert,
                SilkKey.Delete => EngineKeys.Delete,
                SilkKey.Home => EngineKeys.Home,
                SilkKey.End => EngineKeys.End,
                SilkKey.PageUp => EngineKeys.PageUp,
                SilkKey.PageDown => EngineKeys.PageDown,
                SilkKey.Left => EngineKeys.Left,
                SilkKey.Right => EngineKeys.Right,
                SilkKey.Up => EngineKeys.Up,
                SilkKey.Down => EngineKeys.Down,
                SilkKey.ShiftLeft or SilkKey.ShiftRight => EngineKeys.ShiftKey,
                SilkKey.ControlLeft or SilkKey.ControlRight => EngineKeys.ControlKey,
                SilkKey.AltLeft or SilkKey.AltRight => EngineKeys.Menu,
                SilkKey.CapsLock => EngineKeys.CapsLock,
                SilkKey.Pause => EngineKeys.Pause,
                SilkKey.GraveAccent => EngineKeys.Oemtilde,
                SilkKey.Minus => EngineKeys.OemMinus,
                SilkKey.Equal => EngineKeys.Oemplus,
                SilkKey.LeftBracket => EngineKeys.OemOpenBrackets,
                SilkKey.RightBracket => EngineKeys.OemCloseBrackets,
                SilkKey.Semicolon => EngineKeys.OemSemicolon,
                SilkKey.Apostrophe => EngineKeys.OemQuotes,
                SilkKey.Comma => EngineKeys.Oemcomma,
                SilkKey.Period => EngineKeys.OemPeriod,
                SilkKey.Slash => EngineKeys.OemQuestion,
                SilkKey.BackSlash => EngineKeys.OemPipe,
                _ => EngineKeys.None
            };
        }

        #endregion

        /// <summary>
        /// Остановить игру: сервер сохраняет мир. Ждём подтверждения не дольше timeoutMs.
        /// </summary>
        public void Stop(int timeoutMs = 15000)
        {
            if (Stopped == null && Error == null)
            {
                _game.GameStoping("exit", false);
                Stopwatch wait = Stopwatch.StartNew();
                while (Stopped == null && Error == null && wait.ElapsedMilliseconds < timeoutMs)
                {
                    _game.OnTick(TickMs);
                    System.Threading.Thread.Sleep(10);
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _game.Dispose();
            _skyTextures.Dispose();
            _host.Context.Dispose();
        }
    }
}
