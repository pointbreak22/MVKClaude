"""Отладка (без изменения поведения игры): поворот камеры третьего лица вокруг игрока (--orbit в Mvk.Desktop),
чтобы снимать модель и предметы в руках сбоку. По умолчанию 0 — как раньше."""
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
save(rel, t)
print("debug_orbit ok")
