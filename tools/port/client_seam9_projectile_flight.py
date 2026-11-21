"""[ПОВЕДЕНИЕ] Шов клиента: летящий снаряд с FlightAlign в json (стрела, нож, копьё) рисуется моделью в руке,
повёрнутой по вектору скорости (+Y модели — по полёту): матрица кости 0 и anim = 1 в шейдере сущностей."""
import os
from portlib import load, save, rep, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

rel = r"Renderer\World\Entity\EntitiesRenderer.cs"
t = load(rel, C)
t = rep(t, """        public EntityRender GetItemRender(int indexEntity) => _itemRender[indexEntity];""",
        """        public EntityRender GetItemRender(int indexEntity) => _itemRender[indexEntity];

        /// <summary>
        /// [ПОВЕДЕНИЕ] Сетки предметов в полёте (модель в руке), создаются по первому запросу
        /// </summary>
        private EntityRender?[] _itemFlightRender = new EntityRender?[0];

        /// <summary>
        /// [ПОВЕДЕНИЕ] Сетка предмета в полёте: модель в руке (+Y по длине предмета)
        /// </summary>
        public EntityRender GetItemFlightRender(int indexItem)
        {
            if (_itemFlightRender.Length != Ce.Items.Count) _itemFlightRender = new EntityRender?[Ce.Items.Count];
            return _itemFlightRender[indexItem] ??= new EntityRender(this, Ce.Items.ItemObjects[indexItem].Buffer.GetBufferHold());
        }""", rel, 1)
t = rep(t, """            for (int i = 0; i < _itemRender.Length; i++) _itemRender[i].Dispose();""",
        """            for (int i = 0; i < _itemRender.Length; i++) _itemRender[i].Dispose();
            for (int i = 0; i < _itemFlightRender.Length; i++) _itemFlightRender[i]?.Dispose();""", rel, 1)
save(rel, t, C)

rel = r"Entity\Render\EntityRenderItem.cs"
t = load(rel, C)
t = rep(t, """            if (_entityRender != null) // Может быть случай, когда пакет данных стака ещё не пришёл
            {""", """            // [ПОВЕДЕНИЕ] Летящий снаряд с FlightAlign — модель по вектору скорости
            if (_DrawFlight(timeIndex)) return;
            if (_entityRender != null) // Может быть случай, когда пакет данных стака ещё не пришёл
            {""", rel, 1)
t = rep(t, """        public EntityRenderItem(EntityBase entity, EntitiesRenderer entities)
            : base(entity) => _entities = entities;""", """        public EntityRenderItem(EntityBase entity, EntitiesRenderer entities)
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
        }""", rel, 1)
save(rel, t, C)
print("client projectile flight ok")
