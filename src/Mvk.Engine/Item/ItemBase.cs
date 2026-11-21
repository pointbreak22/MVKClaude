using Mvk.Engine.Entity.List;
#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mvk.Engine.Audio;
using Mvk.Engine.Entity.Player;
using Mvk.Core.Json;
using Mvk.Engine.Json;
using Mvk.Core.Util;
using Mvk.Engine.Util;
using Mvk.Engine.World;
using Mvk.Engine.World.Block;
using Mvk.Core.Maths;

namespace Mvk.Engine.Item
{
    /// <summary>
    /// Базовый объект Предмета
    /// </summary>
    public class ItemBase
    {
        /// <summary>
        /// Индекс предмета из таблицы
        /// </summary>
        public ushort IndexItem { get; private set; }
        /// <summary>
        /// Псевдоним предмета из таблицы
        /// </summary>
        public string Alias { get; private set; } = null!;

        /// <summary>
        /// Название предмета на текущем языке
        /// </summary>
        public string CurentName { get; protected set; } = "";
        /// <summary>
        /// Текст в подсказке для GUI
        /// </summary>
        protected string _toolTip = "";
        /// <summary>
        /// Описание предмета на текущем языке
        /// </summary>
        public string Description { get; protected set; } = "";

        /// <summary>
        /// Максимальное количество однотипный вещей в одной ячейке
        /// </summary>
        public byte MaxStackSize { get; private set; }
        /// <summary>
        /// Максимальное количество урона, при 0, нет учёта урона
        /// </summary>
        public int MaxDamage { get; private set; }

        /// <summary>
        /// Пол ширина предмета
        /// </summary>
        public float Width { get; protected set; } = .4375f;
        /// <summary>
        /// Высота предмета
        /// </summary>
        public float Height { get; protected set; } = .125f;
        /// <summary>
        /// Вес предмета. В килограммах.
        /// </summary>
        public int Weight { get; protected set; } = 2;
        /// <summary>
        /// Коэффициент рикошета, 0 нет отскока, 1 максимальный
        /// </summary>
        public float Rebound { get; protected set; } = .5f;

        /// <summary>
        /// Объект буферов для рендера
        /// </summary>
        public ItemRenderBuffer Buffer { get; protected set; } = null!;

        /// <summary>
        /// Название анимации держать, если не указана, то будет по умолчанию 
        /// </summary>
        public string Hold { get; protected set; } = "";
        /// <summary>
        /// Название анимации действия предмета, если не указана, то будет по умолчанию 
        /// </summary>
        public string Action { get; protected set; } = "";
        /// <summary>
        /// Предмет держим двумя руками
        /// </summary>
        public bool Two { get; protected set; } = false;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Носится на предплечье (щит): кисть руки свободна, двуручный предмет в другой руке её не занимает
        /// </summary>
        public bool Forearm { get; protected set; } = false;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Тяжёлый двуручный (json Heavy): два в руках держать нельзя, при действии им вторая рука занята,
        /// ход медленнее
        /// </summary>
        public bool Heavy { get; protected set; } = false;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Множитель отталкивания при ударе предметом (щит бьёт слабо, но отталкивает сильно)
        /// </summary>
        public float Knockback { get; protected set; } = 1;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Сила броска без замаха (json ThrowMin)
        /// </summary>
        public float ThrowMin { get; protected set; } = 0;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Сила броска с полным замахом (json ThrowMax); 0 — предмет не бросается
        /// </summary>
        public float ThrowMax { get; protected set; } = 0;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Тактов до полного замаха (json ThrowCharge)
        /// </summary>
        public int ThrowCharge { get; protected set; } = 20;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Псевдоним снаряда (json ThrowEntity), пусто — простой снаряд-предмет
        /// </summary>
        public string ThrowEntity { get; protected set; } = "";
        /// <summary>
        /// [ПОВЕДЕНИЕ] Предмет можно бросить с замахом
        /// </summary>
        public bool Throwable => ThrowMax > 0;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Свет предмета в руке (json HandLight, 0 — не светит)
        /// </summary>
        public int HandLight { get; protected set; } = 0;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Колющий выпад: урон × (1 + скорость игрока × ThrustSpeed) (json ThrustSpeed, 0 — нет выпада)
        /// </summary>
        public float ThrustSpeed { get; protected set; } = 0;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Поджечь предмет (сервер) предметом другой руки lighter; true — подожжён
        /// </summary>
        public virtual bool OnIgnite(ItemStack stack, PlayerServer player, ItemStack lighter, bool left) => false;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Фитиль догорел, предмет ещё в руке (сервер; рука действия — та, что держала)
        /// </summary>
        public virtual void OnFuseEnd(ItemStack stack, PlayerServer player) { }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Бросок одного предмета силой power (сервер); true — брошен (расход делает вызывающий)
        /// </summary>
        public virtual bool OnThrow(ItemStack stack, PlayerServer player, float power)
            => EntityProjectile.Throw(player, stack.Copy(1), power,
                ThrowEntity != "" ? ThrowEntity : EntityProjectile.Alias) != null;
        /// <summary>
        /// Группа предметов, образно все виды топоров, если 0, нет группы
        /// </summary>
        //public int Group { get; protected set; } = 0;

