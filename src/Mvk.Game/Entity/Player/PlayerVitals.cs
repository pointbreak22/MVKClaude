#nullable enable
using System;
using Mvk.Core.Json;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Util;
using Mvk.Game.Entity.List;

namespace Mvk.Game.Entity.Player
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Здоровье и выносливость игрока в выживании (по образцу Hytale), сервер. Здоровье восстанавливается
    /// после паузы без урона. Выносливость тратится на бег, прыжок, удар, бросок, блок щитом и натяжение лука,
    /// восстанавливается после короткой паузы без трат; без выносливости бег выключается (клиент), удар и бросок
    /// слабее. Значения — в Mvk2Entities/Vitals.json (если файла нет — значения по умолчанию).
    /// </summary>
    public sealed class PlayerVitals
    {
        /// <summary>
        /// Настройки (json Vitals)
        /// </summary>
        public sealed class Config
        {
            public int HealthDelay = 100;
            public int HealthInterval = 20;
            public float HealthAmount = 1;
            public float Sprint = .5f;
            public float Jump = 6;
            public float Attack = 8;
            public float Throw = 10;
            public float Guard = .25f;
            public float Draw = .2f;
            public int StaminaDelay = 20;
            public float StaminaRegen = .8f;
            public float WeakFactor = .5f;
            public int RespawnDelay = 20;
            public bool DropOnDeath;

            public static Config Load()
            {
                Config c = new Config();
                try
                {
                    JsonRead json = new JsonRead(Options.PathEntities + "Vitals.json");
                    if (!json.IsThereFile) return c;
                    JsonCompound s = json.Compound;
                    if (s.IsKey("HealthDelay")) c.HealthDelay = s.GetInt("HealthDelay");
                    if (s.IsKey("HealthInterval")) c.HealthInterval = Math.Max(1, s.GetInt("HealthInterval"));
                    if (s.IsKey("HealthAmount")) c.HealthAmount = s.GetFloat("HealthAmount");
                    if (s.IsKey("Sprint")) c.Sprint = s.GetFloat("Sprint");
                    if (s.IsKey("Jump")) c.Jump = s.GetFloat("Jump");
                    if (s.IsKey("Attack")) c.Attack = s.GetFloat("Attack");
                    if (s.IsKey("Throw")) c.Throw = s.GetFloat("Throw");
                    if (s.IsKey("Guard")) c.Guard = s.GetFloat("Guard");
                    if (s.IsKey("Draw")) c.Draw = s.GetFloat("Draw");
                    if (s.IsKey("StaminaDelay")) c.StaminaDelay = s.GetInt("StaminaDelay");
                    if (s.IsKey("StaminaRegen")) c.StaminaRegen = s.GetFloat("StaminaRegen");
                    if (s.IsKey("WeakFactor")) c.WeakFactor = s.GetFloat("WeakFactor");
                    if (s.IsKey("RespawnDelay")) c.RespawnDelay = Math.Clamp(s.GetInt("RespawnDelay"), 1, 50);
                    if (s.IsKey("DropOnDeath")) c.DropOnDeath = s.GetBool("DropOnDeath");
                }
                catch (Exception)
                {
                    // Ошибка в файле — значения по умолчанию
                }
                return c;
            }
        }

        private static Config? _config;
        private static Config _Cfg => _config ??= Config.Load();

        private float _healthPrev = -1;
        private int _deadTicks;

        /// <summary>
        /// Точка возрождения (как первый спавн игрока, PlayerServerMvk.CreatePlayer)
        /// </summary>
        private const int _SpawnX = -320, _SpawnZ = 130;

        /// <summary>
        /// Выбросить карманы, левую руку и рюкзак на месте гибели
        /// </summary>
        private static void _DropInventory(PlayerServerMvk player)
        {
            Inventory.InventoryPlayerMvk inventory = player.InvPlayer;
            int pockets = Inventory.InventoryPlayerMvk.PocketCount;
            int backpack = pockets + Inventory.InventoryPlayerMvk.ClothCount;
            int all = backpack + Inventory.InventoryPlayerMvk.BackpackCount;
            for (int slot = 0; slot < all; slot++)
            {
                if (slot > pockets && slot < backpack) continue;
                Mvk.Engine.Item.ItemStack stack = inventory.GetStackInSlot(slot);
                if (stack == null) continue;
                Mvk.Engine.Item.ItemStack.SpawnAsEntity(player.GetWorldServer(),
                    new Mvk.Engine.World.Block.BlockPos(player.PosX, player.PosY + .5f, player.PosZ), stack);
                inventory.SetInventorySlotContents(slot, null!);
            }
        }

        /// <summary>
        /// Возрождение: полные здоровье и выносливость, точка спавна, сброс блока и натяжения, сообщение
        /// </summary>
        private void _Respawn(PlayerServerMvk player)
        {
            _deadTicks = 0;
            player.SetHealth(player.GetHealthMax());
            player.SetStamina(PlayerBase.StaminaMax);
            _healthPrev = player.GetHealth();
            _healTicks = 0;
            player.SetHandPose(false);
            int y = player.GetWorldServer().GetHeightMap(_SpawnX, _SpawnZ);
            player.SetPositionServer(_SpawnX + .5f, y > 0 ? y + .1f : 100, _SpawnZ + .5f);
            player.SendMessage(Mvk.Engine.Realms.ChatStyle.Red + "Вы погибли, возрождение");
        }
        private int _healTicks;
        private int _staminaIdle;
        private bool _groundPrev = true;
        private float _yPrev;

        /// <summary>
        /// Такт игрока на сервере
        /// </summary>
        public void Update(PlayerServerMvk player)
        {
            Config c = _Cfg;
            float health = player.GetHealth();
            // [ПОВЕДЕНИЕ] Временное авто-возрождение: через секунду после гибели (до того, как DamageLiving
            // через 3 с пометит игрока удалённым из мира — отсюда было «сломанное» состояние после смерти)
            if (health <= 0 && !player.CreativeMode && !player.IsSpectator())
            {
                // [ПОВЕДЕНИЕ] Штраф за смерть (по умолчанию выключен, DropOnDeath): карманы, левая рука и рюкзак
                // выпадают на месте гибели, одежда остаётся
                if (_deadTicks == 0 && c.DropOnDeath) _DropInventory(player);
                if (++_deadTicks >= c.RespawnDelay) _Respawn(player);
                return;
            }
            _deadTicks = 0;
            if (player.CreativeMode || player.IsSpectator() || health <= 0)
            {
                _healthPrev = health;
                if (player.GetStamina() < PlayerBase.StaminaMax) player.SetStamina(PlayerBase.StaminaMax);
                return;
            }
            // Здоровье: пауза после урона, затем понемногу
            if (_healthPrev >= 0 && health < _healthPrev) _healTicks = 0;
            _healthPrev = health;
            if (++_healTicks > c.HealthDelay && !player.IsHealthMax() && (_healTicks - c.HealthDelay) % c.HealthInterval == 0)
            {
                player.SetHealth(health + c.HealthAmount);
                _healthPrev = player.GetHealth();
            }
            // Выносливость
            float spend = 0;
            if (player.IsSprinting()) spend += c.Sprint;
            if (player.IsBlocking) spend += c.Guard;
            else if (player.IsHandPose()) spend += c.Draw;
            if (_groundPrev && !player.OnGround && player.PosY > _yPrev + .05f) spend += c.Jump;
            _groundPrev = player.OnGround;
            _yPrev = player.PosY;
            Spend(player, spend);
            if (_staminaIdle > 0) _staminaIdle--;
            else if (player.GetStamina() < PlayerBase.StaminaMax) player.SetStamina(player.GetStamina() + c.StaminaRegen);
        }

        /// <summary>
        /// Потратить выносливость (пауза восстановления начинается заново)
        /// </summary>
        public void Spend(PlayerServerMvk player, float amount)
        {
            if (amount <= 0 || player.CreativeMode) return;
            player.SetStamina(player.GetStamina() - amount);
            _staminaIdle = _Cfg.StaminaDelay;
        }

        /// <summary>
        /// Действие игрока: тратит выносливость, без неё — ослабленное
        /// </summary>
        public float Action(PlayerServerMvk player, PlayerServer.StaminaAction action)
        {
            if (player.CreativeMode) return 1;
            bool weak = player.GetStamina() <= 0;
            Spend(player, action == PlayerServer.StaminaAction.Attack ? _Cfg.Attack : _Cfg.Throw);
            return weak ? _Cfg.WeakFactor : 1;
        }
    }
}
