#nullable enable
using System;
using System.Runtime.CompilerServices;
using Mvk.Engine.Entity.List;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;
using Mvk.Game.Entity.List;

namespace Mvk.Game.Item.List
{
    /// <summary>
    /// Лук (двуручный, json Two: держится кистью одной руки, тетиву тянет другая; щит на предплечье не мешает).
    /// Клик — слабый удар, как рукой; удержание — натяжение до 20 тактов, отпускание — выстрел. [ПОВЕДЕНИЕ] Клиент
    /// сообщает только начало и конец натяжения; время натяжения по своим тактам, стрелу из инвентаря, выстрел,
    /// попадание и износ лука считает сервер.
    /// </summary>
    public class ItemBow : ItemBase
    {
        /// <summary>
        /// Такты полного натяжения
        /// </summary>
        public const int FullDraw = 20;

        /// <summary>
        /// Такт сервера начала натяжения у игрока (сервер)
        /// </summary>
        private readonly ConditionalWeakTable<PlayerServer, StrongBox<uint>> _drawStart = new();

        public override ResultHandSecond OnSecond(bool begin, ItemStack stack, PlayerClientOwner player, int counter)
        {
            if (counter > FullDraw) counter = FullDraw;
            return new ResultHandSecond(5, counter);
        }

        public override int OnSecondEnd(ItemStack stack, PlayerClientOwner player, bool abort, int counter)
        {
            if (abort) return 255;
            if (counter > FullDraw) counter = FullDraw;
            return 100 + counter;
        }

        public override void OnUseSecond(ItemStack stack, PlayerServer player, int number)
        {
            uint tick = player.GetWorldServer().Server.TickCounterGlobal;
            if (number == 0)
            {
                // Начало натяжения
                _drawStart.AddOrUpdate(player, new StrongBox<uint>(tick));
                return;
            }
            if (number < 100 || player is not PlayerServerMvk mvk) return;
            if (!_drawStart.TryGetValue(player, out StrongBox<uint>? start)) return;
            _drawStart.Remove(player);
            if (number == 255) return;
            // Натяжение по тактам сервера, не больше присланного клиентом
            int draw = (int)Math.Min(Math.Min(tick - start.Value, (uint)FullDraw), (uint)(number - 100));
            if (draw < 3) return; // не натянул
            int slot = ItemAmmo.Find(mvk, item => item is ItemArrow);
            if (slot < 0) return;
            ItemStack arrow = mvk.InvPlayer.GetStackInSlot(slot).Copy(1);
            float power = .4f + 2.1f * draw / FullDraw;
            if (EntityProjectile.Throw(player, arrow, power, EntityArrowMvk.AliasArrow) == null) return;
            ItemAmmo.ConsumeOne(mvk, slot);
            ItemWear.DamageHeld(player, 1);
        }
    }
}
