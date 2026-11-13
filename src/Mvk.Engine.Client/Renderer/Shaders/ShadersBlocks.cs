#nullable enable
using System;
using Mvk.Core.Maths;
using Mvk.Engine.Util;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.Shaders
{
    /// <summary>
    /// Шейдеры блоков и их конвейеры на RHI. Как в оригинале (Vge ShadersBlocks) три программы:
    /// Low (без теней), High (с тенями, Options.Shadow) и DepthMap (проход в карту теней).
    /// </summary>
    public sealed class ShadersBlocks : IDisposable
    {
        /// <summary>Слот текстуры атласа с мипмапами</summary>
        public const int SlotAtlasBlurry = 0;
        /// <summary>Слот текстуры атласа без мипмапов</summary>
        public const int SlotAtlasSharpness = 1;
        /// <summary>Слот карты света</summary>
        public const int SlotLightMap = 2;
        /// <summary>Слот карты теней (только High)</summary>
        public const int SlotDepthMap = 3;

        /// <summary>Сплошные блоки: отсечение задних граней</summary>
        public IPipeline Dense => _action.Dense;
        /// <summary>Сплошные блоки каркасом (Debug.IsDrawVoxelLine): без отсечения</summary>
        public IPipeline DenseLine => _action.DenseLine;
        /// <summary>Альфа блоки дальних чанков</summary>
        public IPipeline Alpha => _action.Alpha;
        /// <summary>Альфа блоки ближних 9 чанков: сдвиг глубины как glPolygonOffset(0.5, -10)</summary>
        public IPipeline AlphaNear => _action.AlphaNear;

        private readonly Program _low, _high;
        /// <summary>Шейдер карты теней (сплошные блоки, без цвета)</summary>
        private readonly IShader _depthShader;
        private readonly IPipeline _depthPipeline;
        private readonly UniformHandle _dView, _dTakt, _dAnimOffset, _dWind, _dPlayer, _dChunk;
        /// <summary>Программа текущего прохода (_shderAction оригинала)</summary>
        private Program _action;
        /// <summary>Проход в карту теней</summary>
        private bool _depthPass;
        /// <summary>Графика качественнее, с тенями</summary>
        private bool _qualitatively;
        /// <summary>Текстуры кадра из Begin: Switch ставит их заново</summary>
        private ITexture? _atlasBlurry, _atlasSharpness, _lightMap, _depthMap;

        public ShadersBlocks(RenderContext context, string nameLow, string nameHigh, string nameDepthMap)
        {
            IGraphicsDevice device = context.Device;
            _low = new Program(device, nameLow, false);
            _high = new Program(device, nameHigh, true);
            _qualitatively = Options.Shadow;
            _action = _qualitatively ? _high : _low;

            _depthShader = device.CreateShader(new ShaderDescription(nameDepthMap,
                FileAssets.ReadStringToShader(nameDepthMap + ".vsh"),
                FileAssets.ReadStringToShader(nameDepthMap + ".fsh"),
                new[] { "v_position", "v_texCoord", "v_rgbl", "v_anim", "v_normal" },
                new[] { "atlas_blurry", "atlas_sharpness" }));
            _dView = _depthShader.GetUniform("view");
            _dTakt = _depthShader.GetUniform("takt");
            _dAnimOffset = _depthShader.GetUniform("animOffset");
            _dWind = _depthShader.GetUniform("wind");
            _dPlayer = _depthShader.GetUniform("player");
            _dChunk = _depthShader.GetUniform("chunk");
            // В кадровом буфере теней цвета нет (glDrawBuffer NONE)
            _depthPipeline = device.CreatePipeline(new PipelineDescription(_depthShader,
                RenderContext.BlockFloats, RenderContext.BlockInts)
            {
                Raster = RasterState.Default with { Cull = CullMode.Back },
                Blend = BlendState.Alpha,
                ColorWrite = false
            });
        }

        /// <summary>
        /// Изменено качество (тени)
        /// </summary>
        public void UpdateOptions() => _qualitatively = Options.Shadow;

        /// <summary>
        /// Выбрать конвейер и занести униформы кадра (BindUniformBigin оригинала)
        /// </summary>
        public void Begin(CommandList commands, Func<ShadersBlocks, IPipeline> pipeline,
            ITexture atlasBlurry, ITexture atlasSharpness, ITexture lightMap, ITexture? depthMap,
            float x, float y, float z, int takt, float wind,
            float overview, Vector3 colorFog, float torchInHand)
        {
            _depthPass = false;
            _action = _qualitatively ? _high : _low;
            commands.SetPipeline(pipeline(this));
            commands.SetTexture(SlotAtlasBlurry, atlasBlurry);
            commands.SetTexture(SlotAtlasSharpness, atlasSharpness);
            commands.SetTexture(SlotLightMap, lightMap);
            if (_qualitatively && depthMap != null) commands.SetTexture(SlotDepthMap, depthMap);
            _atlasBlurry = atlasBlurry;
            _atlasSharpness = atlasSharpness;
            _lightMap = lightMap;
            _depthMap = depthMap;
            Program p = _action;
            commands.SetUniformMatrix4(p.View, Gi.MatrixView);
            commands.SetUniform(p.Takt, takt);
            commands.SetUniform(p.AnimOffset, Ce.ShaderAnimOffset);
            // Ветер, значение от -1 до 1
            commands.SetUniform(p.Wind, wind);
            commands.SetUniform(p.Player, x, y, z);
            commands.SetUniform(p.Overview, overview);
            commands.SetUniform(p.Colorfog, colorFog);
            commands.SetUniform(p.Torch, torchInHand);
            commands.SetUniform(p.Brightness, Gi.BlockBrightness);
            commands.SetUniform(p.LightDir, Gi.ViewLightDir);
            if (_qualitatively)
            {
                commands.SetUniformMatrix4(p.LightMatrix, Gi.MatrixViewDepthMap);
            }
        }

        /// <summary>
        /// Занести униформы прохода карты теней (BindUniformBiginDepthMap оригинала)
        /// </summary>
        public void BeginDepthMap(CommandList commands, ITexture atlasBlurry, ITexture atlasSharpness,
            float x, float y, float z, int takt, float wind)
        {
            _depthPass = true;
            commands.SetPipeline(_depthPipeline);
            commands.SetTexture(SlotAtlasBlurry, atlasBlurry);
            commands.SetTexture(SlotAtlasSharpness, atlasSharpness);
            commands.SetUniformMatrix4(_dView, Gi.MatrixViewDepthMap);
            commands.SetUniform(_dTakt, takt);
            commands.SetUniform(_dAnimOffset, Ce.ShaderAnimOffset);
            // Ветер, значение от -1 до 1
            commands.SetUniform(_dWind, wind);
            commands.SetUniform(_dPlayer, x, y, z);
        }

        /// <summary>
        /// Сменить конвейер того же шейдера, униформы кадра уже занесены в Begin.
        /// Текстуры ставятся заново: между проходами сплошных и альфа блоков рисуются сущности, облака и дождь,
        /// они занимают те же слоты (в оригинале атласы жили на отдельных блоках 5-7 и не перебивались).
        /// </summary>
        public void Switch(CommandList commands, IPipeline pipeline)
        {
            commands.SetPipeline(pipeline);
            if (_atlasBlurry != null) commands.SetTexture(SlotAtlasBlurry, _atlasBlurry);
            if (_atlasSharpness != null) commands.SetTexture(SlotAtlasSharpness, _atlasSharpness);
            if (_lightMap != null) commands.SetTexture(SlotLightMap, _lightMap);
            if (_qualitatively && _depthMap != null) commands.SetTexture(SlotDepthMap, _depthMap);
        }

        /// <summary>
        /// Смещение чанка в блоках (UniformDataChunk оригинала)
        /// </summary>
        public void UniformDataChunk(CommandList commands, int x, int y)
            => commands.SetUniform(_depthPass ? _dChunk : _action.Chunk, (float)x, (float)y);

        public void Dispose()
        {
            _low.Dispose();
            _high.Dispose();
            _depthPipeline.Dispose();
            _depthShader.Dispose();
        }

        /// <summary>
        /// Программа блоков (Low или High) и её конвейеры
        /// </summary>
        private sealed class Program : IDisposable
        {
            public readonly IShader Shader;
            public readonly IPipeline Dense, DenseLine, Alpha, AlphaNear;
            public readonly UniformHandle View, Takt, AnimOffset, Wind, Player, Overview,
                Colorfog, Torch, Brightness, LightDir, Chunk, LightMatrix;

            public Program(IGraphicsDevice device, string name, bool shadow)
            {
                Shader = device.CreateShader(new ShaderDescription(name,
                    FileAssets.ReadStringToShader(name + ".vsh"),
                    FileAssets.ReadStringToShader(name + ".fsh"),
                    new[] { "v_position", "v_texCoord", "v_rgbl", "v_anim", "v_normal" },
                    shadow
                        ? new[] { "atlas_blurry", "atlas_sharpness", "light_map", "depth_map" }
                        : new[] { "atlas_blurry", "atlas_sharpness", "light_map" }));
                View = Shader.GetUniform("view");
                Takt = Shader.GetUniform("takt");
                AnimOffset = Shader.GetUniform("animOffset");
                Wind = Shader.GetUniform("wind");
                Player = Shader.GetUniform("player");
                Overview = Shader.GetUniform("overview");
                Colorfog = Shader.GetUniform("colorfog");
                Torch = Shader.GetUniform("torch");
                Brightness = Shader.GetUniform("brightness");
                LightDir = Shader.GetUniform("lightDir");
                Chunk = Shader.GetUniform("chunk");
                LightMatrix = Shader.GetUniform("lightMatrix");
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
                => device.CreatePipeline(new PipelineDescription(Shader,
                    RenderContext.BlockFloats, RenderContext.BlockInts)
                {
                    Raster = raster,
                    Blend = BlendState.Alpha
                });

            public void Dispose()
            {
                Dense.Dispose();
                DenseLine.Dispose();
                Alpha.Dispose();
                AlphaNear.Dispose();
                Shader.Dispose();
            }
        }
    }
}
