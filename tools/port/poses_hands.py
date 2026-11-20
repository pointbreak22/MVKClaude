"""[ПОВЕДЕНИЕ] Этап 8: разгон и позы рук (уточнения пользователя п. 7 и 8, docs/controls-survival-spec.md).

- Разгон (PlayerServer.IsMomentum): бег, полёт или прыжок, либо скорость по пакетам позиции быстрее шага
  (спуск, падение, отброс). От разгона зависят крит удара и удар щитом.
- Флаг позы руки (EntityLiving флаг 7, рассылается всем клиентам): игрок держит блок щитом или натягивает лук.
  По нему рендер ставит позу щита или лука (client_seam9_view_hands.py)."""
from portlib import load, save, rep

rel = r"Entity\EntityLiving.cs"
t = load(rel)
t = rep(t, """        public bool IsSprinting() => GetFlag(3);""", """        public bool IsSprinting() => GetFlag(3);

        /// <summary>
        /// [ПОВЕДЕНИЕ] Поза руки: блок щитом или натяжение лука (флаг 7, задаёт сервер)
        /// </summary>
        public bool IsHandPose() => GetFlag(7);

        /// <summary>
        /// [ПОВЕДЕНИЕ] Задать позу руки (сервер)
        /// </summary>
        public void SetHandPose(bool pose) => SetFlag(7, pose);""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """        public void PacketBlocking(PacketC0BBlocking packet) => IsBlocking = packet.Blocking;""", """        public void PacketBlocking(PacketC0BBlocking packet)
        {
            IsBlocking = packet.Blocking;
            SetHandPose(IsBlocking || IsDrawing);
        }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Игрок натягивает лук (задаёт лук на сервере)
        /// </summary>
        public bool IsDrawing { get; private set; }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Начало или конец натяжения лука: поза руки
        /// </summary>
        public void SetDrawing(bool drawing)
        {
            IsDrawing = drawing;
            SetHandPose(IsBlocking || IsDrawing);
        }

        /// <summary>
        /// Скорость последнего перемещения (по горизонтали или падения) и такт сервера, когда она была
        /// </summary>
        private float _speed;
        private uint _speedTick;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Разгон: бег, в воздухе или скорость быстрее шага в последние 3 такта
        /// </summary>
        public bool IsMomentum() => IsSprinting() || !OnGround
            || (_server.TickCounterGlobal - _speedTick <= 3 && _speed > .3f);""", rel, 1)
t = rep(t, """            OnGround = packet.OnGround;
            if (packet.IsPosition)
            {""", """            OnGround = packet.OnGround;
            if (packet.IsPosition)
            {
                float dx = packet.X - PosX, dy = packet.Y - PosY, dz = packet.Z - PosZ;
                _speed = Math.Max(MathF.Sqrt(dx * dx + dz * dz), -dy);
                _speedTick = _server.TickCounterGlobal;""", rel, 1)
t = rep(t, "if (Inventory.GetCurrentItem()?.Item.Forearm == true && !IsSprinting()) return;",
        "if (Inventory.GetCurrentItem()?.Item.Forearm == true && !IsMomentum()) return;", rel, 1)
t = rep(t, "else if (!OnGround || IsSprinting()) damage *=", "else if (IsMomentum()) damage *=", rel, 1)
save(rel, t)
print("poses_hands ok")
