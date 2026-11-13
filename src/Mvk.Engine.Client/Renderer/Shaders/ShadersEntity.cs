#nullable enable
using System;
using System.Runtime.CompilerServices;
using Mvk.Engine.Util;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.Shaders
{
    /// <summary>
    /// Шейдер сущностей и его конвейер на RHI. Перенос ShadersEntity оригинала.
    /// Методы с теми же именами и параметрами, что в оригинале (UniformData и BindUniform*), чтобы
    /// перенесённые без изменений рендеры сущностей писали юниформы так же; запись идёт в список команд кадра.
    /// В оригинале четыре программы: Gui, Low, High (тени) и DepthMap. Пока перенесён только Low;
    /// High и DepthMap появятся с кадровыми буферами в RHI, Gui вместе с GUI.
    /// </summary>
    public sealed class ShadersEntity : IDisposable
    {
        /// <summary>Слот массива маленьких текстур сущностей</summary>
        public const int SlotSamplerSmall = 0;
        /// <summary>Слот массива больших текстур сущностей</summary>
        public const int SlotSamplerBig = 1;
        /// <summary>Слот атласа блоков и предметов без мипмапов</summary>
        public const int SlotAtlas = 2;
        /// <summary>Слот карты света</summary>
        public const int SlotLightMap = 3;

        /// <summary>
        /// Раскладка вершин сущности, слот 0: позиция, нормаль (3 float) и текстурные координаты (2 float)
        /// </summary>
        public static readonly VertexBufferLayout LayoutFloats = new VertexBufferLayout(32,
            new VertexAttribute(0, VertexFormat.Float3, 0),
            new VertexAttribute(1, VertexFormat.Float3, 12),
            new VertexAttribute(2, VertexFormat.Float2, 24));
        /// <summary>
        /// Раскладка вершин сущности, слот 1: v_jointId и v_clothId (по одному int)
        /// </summary>
        public static readonly VertexBufferLayout LayoutInts = new VertexBufferLayout(8,
            new VertexAttribute(3, VertexFormat.Int1, 0),
            new VertexAttribute(4, VertexFormat.Int1, 4));

        /// <summary>
        /// Конвейер сущностей. Состояние как в оригинале после прохода сплошных блоков:
        /// тест и запись глубины, отсечение задних граней, смешивание SRC_ALPHA / ONE_MINUS_SRC_ALPHA.
        /// </summary>
        public readonly IPipeline Pipeline;

        /// <summary>
        /// Список команд текущего прохода, его задают BindUniformBegin*
        /// </summary>
        public CommandList Commands { get; private set; } = new CommandList();

        private readonly IShader _shader;
        private readonly UniformHandle _view, _scale, _brightness, _lightDir, _pos, _depth, _anim,
            _eyeMouth, _light, _elementTransforms;

        public ShadersEntity(RenderContext context, string nameLow)
        {
            IGraphicsDevice device = context.Device;
            _shader = device.CreateShader(new ShaderDescription(nameLow,
                FileAssets.ReadStringToShader(nameLow + ".vsh"),
                FileAssets.ReadStringToShader(nameLow + ".fsh"),
                new[] { "v_position", "v_normal", "v_texCoord", "v_jointId", "v_clothId" },
                new[] { "sampler_small", "sampler_big", "atlas", "light_map" }));
            _view = _shader.GetUniform("view");
            _scale = _shader.GetUniform("scale");
            _brightness = _shader.GetUniform("brightness");
            _lightDir = _shader.GetUniform("lightDir");
            _pos = _shader.GetUniform("pos");
            _depth = _shader.GetUniform("depth");
            _anim = _shader.GetUniform("anim");
            _eyeMouth = _shader.GetUniform("eyeMouth");
            _light = _shader.GetUniform("light");
            _elementTransforms = _shader.GetUniform("elementTransforms[0]");
            if (!_elementTransforms.IsValid) _elementTransforms = _shader.GetUniform("elementTransforms");

            Pipeline = device.CreatePipeline(new PipelineDescription(_shader, LayoutFloats, LayoutInts)
            {
                Raster = RasterState.Default with { Cull = CullMode.Back },
                Blend = BlendState.Alpha
            });
        }

        #region Bind Uniform

        /// <summary>
        /// Занести начальные юниформы (BindUniformBegin оригинала) и текстуры прохода
        /// </summary>
        public void BindUniformBegin(CommandList commands, ITexture small, ITexture big, ITexture atlas, ITexture lightMap)
            => _Begin(commands, small, big, atlas, lightMap, Gi.MatrixView);

        /// <summary>
        /// Занести начальные юниформы для моделей с глаз (BindUniformBeginEye оригинала)
        /// </summary>
        public void BindUniformBeginEye(CommandList commands, ITexture small, ITexture big, ITexture atlas, ITexture lightMap)
            => _Begin(commands, small, big, atlas, lightMap, Gi.MatrixViewEye);

        private void _Begin(CommandList commands, ITexture small, ITexture big, ITexture atlas, ITexture lightMap, float[] view)
        {
            Commands = commands;
            commands.SetPipeline(Pipeline);
            commands.SetTexture(SlotSamplerSmall, small);
            commands.SetTexture(SlotSamplerBig, big);
            commands.SetTexture(SlotAtlas, atlas);
            commands.SetTexture(SlotLightMap, lightMap);
            commands.SetUniformMatrix4(_view, view);
            commands.SetUniform(_scale, 1f);
            commands.SetUniform(_brightness, Gi.EntityBrightness);
            commands.SetUniform(_lightDir, Gi.ViewLightDir);
        }

        /// <summary>
        /// Обнулить для предметов
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindUniformItems()
        {
            Commands.SetUniform(_depth, 0f);
            Commands.SetUniform(_anim, 0f);
            Commands.SetUniform(_eyeMouth, 0);
        }

        /// <summary>
        /// Внести данные в юниформы
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UniformData(float x, float y, float z,
            float lightBlock, float lightSky, float depth, float anim, int eyeMouth)
        {
            Commands.SetUniform(_pos, x, y, z);
            Commands.SetUniform(_depth, depth);
            Commands.SetUniform(_anim, anim);
            Commands.SetUniform(_eyeMouth, eyeMouth);
            Commands.SetUniform(_light, lightBlock, lightSky);
        }

        /// <summary>
        /// Внести данные в юниформы для претмета
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UniformData(float x, float y, float z, float lightBlock, float lightSky)
        {
            Commands.SetUniform(_pos, x, y, z);
            Commands.SetUniform(_light, lightBlock, lightSky);
        }

        /// <summary>
        /// Внести матрицы костей (4x3) в юниформы
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UniformData(float[] elementTransforms)
            => Commands.SetUniformMatrix4x3(_elementTransforms, elementTransforms, Ce.MaxAnimatedBones);

        #endregion

        public void Dispose()
        {
            Pipeline.Dispose();
            _shader.Dispose();
        }
    }
}
