using System;
using System.Numerics;
using Mvk.Engine;
using Mvk.Engine.Games;
using Mvk.Engine.Network.Packets.Client;
using Mvk.Game.Client;
using Mvk.Rendering;
using Mvk.Platform.Input;
using Silk.NET.Input;
using Silk.NET.Windowing;
using IWindowHost = Mvk.Engine.IWindowHost;
using EngineKeys = Mvk.Engine.Actions.Keys;
using EngineMouse = Mvk.Engine.Actions.MouseButton;
using SilkKey = Silk.NET.Input.Key;
using SilkMouse = Silk.NET.Input.MouseButton;

namespace Mvk.Desktop
{
    /// <summary>
    /// Окно Малювек 2 на Silk.NET: хост платформы для WindowMvk (то, что в оригинале делал WinGL.Window):
    /// полноэкранный режим, синхронизация, курсор и захват мыши, буфер обмена, закрытие.
    /// Переводит ввод Silk в клавиши и кнопки оригинала и вызывает такты и кадры приложения.
    /// </summary>
    internal sealed class DesktopApp : IWindowHost, IDisposable
    {
        private readonly IWindow _window;
        private readonly IInputContext _input;
        private readonly CommandList _commands = new CommandList();
        private Vector2 _mouse;
        /// <summary>Обзор мышью: курсор скрыт и захвачен, игра ждёт смещение от центра</summary>
        private bool _mouseLook;

        /// <summary>Приложение клиента</summary>
        public readonly WindowMvk App;
        /// <summary>Ошибка игры</summary>
        public Exception? Error { get; private set; }
        /// <summary>Звук OpenAL или null (снимки без звука быстрее загружаются)</summary>
        public Mvk.Engine.Audio.IWindowAudio? Audio { get; }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Раскладка мыши в мире по умолчанию: правая ломает, левая ставит.
        /// Вернуть как в оригинале: WorldMouseLayout.Original (или ключ --mouse original).
        /// </summary>
        public const WorldMouseLayout DefaultMouseLayout = WorldMouseLayout.Swapped;
        /// <summary>
        /// Не сообщать игре о потере фокуса (сценарий механики --script идёт в фоновом окне, а игра
        /// при потере фокуса выключает вид от первого лица и отпускает действия руки)
        /// </summary>
        public bool IgnoreFocusLoss;
        /// <summary>Действия мыши в мире (ломать, ставить)</summary>
        public readonly WorldMouseActions WorldMouse;
        private readonly InputActionMap _actions = new InputActionMap();
        /// <summary>Какой кнопкой игры ушло нажатие физической кнопки (отпускание идёт той же)</summary>
        private readonly EngineMouse[] _pressedAs = new EngineMouse[16];

        public DesktopApp(IWindow window, IGraphicsDevice device, bool debugCommands, bool sound,
            WorldMouseLayout mouseLayout = DefaultMouseLayout)
        {
            _window = window;
            WorldMouse = new WorldMouseActions(_actions, mouseLayout);
            Audio = sound ? new DesktopAudio() : null;
            App = new WindowMvkDesktop(this, device, debugCommands);
            _input = window.CreateInput();
            foreach (IKeyboard k in _input.Keyboards)
            {
                k.KeyDown += (_, key, _) => _KeyDown(key);
                k.KeyUp += (_, key, _) => _KeyUp(key);
                k.KeyChar += (_, c) => App.OnKeyPress(c);
            }
            foreach (IMouse m in _input.Mice)
            {
                m.MouseDown += (_, b) => _MouseDown(b);
                m.MouseUp += (_, b) => _MouseUp(b);
                m.MouseMove += (_, p) => _MouseMove(p);
                m.Scroll += (_, w) => App.OnMouseWheel(Math.Sign(w.Y), (int)_mouse.X, (int)_mouse.Y);
            }
            window.FocusChanged += focused =>
            {
                if (!focused && IgnoreFocusLoss) return;
                App.OnActivate(focused);
            };
            App.OnResized(window.FramebufferSize.X, window.FramebufferSize.Y);
            App.Begin();
        }

        /// <summary>Такты по реальному времени</summary>
        public void Update() => App.Update();

        /// <summary>Нарисовать кадр</summary>
        public void Render(int width, int height) => App.Draw(_commands, width, height);

        /// <summary>
        /// Нажатие мыши. В мире (игра без открытого экрана) кнопка переводится через действия раскладки:
        /// «ломать» идёт игре как левая кнопка оригинала, «ставить» как правая. GUI получает кнопки как есть.
        /// </summary>
        private void _MouseDown(SilkMouse button)
        {
            EngineMouse m = _MapMouse(button);
            if (App.Screen == null && App.Game != null)
            {
                int action = WorldMouse.ActionOf(button);
                if (action == 0) m = EngineMouse.Left;
                else if (action == 1) m = EngineMouse.Right;
            }
            if ((uint)button < (uint)_pressedAs.Length) _pressedAs[(int)button] = m;
            App.OnMouseDown(m, (int)_mouse.X, (int)_mouse.Y);
        }

        private void _MouseUp(SilkMouse button)
        {
            EngineMouse m = (uint)button < (uint)_pressedAs.Length && _pressedAs[(int)button] != EngineMouse.None
                ? _pressedAs[(int)button] : _MapMouse(button);
            if ((uint)button < (uint)_pressedAs.Length) _pressedAs[(int)button] = EngineMouse.None;
            App.OnMouseUp(m, (int)_mouse.X, (int)_mouse.Y);
        }

        private void _KeyDown(SilkKey key)
        {
            EngineKeys k = MapKey(key);
            if (k != EngineKeys.None) App.OnKeyDown(k);
        }

