using Mvk.Platform;
using Mvk.Rendering;
using Mvk.Rendering.OpenGL;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Mvk.Desktop
{
    /// <summary>
    /// Точка входа для ПК.
    /// Этап 0: открывает окно и очищает экран белым, как загрузчик в оригинале (WindowMvk.RenderInitialized).
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            using IWindow window = WindowFactory.CreateOpenGL("Малювекi 2 - MvkClaude", 1280, 720);
            IGraphicsDevice? device = null;

            window.Load += () =>
            {
                device = new OpenGLDevice(GL.GetApi(window));
                device.SetViewport(window.FramebufferSize.X, window.FramebufferSize.Y);
            };
            window.FramebufferResize += size => device?.SetViewport(size.X, size.Y);
            window.Render += _ => device?.Clear(1f, 1f, 1f, 1f);
            window.Closing += () => device?.Dispose();

            window.Run();
        }
    }
}
