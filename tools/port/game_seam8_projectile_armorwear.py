"""[ПОВЕДЕНИЕ] Износ брони на сервере: вещь, которая приняла удар, изнашивается на 1 (удар по всему телу — все четыре).
Броня защищает только от ударов, снарядов и взрывов (источники Player, Piece, ExplosionSource), не от падения,
огня, лавы и утопления."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\Damage\DamageLiving.cs"
t = load(rel, GAME)
t = rep(t, """            // Защита надетой брони по части тела (сервер)
            damage *= 1f - _Protection(enumBody);""", """            // Защита надетой брони по части тела (сервер): только от ударов, снарядов и взрывов, вещь изнашивается
            if (source == (byte)EnumDamageSource.Player || source == (byte)EnumDamageSource.Piece
                || source == (byte)EnumDamageSource.ExplosionSource)
            {
                float protection = _Protection(enumBody);
                if (protection > 0)
                {
                    damage *= 1f - protection;
                    _WearArmor(enumBody);
                }
            }""", rel, 1)
t = rep(t, """        /// <summary>
        /// Защита вещи в слоте одежды""", """        /// <summary>
        /// Износ брони, принявшей удар (сервер, ItemWear рассылает слот)
        /// </summary>
        private void _WearArmor(EnumBodyDamage body)
        {
            if (_entity is not PlayerServerMvk player) return;
            if (body == EnumBodyDamage.Head || body == EnumBodyDamage.All) _WearCloth(player, EnumCloth.Hat);
            if (body == EnumBodyDamage.Body || body == EnumBodyDamage.All) _WearCloth(player, EnumCloth.Jacket);
            if (body == EnumBodyDamage.Legs || body == EnumBodyDamage.All) _WearCloth(player, EnumCloth.Pants);
            if (body == EnumBodyDamage.Feet || body == EnumBodyDamage.All) _WearCloth(player, EnumCloth.Shoes);
        }

        private static void _WearCloth(PlayerServerMvk player, EnumCloth part)
        {
            int slot = InventoryPlayerMvk.PocketCount + (int)part - 1;
            if (player.InvPlayer.GetStackInSlot(slot)?.Item is ItemCloth cloth && cloth.Protection > 0)
            {
                ItemWear.DamageSlot(player, slot, 1);
            }
        }

        /// <summary>
        /// Защита вещи в слоте одежды""", rel, 1)
if "using Mvk.Engine.Item;" not in t:
    t = "using Mvk.Engine.Item;\n" + t
save(rel, t, GAME)
print("game_seam8_projectile_armorwear ok")