        private void _KeyUp(SilkKey key)
        {
            EngineKeys k = MapKey(key);
            if (k != EngineKeys.None) App.OnKeyUp(k);
        }

        /// <summary>
        /// Движение мыши. При обзоре мышью игра ждёт координаты относительно центра окна
        /// (в оригинале курсор после каждого движения возвращался в центр).
        /// </summary>
        private void _MouseMove(Vector2 position)
        {
            Vector2 delta = position - _mouse;
            _mouse = position;
            if (_mouseLook) App.OnMouseMove(Gi.Width / 2 + (int)delta.X, Gi.Height / 2 + (int)delta.Y);
            else App.OnMouseMove((int)position.X, (int)position.Y);
        }

        #region IWindowHost

        public bool FullScreen => _window.WindowState == WindowState.Fullscreen;

        public void SetFullScreen(bool fullScreen)
            => _window.WindowState = fullScreen ? WindowState.Fullscreen : WindowState.Normal;

        public void SetVSync(bool on) => _window.VSync = on;

        public void CursorShow(bool show)
        {
            _mouseLook = !show;
            foreach (IMouse m in _input.Mice)
            {
                m.Cursor.CursorMode = show ? CursorMode.Normal : CursorMode.Raw;
                _mouse = m.Position;
            }
        }

        public void SetCursorPosition(int x, int y)
        {
            // В режиме Raw курсор не двигается, смещение считается от прошлой позиции
            if (_mouseLook) return;
            foreach (IMouse m in _input.Mice) m.Position = new Vector2(x, y);
        }

        public void Close() => _window.Close();

        public void Crash(Exception exception)
        {
            Error = exception;
            Console.Error.WriteLine("Ошибка игры: " + exception);
            _window.Close();
        }

        public string? ClipboardGetText()
        {
            foreach (IKeyboard k in _input.Keyboards) return k.ClipboardText;
            return null;
        }

        public void ClipboardSetText(string text)
        {
            foreach (IKeyboard k in _input.Keyboards) k.ClipboardText = text;
        }

        #endregion

        #region Отладка без человека

        /// <summary>
        /// Отправить серверу команду чата (например "/time n"), как ScreenChat оригинала
        /// </summary>
        public void SendCommand(string message)
        {
            GameBase? game = App.Game;
            game?.TrancivePacket(new PacketC14Message(message, game.Player.MovingObject));
        }

        /// <summary>
        /// Задать направление взгляда игрока в радианах
        /// </summary>
        public void Look(float yaw, float pitch)
        {
            if (App.Game == null) return;
            var p = App.Game.Player;
            p.RotationYaw = p.RotationPrevYaw = p.RotationFrameYaw = yaw;
            p.RotationPitch = p.RotationPrevPitch = p.RotationFramePitch = pitch;
        }

        /// <summary>
        /// Плоские частицы перед игроком: 40 дыма и 40 капель (WorldClient.SpawnParticle)
        /// </summary>
        public void SpawnFlatParticles()
        {
            if (App.Game?.World == null) return;
            var p = App.Game.Player;
            var front = Mvk.Core.Maths.Glm.Ray(p.RotationYaw, 0) * 3f;
            var pos = new Mvk.Core.Maths.Vector3(p.PosX + front.X, p.PosY + 1.2f, p.PosZ + front.Z);
            App.Game.World.SpawnParticle(Mvk.Game.Entity.Particle.EntitiesFXRegMvk.SmokeId, 40, pos,
                new Mvk.Core.Maths.Vector3(1.5f), .1f, 0);
            App.Game.World.SpawnParticle(Mvk.Game.Entity.Particle.EntitiesFXRegMvk.DropletId, 40, pos,
                new Mvk.Core.Maths.Vector3(1.5f), .1f, 0);
        }

        /// <summary>
        /// Переключить вид камеры (как F5)
        /// </summary>
        public void ViewCameraNext(int count)
        {
            for (int i = 0; i < count; i++) App.Game?.Player.ViewCameraNext();
        }

        #endregion

        /// <summary>
        /// Закрытие окна: если идёт игра, остановить её (сервер сохраняет мир), как Close окна оригинала
        /// </summary>
        public void Dispose()
        {
            if (App.Game != null)
            {
                App.GameStoping();
                var wait = System.Diagnostics.Stopwatch.StartNew();
                while (App.Game != null && Error == null && wait.ElapsedMilliseconds < 15000)
                {
                    App.Update();
                    System.Threading.Thread.Sleep(10);
                }
            }
            App.Dispose();
            _input.Dispose();
        }

        private static EngineMouse _MapMouse(SilkMouse button) => button switch
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
    }

    /// <summary>
    /// Окно Малювек хоста ПК: отладочные команды сервера (/summon, только для снимков и замеров с --cmd)
    /// и обработчики до и после такта (сценарий механики --script)
    /// </summary>
    internal sealed class WindowMvkDesktop : WindowMvk
    {
        /// <summary>Перед тактом клиента</summary>
        public Action? BeforeTick;
        /// <summary>После такта клиента</summary>
        public Action? AfterTick;
        private readonly bool _debugCommands;

        public WindowMvkDesktop(IWindowHost host, IGraphicsDevice device, bool debugCommands) : base(host, device)
            => _debugCommands = debugCommands;

        public override GameModServer CreateGameModServer()
            => _debugCommands ? new GameModServerDebug() : base.CreateGameModServer();

        protected override void OnTick()
        {
            BeforeTick?.Invoke();
            base.OnTick();
            AfterTick?.Invoke();
        }
    }
}
