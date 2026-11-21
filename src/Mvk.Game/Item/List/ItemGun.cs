#nullable enable
using System;
using Mvk.Core.Json;
using Mvk.Core.Maths;
using Mvk.Engine.Audio;
using Mvk.Engine.Entity.Inventory;
using Mvk.Engine.Entity.List;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;
using Mvk.Engine.World;
using Mvk.Game.Entity.Inventory;
using Mvk.Game.Entity.List;
using Mvk.Game.Entity.Particle;

namespace Mvk.Game.Item.List
{
    /// <summary>
    /// Огнестрельное оружие (мушкет, двустволка) по json: стволы (Barrels), калибр (Caliber), урон пули (Damage),
    /// такты перезарядки одного ствола (ReloadTicks), заряд (Charge: Powder или Shell), сила пули (Speed), отдача (Recoil).
    /// Заряженные стволы хранятся в стаке (Met). Клик — выстрел; [ПОВЕДЕНИЕ] перезарядка автоматическая по стволу
    /// за ReloadTicks (GunReload), идёт у оружия в любом слоте кармана и в левой руке. Клиент только сообщает намерение; заряд, время перезарядки по тактам сервера, расход пули и заряда
    /// из инвентаря, выстрел (быстрый снаряд-пуля с серверным попаданием), отдача, дым, звук и износ — сервер.
    /// Одноручное или двуручное — признак Two в json. [ПОВЕДЕНИЕ] Одноручное (мушкет) стреляет при закрытом щите
    /// (щит раскрывается на выстрел) и перезаряжается под ним; двуручное (двустволка) при закрытом щите только
    /// держится. Вблизи (CloseRange) урон пули ×CloseMultiplier, по блоку пуля бьёт силой BlockStrength.
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
        /// [ПОВЕДЕНИЕ] Сила удара пули по блоку (как удар рукой силой 1; json BlockStrength)
        /// </summary>
        public float BlockStrength { get; private set; } = 1;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Дистанция ближнего выстрела в блоках (json CloseRange; 0 — нет)
        /// </summary>
        public float CloseRange { get; private set; }
        /// <summary>
        /// [ПОВЕДЕНИЕ] Множитель урона ближнего выстрела (json CloseMultiplier)
        /// </summary>
        public float CloseMultiplier { get; private set; } = 1;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Залп: выстрел всеми заряженными стволами сразу (json Volley, двустволка)
        /// </summary>
        public bool Volley { get; private set; }
        /// <summary>
        /// [ПОВЕДЕНИЕ] Пауза после выстрела, тактов (json ShotPause)
        /// </summary>
        public int ShotPause { get; private set; } = 8;

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
            if (state.IsKey("BlockStrength")) BlockStrength = state.GetFloat("BlockStrength");
            if (state.IsKey("CloseRange")) CloseRange = state.GetFloat("CloseRange");
            if (state.IsKey("CloseMultiplier")) CloseMultiplier = state.GetFloat("CloseMultiplier");
            if (state.IsKey("Volley")) Volley = state.GetBool("Volley");
            if (state.IsKey("ShotPause")) ShotPause = state.GetInt("ShotPause");
        }

        public override string GetToolTip(ItemStack stack) => base.GetToolTip(stack) + " [" + stack.Met + "/" + Barrels + "]";

        #region Выстрел

        public override ResultHandAction OnAction(bool begin, ItemStack stack, PlayerClientOwner player)
        {
            // Выстрел по нажатию, без автоогня; пустое оружие не стреляет
            // [ПОВЕДЕНИЕ] Во время перезарядки оружие не стреляет
            if (!begin || stack.Met == 0 || IsReloading(stack, player.Inventory, player.CreativeMode))
                return new ResultHandAction(ResultHandAction.ActionType.None);
            // [ПОВЕДЕНИЕ] Пауза после выстрела — у самого оружия (GunReload); рука после выстрела ждёт 2 такта, другое
            // оружие в этой руке стреляет сразу
            return new ResultHandAction(ResultHandAction.ActionType.UseItem, 2);
        }

        public override void OnUseAction(ItemStack stack, PlayerServer player)
        {
            if (player is not PlayerServerMvk mvk || stack.Met == 0 || _GuardBlocks(player)) return;
            int slot = mvk.InvPlayer.GetCurrentIndex();
            // [ПОВЕДЕНИЕ] Перезарядка и пауза после выстрела по тактам сервера (GunReload)
            if (!mvk.Guns.CanShoot(slot, stack, mvk.InvPlayer, mvk.CreativeMode)) return;
            WorldServer world = player.GetWorldServer();
            // Пуля: снаряд с серверным попаданием; залп — пуля из каждого заряженного ствола
            int shots = Volley ? stack.Met : 1;
            for (int n = 0; n < shots; n++)
            {
                if (EntityProjectile.Throw(player, new ItemStack(ItemsRegMvk.Bullet, 1), Speed, EntityBulletMvk.AliasBullet) is EntityBulletMvk shot)
                {
                    shot.BulletDamage = BulletDamage;
                    shot.BlockStrength = BlockStrength;
                    shot.CloseRange = CloseRange;
                    shot.CloseMultiplier = CloseMultiplier;
                    shot.Knockback = Knockback;
                    shot.Origin = new Vector3(player.PosX, player.PosY + player.GetEyeHeight(), player.PosZ);
                }
            }
            mvk.InvPlayer.SetInventorySlotContents(slot, new ItemStack(this, 1, stack.ItemDamaged, (byte)(stack.Met - shots)));
            mvk.Guns.Shot(slot, mvk.InvPlayer.GetStackInSlot(slot), ShotPause);
            ItemWear.DamageHeld(player, shots);

            // Отдача, дым у ствола, звук
            float sin = MathF.Sin(player.RotationYaw), cos = MathF.Cos(player.RotationYaw);
            player.SetPhysicsImpulse(-sin * Recoil * shots, 0, cos * Recoil * shots);
            Vector3 muzzle = new Vector3(player.PosX + sin * .8f, player.PosY + player.GetEyeHeight() - .2f, player.PosZ - cos * .8f);
            world.SpawnParticle(EntitiesFXRegMvk.SmokeId, 8, muzzle, new Vector3(.3f), 0, 0);
            world.PlaySoundDistance(AudioIndexs.GetKey("Shot"), muzzle, 1, .9f + world.Rnd.NextFloat() * .2f);
        }

        #endregion

        /// <summary>
        /// [ПОВЕДЕНИЕ] Двуручное оружие не стреляет и не перезаряжается, пока игрок держит закрытый щит
        /// </summary>
        private bool _GuardBlocks(PlayerServer player) => Two && player.IsBlocking;

        #region Перезарядка

        /// <summary>
        /// [ПОВЕДЕНИЕ] Удержание оружия ничего не делает: перезарядка автоматическая (GunReload, сервер)
        /// </summary>
        public override ResultHandSecond OnSecond(bool begin, ItemStack stack, PlayerClientOwner player, int counter)
            => new ResultHandSecond(ResultHandSecond.ActionType.None);

        /// <summary>
        /// [ПОВЕДЕНИЕ] Оружие перезаряжается: не все стволы заряжены и в инвентаре есть пуля и заряд калибра.
        /// Пока идёт перезарядка, оружие не стреляет (одинаково решают клиент и сервер по данным стака и инвентаря).
        /// В творческом режиме боеприпасы не нужны
        /// </summary>
        public bool IsReloading(ItemStack stack, InventoryBase inventory, bool creative)
            => stack.Met < Barrels && (creative || HasAmmo(inventory));

        /// <summary>
        /// Есть ли в инвентаре пуля и заряд калибра оружия
        /// </summary>
        public bool HasAmmo(InventoryBase inventory)
        {
            bool bullet = false, charge = false;
            for (int i = 0; i < InventoryPlayerMvk.PocketCount + InventoryPlayerMvk.ClothCount
                + InventoryPlayerMvk.BackpackCount; i++)
            {
                ItemStack? stack = inventory.GetStackInSlot(i);
                if (stack != null && stack.Amount > 0 && stack.Item is ItemAmmoPart part)
                {
                    if (part.Fits("Bullet", Caliber)) bullet = true;
                    if (part.Fits(Charge, Caliber)) charge = true;
                }
            }
            return bullet && charge;
        }

        /// <summary>
        /// Зарядить один ствол оружия в слоте (сервер, GunReload): расход пули и заряда, щелчок
        /// </summary>
        public bool LoadBarrel(PlayerServerMvk player, int slot)
        {
            if (!player.CreativeMode)
            {
                int bulletSlot = ItemAmmo.Find(player, item => item is ItemAmmoPart part && part.Fits("Bullet", Caliber));
                int chargeSlot = ItemAmmo.Find(player, item => item is ItemAmmoPart part && part.Fits(Charge, Caliber));
                if (bulletSlot < 0 || chargeSlot < 0) return false;
                ItemAmmo.ConsumeOne(player, bulletSlot);
                ItemAmmo.ConsumeOne(player, chargeSlot);
            }
            ItemStack current = player.InvPlayer.GetStackInSlot(slot);
            if (current?.Item != this) return false;
            player.InvPlayer.SetInventorySlotContents(slot, new ItemStack(this, 1, current.ItemDamaged, (byte)(current.Met + 1)));
            player.GetWorldServer().PlaySoundDistance(AudioIndexs.GetKey("Click"), player.GetPositionCenterVec(), .6f, .7f);
            return true;
        }

        #endregion
    }
}
