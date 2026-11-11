using Silk.NET.OpenGL;

namespace Mvk.Rendering.OpenGL
{
    /// <summary>
    /// Реализация графического устройства на OpenGL 3.3 core / OpenGL ES 3.0
    /// </summary>
    public sealed class OpenGLDevice : IGraphicsDevice
    {
        /// <summary>
        /// Объект вызовов OpenGL
        /// </summary>
        private readonly GL _gl;

        public OpenGLDevice(GL gl)
        {
            _gl = gl;
            _gl.Enable(EnableCap.DepthTest);
        }

        public GraphicsBackend Backend => GraphicsBackend.OpenGL;

        public void SetViewport(int width, int height)
            => _gl.Viewport(0, 0, (uint)width, (uint)height);

        public void Clear(float r, float g, float b, float a)
        {
            _gl.ClearColor(r, g, b, a);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public void Dispose() => _gl.Dispose();
    }
}
