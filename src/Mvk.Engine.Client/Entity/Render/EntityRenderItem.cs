using System.Runtime.CompilerServices;
using Mvk.Engine.Entity.List;
using Mvk.Engine.Item;
using Mvk.Engine.Renderer.World.Entity;

namespace Mvk.Engine.Entity.Render
{
    /// <summary>
    /// Объект рендера сущности, хранящий данные рендера, без анимации. Только для клиента
    /// </summary>
    public class EntityRenderItem : EntityRenderAbstract
    {
        /// <summary>
        /// Объект рендера всех сущностей
        /// </summary>
        protected readonly EntitiesRenderer _entities;

        public EntityRenderItem(EntityBase entity, EntitiesRenderer entities)
            : base(entity) => _entities = entities;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Матрица кости 0 (4x3) для поворота снаряда по полёту
        /// </summary>
        private readonly float[] _flightTransform = new float[Ce.MaxAnimatedBones * 12];
        /// <summary>
        /// Последнее направление полёта (снаряд, воткнувшийся в блок, сохраняет его)
        /// </summary>
        private float _dirX, _dirY = 1, _dirZ;
        private bool _hasDir;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Снаряд с FlightAlign: направление — смещение за такт (пока летит), модель в руке, +Y модели
        /// по направлению; лежащий на земле после полёта (скорость около нуля) — как воткнувшийся, по последнему
        /// направлению. Обычные предметы рисуются как раньше
        /// </summary>
        private bool _DrawFlight(float timeIndex)
        {
            if (Entity is not EntityProjectile projectile) return false;
            ItemStack itemStack = projectile.GetEntityItemStack();
            if (itemStack == null || !itemStack.Item.FlightAlign) return false;
            float dx = Entity.PosX - Entity.PosPrevX, dy = Entity.PosY - Entity.PosPrevY, dz = Entity.PosZ - Entity.PosPrevZ;
            float len = Mvk.Core.Maths.Mth.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len > .05f)
            {
                _dirX = dx / len; _dirY = dy / len; _dirZ = dz / len;
                _hasDir = true;
            }
            if (!_hasDir) return false;
            // Базис: Y — по полёту, X — горизонталь поперёк, Z = X × Y
            float xx = _dirZ, xz = -_dirX;
            float xl = Mvk.Core.Maths.Mth.Sqrt(xx * xx + xz * xz);
            if (xl < .001f) { xx = 1; xz = 0; xl = 1; }
            xx /= xl; xz /= xl;
            float zx = -xz * _dirY, zy = xz * _dirX - xx * _dirZ, zz = xx * _dirY;
            float[] m = _flightTransform;
            m[0] = xx; m[1] = 0; m[2] = xz;
            m[3] = _dirX; m[4] = _dirY; m[5] = _dirZ;
            m[6] = zx; m[7] = zy; m[8] = zz;
            m[9] = 0; m[10] = 0; m[11] = 0;
            _entities.ShsEntity.UniformData(
                Entity.GetPosFrameX(timeIndex) - _entities.Player.PosFrameX,
                Entity.GetPosFrameY(timeIndex) - _entities.Player.PosFrameY,
                Entity.GetPosFrameZ(timeIndex) - _entities.Player.PosFrameZ,
                _lightBlock, _lightSky, 0, 1, 0);
            _entities.ShsEntity.UniformData(_flightTransform);
            _entities.GetItemFlightRender(itemStack.Item.IndexItem).MeshDraw();
            _entities.ShsEntity.BindUniformItems();
            return true;
        }

        /// <summary>
        /// Обновить рассчитать матрицы для кадра
        /// </summary>
        /// <param name="timeIndex">коэффициент времени от прошлого TPS клиента в диапазоне 0 .. 1</param>
        /// <param name="deltaTime">Дельта последнего кадра в mc</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void UpdateMatrix(float timeIndex, float deltaTime)
        {
            if (_entityRender == null && Entity is EntityItem entityItem)
            {
                ItemStack itemStack = entityItem.GetEntityItemStack();
                if (itemStack != null)
                {
                    _entityRender = _entities.GetItemRender(itemStack.Item.IndexItem);
                }
            }
        }

        /// <summary>
        /// Метод для прорисовки
        /// </summary>
        /// <param name="timeIndex">коэффициент времени от прошлого TPS клиента в диапазоне 0 .. 1</param>
        /// <param name="deltaTime">Дельта последнего кадра в mc</param>
        public override void Draw(float timeIndex, float deltaTime)
        {
            // [ПОВЕДЕНИЕ] Летящий снаряд с FlightAlign — модель по вектору скорости
            if (_DrawFlight(timeIndex)) return;
            if (_entityRender != null) // Может быть случай, когда пакет данных стака ещё не пришёл
            {
                // Заносим в шейдор
                _entities.ShsEntity.UniformData(
                    Entity.GetPosFrameX(timeIndex) - _entities.Player.PosFrameX,
                    Entity.GetPosFrameY(timeIndex) - _entities.Player.PosFrameY,
                    Entity.GetPosFrameZ(timeIndex) - _entities.Player.PosFrameZ,
                    _lightBlock, _lightSky);

                // Рисуем основную сетку сущности
                _entityRender.MeshDraw();
            }
        }
    }
}