        /// <summary>
        /// Индекс звукового эффека поломки предмета
        /// </summary>
        public int SampleBreak { get; protected set; } = -1;

        /// <summary>
        /// Массив ключей ячеек одежды инвентаря, куда можно устанавливать этот предмет.
        /// Если null, то можно установить только в ячейку с ключём 0
        /// </summary>
        protected byte[] _slotClothIndex = null!;

        #region Init

        /// <summary>
        /// Инициализация предметов данные с json, 
        /// </summary>
        public virtual void Init(JsonCompound state)
        {
            _ReadStateFromJson(state);

            if (SampleBreak == -1)
            {
                // Если звуковой эффект поломки не задан, мы берём по умолчанию
                SampleBreak = AudioIndexs.GetKey("Break");
            }
            // Создание буфера для рендера
            Buffer = new ItemRenderBuffer();
        }
        /// <summary>
        /// Удалить объект данных буфера, он уже должен быть в буфере GPU
        /// </summary>
        public void BufferClear()
        {
            Buffer.Clear();
            //Buffer = null;
        }

        /// <summary>
        /// Задать индекс, из таблицы
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetIndex(ushort id) => IndexItem = id;

        /// <summary>
        /// Задать псевдоним
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAlias(string alias) => Alias = alias;

        /// <summary>
        /// Задать название и описание
        /// </summary>
        public void SetNameLang(string name, string des)
        {
            CurentName = name;
            Description = des;
        }

        /// <summary>
        /// Задать подсказку
        /// </summary>
        public virtual void SetToolTipLang(string toolTip)
        {
            _toolTip = (toolTip.Length > 3 && toolTip.Substring(toolTip.Length - 4) == ".tip")
                ? CurentName : toolTip;
        }

        #endregion

        /// <summary>
        /// Проверить может ли предмет быть в текущем слоте одежды с данным ключём
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool CheckSlotClothKey(byte key)
        {
            if (_slotClothIndex == null) return key == 0;
            foreach(byte slot in _slotClothIndex)
            {
                if (slot == key) return true;
            }
            return false;
        }

        /// <summary>
        /// Имеется ли замена предмета при выборе его в StackAir
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool CheckToAir() => false;

        /// <summary>
        /// Заменить предмет из-за воздуха, при выборе его в StackAir
        /// TODO::2025-09-22 это надо горящий факел, чтоб зутухал, для горяжего факела, сделать доп индекс хранения
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual ushort IndexReplaceItemDueAir() => 0;

        /// <summary>
        /// Текст в подсказке для GUI
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual string GetToolTip(ItemStack stack) => _toolTip;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Ускорение добычи блока предметом (для сервера): ударов на разрушение = твёрдость × ускорение;
        /// 0 — предмет этот блок не добывает. Повторяет выбор OnAction клиента: без своего инструмента только блоки,
        /// которые ломаются рукой
        /// </summary>
        public virtual float DigAcceleration(BlockBase block, PlayerBase player)
            => player.CanHitBlock(block) ? 1 : 0;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Сила одного удара по блоку (сервер): рука и чужой материал — 1, инструмент по своему — больше
        /// </summary>
        public virtual float DigStrength(BlockBase block, PlayerBase player) => 1;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Множитель крита удара по сущности в прыжке или на бегу
        /// </summary>
        public virtual float CritMultiplier => 1.5f;

        /// <summary>
        /// Получить урон для атаки предметом который в руке
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual float GetDamageToAttack() => 1;

        #region Дейстыия рук, ЛКМ и ПКМ

