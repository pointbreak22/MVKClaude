namespace Mvk.Rendering
{
    /// <summary>
    /// Графический API, на котором работает устройство
    /// </summary>
    public enum GraphicsBackend
    {
        /// <summary>
        /// OpenGL 3.3 core или OpenGL ES 3.0
        /// </summary>
        OpenGL = 0,
        /// <summary>
        /// Vulkan 1.x (на Apple через MoltenVK)
        /// </summary>
        Vulkan = 1
    }
}
