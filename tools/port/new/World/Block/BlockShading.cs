namespace Mvk.Engine.World.Block
{
    /// <summary>
    /// Вид рендера блока. Клиент сопоставляет его со своим рендером
    /// (в оригинале блок хранил ссылку на рендер из Gi).
    /// </summary>
    public enum EnumBlockRender : byte
    {
        /// <summary>Плотный блок (Gi.BlockRendFull)</summary>
        Full = 0,
        /// <summary>Блок с прозрачностью (Gi.BlockAlphaRendFull)</summary>
        AlphaFull = 1,
        /// <summary>Жидкость с прозрачностью (Gi.BlockLiquidAlphaRendFull)</summary>
        LiquidAlpha = 2
    }

    /// <summary>
    /// Затемнение сторон блока. Нужно при регистрации блоков (QuadSide, SideLiquid),
    /// поэтому живёт в движке, а не в клиентском Gi.
    /// </summary>
    public static class BlockShading
    {
        /// <summary>
        /// Затемнение стороны куба, низ
        /// </summary>
        public static float DarkeningSideDown = .6f;
        /// <summary>
        /// Затемнение стороны куба, сторона А
        /// </summary>
        public static float DarkeningSideA = .7f;
        /// <summary>
        /// Затемнение стороны куба, сторона B
        /// </summary>
        public static float DarkeningSideB = .85f;
        /// <summary>
        /// Затемнение стороны от стороны блока (с Diffuse)
        /// </summary>
        public readonly static float[] LightPoles = new float[] {
            1, DarkeningSideDown,
            DarkeningSideA, DarkeningSideA,
            DarkeningSideB, DarkeningSideB };
    }
}
