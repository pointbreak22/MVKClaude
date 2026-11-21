"""[ПОВЕДЕНИЕ] Здоровье и выносливость игрока (просьба пользователя 21.11, как в Hytale): выносливость — третье
поле метаданных игрока (сервер считает, клиент получает своё значение синхронизацией метаданных); при нуле бег
выключается на клиенте, удар и бросок слабее (решает наследник сервера через StaminaFactor)."""
from portlib import load, save, rep

rel = r"Entity\EntityLiving.cs"
t = load(rel)
t = rep(t, """            MetaData = new DataWatcher(2);
            // флаги""", """            MetaData = new DataWatcher(_MetaDataCount());
            // флаги""", rel, 1)
t = rep(t, """        protected override void _InitMetaData()
        {
            MetaData = new DataWatcher(_MetaDataCount());""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Число полей метаданных (у игрока ещё выносливость)
        /// </summary>
        protected virtual int _MetaDataCount() => 2;

        protected override void _InitMetaData()
        {
            MetaData = new DataWatcher(_MetaDataCount());""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerBase.cs"
t = load(rel)
t = rep(t, """        public void SetSpectator(bool invisible) => SetFlag(5, invisible);

        #endregion""", """        public void SetSpectator(bool invisible) => SetFlag(5, invisible);

        /// <summary>
        /// [ПОВЕДЕНИЕ] Наибольшая выносливость
        /// </summary>
        public const float StaminaMax = 100;

        protected override int _MetaDataCount() => 3;

        protected override void _InitMetaData()
        {
            base._InitMetaData();
            MetaData.Set(2, StaminaMax);
        }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Выносливость 0..StaminaMax (поле метаданных 2)
        /// </summary>
        public float GetStamina() => MetaData.GetFloat(2);

        /// <summary>
        /// [ПОВЕДЕНИЕ] Задать выносливость (сервер)
        /// </summary>
        public void SetStamina(float stamina) => MetaData.UpdateObject(2, Math.Clamp(stamina, 0, StaminaMax));

        #endregion""", rel, 1)
if "using System;" not in t:
    t = "using System;\n" + t.lstrip("﻿")
save(rel, t)

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """                        else if (IsMomentum()) damage *= stack != null ? stack.Item.CritMultiplier : 1.5f;""",
        """                        else if (IsMomentum()) damage *= stack != null ? stack.Item.CritMultiplier : 1.5f;
                        // [ПОВЕДЕНИЕ] Удар тратит выносливость, без неё слабее
                        damage *= StaminaFactor(StaminaAction.Attack);""", rel, 1)
t = rep(t, """                    float power = stack.Item.ThrowMin + (stack.Item.ThrowMax - stack.Item.ThrowMin) * charge;""",
        """                    float power = stack.Item.ThrowMin + (stack.Item.ThrowMax - stack.Item.ThrowMin) * charge;
                    // [ПОВЕДЕНИЕ] Бросок тратит выносливость, без неё слабее
                    power *= StaminaFactor(StaminaAction.Throw);""", rel, 1)
t = rep(t, """        /// <summary>
        /// Пакет взаимодействие с выбранным предметом в руке без RayCast блока""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Действия, тратящие выносливость
        /// </summary>
        public enum StaminaAction { Attack, Throw }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Потратить выносливость на действие; множитель силы действия (1 — в полную силу)
        /// </summary>
        public virtual float StaminaFactor(StaminaAction action) => 1;

        /// <summary>
        /// Пакет взаимодействие с выбранным предметом в руке без RayCast блока""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerClientOwner.cs"
t = load(rel)
t = rep(t, """            Eye.Update();
            Fov.Update();
""", """            Eye.Update();
            Fov.Update();
            // [ПОВЕДЕНИЕ] Без выносливости бег прекращается (в выживании)
            if (IsSprinting() && !CreativeMode && GetStamina() <= 0) Movement.SetSprinting(false);
""", rel, 1)
save(rel, t)
print("player_vitals ok")

# Признак бега из пакета позиции (клиент присылал, сервер не применял): трата выносливости на бег и разгон
rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """                if (packet.IsSneaking) Sitting(); else Standing();
            }
            LevelMotionChange = 1;""", """                if (packet.IsSneaking) Sitting(); else Standing();
            }
            // [ПОВЕДЕНИЕ] Бег с клиента (выносливость, разгон)
            if (IsSprinting() != packet.IsSprinting) SetSprinting(packet.IsSprinting);
            LevelMotionChange = 1;""", rel, 1)
save(rel, t)
print("player_vitals sprint ok")
