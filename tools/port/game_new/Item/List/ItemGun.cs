#nullable enable
using System;
using System.Runtime.CompilerServices;
using Mvk.Core.Json;
using Mvk.Core.Maths;
using Mvk.Engine.Audio;
using Mvk.Engine.Entity.List;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;
using Mvk.Engine.World;
using Mvk.Game.Entity.List;
using Mvk.Game.Entity.Particle;

namespace Mvk.Game.Item.List
{
    /// <summary>
    /// Огнестрельное оружие (мушкет, двустволка) по json: стволы (Barrels), калибр (Caliber), урон пули (Damage),
    /// такты перезарядки одного ствола (ReloadTicks), заряд (Charge: Powder или Shell), сила пули (Speed), отдача (Recoil).
    /// Заряженные стволы хранятся в стаке (Met). Первое действие — выстрел, удержание второго — перезарядка всех пустых
    /// стволов. Клиент только сообщает намерение; заряд, время перезарядки по тактам сервера, расход пули и заряда
    /// из инвентаря, выстрел (быстрый снаряд-пуля с серверным попаданием), отдача, дым, звук и износ — сервер.
    /// Одноручное или двуручное — признак Two в json (двуручное занимает обе руки: щит при нём не блокирует).
    /// </summary>
    public class ItemGun : ItemBase
    {
        public int Barrels { get; private set; } = 1;
        public string Caliber { get; private set; } = "12";
        public float BulletDamage { get; private set; } = 10;
        public int ReloadTicks { get; private set; } = 40;
        public string Charge { get; private set; } = "Powder";
        public float Speed { get; private set; } = 4;
        public float Recoil { get; private set; } = .3f;

        /// <summary>
        /// Такт сервера начала перезарядки у игрока (сервер)
        /// </summary>
        private readonly ConditionalWeakTable<PlayerServer, StrongBox<uint>> _reloadStart = new();

        protected override void _ReadStateFromJson(JsonCompound state)
        {
            base._ReadStateFromJson(state);
            if (state.IsKey("Barrels")) Barrels = Math.Clamp(state.GetInt("Barrels"), 1, 8);
            if (state.IsKey("Caliber")) Caliber = state.GetString("Caliber");
            if (state.IsKey("Damage")) BulletDamage = state.GetFloat("Damage");
            if (state.IsKey("ReloadTicks")) ReloadTicks = state.GetInt("ReloadTicks");
            if (state.IsKey("Charge")) Charge = state.GetString("Charge");
            if (state.IsKey("Speed")) Speed = state.GetFloat("Speed");
            if (state.IsKey("Recoil")) Recoil = state.GetFloat("Recoil");
        }

        public override string GetToolTip(ItemStack stack) => base.GetToolTip(stack) + " [" + stack.Met + "/" + Barrels + "]";

        #region Выстрел

        public override ResultHandAction OnAction(bool begin, ItemStack stack, PlayerClientOwner player)
        {
            // Выстрел по нажатию, без автоогня; пустое оружие не стреляет
            if (!begin || stack.Met == 0) return new ResultHandAction(ResultHandAction.ActionType.None);
            return new ResultHandAction(ResultHandAction.ActionType.UseItem, 8);
        }

        public override void OnUseAction(ItemStack stack, PlayerServer player)
        {
            if (player is not PlayerServerMvk mvk || stack.Met == 0) return;
            int slot = mvk.InvPlayer.GetCurrentIndex();
            WorldServer world = player.GetWorldServer();
            // Пуля: снаряд с серверным попаданием
            if (EntityProjectile.Throw(player, new ItemStack(ItemsRegMvk.Bullet, 1), Speed, EntityBulletMvk.AliasBullet) is EntityBulletMvk shot)
            {
                shot.BulletDamage = BulletDamage;
            }
            mvk.InvPlayer.SetInventorySlotContents(slot, new ItemStack(this, 1, stack.ItemDamaged, (byte)(stack.Met - 1)));
            ItemWear.DamageHeld(player, 1);

            // Отдача, дым у ствола, звук
            float sin = MathF.Sin(player.RotationYaw), cos = MathF.Cos(player.RotationYaw);
            player.SetPhysicsImpulse(-sin * Recoil, 0, cos * Recoil);
            Vector3 muzzle = new Vector3(player.PosX + sin * .8f, player.PosY + player.GetEyeHeight() - .2f, player.PosZ - cos * .8f);
            world.SpawnParticle(EntitiesFXRegMvk.SmokeId, 8, muzzle, new Vector3(.3f), 0, 0);
            world.PlaySoundDistance(AudioIndexs.GetKey("Shot"), muzzle, 1, .9f + world.Rnd.NextFloat() * .2f);
        }

        #endregion

        #region Перезарядка

        public override ResultHandSecond OnSecond(bool begin, ItemStack stack, PlayerClientOwner player, int counter)
        {
            if (stack.Met >= Barrels) return new ResultHandSecond(ResultHandSecond.ActionType.None);
            // 0 — начало перезарядки, 1 — продолжение (серверу только для отсчёта времени)
            return new ResultHandSecond(10, begin ? 0 : 1);
        }

        public override int OnSecondEnd(ItemStack stack, PlayerClientOwner player, bool abort, int counter)
            => abort ? 255 : 100;

        public override void OnUseSecond(ItemStack stack, PlayerServer player, int number)
        {
            if (player is not PlayerServerMvk mvk) return;
            uint tick = player.GetWorldServer().Server.TickCounterGlobal;
            if (number == 0)
            {
                _reloadStart.AddOrUpdate(player, new StrongBox<uint>(tick));
                return;
            }
            if (number != 100 || !_reloadStart.TryGetValue(player, out StrongBox<uint>? start)) return;
            _reloadStart.Remove(player);
            int empty = Barrels - stack.Met;
            // Время по тактам сервера: каждый пустой ствол — ReloadTicks
            int loadable = Math.Min(empty, (int)((tick - start.Value) / (uint)Math.Max(1, ReloadTicks)));
            int loaded = 0;
            for (int i = 0; i < loadable; i++)
            {
                if (!player.CreativeMode)
                {
                    int bulletSlot = ItemAmmo.Find(mvk, item => item is ItemAmmoPart part && part.Fits("Bullet", Caliber));
                    int chargeSlot = ItemAmmo.Find(mvk, item => item is ItemAmmoPart part && part.Fits(Charge, Caliber));
                    if (bulletSlot < 0 || chargeSlot < 0) break;
                    ItemAmmo.ConsumeOne(mvk, bulletSlot);
                    ItemAmmo.ConsumeOne(mvk, chargeSlot);
                }
                loaded++;
            }
            if (loaded == 0) return;
            int slot = mvk.InvPlayer.GetCurrentIndex();
            ItemStack current = mvk.InvPlayer.GetStackInSlot(slot);
            if (current?.Item != this) return;
            mvk.InvPlayer.SetInventorySlotContents(slot, new ItemStack(this, 1, current.ItemDamaged, (byte)(current.Met + loaded)));
            WorldServer world = player.GetWorldServer();
            world.PlaySoundDistance(AudioIndexs.GetKey("Click"), player.GetPositionCenterVec(), .6f, .7f);
        }

        #endregion
    }
}
