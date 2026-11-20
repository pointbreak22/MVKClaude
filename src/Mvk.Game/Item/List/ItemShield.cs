#nullable enable
using Mvk.Core.Json;
using Mvk.Engine.Item;

namespace Mvk.Game.Item.List
{
    /// <summary>
    /// Щит: носится во второй (левой) руке. Пока вторая кнопка зажата и правая рука свободна или держит оружие либо
    /// инструмент, игрок блокирует: удар атакующего спереди (в пределах 90° от взгляда) снижается на Protection,
    /// щит изнашивается. Блок, снижение урона и износ считает сервер (DamageLiving), клиент сообщает только начало
    /// и конец блока (пакет C0B).
    /// </summary>
    public class ItemShield : ItemBase
    {
        /// <summary>
        /// Доля урона, которую щит снимает при блоке (json Protection)
        /// </summary>
        public float Protection { get; private set; } = .75f;

        protected override void _ReadStateFromJson(JsonCompound state)
        {
            base._ReadStateFromJson(state);
            if (state.IsKey("Protection")) Protection = System.Math.Clamp(state.GetFloat("Protection"), 0, 1);
        }
    }
}
