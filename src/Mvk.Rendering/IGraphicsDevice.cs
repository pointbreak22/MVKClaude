using System;

namespace Mvk.Rendering
{
    /// <summary>
    /// Графическое устройство. Общий интерфейс для всех бэкендов (OpenGL, Vulkan).
    /// Игровой код работает только через него и не знает, какой API под ним.
    /// </summary>
    /// <remarks>
    /// Заготовка этапа 0. На этапе 4 добавятся буферы, текстуры, пайплайны и списки команд
    /// (модель в стиле Vulkan, см. docs/adr/0002-rhi.md).
    /// </remarks>
    public interface IGraphicsDevice : IDisposable
    {
        /// <summary>
        /// Какой бэкенд реализует устройство
        /// </summary>
        GraphicsBackend Backend { get; }

        /// <summary>
        /// Задать размер области вывода в пикселях
        /// </summary>
        void SetViewport(int width, int height);

        /// <summary>
        /// Очистить экран цветом и буфер глубины
        /// </summary>
        void Clear(float r, float g, float b, float a);
    }
}
