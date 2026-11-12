using System;

namespace Mvk.Rendering
{
    /// <summary>
    /// Графическое устройство. Общий интерфейс для всех бэкендов (OpenGL, Vulkan).
    /// Игровой код работает только через него и не знает, какой API под ним.
    /// </summary>
    /// <remarks>
    /// Модель в стиле Vulkan (docs/adr/0002-rhi.md): ресурсы создаются заранее,
    /// конвейеры неизменяемы, кадр записывается в <see cref="CommandList"/> и отдаётся в <see cref="Submit"/>.
    /// Create*, Submit и ReadPixels вызываются в потоке, которому принадлежит контекст (поток окна).
    /// </remarks>
    public interface IGraphicsDevice : IDisposable
    {
        /// <summary>Какой бэкенд реализует устройство</summary>
        GraphicsBackend Backend { get; }

        /// <summary>Название GPU и версии API, для лога</summary>
        string Description { get; }

        IGpuBuffer CreateBuffer(BufferKind kind, BufferUsage usage);

        ITexture CreateTexture(in TextureDescription description);

        /// <summary>Скомпилировать шейдер. Ошибка компиляции даёт исключение с логом драйвера.</summary>
        IShader CreateShader(ShaderDescription description);

        IPipeline CreatePipeline(PipelineDescription description);

        /// <summary>Выполнить записанные команды</summary>
        void Submit(CommandList commands);

        /// <summary>
        /// Прочитать пиксели текущего кадра (до смены буферов) в RGBA8, строки сверху вниз.
        /// Для снимков экрана и визуальных тестов.
        /// </summary>
        void ReadPixels(int x, int y, int width, int height, Span<byte> rgba);
    }
}
