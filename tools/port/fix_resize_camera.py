"""[ПОВЕДЕНИЕ] Исправление бага оригинала (docs/known-issues.md п. 8):
после изменения размера окна матрица проекции игрока не пересчитывалась до движения камеры,
кадр оставался сжатым или растянутым. Теперь GameBase.OnResized помечает камеру к пересчёту.
Механику игры не меняет: только матрица вида в следующем кадре."""
from portlib import load, save, rep

rel = r"Entity\Player\PlayerClientOwner.cs"
t = load(rel)
t = rep(t, """        /// <summary>
        /// Следующий вид камеры
        /// </summary>
        public void ViewCameraNext()""", """        /// <summary>
        /// Изменён размер окна: пересчитать матрицы камеры в следующем кадре
        /// </summary>
        public void ViewSizeChanged() => _updateCamera = true;

        /// <summary>
        /// Следующий вид камеры
        /// </summary>
        public void ViewCameraNext()""", rel, 1)
save(rel, t)

rel = r"Games\GameBase.cs"
t = load(rel)
t = rep(t, """            // Тут индикация если имеется
            Host.OnResized(width, height);""", """            // Матрица проекции зависит от размера окна
            Player.ViewSizeChanged();
            // Тут индикация если имеется
            Host.OnResized(width, height);""", rel, 1)
save(rel, t)
print("fix_resize_camera ok")
