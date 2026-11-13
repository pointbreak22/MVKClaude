using System;
using Mvk.Core.Maths;
using Mvk.Engine.Util;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.Shaders
{
    /// <summary>
    /// Шейдер блоков и его конвейеры на RHI.
    /// В оригинале (Vge ShadersBlocks) три программы: Low, High (тени) и DepthMap.
    /// Пока перенесён только Low; High и DepthMap появятся вместе с кадровыми буферами в RHI.
    /// </summary>
    public sealed class ShadersBlocks : IDisposable
    {
        /// <summary>Слот текстуры атласа с мипмапами</summary>
        public const int SlotAtlasBlurry = 0;
        /// <summary>Слот текстуры атласа без мипмапов</summary>
        public const int SlotAtlasSharpness = 1;
        /// <summary>Слот карты света</summary>
        public const int SlotLightMap = 2;

        /// <summary>Сплошные блоки: отсечение задних граней</summary>
        public readonly IPipeline Dense;
        /// <summary>Сплошные блоки каркасом (Debug.IsDrawVoxelLine): без отсечения</summary>
        public readonly IPipeline DenseLine;
        /// <summary>Альфа блоки дальних чанков</summary>
        public readonly IPipeline Alpha;
        /// <summary>Альфа блоки ближних 9 чанков: сдвиг глубины как glPolygonOffset(0.5, -10)</summary>
        public readonly IPipeline AlphaNear;

        private readonly IShader _shader;
        private readonly UniformHandle _view, _takt, _animOffset, _wind, _player, _overview,
            _colorfog, _torch, _brightness, _lightDir, _chunk;

        public ShadersBlocks(RenderContext context, string nameLow)
        {
            IGraphicsDevice device = context.Device;
            _shader = device.CreateShader(new ShaderDescription(nameLow,
                FileAssets.ReadStringToShader(nameLow + ".vsh"),
                FileAssets.ReadStringToShader(nameLow + ".fsh"),
                new[] { "v_position", "v_texCoord", "v_rgbl", "v_anim", "v_normal" },
                new[] { "atlas_blurry", "atlas_sharpness", "light_map" }));

            _view = _shader.GetUniform("view");
            _takt = _shader.GetUniform("takt");
            _animOffset = _shader.GetUniform("animOffset");
            _wind = _shader.GetUniform("wind");
            _player = _shader.GetUniform("player");
            _overview = _shader.GetUniform("overview");
            _colorfog = _shader.GetUniform("colorfog");
            _torch = _shader.GetUniform("torch");
            _brightness = _shader.GetUniform("brightness");
            _lightDir = _shader.GetUniform("lightDir");
            _chunk = _shader.GetUniform("chunk");

            // В оригинале смешивание SRC_ALPHA / ONE_MINUS_SRC_ALPHA включено всегда,
            // в том числе для сплошных блоков, поэтому BlendState.Alpha у всех конвейеров.
            RasterState cull = RasterState.Default with { Cull = CullMode.Back };
            Dense = _Create(device, cull);
            DenseLine = _Create(device, RasterState.Default with { Fill = FillMode.Wireframe });
            // После прохода сплошных блоков отсечение в оригинале остаётся включённым
            Alpha = _Create(device, cull);
            AlphaNear = _Create(device, cull with { DepthBiasFactor = 0.5f, DepthBiasUnits = -10f });
        }

        private IPipeline _Create(IGraphicsDevice device, RasterState raster)
            => device.CreatePipeline(new PipelineDescription(_shader,
                RenderContext.BlockFloats, RenderContext.BlockInts)
            {
                Raster = raster,
                Blend = BlendState.Alpha
            });

        /// <summary>
        /// Выбрать конвейер и занести униформы кадра (BindUniformBigin оригинала)
        /// </summary>
        public void Begin(CommandList commands, IPipeline pipeline,
            ITexture atlasBlurry, ITexture atlasSharpness, ITexture lightMap,
            float x, float y, float z, int takt, float wind,
            float overview, Vector3 colorFog, float torchInHand)
        {
            commands.SetPipeline(pipeline);
            commands.SetTexture(SlotAtlasBlurry, atlasBlurry);
            commands.SetTexture(SlotAtlasSharpness, atlasSharpness);
            commands.SetTexture(SlotLightMap, lightMap);
            commands.SetUniformMatrix4(_view, Gi.MatrixView);
            commands.SetUniform(_takt, takt);
            commands.SetUniform(_animOffset, Ce.ShaderAnimOffset);
            // Ветер, значение от -1 до 1
            commands.SetUniform(_wind, wind);
            commands.SetUniform(_player, x, y, z);
            commands.SetUniform(_overview, overview);
            commands.SetUniform(_colorfog, colorFog);
            commands.SetUniform(_torch, torchInHand);
            commands.SetUniform(_brightness, Gi.BlockBrightness);
            commands.SetUniform(_lightDir, Gi.ViewLightDir);
        }

        /// <summary>
        /// Сменить конвейер того же шейдера, униформы кадра уже занесены в Begin
        /// </summary>
        public void Switch(CommandList commands, IPipeline pipeline) => commands.SetPipeline(pipeline);

        /// <summary>
        /// Смещение чанка в блоках (UniformDataChunk оригинала)
        /// </summary>
        public void UniformDataChunk(CommandList commands, int x, int y)
            => commands.SetUniform(_chunk, (float)x, (float)y);

        public void Dispose()
        {
            Dense.Dispose();
            DenseLine.Dispose();
            Alpha.Dispose();
            AlphaNear.Dispose();
            _shader.Dispose();
        }
    }
}
