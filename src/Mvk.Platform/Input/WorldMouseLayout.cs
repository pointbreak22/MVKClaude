using Silk.NET.Input;

namespace Mvk.Platform.Input
{
    /// <summary>
    /// Раскладка кнопок мыши в мире (без открытого экрана)
    /// </summary>
    public enum WorldMouseLayout
    {
        /// <summary>По умолчанию в порте: правая ломает блок, левая ставит и использует</summary>
        Swapped,
        /// <summary>Как в оригинале: левая ломает, правая ставит и использует</summary>
        Original
    }

    /// <summary>
    /// Действия мыши в мире: «ломать» (в игре оригинала левая кнопка) и «ставить/использовать» (правая).
    /// GUI (экраны, слоты, кнопки меню) получает физические кнопки как есть, раскладка его не касается.
    /// </summary>
    public sealed class WorldMouseActions
    {
        /// <summary>Действие «ломать блок» (удержание)</summary>
        public readonly InputAction Break;
        /// <summary>Действие «ставить блок / использовать предмет»</summary>
        public readonly InputAction Use;

        private readonly InputActionMap _map;

        /// <summary>Зарегистрировать действия в карте с привязками выбранной раскладки</summary>
        public WorldMouseActions(InputActionMap map, WorldMouseLayout layout)
        {
            _map = map;
            Break = map.Register("WorldBreak");
            Use = map.Register("WorldUse");
            SetLayout(layout);
        }

        /// <summary>
        /// Переназначить кнопки на раскладку
        /// </summary>
        public void SetLayout(WorldMouseLayout layout)
        {
            _map.Unbind(Break);
            _map.Unbind(Use);
            bool swapped = layout == WorldMouseLayout.Swapped;
            _map.Bind(Break, InputBinding.Of(swapped ? MouseButton.Right : MouseButton.Left));
            _map.Bind(Use, InputBinding.Of(swapped ? MouseButton.Left : MouseButton.Right));
        }

        /// <summary>
        /// Какое действие в мире у физической кнопки: 0 ломать, 1 ставить, -1 ни то ни другое
        /// </summary>
        public int ActionOf(MouseButton button)
        {
            InputBinding binding = InputBinding.Of(button);
            if (_map.IsBound(Break, binding)) return 0;
            if (_map.IsBound(Use, binding)) return 1;
            return -1;
        }
    }
}