        /// <summary>
        /// Имеется ли сетка контура для предмета
        /// </summary>
        public virtual bool CanOutlineBlock() => false;

        /// <summary>
        /// Получить сетку контура предмета
        /// </summary>
        public virtual YouCanPlaceIt GetOutlineBlock(ItemStack stack, PlayerBase player, 
            BlockPos blockPos, Pole side, Vector3 facing) => new YouCanPlaceIt();

        /// <summary>
        /// Проверка, может ли блок устанавливаться в этом месте
        /// </summary>
        /// <param name="blockPos">Координата блока, по которому щелкают правой кнопкой мыши</param>
        /// <param name="block">Объект блока который хотим установить</param>
        /// <param name="side">Сторона, по которой щелкнули правой кнопкой мыши</param>
        /// <param name="facing">Значение в пределах 0..1, образно фиксируем пиксел клика на стороне</param>
        protected virtual bool _CanPlaceBlockOnSide(ItemStack stack, PlayerBase player, WorldBase world, 
            BlockPos blockPos, BlockBase block, Pole side, Vector3 facing, float playerYaw)
        {
            // Если стак пуст
            if (stack == null || stack.Amount == 0) return false;

            BlockState blockState = world.GetBlockState(blockPos);
            BlockBase blockOld = blockState.GetBlock();
            // Проверка ставить блоки, на те которые можно, к примеру на траву
            if (!blockOld.IsReplaceable) return false;
            // Если устанавливаемый блок такой же как стоит
            if (_CheckEqualsBlockPlace(block, blockOld)) return false;

            if (block.IsCollidable)
            {
                // Определяем желаемый Met
                int met = block.OnBlockPlaced(world, blockPos,
                    new BlockState(block.IndexBlock), side, facing, playerYaw).Met;

                AxisAlignedBB axisBlock = block.GetCollisionOne(blockPos, met);
                // Проверка коллизии сущностей и блока
                return !player.Size.IntersectsWith(axisBlock);
                    //world.GetEntitiesWithinAABB(ChunkBase.EnumEntityClassAABB.EntityLiving, axisBlock, player.Id).Count == 0;
            }

            return true;
        }

        /// <summary>
        /// Проверка равенства старого и нового блока, при установке
        /// </summary>
        protected virtual bool _CheckEqualsBlockPlace(BlockBase block, BlockBase blockOld) => blockOld == block;

        /// <summary>
        /// Действие предмета ЛКМ.
        /// Для клиента.
        /// </summary>
        /// <param name="begin">Первый клик, без паузы</param>
        public virtual ResultHandAction OnAction(bool begin, ItemStack stack, PlayerClientOwner player)
        {
            MovingObjectPosition moving = player.MovingObject;
            if (moving.IsEntity())
            {
                // Урон для сущности
                return new ResultHandAction(ResultHandAction.ActionType.AttackEntity);
            }
            if (moving.IsBlock() && (player.CanDestroyedBlock(moving.Block.GetBlock())
                || (player.IsTwoHands() && player.CanHitBlock(moving.Block.GetBlock()))))
            {
                // Можно разрушить блок без предмета
                return new ResultHandAction(8, 1);
            }
            return new ResultHandAction(ResultHandAction.ActionType.None);
        }

        /// <summary>
        /// Действие текущего предмета.
        /// Для сервера
        /// </summary>
        
        public virtual void OnUseAction(ItemStack stack, PlayerServer player) { }

        /// <summary>
        /// Вспомогательное действие предмета ПКМ.
        /// Для клиента.
        /// </summary>
        /// <param name="begin">Первый клик, без паузы</param>
        /// <param name="counter">Счётчик тактов от нажатия вспомогательное действия ПКМ</param>
        public virtual ResultHandSecond OnSecond(bool begin, ItemStack stack, 
            PlayerClientOwner player, int counter)
        {
            return _CheckEntitySecond(player);
        }

        /// <summary>
        /// Провекрка на сущность для Вспомогательное действие предмета ПКМ.
        /// Для клиента.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ResultHandSecond _CheckEntitySecond(PlayerClientOwner player)
        {
            MovingObjectPosition moving = player.MovingObject;
            if (moving.IsEntity())
            {
                return new ResultHandSecond(ResultHandSecond.ActionType.InteractEntity);
            }
            return new ResultHandSecond(ResultHandSecond.ActionType.None);
        }

