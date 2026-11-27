"""Отладка (без изменения поведения игры): поворот камеры третьего лица вокруг игрока (--orbit в Mvk.Desktop),
чтобы снимать модель и предметы в руках сбоку; --orbit-dist — расстояние камеры (крупные снимки). По умолчанию 0 — как раньше."""
from portlib import load, save, rep

rel = r"Entity\Player\PlayerClientOwner.cs"
t = load(rel)
t = rep(t, """            if (ViewCamera == EnumViewCamera.Back)
            {
                // вид сзади
                pos = _GetPositionCamera(pos, front * -1f, 8); // 8""", """            // Отладочный облёт камеры третьего лица вокруг игрока (--orbit)
            if (DebugOrbitYaw != 0 && ViewCamera != EnumViewCamera.Eye)
            {
                front = Glm.Ray(RotationFrameYaw + DebugOrbitYaw, RotationFramePitch);
            }
            if (ViewCamera == EnumViewCamera.Back)
            {
                // вид сзади
                pos = _GetPositionCamera(pos, front * -1f, 8); // 8""", rel, 1)
t = rep(t, """        private void _UpdateMatrixCamera()
        {""", """        /// <summary>
        /// Отладка: поворот камеры третьего лица вокруг игрока, радианы (0 — как в игре)
        /// </summary>
        public static float DebugOrbitYaw;

        private void _UpdateMatrixCamera()
        {""", rel, 1)
t = rep(t, """                pos = _GetPositionCamera(pos, front * -1f, 8); // 8""",
        """                pos = _GetPositionCamera(pos, front * -1f, DebugOrbitDistance); // 8""", rel, 1)
t = rep(t, """                pos = _GetPositionCamera(pos, front, 8); // 8""",
        """                pos = _GetPositionCamera(pos, front, DebugOrbitDistance); // 8""", rel, 1)
t = rep(t, """        public static float DebugOrbitYaw;""", """        public static float DebugOrbitYaw;

        /// <summary>
        /// Отладка: расстояние камеры третьего лица до игрока, блоков (8 — как в игре)
        /// </summary>
        public static float DebugOrbitDistance = 8;""", rel, 1)
save(rel, t)
print("debug_orbit ok")
