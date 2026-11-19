using System.Runtime.CompilerServices;
using Mvk.Core.Maths;
using Mvk.Core.Util;
using Mvk.Engine.Util;

namespace Mvk.Engine
{
    /// <summary>
    /// Состояние вида клиента: размер окна, размер интерфейса, матрицы камеры и теней.
    /// В оригинале статические поля Gi; в порте объект окна (WindowMain.View), клиентская игра берёт его
    /// у хоста (GameBase.View). Сервер его не знает, второй клиент в процессе получает свой вид.
    /// </summary>
    public sealed class ViewState
    {
        /// <summary>
        /// Ширина окна
        /// </summary>
        public int Width = 1280;
        /// <summary>
        /// Высота окна
        /// </summary>
        public int Height = 720;

        /// <summary>
        /// Размер интерфейса
        /// </summary>
        public int Si = 1;

        /// <summary>
        /// Ширина текстового окна чата
        /// </summary>
        public int WindowsChatWidthMessage = 472;
        /// <summary>
        /// Высота текстового окна чата
        /// </summary>
        public int WindowsChatHeightMessage = 333;

        #region Для отладки ортоганальная камера мира

        /// <summary>
        /// Демонстрируем ли мы прорисовку ортоганальной камеры
        /// </summary>
        public bool IsDrawOrto { get; private set; }
        /// <summary>
        /// Увеличение ортоганальной камеры
        /// </summary>
        public int ZoomDrawOrto { get; private set; }

        /// <summary>
        /// Клик изменения отладочной ортоганальной камеры
        /// </summary>
        public void DebugOrtoNext()
        {
            if (--ZoomDrawOrto < 0)
            {
                ZoomDrawOrto = 6;
            }
            IsDrawOrto = ZoomDrawOrto > 0;
        }

        #endregion

        /// <summary>
        /// Массив матрицы для проецирования дрехмерных координат на экран
        /// </summary>
        public readonly float[] Ortho = new float[16];
        /// <summary>
        /// Матрица просмотра Projection * LookAt
        /// </summary>
        public readonly float[] MatrixView = new float[16];
        /// <summary>
        /// Матрица просмотра Projection * LookAt для модели с глаз
        /// </summary>
        public readonly float[] MatrixViewEye = new float[16];
        /// <summary>
        /// Матрица просмотра Projection * LookAt для карты теней, от солнца или луны
        /// </summary>
        public readonly float[] MatrixViewDepthMap = new float[16];
        /// <summary>
        /// Вектор источника света для тени
        /// </summary>
        public Vector3 ViewLightDir = new Vector3(0);
        /// <summary>
        /// Яркость теней сущностей
        /// </summary>
        public float EntityBrightness = 0.3f;
        /// <summary>
        /// Яркость теней блоков
        /// </summary>
        public float BlockBrightness = 0.3f;

        /// <summary>
        /// Обновить размер инерфейса
        /// </summary>
        public void UpdateSizeInterface()
        {
            Si = Options.SizeInterface;

            // Автоматический SizeInterface
            if (Si == 2)
            {
                // Крупный стиль
                if (Height < 832) Si = 1; // 960
                else if (Height < 1664) Si = 2; // 1920
                else Si = 4;
            }
            else
            {
                // Мелкий стиль
                if (Height < 1664) Si = 1;
                else Si = 2;
            }
        }

        /// <summary>
        /// Обновить ортогональную проекцию
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpOrtho()
            => Glm.Ortho(0, Width, Height, 0, -100 * Si, 100 * Si).ConvArray(Ortho);
    }
}
