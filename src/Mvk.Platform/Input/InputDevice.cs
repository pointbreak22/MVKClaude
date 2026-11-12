using System;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Mvk.Platform.Input
{
    /// <summary>
    /// Ввод окна: клавиатура и мышь через Silk.NET.
    /// Нажатия уходят в <see cref="InputActionMap"/> (для игры) и в события (для GUI и ввода текста).
    /// </summary>
    public sealed class InputDevice : IDisposable
    {
        private readonly IInputContext _context;
        private Vector2 _lastMouse;
        private bool _hasLastMouse;
        private bool _captured;

        /// <summary>Карта действий игры</summary>
        public InputActionMap Actions { get; } = new InputActionMap();

        /// <summary>Позиция курсора в пикселях окна, начало слева сверху</summary>
        public Vector2 MousePosition { get; private set; }
        /// <summary>Смещение мыши за кадр (для обзора камерой)</summary>
        public Vector2 MouseDelta { get; private set; }
        /// <summary>Прокрутка колеса за кадр</summary>
        public float ScrollDelta { get; private set; }

        /// <summary>Клавиша нажата (без автоповтора считать нельзя: повтор тоже приходит, как в WinForms)</summary>
        public event Action<Key>? KeyDown;
        public event Action<Key>? KeyUp;
        /// <summary>Введён символ (для текстовых полей)</summary>
        public event Action<char>? Char;
        public event Action<MouseButton, Vector2>? MouseDown;
        public event Action<MouseButton, Vector2>? MouseUp;

        public InputDevice(IView window)
        {
            _context = window.CreateInput();
            foreach (IKeyboard kb in _context.Keyboards)
            {
                kb.KeyDown += (_, key, _) =>
                {
                    Actions.Feed(InputBinding.Of(key), true);
                    KeyDown?.Invoke(key);
                };
                kb.KeyUp += (_, key, _) =>
                {
                    Actions.Feed(InputBinding.Of(key), false);
                    KeyUp?.Invoke(key);
                };
                kb.KeyChar += (_, c) => Char?.Invoke(c);
            }
            foreach (IMouse mouse in _context.Mice)
            {
                mouse.MouseDown += (m, b) =>
                {
                    Actions.Feed(InputBinding.Of(b), true);
                    MouseDown?.Invoke(b, m.Position);
                };
                mouse.MouseUp += (m, b) =>
                {
                    Actions.Feed(InputBinding.Of(b), false);
                    MouseUp?.Invoke(b, m.Position);
                };
                mouse.MouseMove += (_, pos) =>
                {
                    if (_hasLastMouse) MouseDelta += pos - _lastMouse;
                    _lastMouse = pos;
                    _hasLastMouse = true;
                    MousePosition = pos;
                };
                mouse.Scroll += (_, wheel) => ScrollDelta += wheel.Y;
            }
        }

        /// <summary>
        /// Захват курсора: скрыт и не выходит из окна, приходят «сырые» смещения (режим игры).
        /// </summary>
        public bool CursorCaptured
        {
            get => _captured;
            set
            {
                if (_captured == value) return;
                _captured = value;
                foreach (IMouse mouse in _context.Mice)
                {
                    mouse.Cursor.CursorMode = value ? CursorMode.Raw : CursorMode.Normal;
                }
                // Скачок курсора при смене режима не должен повернуть камеру
                _hasLastMouse = false;
                MouseDelta = Vector2.Zero;
            }
        }

        /// <summary>Конец кадра: сбросить накопленные смещения и признаки нажатий</summary>
        public void EndFrame()
        {
            MouseDelta = Vector2.Zero;
            ScrollDelta = 0;
            Actions.EndFrame();
        }

        public void Dispose() => _context.Dispose();
    }
}
