using Mvk.Engine.Item;
using Mvk.Game.Item;
using Mvk.Game.Entity.List;
using Mvk.Game.Entity.Inventory;
using Mvk.Engine.Item.List;
using System;
using System.Runtime.CompilerServices;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Particle;
using Mvk.Engine.World;
using Mvk.Core.Maths;

namespace Mvk.Game.Entity.Damage
{
    /// <summary>
    /// Класс урона для живых сущностей, мобов и игроков
    /// </summary>
    public class DamageLiving : DamageBase
    {
        /// <summary>
        /// Сколько тактов сущность будет видна, покуда не исчезнет
        /// </summary>
        private static int _tiktOfDeath = 60;
        /// <summary>
        /// Сущность к которой прекреплена физика
        /// </summary>
        protected readonly EntityLiving _entity;
        /// <summary>
        /// Мир сервера где находится сущность
        /// </summary>
        protected WorldServer _worldServer => _entity.GetWorldServer();

        /// <summary>
        /// Оставшееся время эта сущность должна вести себя как «мертвая», то есть иметь в мире труп.
        /// </summary>
        public int DeathTime { get; protected set; } = 0;

        public DamageLiving(EntityLiving entity)
        {
            _entity = entity;
        }

        /// <summary>
        /// Обновление в игровом такте
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Update()
        {
            // если нет хп обновлям смертельную картинку
            if (DeathTime != -1 && _entity.GetHealth() <= 0)
            {
                DeathTime++;
                if (DeathTime >= _tiktOfDeath)
                {
                    // Исчизновение сущности после смерти
                    _worldServer.SpawnParticle(EntitiesFXReg.PartColorId, 50,
                        _entity.GetPositionCenterVec(), new Vector3(2f, .5f, 2f), 25, 0x882222);
                    _worldServer.SpawnParticle(EntitiesFXReg.PartColorId, 50,
                        _entity.GetPositionCenterVec(), new Vector3(2f, .5f, 2f), 25, 0x1000000);

                    //World.SpawnParticle(EnumParticle.Test, 100, Position, new vec3(Width, .25f, Width), 0);
                    DeathTime = -1;
                    _entity.SetDead();
                }

                //DeathUpdate();
            }
        }

        /// <summary>
        /// Сущность получает урон, только на сервере.
        /// hitY = -1 удар по всему телу, 
        /// hitY = -2 = удар повсему телу без урона по одежде
        /// </summary>
        /// <returns>true - урон был нанесён</returns>
        public override bool OnAttack(byte source, float hitY, float damage,
            EntityLiving entityAttack = null)
        {
            if (_entity is EntityMob entityMob) entityMob.EntityAge = 0;
            if (_entity.GetHealth() <= 0) return false;

            return base.OnAttack(source, hitY, damage, entityAttack);
        }

        /// <summary>
        /// Атака уже после проверок имунитетов
        /// </summary>
        protected override void _OnAttack(byte source, float damage, EntityLiving entityAttack)
        {
            float health = _entity.GetHealth() - damage;
            _entity.SetHealth(health);

            Console.WriteLine("GetHealth" + _entity.GetHealth());
            // Анимация урона
            _worldServer.SpawnParticle(EntitiesFXReg.PartColorId, 5, 
                _entity.GetPositionCenterVec(), new Vector3(.5f), 1, 0x1000000);
            //worldServer.Tracker.SendToAllTrackingEntityCurrent(this, new PacketS0BAnimation(Id,
            //        PacketS0BAnimation.EnumAnimation.Hurt, (byte)enumBody));

            if (health <= 0)
            {
                // Звук смерти
               // _worldServer.PlaySound
                //World.PlaySound(GetDeathSound(), Position, 1, (rand.NextFloat() - rand.NextFloat()) * .2f + 1f);
               // _entity.OnDeath(source, entityAttack == null ? -1 : entityAttack.IndexEntity);

                //entityPlayerServer.OnDeathPlayerServer(worldServer, source, entityAttacks);
            }
            else
            {
                // Звук урона
                //if (source != EnumDamageSource.OnFire && source != EnumDamageSource.Drown
                //    && source != EnumDamageSource.Fall)
                //{
                //    World.PlaySound(SampleHurt(), Position, 1, (rand.NextFloat() - rand.NextFloat()) * .2f + 1f);
                //}

                if (entityAttack != null && _entity is EntityMob entityMob)
                {
                    entityMob.SetLastAttacker(entityAttack);
                }
            }
        }

        /// <summary>
        /// Получить урон от расположения по телу.
        /// 1. Коэффицент в голову или другую чась тиела
        /// 2. Защита от брони
        /// 3. Защита от магии
        /// </summary>
        /// <param name="hitY">если -1 = удар по всему телу, если -2 = удар повсему телу без урона по одежде</param>
        /// <summary>
        /// Защита одежды игрока по части тела: голова — шапка, тело — куртка, ноги — штаны, стопы — обувь,
        /// всё тело — средняя защита четырёх частей
        /// </summary>
        private float _Protection(EnumBodyDamage body)
        {
            if (_entity is not PlayerServerMvk player) return 0;
            InventoryPlayerMvk inventory = player.InvPlayer;
            return body switch
            {
                EnumBodyDamage.Head => _Cloth(inventory, EnumCloth.Hat),
                EnumBodyDamage.Body => _Cloth(inventory, EnumCloth.Jacket),
                EnumBodyDamage.Legs => _Cloth(inventory, EnumCloth.Pants),
                EnumBodyDamage.Feet => _Cloth(inventory, EnumCloth.Shoes),
                EnumBodyDamage.All => (_Cloth(inventory, EnumCloth.Hat) + _Cloth(inventory, EnumCloth.Jacket)
                    + _Cloth(inventory, EnumCloth.Pants) + _Cloth(inventory, EnumCloth.Shoes)) / 4f,
                _ => 0
            };
        }

        /// <summary>
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
        /// Защита вещи в слоте одежды (слот одежды = карманы + ключ части тела - 1)
        /// </summary>
        private static float _Cloth(InventoryPlayerMvk inventory, EnumCloth part)
            => inventory.GetStackInSlot(InventoryPlayerMvk.PocketCount + (int)part - 1)?.Item is ItemCloth cloth
                ? cloth.Protection : 0;

        protected override float _GetDamageFromBody(byte source, float hitY, float damage)
        {
            // Определяем куда попали
            EnumBodyDamage enumBody;

            if (hitY == -1) enumBody = EnumBodyDamage.All;
            else if (hitY == -2) enumBody = EnumBodyDamage.Null;
            else
            {
                float height = _entity.Size.GetHeight();

                if (hitY < .1f) enumBody = EnumBodyDamage.Feet; // Стопы
                else if (hitY >= height * .75f)
                {
                    enumBody = EnumBodyDamage.Head; // Голова
                    damage *= 1.25f; // в голову урон на 25% больше
                }
                else if (hitY < height * .375f)
                {
                    enumBody = EnumBodyDamage.Legs; // Ноги
                    damage *= .85f; // в ноги на 15% меньше
                }
                else enumBody = EnumBodyDamage.Body; // Тело
            }

            // Защита надетой брони по части тела (сервер): только от ударов, снарядов и взрывов, вещь изнашивается
            if (source == (byte)EnumDamageSource.Player || source == (byte)EnumDamageSource.Piece
                || source == (byte)EnumDamageSource.ExplosionSource)
            {
                float protection = _Protection(enumBody);
                if (protection > 0)
                {
                    damage *= 1f - protection;
                    _WearArmor(enumBody);
                }
            }
            Console.Write(" " + enumBody);

            return damage;
        }
    }
}
