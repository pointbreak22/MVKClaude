using System;
using System.Collections.Generic;
using Mvk.Platform;
using Mvk.Rendering;
using Mvk.Rendering.OpenGL;
using Mvk.Rendering.Vulkan;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Mvk.Desktop
{
    /// <summary>
    /// Выбор бэкенда графики хостом (ключ --backend opengl|vulkan, по умолчанию OpenGL): окно, устройство RHI
    /// и кадр окна. Остальной код знает только IGraphicsDevice.
    /// </summary>
    internal static class GraphicsHost
    {
        /// <summary>Бэкенд Vulkan (этап 10)</summary>
        public static bool Vulkan;

        /// <summary>Окно под выбранный бэкенд</summary>
        public static IWindow CreateWindow(string title, int width, int height)
            => Vulkan ? WindowFactory.CreateVulkan(title, width, height) : WindowFactory.CreateOpenGL(title, width, height);

        /// <summary>Устройство RHI для окна (после события Load)</summary>
        public static unsafe IGraphicsDevice CreateDevice(IWindow window)
        {
            if (!Vulkan)
            {
                GL gl = GL.GetApi(window);
                var device = new OpenGLDevice(gl);
                gl.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, (FramebufferAttachment)0x1801,
                    FramebufferAttachmentParameterName.DepthSize, out int depthBits);
                Console.WriteLine(device.Description);
                Console.WriteLine("Буфер глубины: " + depthBits + " бит");
                return device;
            }
            var surface = window.VkSurface ?? throw new NotSupportedException("Окно без поверхности Vulkan");
            byte** names = surface.GetRequiredExtensions(out uint count);
            var extensions = new List<string>();
            for (int i = 0; i < count; i++) extensions.Add(SilkMarshal.PtrToString((nint)names[i])!);
            var vk = new VulkanDevice(extensions,
                instance => surface.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface(),
                window.FramebufferSize.X, window.FramebufferSize.Y, window.VSync);
            Console.WriteLine(vk.Description);
            return vk;
        }

        /// <summary>Начало кадра окна (Vulkan: взять изображение цепочки показа)</summary>
        public static void BeginFrame(IGraphicsDevice? device)
        {
            if (device is VulkanDevice vk) vk.BeginFrame();
        }

        /// <summary>Конец кадра окна (Vulkan: показать; GL показывает само окно)</summary>
        public static void EndFrame(IGraphicsDevice? device)
        {
            if (device is VulkanDevice vk) vk.EndFrame();
        }

        /// <summary>Изменился размер окна</summary>
        public static void Resize(IGraphicsDevice? device, int width, int height)
        {
            if (device is VulkanDevice vk) vk.Resize(width, height);
        }

        /// <summary>Вертикальная синхронизация</summary>
        public static void SetVSync(IWindow window, IGraphicsDevice? device, bool on)
        {
            if (device is VulkanDevice vk) vk.SetVSync(on);
            else window.VSync = on;
        }
    }
}
