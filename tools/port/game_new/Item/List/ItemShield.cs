#nullable enable
using Mvk.Core.Json;
using Mvk.Engine.Item;

namespace Mvk.Game.Item.List
{
    /// <summary>
    /// Щит: носится на предплечье (Forearm), в любой руке, кисть остаётся свободной. В выживании нажатие руки со щитом —
    /// блок, пока кнопка зажата; действие другой руки раскрывает щит на своё время. Удар атакующего спереди
    /// (в пределах 90° от взгляда) снижается на Protection, щит изнашивается. С разгона клик рукой со щитом по мобу —
    /// удар щитом: малый урон, сильное отталкивание (Knockback). Блок, урон, отталкивание и износ считает сервер
    /// (DamageLiving, PlayerServer), клиент сообщает начало и конец блока (пакет C0B) и удар.
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
