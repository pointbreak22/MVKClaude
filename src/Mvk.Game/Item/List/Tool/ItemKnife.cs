#nullable enable
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;

namespace Mvk.Game.Item.List.Tool
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Метательное оружие ближнего боя (нож, копьё): как меч — клик бьёт, режет паутину, удержание
    /// и отпускание — бросок (json ThrowMin, ThrowMax, ThrowCharge). Брошенный предмет ранит уроном оружия × силу
    /// броска и остаётся лежать, его можно подобрать. Каждый бросок изнашивает его на ThrowWear (json). Сервер.
    /// </summary>
    public class ItemKnife : ItemSword
    {
        /// <summary>
        /// Износ за бросок (json ThrowWear)
        /// </summary>
        public int ThrowWear { get; private set; } = 5;

        protected override void _ReadStateFromJson(Mvk.Core.Json.JsonCompound state)
        {
            base._ReadStateFromJson(state);
            if (state.IsKey("ThrowWear")) ThrowWear = state.GetInt("ThrowWear");
        }

        public override bool OnThrow(ItemStack stack, PlayerServer player, float power)
        {
            int damaged = stack.ItemDamaged + ThrowWear;
            if (MaxDamage > 0 && damaged >= MaxDamage) damaged = MaxDamage - 1;
            ItemStack thrown = new ItemStack(this, 1, damaged, stack.Met);
            return base.OnThrow(thrown, player, power);
        }
    }
}
