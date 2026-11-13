using System;
using System.Collections.Generic;
using Silk.NET.Input;

namespace Mvk.Platform.Input
{
    /// <summary>
    /// Откуда приходит нажатие
    /// </summary>
    public enum InputSource : byte
    {
        Key = 0,
        MouseButton = 1
    }

    /// <summary>
    /// Привязка действия к физической кнопке
    /// </summary>
    public readonly record struct InputBinding(InputSource Source, int Code)
    {
        public static InputBinding Of(Key key) => new(InputSource.Key, (int)key);
        public static InputBinding Of(MouseButton button) => new(InputSource.MouseButton, (int)button);
    }

    /// <summary>
    /// Номер действия в карте (выдаётся при регистрации)
    /// </summary>
    public readonly record struct InputAction(int Id);

    /// <summary>
    /// Карта действий: игра спрашивает «прыжок нажат?», а не «пробел нажат?».
    /// Одно действие может иметь несколько привязок (клавиатура, мышь, позже геймпад и сенсорные кнопки),
    /// поэтому переназначение клавиш и управление с телефона не трогают игровой код.
    /// </summary>
    public sealed class InputActionMap
    {
        private readonly List<string> _names = new();
        private readonly List<int> _held = new();
        private readonly List<bool> _pressed = new();
        private readonly List<bool> _released = new();
        private readonly Dictionary<InputBinding, List<int>> _bindings = new();
        /// <summary>Кнопки, которые сейчас удерживаются, чтобы не считать автоповтор и лишние отпускания</summary>
        private readonly HashSet<InputBinding> _down = new();

        /// <summary>Сколько действий зарегистрировано</summary>
        public int Count => _names.Count;

        /// <summary>Добавить действие с привязками по умолчанию</summary>
        public InputAction Register(string name, params InputBinding[] bindings)
        {
            int id = _names.Count;
            _names.Add(name);
            _held.Add(0);
            _pressed.Add(false);
            _released.Add(false);
            foreach (InputBinding b in bindings) Bind(new InputAction(id), b);
            return new InputAction(id);
        }

        public string GetName(InputAction action) => _names[action.Id];

        /// <summary>Добавить привязку к действию</summary>
        public void Bind(InputAction action, InputBinding binding)
        {
            if (!_bindings.TryGetValue(binding, out List<int>? list))
            {
                list = new List<int>();
                _bindings.Add(binding, list);
            }
            if (!list.Contains(action.Id)) list.Add(action.Id);
        }

        /// <summary>Привязана ли кнопка к действию</summary>
        public bool IsBound(InputAction action, InputBinding binding)
            => _bindings.TryGetValue(binding, out List<int>? list) && list.Contains(action.Id);

        /// <summary>Убрать все привязки действия (перед переназначением)</summary>
        public void Unbind(InputAction action)
        {
            foreach (KeyValuePair<InputBinding, List<int>> kv in _bindings)
            {
                if (kv.Value.Remove(action.Id) && _down.Contains(kv.Key) && _held[action.Id] > 0)
                {
                    _held[action.Id]--;
                }
            }
        }

        /// <summary>Действие удерживается</summary>
        public bool IsDown(InputAction action) => _held[action.Id] > 0;
        /// <summary>Действие нажато в этом кадре</summary>
        public bool WasPressed(InputAction action) => _pressed[action.Id];
        /// <summary>Действие отпущено в этом кадре</summary>
        public bool WasReleased(InputAction action) => _released[action.Id];

        /// <summary>Сообщить о нажатии или отпускании физической кнопки</summary>
        public void Feed(InputBinding binding, bool down)
        {
            if (down ? !_down.Add(binding) : !_down.Remove(binding)) return;
            if (!_bindings.TryGetValue(binding, out List<int>? list)) return;
            foreach (int id in list)
            {
                if (down)
                {
                    if (_held[id]++ == 0) _pressed[id] = true;
                }
                else if (_held[id] > 0 && --_held[id] == 0)
                {
                    _released[id] = true;
                }
            }
        }

        /// <summary>Конец кадра: сбросить признаки «нажато/отпущено в этом кадре»</summary>
        public void EndFrame()
        {
            for (int i = 0; i < _pressed.Count; i++)
            {
                _pressed[i] = false;
                _released[i] = false;
            }
        }

        /// <summary>Отпустить всё (окно потеряло фокус)</summary>
        public void ReleaseAll()
        {
            _down.Clear();
            for (int i = 0; i < _held.Count; i++)
            {
                if (_held[i] > 0) _released[i] = true;
                _held[i] = 0;
            }
        }
    }
}
