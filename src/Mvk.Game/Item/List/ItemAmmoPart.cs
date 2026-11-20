#nullable enable
using Mvk.Core.Json;
using Mvk.Engine.Item;

namespace Mvk.Game.Item.List
{
    /// <summary>
    /// Боеприпас огнестрельного оружия: вид (json Kind) и калибр (json Caliber).
    /// Пуля (Bullet) — общая для мушкета и двустволки одного калибра; заряд: порох (Powder) для мушкета,
    /// гильза (Shell, готовый заряд с капсюлем) для двустволки. Оружие указывает калибр и вид заряда в своём json.
    /// </summary>
    public class ItemAmmoPart : ItemBase
    {
        /// <summary>
        /// Вид: Bullet, Powder, Shell
        /// </summary>
        public string Kind { get; private set; } = "";
        /// <summary>
        /// Калибр (пули и гильзы), у пороха пусто — подходит любому
        /// </summary>
        public string Caliber { get; private set; } = "";

        protected override void _ReadStateFromJson(JsonCompound state)
        {
            base._ReadStateFromJson(state);
            if (state.IsKey("Kind")) Kind = state.GetString("Kind");
            if (state.IsKey("Caliber")) Caliber = state.GetString("Caliber");
        }

        /// <summary>
        /// Подходит ли как вид kind калибра caliber
        /// </summary>
        public bool Fits(string kind, string caliber) => Kind == kind && (Caliber == "" || Caliber == caliber);
    }
}
