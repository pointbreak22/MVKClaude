#nullable enable
using Mvk.Core.Json;
using Mvk.Engine.Item;

namespace Mvk.Game.Item.List
{
    /// <summary>
    /// Стрела: боеприпас лука. Урон стрелы (json Damage) умножается на силу натяжения.
    /// </summary>
    public class ItemArrow : ItemBase
    {
        private float _damage = 3;

        protected override void _ReadStateFromJson(JsonCompound state)
        {
            base._ReadStateFromJson(state);
            if (state.IsKey("Damage")) _damage = state.GetFloat("Damage");
        }

        public override float GetDamageToAttack() => _damage;
    }
}
