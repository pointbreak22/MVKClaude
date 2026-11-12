namespace Mvk.Rendering
{
    /// <summary>
    /// Назначение буфера на GPU
    /// </summary>
    public enum BufferKind
    {
        /// <summary>Вершины</summary>
        Vertex = 0,
        /// <summary>Индексы</summary>
        Index = 1
    }

    /// <summary>
    /// Как часто меняется содержимое буфера (подсказка драйверу)
    /// </summary>
    public enum BufferUsage
    {
        /// <summary>Загружается редко (меши чанков)</summary>
        Static = 0,
        /// <summary>Меняется почти каждый кадр (GUI, линии, частицы)</summary>
        Dynamic = 1
    }

    /// <summary>
    /// Формат одного атрибута вершины
    /// </summary>
    public enum VertexFormat
    {
        Float1, Float2, Float3, Float4,
        /// <summary>Целые без нормализации (в шейдере int/ivec, glVertexAttribIPointer)</summary>
        Int1, Int2, Int3, Int4,
        /// <summary>4 байта, нормализуются в 0..1 (цвет)</summary>
        UByte4Norm
    }

    /// <summary>
    /// Размер индекса
    /// </summary>
    public enum IndexFormat
    {
        UInt16 = 0,
        UInt32 = 1
    }

    /// <summary>
    /// Тип примитивов
    /// </summary>
    public enum PrimitiveTopology
    {
        Triangles = 0,
        Lines = 1,
        LineStrip = 2,
        Points = 3
    }

    /// <summary>
    /// Отсечение граней
    /// </summary>
    public enum CullMode
    {
        None = 0,
        Back = 1,
        Front = 2
    }

    /// <summary>
    /// Заливка полигонов. Wireframe есть только на desktop GL и Vulkan (fillModeNonSolid)
    /// </summary>
    public enum FillMode
    {
        Solid = 0,
        Wireframe = 1
    }

    /// <summary>
    /// Функция сравнения глубины
    /// </summary>
    public enum CompareFunc
    {
        Never, Less, Equal, LessEqual, Greater, NotEqual, GreaterEqual, Always
    }

    /// <summary>
    /// Множитель смешивания
    /// </summary>
    public enum BlendFactor
    {
        Zero, One, SrcAlpha, OneMinusSrcAlpha, SrcColor, OneMinusSrcColor, DstAlpha, OneMinusDstAlpha
    }

    /// <summary>
    /// Формат пикселей, в котором приходят данные текстуры.
    /// На GPU всегда хранится RGBA8.
    /// </summary>
    public enum PixelFormat
    {
        /// <summary>Порядок байт R,G,B,A</summary>
        Rgba8 = 0,
        /// <summary>Порядок байт B,G,R,A (так хранит атлас оригинала)</summary>
        Bgra8 = 1
    }

    /// <summary>
    /// Фильтр выборки из текстуры
    /// </summary>
    public enum TextureFilter
    {
        Nearest = 0,
        Linear = 1
    }

    /// <summary>
    /// Выборка между уровнями мипмапов
    /// </summary>
    public enum MipFilter
    {
        /// <summary>Мипмапы не используются</summary>
        None = 0,
        Nearest = 1,
        Linear = 2
    }

    /// <summary>
    /// Поведение координат за пределами 0..1
    /// </summary>
    public enum TextureWrap
    {
        Repeat = 0,
        ClampToEdge = 1,
        /// <summary>Нет в GLES 3.0 без расширения, там заменяется на ClampToEdge</summary>
        ClampToBorder = 2
    }
}
