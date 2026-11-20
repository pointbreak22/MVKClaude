using Mvk.Engine.Entity.Player;
using System;
using System.Runtime.CompilerServices;
using Mvk.Engine.Item;
using Mvk.Core.Json;
using Mvk.Engine.Json;
using Mvk.Engine.Realms;

namespace Mvk.Game.Item.List.Tool
{
    /// <summary>
    /// Абстрактный класс предмета инструмент
    /// </summary>
    public abstract class ItemAbTool : ItemBase
    {
        /// <summary>
        /// уровень инструмента, Качество дропа, 0-минимум, 1-левел инструмента, 2-левел, 3-левел
        /// </summary>
        public int Level { get; private set; }
        /// <summary>
        /// Пауза между ударами в тактах
        /// </summary>
        public int Pause { get; protected set; }
        /// <summary>
        /// Ускорение разрушении блока, по умолчанию 1, при 0 ломает за один удар.
        /// </summary>
        public float Acceleration { get; protected set; }
        /// <summary>
        /// [ПОВЕДЕНИЕ] Сила удара по своему материалу (json DigBonus)
        /// </summary>
        public float DigBonus { get; protected set; } = 3;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Крит удара по сущности в прыжке или на бегу (json Crit)
        /// </summary>
        public float Crit { get; protected set; } = 1.5f;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Пауза между ударами по сущности при удержании, тактов (json AttackPause; 0 — по умолчанию 10)
        /// </summary>
        public int AttackPause { get; protected set; }

        /// <summary>
        /// ID для графики стола, значение инструмента от 1 до 10
        /// </summary>
        //public ushort TableId { get; private set; } = 0;
        /// <summary>
        /// Сила урона для атаки
        /// </summary>
        private float _damage = 1;

        #region Методы для импорта данных с json

        /// <summary>
        /// Прочесть состояние блока из Json формы
        /// </summary>
        protected override void _ReadStateFromJson(JsonCompound state)
        {
            base._ReadStateFromJson(state);
            if (state.Items != null)
            {
                try
                {
                    // Статы
                    foreach (JsonKeyValue json in state.Items)
                    {
                        if (json.IsKey(Cti.Damage)) _damage = json.GetFloat();
                        else if (json.IsKey(Cti.Level)) Level = json.GetInt();
                        else if (json.IsKey(Cti.Pause)) Pause = json.GetInt();
                        else if(json.IsKey(Cti.Acceleration)) Acceleration = json.GetFloat();
                        else if (json.IsKey("DigBonus")) DigBonus = json.GetFloat();
                        else if (json.IsKey("Crit")) Crit = json.GetFloat();
                        else if (json.IsKey("AttackPause")) AttackPause = json.GetInt();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(Sr.GetString(Sr.ErrorReadJsonItemStat, Alias), ex);
                }
            }
        }

        #endregion

        /// <summary>
        /// Задать подсказку
        /// </summary>
        public override void SetToolTipLang(string toolTip)
        {
            base.SetToolTipLang(toolTip);
            _toolTip = _toolTip + ChatStyle.Br + "-" + ChatStyle.Br
                + "Level {0}" + ChatStyle.Br
                + "Damage {1}" + ChatStyle.Br
                + "{2} [{3} {4}]";
        }

        /// <summary>
        /// Текст в подсказке для GUI
        /// </summary>
        public override string GetToolTip(ItemStack stack)
            => string.Format(_toolTip, 
                Level, _damage,
                stack.ToStringDamaged(),
                Pause, Acceleration);

        /// <summary>
        /// Получить урон для атаки предметом который в руке
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float GetDamageToAttack() => _damage;

        public override float CritMultiplier => Crit;

        /// <summary>
        /// Удар по сущности с паузой инструмента (AttackPause), остальное как у предмета
        /// </summary>
        public override ResultHandAction OnAction(bool begin, ItemStack stack, PlayerClientOwner player)
        {
            if (AttackPause > 0 && player.MovingObject.IsEntity())
            {
                return new ResultHandAction(ResultHandAction.ActionType.AttackEntity, AttackPause);
            }
            return base.OnAction(begin, stack, player);
        }
    }
}
