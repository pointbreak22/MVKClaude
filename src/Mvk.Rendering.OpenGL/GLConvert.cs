using System;
using Silk.NET.OpenGL;

namespace Mvk.Rendering.OpenGL
{
    /// <summary>
    /// Перевод перечислений RHI в константы OpenGL
    /// </summary>
    internal static class GLConvert
    {
        public static PrimitiveType Topology(PrimitiveTopology t) => t switch
        {
            PrimitiveTopology.Triangles => PrimitiveType.Triangles,
            PrimitiveTopology.Lines => PrimitiveType.Lines,
            PrimitiveTopology.LineStrip => PrimitiveType.LineStrip,
            PrimitiveTopology.Points => PrimitiveType.Points,
            _ => throw new ArgumentOutOfRangeException(nameof(t))
        };

        public static DepthFunction Depth(CompareFunc f) => f switch
        {
            CompareFunc.Never => DepthFunction.Never,
            CompareFunc.Less => DepthFunction.Less,
            CompareFunc.Equal => DepthFunction.Equal,
            CompareFunc.LessEqual => DepthFunction.Lequal,
            CompareFunc.Greater => DepthFunction.Greater,
            CompareFunc.NotEqual => DepthFunction.Notequal,
            CompareFunc.GreaterEqual => DepthFunction.Gequal,
            CompareFunc.Always => DepthFunction.Always,
            _ => throw new ArgumentOutOfRangeException(nameof(f))
        };

        public static BlendingFactor Blend(BlendFactor f) => f switch
        {
            BlendFactor.Zero => BlendingFactor.Zero,
            BlendFactor.One => BlendingFactor.One,
            BlendFactor.SrcAlpha => BlendingFactor.SrcAlpha,
            BlendFactor.OneMinusSrcAlpha => BlendingFactor.OneMinusSrcAlpha,
            BlendFactor.SrcColor => BlendingFactor.SrcColor,
            BlendFactor.OneMinusSrcColor => BlendingFactor.OneMinusSrcColor,
            BlendFactor.DstAlpha => BlendingFactor.DstAlpha,
            BlendFactor.OneMinusDstAlpha => BlendingFactor.OneMinusDstAlpha,
            _ => throw new ArgumentOutOfRangeException(nameof(f))
        };

        /// <summary>Число компонент и признак целочисленного атрибута</summary>
        public static (int size, bool integer) Attribute(VertexFormat f) => f switch
        {
            VertexFormat.Float1 => (1, false),
            VertexFormat.Float2 => (2, false),
            VertexFormat.Float3 => (3, false),
            VertexFormat.Float4 => (4, false),
            VertexFormat.Int1 => (1, true),
            VertexFormat.Int2 => (2, true),
            VertexFormat.Int3 => (3, true),
            VertexFormat.Int4 => (4, true),
            VertexFormat.UByte4Norm => (4, false),
            _ => throw new ArgumentOutOfRangeException(nameof(f))
        };

        public static int MinFilter(TextureFilter min, MipFilter mip) => (int)(mip switch
        {
            MipFilter.None => min == TextureFilter.Nearest ? GLEnum.Nearest : GLEnum.Linear,
            MipFilter.Nearest => min == TextureFilter.Nearest ? GLEnum.NearestMipmapNearest : GLEnum.LinearMipmapNearest,
            _ => min == TextureFilter.Nearest ? GLEnum.NearestMipmapLinear : GLEnum.LinearMipmapLinear
        });

        public static int MagFilter(TextureFilter mag)
            => (int)(mag == TextureFilter.Nearest ? GLEnum.Nearest : GLEnum.Linear);

        public static int Wrap(TextureWrap w, bool gles) => (int)(w switch
        {
            TextureWrap.Repeat => GLEnum.Repeat,
            TextureWrap.ClampToEdge => GLEnum.ClampToEdge,
            _ => gles ? GLEnum.ClampToEdge : GLEnum.ClampToBorder
        });

        public static Silk.NET.OpenGL.PixelFormat Pixel(PixelFormat f)
            => f == PixelFormat.Bgra8 ? Silk.NET.OpenGL.PixelFormat.Bgra : Silk.NET.OpenGL.PixelFormat.Rgba;
    }
}
