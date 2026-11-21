#nullable enable
using Mvk.Core.Maths;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.AI;
using Mvk.Engine.Entity.List;
using Mvk.Engine.Item;
using Mvk.Game.Entity.List;
using Mvk.Game.Item;

namespace Mvk.Game.Entity.AI
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Ближний бой враждебного моба: путь к цели пересчитывается раз в 10 тактов, в пределах досягаемости —
    /// удар с откатом
    /// </summary>
    public sealed class EntityAIAttackMeleeMvk : EntityAIBase
    {
        private readonly EntityHostileMvk _hostile;
        private readonly float _speed;
        private readonly float _damage;
        private readonly int _cooldown;
        private readonly float _knockback;
        private EntityLiving? _target;
        private int _pathTicks;
        private int _attackTicks;

        public EntityAIAttackMeleeMvk(EntityHostileMvk entity, float speed, float damage, int cooldown, float knockback)
            : base(entity, 3)
        {
            _hostile = entity;
            _speed = speed;
            _damage = damage;
            _cooldown = cooldown;
            _knockback = knockback;
        }

        public override bool ShouldExecute() => (_target = _hostile.FindTarget()) != null;

        public override void StartExecuting() => _pathTicks = 0;

        public override void ResetTask()
        {
            _target = null;
            _entity.Navigator.ClearPathEntity();
        }

        public override void UpdateTask()
        {
            if (_target == null) return;
            _entity.LookHelper.SetLookPositionWithEntity(_target, Glm.Pi30, Glm.Pi30);
            float distance = _entity.Distance(_target);
            if (--_pathTicks <= 0)
            {
                _pathTicks = 10;
                _entity.Navigator.TryMoveToEntityLiving(_target, _speed, true, false, 0);
            }
            if (_attackTicks > 0) _attackTicks--;
            if (distance < 5f && _attackTicks == 0)
            {
                _attackTicks = _cooldown;
                _hostile.Swing();
                _hostile.HitTarget(_target, _damage, _knockback);
            }
        }
    }

    /// <summary>
    /// [ПОВЕДЕНИЕ] Стрельба враждебного моба: держится между near и far от цели (ближе — отходит, дальше — подходит),
    /// раз в cooldown тактов стреляет стрелой в цель с упреждением по высоте
    /// </summary>
    public sealed class EntityAIAttackRangedMvk : EntityAIBase
    {
        private readonly EntityHostileMvk _hostile;
        private readonly float _speed;
        private readonly float _near;
        private readonly float _far;
        private readonly int _cooldown;
        private EntityLiving? _target;
        private int _pathTicks;
        private int _attackTicks;

        public EntityAIAttackRangedMvk(EntityHostileMvk entity, float speed, float near, float far, int cooldown)
            : base(entity, 3)
        {
            _hostile = entity;
            _speed = speed;
            _near = near;
            _far = far;
            _cooldown = cooldown;
        }

        public override bool ShouldExecute() => (_target = _hostile.FindTarget()) != null;

        public override void StartExecuting()
        {
            _pathTicks = 0;
            _attackTicks = _cooldown / 2;
        }

        public override void ResetTask()
        {
            _target = null;
            _entity.Navigator.ClearPathEntity();
        }

        public override void UpdateTask()
        {
            if (_target == null) return;
            _entity.LookHelper.SetLookPositionWithEntity(_target, Glm.Pi30, Glm.Pi30);
            float distance = _entity.Distance(_target);
            if (--_pathTicks <= 0)
            {
                _pathTicks = 10;
                if (distance > _far)
                {
                    _entity.Navigator.TryMoveToEntityLiving(_target, _speed, true, false, 0);
                }
                else if (distance < _near)
                {
                    // Отойти от цели
                    float dx = _entity.PosX - _target.PosX, dz = _entity.PosZ - _target.PosZ;
                    float len = Mth.Max(.001f, Mth.Sqrt(dx * dx + dz * dz));
                    _entity.Navigator.TryMoveToXYZ(_entity.PosX + dx / len * 10, _entity.PosY, _entity.PosZ + dz / len * 10,
                        _speed, true, true, 0);
                }
                else
                {
                    _entity.Navigator.ClearPathEntity();
                }
            }
            if (_attackTicks > 0) _attackTicks--;
            if (_attackTicks == 0 && distance <= _far + 4)
            {
                _attackTicks = _cooldown;
                _Shoot(_target, distance);
            }
        }

        /// <summary>
        /// Выстрел стрелой от глаз моба в центр цели, с поправкой на падение
        /// </summary>
        private void _Shoot(EntityLiving target, float distance)
        {
            float dx = target.PosX - _entity.PosX;
            float dz = target.PosZ - _entity.PosZ;
            float dy = target.PosY + target.Size.GetHeight() * .6f - (_entity.PosY + _entity.SizeLiving.GetEye());
            float horizontal = Mth.Sqrt(dx * dx + dz * dz);
            float yaw = Glm.Atan2(dx, -dz);
            float pitch = Glm.Atan2(dy + horizontal * .06f, horizontal);
            _hostile.Swing("ShotRight");
            for (int i = 0; i < Ce.Items.ItemAlias.Length; i++)
            {
                if (Ce.Items.ItemAlias[i] != "Arrow") continue;
                EntityProjectile.ThrowFrom(_entity, new ItemStack(Ce.Items.ItemObjects[i], 1), 1.6f,
                    EntityArrowMvk.AliasArrow, yaw, pitch);
                break;
            }
        }
    }
}
