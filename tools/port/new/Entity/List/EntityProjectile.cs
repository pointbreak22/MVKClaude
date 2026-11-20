#nullable enable
using System;
using Mvk.Core.Maths;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;
using Mvk.Engine.Util;
using Mvk.Engine.World;

namespace Mvk.Engine.Entity.List
{
    /// <summary>
    /// Снаряд: брошенный предмет, который попадает в блок или сущность (камень, динамит, позже стрела и пуля).
    /// Рисуется как предмет (EntityItem). По идее EntityThrowable и EntityExplosives старого проекта автора (Mvk),
    /// код свой. Попадание считает только сервер: в каждом такте полёта луч от позиции по вектору движения такта
    /// (CollisionBase.RayCast, блоки и живые сущности); клиент только показывает полёт. После попадания снаряд
    /// становится обычным лежащим предметом, если наследник не решит иначе (динамит взрывается).
    /// </summary>
    public class EntityProjectile : EntityItem
    {
        /// <summary>
        /// Псевдоним сущности снаряда в реестре
        /// </summary>
        public const string Alias = "Projectile";
        /// <summary>
        /// Сколько тактов после броска луч не задевает бросившего
        /// </summary>
        private const int _IgnoreThrowerTicks = 5;
        /// <summary>
        /// Наибольшее время полёта в тактах, дальше снаряд считается упавшим
        /// </summary>
        private const int _MaxFlightTicks = 400;

        /// <summary>
        /// Id сущности, бросившей снаряд
        /// </summary>
        public int ThrowerId { get; private set; } = -1;
        /// <summary>
        /// Сила броска: множитель скорости и урона
        /// </summary>
        public float Power { get; private set; } = 1;
        /// <summary>
        /// Снаряд летит (после попадания или загрузки из файла — обычный предмет)
        /// </summary>
        public bool IsFlying { get; private set; }

        private int _flightTicks;

        /// <summary>
        /// Бросить стак предмета снарядом от игрока (сервер). Сила 1 — обычный дальний бросок предмета.
        /// alias — сущность снаряда в реестре (наследник EntityProjectile, например динамит)
        /// </summary>
        public static EntityProjectile? Throw(PlayerServer player, ItemStack stack, float power, string alias = Alias)
        {
            WorldServer worldServer = player.GetWorldServer();
            if (worldServer == null) return null;
            int index = _IndexEntity(alias);
            if (index < 0) return null;
            if (Ce.Entities.CreateEntityServer((ushort)index, worldServer) is not EntityProjectile projectile) return null;
            projectile.SetEntityItemStack(stack);
            projectile.ThrowerId = player.Id;
            projectile.Power = power;
            projectile.IsFlying = true;
            worldServer.EntityDropsEntityInWorld(player, projectile, true, true);
            projectile.Physics.MotionX *= power;
            projectile.Physics.MotionY *= power;
            projectile.Physics.MotionZ *= power;
            return projectile;
        }

        /// <summary>
        /// Индекс сущности в активных реестрах, -1 если мод её не зарегистрировал
        /// </summary>
        private static int _IndexEntity(string alias)
        {
            string[] aliases = Ce.Entities.EntitiesAlias;
            for (int i = 0; i < aliases.Length; i++)
            {
                if (aliases[i] == alias) return i;
            }
            return -1;
        }

        /// <summary>
        /// Снаряд не цель для луча игрока и других снарядов
        /// </summary>
        public override bool CanBeCollidedWith() => !IsFlying && base.CanBeCollidedWith();

        public override void Update()
        {
            if (IsFlying && !IsDead)
            {
                if (_CheckImpact()) return;
                if (++_flightTicks > _MaxFlightTicks)
                {
                    _Impact(null, new Vector3(PosX, PosY, PosZ), -2);
                    if (IsDead) return;
                }
            }
            base.Update();
            if (IsFlying && !IsDead && OnGround)
            {
                // Упал, не задев луч (медленный снаряд): попадание в землю под собой
                _Impact(null, new Vector3(PosX, PosY, PosZ), -2);
            }
        }

        /// <summary>
        /// Луч по движению такта: первое пересечение с блоком или живой сущностью
        /// </summary>
        private bool _CheckImpact()
        {
            float mx = Physics.MotionX, my = Physics.MotionY, mz = Physics.MotionZ;
            float distance = Mth.Sqrt(mx * mx + my * my + mz * mz);
            if (distance < .001f) return false;
            Vector3 dir = new Vector3(mx / distance, my / distance, mz / distance);
            float py = PosY + Size.GetHeight() * .5f;
            int exclude = _flightTicks < _IgnoreThrowerTicks ? ThrowerId : Id;
            CollisionBase collision = _worldServer.Collision;
            collision.RayCast(PosX, py, PosZ, dir, distance, true, exclude);
            MovingObjectPosition moving = collision.MovingObject;
            if (moving.IsEntity() && moving.Entity is EntityLiving living && living.Id != Id)
            {
                _Impact(living, new Vector3(living.PosX, living.PosY + moving.RayHit.Y, living.PosZ), moving.RayHit.Y);
                return IsDead;
            }
            if (moving.IsBlock())
            {
                // Остановиться у блока: дальше обычная физика предмета
                _Impact(null, moving.RayHit, -2);
                return IsDead;
            }
            return false;
        }

        /// <summary>
        /// Попадание (сервер): урон сущности и переход в лежащий предмет
        /// </summary>
        private void _Impact(EntityLiving? entity, Vector3 pos, float hitY)
        {
            IsFlying = false;
            EntityLiving? thrower = _worldServer.LoadedEntityList.Get(ThrowerId) as EntityLiving;
            _OnImpact(entity, pos, hitY, thrower);
            if (!IsDead && entity != null)
            {
                // От сущности снаряд падает вниз
                Physics.MotionX *= -.1f;
                Physics.MotionZ *= -.1f;
                Physics.MotionY = 0;
            }
        }

        /// <summary>
        /// Что делает снаряд при попадании (сервер). entity — живая сущность или null (блок, земля),
        /// hitY — высота попадания от ног сущности. По умолчанию урон предмета в руке, умноженный на силу броска.
        /// </summary>
        protected virtual void _OnImpact(EntityLiving? entity, Vector3 pos, float hitY, EntityLiving? thrower)
        {
            if (entity?.Damage == null) return;
            ItemStack stack = GetEntityItemStack();
            float damage = (stack != null ? stack.Item.GetDamageToAttack() : 1) * Power;
            entity.Damage.OnAttack((byte)EnumDamageSource.Piece, hitY, damage, thrower!);
        }
    }
}
