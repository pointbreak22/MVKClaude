namespace Mvk.Engine
{
    /// <summary>
    /// Палитра 16 цветов (перенесена из Gi, нужна частицам в логике клиента)
    /// </summary>
    public static class ColorPalette
    {
        /// <summary>
        /// Перечень 16-ти цветов Красного как в майне
        /// </summary>
        public readonly static float[] ColorRed = new float[] { 0, 0, 0, 0, .67f, .67f, 1, .67f, .33f, .33f, .33f, .33f, 1, 1, 1, 1 };
        /// <summary>
        /// Перечень 16-ти цветов Зелёного как в майне
        /// </summary>
        public readonly static float[] ColorGreen = new float[] { 0, 0, .67f, .67f, 0, 0, .67f, .67f, .33f, .33f, 1, 1, .33f, .33f, 1, 1 };
        /// <summary>
        /// Перечень 16-ти цветов Синего как в майне
        /// </summary>
        public readonly static float[] ColorBlue = new float[] { 0, .67f, 0, .67f, 0, .67f, 0, .67f, .33f, 1, .33f, 1, .33f, 1, .33f, 1 };
    }
}