        /// <summary>
        /// Окончание вспомогательного действия предмета ПКМ, вызывается после отпущения клавиши или смены предмета.
        /// Для клиента.
        /// Возвращает Дополнительный цифровой параметр, если -1 нет действий
        /// </summary>
        /// <param name="abort">Бало ли астановка из-за отмены</param>
        /// <param name="counter">счётчик тактов от нажатия</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual int OnSecondEnd(ItemStack stack, PlayerClientOwner player, 
            bool abort, int counter) => -1;

        /// <summary>
        /// Вспомогательное действие текущего предмета.
        /// Для сервера
        /// </summary>
        /// <param name="number">Дополнительный цифровой параметр</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void OnUseSecond(ItemStack stack, PlayerServer player, int number) { }

        /// <summary>
        /// Вызывается, когда текущий предмет пробуюет установить на блок,
        /// возвращает true если действие состоялось
        /// </summary>
        /// <param name="pos">Позиция блока, по которому щелкают ПКМ</param>
        /// <param name="side">Сторона, по которой щелкнули ПКМ</param>
        /// <param name="facing">Значение в пределах 0..1, образно фиксируем пиксел клика на стороне</param>
        /// <param name="flagReplaceable">Надо ли проверять смещение на установку блока параметра IsReplaceable</param>
        /// <param name="playerYaw">Угол Yaw игрока в момент установки блока</param>
        public virtual bool OnItemOnBlockPlacement(ItemStack stack, PlayerBase player,
            BlockPos blockPos, Pole side, Vector3 facing, bool flagReplaceable, float playerYaw) => false;

        #endregion

        /// <summary>
        /// Нарисовать сетку буффера дополнительный дааных для GUI
        /// </summary>
        public virtual float[] RenderBufferMetGui(byte met, int x, int y, int si) => new float[0];

        #region Методы для импорта данных с json

        /// <summary>
        /// Прочесть состояние блока из Json формы
        /// </summary>
        protected virtual void _ReadStateFromJson(JsonCompound state)
        {
            if (state.Items != null)
            {
                try
                {
                    // Статы
                    foreach (JsonKeyValue json in state.Items)
                    {
                        if (json.IsKey(Cti.MaxStackSize))
                        {
                            int size = json.GetInt();
                            if (size > 255) size = 255;
                            if (size < 1) size = 1;
                            MaxStackSize = (byte)size;
                        }
                        else if (json.IsKey(Cti.MaxDamage)) MaxDamage = json.GetInt();
                        else if (json.IsKey(Cti.SlotClothIndex))
                        {
                            if (json.IsValue()) // byte
                            {
                                _slotClothIndex = new byte[] { (byte)json.GetInt() };
                            }
                            else if (json.IsArray()) // Array
                            {
                                _slotClothIndex = json.GetArray().ToArrayByte();
                            }
                        }
                        else if (json.IsKey(Cti.Width)) Width = json.GetFloat();
                        else if (json.IsKey(Cti.Height)) Height = json.GetFloat();
                        else if (json.IsKey(Cti.Weight)) Weight = json.GetInt();
                        else if (json.IsKey(Cti.Rebound)) Rebound = json.GetFloat();
                        else if (json.IsKey(Cti.Hold)) Hold = json.GetString();
                        else if (json.IsKey(Cti.Action)) Action = json.GetString();
                        else if (json.IsKey(Cti.Two)) Two = json.GetBool();
                        else if (json.IsKey("Forearm")) Forearm = json.GetBool();
                        else if (json.IsKey("Heavy")) Heavy = json.GetBool();
                        else if (json.IsKey("Knockback")) Knockback = json.GetFloat();
                        else if (json.IsKey("ThrowMin")) ThrowMin = json.GetFloat();
                        else if (json.IsKey("HandLight")) HandLight = json.GetInt();
                        else if (json.IsKey("ThrustSpeed")) ThrustSpeed = json.GetFloat();
                        else if (json.IsKey("ThrowMax")) ThrowMax = json.GetFloat();
                        else if (json.IsKey("ThrowCharge")) ThrowCharge = Math.Max(1, json.GetInt());
                        else if (json.IsKey("ThrowEntity")) ThrowEntity = json.GetString();
                    }
                }
                catch
                {
                    throw new Exception(Sr.GetString(Sr.ErrorReadJsonItemStat, Alias));
                }
            }
        }

        #endregion

        public override string ToString() => IndexItem.ToString() + " " + Alias;
    }
}
