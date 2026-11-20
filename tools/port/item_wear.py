"""[ПОВЕДЕНИЕ] Износ на сервере (меч, мотыга, броня, щит): ItemStack.Wear и помощник ItemWear (new/Item/ItemWear.cs).
Износ инструмента при копании по-прежнему считает клиент, как в оригинале; новый износ (удар по сущности, броня,
щит, мотыга, лук, мушкет) считает сервер и рассылает слот клиенту."""
from portlib import load, save, rep

rel = r"Item\ItemStack.cs"
t = load(rel)
t = rep(t, """        public bool IsItemStackDamageable() => Item.MaxDamage > 0;""", """        public bool IsItemStackDamageable() => Item.MaxDamage > 0;

        /// <summary>
        /// Износ на сервере: прибавить урон предмету, true — предмет сломался (износ обнулён)
        /// </summary>
        public bool Wear(int damage)
        {
            ItemDamaged += damage;
            if (ItemDamaged < Item.MaxDamage) return false;
            ItemDamaged = 0;
            return true;
        }""", rel, 1)
save(rel, t)
# Удар по сущности изнашивает предмет в руке (в оригинале атака не изнашивала инструмент): считает сервер
rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """                            // Если урон сработал добавляем импулс
                            entity.SetPhysicsImpulse(packet.X, packet.Y, packet.Z);""", """                            // Если урон сработал добавляем импулс
                            entity.SetPhysicsImpulse(packet.X, packet.Y, packet.Z);
                            // [ПОВЕДЕНИЕ] Износ оружия или инструмента при ударе (сервер)
                            if (stack != null) ItemWear.DamageHeld(this, 1);""", rel, 1)
save(rel, t)
print("item_wear ok")
