using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Mvk.Platform
{
    /// <summary>
    /// Создание игрового окна через Silk.NET (замена WinGL.Window)
    /// </summary>
    public static class WindowFactory
    {
        /// <summary>
        /// Создать окно с контекстом OpenGL 3.3 core
        /// </summary>
        public static IWindow CreateOpenGL(string title, int width, int height)
        {
            WindowOptions options = WindowOptions.Default;
            options.Title = title;
            options.Size = new Vector2D<int>(width, height);
            options.VSync = true;
            // Как в оригинале: WinGL получает от Windows 32-битный буфер глубины (GLFW по умолчанию даёт 24)
            options.PreferredDepthBufferBits = 32;
            options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core,
                ContextFlags.ForwardCompatible, new APIVersion(3, 3));
            return Window.Create(options);
        }
    }
}
