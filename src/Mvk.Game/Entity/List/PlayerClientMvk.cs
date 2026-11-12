using Mvk.Game.Entity.Inventory;
using System;
using System.Runtime.CompilerServices;
using Mvk.Engine.Entity.Inventory;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Games;

namespace Mvk.Game.Entity.List
{
    /// <summary>
    /// Объект игроков на клиентской стороне
    /// </summary>
    public class PlayerClientMvk : PlayerClient
    {
        public PlayerClientMvk(GameBase game, int id, string uuid, string login, byte idWorld) 
            : base(game, id, uuid, login, idWorld) { }

        /// <summary>
        /// Инициализация инвенторя
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void _CreateInventory()
        {
            Inventory = new InventoryList(1, InventoryPlayerMvk.ClothCount, 1 + InventoryPlayerMvk.ClothCount);
            Inventory.OutsideChanged += Inventory_OutsideChanged;
        }

        private void Inventory_OutsideChanged(object sender, EventArgs e)
            => Render.OutsideItemChanged();
    }
}
