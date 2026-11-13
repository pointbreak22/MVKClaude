using System;
using System.Collections.Generic;

namespace Mvk.Rendering
{
    /// <summary>
    /// Один атрибут вершины: номер входа шейдера, формат и смещение в байтах внутри вершины
    /// </summary>
    public readonly record struct VertexAttribute(int Location, VertexFormat Format, int Offset);

    /// <summary>
    /// Раскладка одного вершинного буфера (одного слота).
    /// Меш блоков оригинала использует 2 слота: float (pos, uv) и int (rgbl, anim, normal).
    /// </summary>
    public sealed class VertexBufferLayout
    {
        /// <summary>Размер вершины в байтах</summary>
        public int Stride { get; }
        /// <summary>Атрибуты</summary>
        public IReadOnlyList<VertexAttribute> Attributes { get; }

        public VertexBufferLayout(int stride, params VertexAttribute[] attributes)
        {
            if (stride <= 0) throw new ArgumentOutOfRangeException(nameof(stride));
            Stride = stride;
            Attributes = attributes;
        }
    }

    /// <summary>
    /// Состояние выборки из текстуры
    /// </summary>
    public readonly record struct SamplerState(
        TextureFilter MinFilter, TextureFilter MagFilter, MipFilter MipFilter, TextureWrap Wrap, bool BorderWhite = false)
    {
        /// <summary>Пиксельная графика без мипмапов, с повтором</summary>
        public static readonly SamplerState PointRepeat
            = new(TextureFilter.Nearest, TextureFilter.Nearest, MipFilter.None, TextureWrap.Repeat);
        /// <summary>Пиксельная графика без мипмапов, без повтора (GUI, шрифты)</summary>
        public static readonly SamplerState PointClamp
            = new(TextureFilter.Nearest, TextureFilter.Nearest, MipFilter.None, TextureWrap.ClampToEdge);
        /// <summary>Как атлас блоков оригинала: NEAREST_MIPMAP_NEAREST и REPEAT</summary>
        public static readonly SamplerState PointMipRepeat
            = new(TextureFilter.Nearest, TextureFilter.Nearest, MipFilter.Nearest, TextureWrap.Repeat);
    }

    /// <summary>
    /// Описание текстуры. Массив 2D-текстур, если IsArray или Layers больше 1
    /// (массив из одного слоя нужен шейдеру с sampler2DArray).
    /// </summary>
    public readonly record struct TextureDescription(
        int Width, int Height, int Layers, int MipLevels, SamplerState Sampler, bool IsArray = false,
        TextureFormat Format = TextureFormat.Rgba8)
    {
        /// <summary>
        /// Текстура глубины для карты теней: шейдер читает .r как глубину 0..1
        /// </summary>
        public static TextureDescription Depth(int width, int height, SamplerState sampler)
            => new(width, height, 1, 1, sampler, false, TextureFormat.Depth32F);

        /// <summary>2D-текстура без мипмапов</summary>
        public static TextureDescription Texture2D(int width, int height, SamplerState sampler)
            => new(width, height, 1, 1, sampler);

        /// <summary>Массив 2D-текстур без мипмапов</summary>
        public static TextureDescription Texture2DArray(int width, int height, int layers, SamplerState sampler)
            => new(width, height, layers, 1, sampler, true);
    }

    /// <summary>
    /// Исходники шейдера и привязки, которые в оригинале задавались по имени
    /// </summary>
    public sealed class ShaderDescription
    {
        /// <summary>Вершинный шейдер (GLSL 330 на этапе 4)</summary>
        public string VertexSource { get; }
        /// <summary>Фрагментный шейдер</summary>
        public string FragmentSource { get; }
        /// <summary>Имена входов вершинного шейдера; индекс в массиве = location</summary>
        public IReadOnlyList<string> Attributes { get; }
        /// <summary>Имена sampler-униформ; индекс в массиве = номер текстурного слота</summary>
        public IReadOnlyList<string> Samplers { get; }
        /// <summary>Имя для логов и ошибок</summary>
        public string Name { get; }

        public ShaderDescription(string name, string vertexSource, string fragmentSource,
            IReadOnlyList<string> attributes, IReadOnlyList<string>? samplers = null)
        {
            Name = name;
            VertexSource = vertexSource;
            FragmentSource = fragmentSource;
            Attributes = attributes;
            Samplers = samplers ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Смешивание цветов. Enabled = false значит запись без смешивания.
    /// </summary>
    public readonly record struct BlendState(bool Enabled,
        BlendFactor SrcColor, BlendFactor DstColor, BlendFactor SrcAlpha, BlendFactor DstAlpha)
    {
        /// <summary>Без смешивания</summary>
        public static readonly BlendState Opaque
            = new(false, BlendFactor.One, BlendFactor.Zero, BlendFactor.One, BlendFactor.Zero);
        /// <summary>Обычная прозрачность, как WindowMain оригинала: SRC_ALPHA, ONE_MINUS_SRC_ALPHA</summary>
        public static readonly BlendState Alpha
            = new(true, BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha, BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
        /// <summary>Свечение (солнце, звёзды в SkyIslandRender): SRC_ALPHA, ONE / ONE, ZERO</summary>
        public static readonly BlendState Additive
            = new(true, BlendFactor.SrcAlpha, BlendFactor.One, BlendFactor.One, BlendFactor.Zero);
        /// <summary>Прозрачность неба (SkyIslandRender): SRC_ALPHA, ONE_MINUS_SRC_ALPHA / ONE, ZERO</summary>
        public static readonly BlendState AlphaKeepDst
            = new(true, BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha, BlendFactor.One, BlendFactor.Zero);
    }

    /// <summary>
    /// Растеризация и глубина
    /// </summary>
    public readonly record struct RasterState(
        CullMode Cull, FillMode Fill, bool DepthTest, bool DepthWrite, CompareFunc DepthFunc,
        float DepthBiasFactor, float DepthBiasUnits)
    {
        /// <summary>Как настройка WindowMain оригинала: тест глубины LEQUAL, запись глубины, без отсечения</summary>
        public static readonly RasterState Default
            = new(CullMode.None, FillMode.Solid, true, true, CompareFunc.LessEqual, 0, 0);
        /// <summary>2D поверх всего (GUI)</summary>
        public static readonly RasterState NoDepth
            = new(CullMode.None, FillMode.Solid, false, false, CompareFunc.Always, 0, 0);

        /// <summary>Есть ли сдвиг глубины (glPolygonOffset)</summary>
        public bool HasDepthBias => DepthBiasFactor != 0 || DepthBiasUnits != 0;
    }

    /// <summary>
    /// Описание неизменяемого конвейера (как VkPipeline): шейдер, раскладка вершин и всё фиксированное состояние
    /// </summary>
    public sealed class PipelineDescription
    {
        public IShader Shader { get; }
        /// <summary>Раскладки по слотам вершинных буферов</summary>
        public IReadOnlyList<VertexBufferLayout> VertexLayouts { get; }
        public PrimitiveTopology Topology { get; init; } = PrimitiveTopology.Triangles;
        public RasterState Raster { get; init; } = RasterState.Default;
        public BlendState Blend { get; init; } = BlendState.Opaque;
        /// <summary>Писать ли цвет (false для прохода только глубины, например тени)</summary>
        public bool ColorWrite { get; init; } = true;

        public PipelineDescription(IShader shader, params VertexBufferLayout[] vertexLayouts)
        {
            Shader = shader;
            VertexLayouts = vertexLayouts;
        }
    }
}
