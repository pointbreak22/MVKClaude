#nullable enable
using System;
using System.Runtime.CompilerServices;
using Mvk.Engine.Util;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.Shaders
{
    /// <summary>
    /// Шейдеры сущностей и их конвейеры на RHI. Перенос ShadersEntity оригинала.
    /// Методы с теми же именами и параметрами, что в оригинале (UniformData и BindUniform*), чтобы
    /// перенесённые без изменений рендеры сущностей писали юниформы так же; запись идёт в список команд кадра.
    /// Как в оригинале четыре программы: Gui, Low, High (с тенями, Options.Shadow) и DepthMap (карта теней).
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
        /// <summary>Слот карты теней (только High)</summary>
        public const int SlotDepthMap = 4;

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
        /// Список команд текущего прохода, его задают BindUniformBegin*
        /// </summary>
        public CommandList Commands { get; private set; } = new CommandList();

        /// <summary>
        /// Отрисовка интерфейса: через неё выбираются конвейеры GUI с текущим состоянием глубины
        /// </summary>
        public RenderMain? Gui { get; set; }

        private readonly Program _low, _high, _depthMap;
        /// <summary>Программа текущего прохода (_shderAction оригинала)</summary>
        private Program _action;
        /// <summary>Активна ли карта глубины</summary>
        private bool _flagActionDepthMap;
        /// <summary>Графика качественнее, с тенями</summary>
        private bool _qualitatively;

        /// <summary>Шейдер сущностей и предметов в GUI (EntityGui)</summary>
        private readonly IShader _shaderGui;
        /// <summary>Конвейеры GUI [глубина выключена, включена], глубину выбирает RenderMain</summary>
        private readonly IPipeline[] _pipeGui = new IPipeline[2];
        private readonly UniformHandle _guiView, _guiBrightness, _guiDepth, _guiLightDir, _guiScale, _guiPos;
        /// <summary>Текстуры последнего прохода сущностей: для GUI привязываются заново (слот 0 занят GUI)</summary>
        private ITexture? _small, _big, _atlas, _lightMap, _depthTexture;

        public ShadersEntity(RenderContext context, string nameLow, string nameHigh, string nameDepthMap, string nameGui)
        {
            IGraphicsDevice device = context.Device;
            _low = new Program(device, nameLow, Kind.Low);
            _high = new Program(device, nameHigh, Kind.High);
            _depthMap = new Program(device, nameDepthMap, Kind.DepthMap);
            _qualitatively = Options.Shadow;
            _action = _qualitatively ? _high : _low;

            _shaderGui = device.CreateShader(new ShaderDescription(nameGui,
                FileAssets.ReadStringToShader(nameGui + ".vsh"),
                FileAssets.ReadStringToShader(nameGui + ".fsh"),
                new[] { "v_position", "v_normal", "v_texCoord" },
                new[] { "sampler_small", "sampler_big", "atlas" }));
            _guiView = _shaderGui.GetUniform("view");
            _guiBrightness = _shaderGui.GetUniform("brightness");
            _guiDepth = _shaderGui.GetUniform("depth");
            _guiLightDir = _shaderGui.GetUniform("lightDir");
            _guiScale = _shaderGui.GetUniform("scale");
            _guiPos = _shaderGui.GetUniform("pos");
            for (int i = 0; i < 2; i++)
            {
                _pipeGui[i] = device.CreatePipeline(new PipelineDescription(_shaderGui, LayoutFloats)
                {
                    Raster = _GuiRaster(i == 1),
                    Blend = BlendState.Alpha
                });
            }
        }

        /// <summary>Состояние GUI: отсечение задних граней, глубина по DepthOn/DepthOff</summary>
        private static RasterState _GuiRaster(bool depth) => RasterState.Default with
        {
            Cull = CullMode.Back, DepthTest = depth, DepthWrite = depth,
            DepthFunc = depth ? CompareFunc.LessEqual : CompareFunc.Always
        };

        /// <summary>
        /// Изменено качество (тени)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateOptions() => _qualitatively = Options.Shadow;

        #region Bind Uniform

        /// <summary>
        /// Занести начальные юниформы (BindUniformBegin оригинала) и текстуры прохода
        /// </summary>
        public void BindUniformBegin(CommandList commands, ITexture small, ITexture big, ITexture atlas,
            ITexture lightMap, ITexture? depthMap)
            => _Begin(commands, small, big, atlas, lightMap, depthMap, Gi.MatrixView);

        /// <summary>
        /// Занести начальные юниформы для моделей с глаз (BindUniformBeginEye оригинала)
        /// </summary>
        public void BindUniformBeginEye(CommandList commands, ITexture small, ITexture big, ITexture atlas,
            ITexture lightMap, ITexture? depthMap)
            => _Begin(commands, small, big, atlas, lightMap, depthMap, Gi.MatrixViewEye);

        private void _Begin(CommandList commands, ITexture small, ITexture big, ITexture atlas,
            ITexture lightMap, ITexture? depthMap, float[] view)
        {
            Commands = commands;
            _flagActionDepthMap = false;
            _action = _qualitatively ? _high : _low;
            _small = small;
            _big = big;
            _atlas = atlas;
            _lightMap = lightMap;
            _depthTexture = depthMap;
            commands.SetPipeline(_action.Pipeline);
            _BindTextures(commands);
            commands.SetUniformMatrix4(_action.View, view);
            commands.SetUniform(_action.Scale, 1f);
            commands.SetUniform(_action.Brightness, Gi.EntityBrightness);
            commands.SetUniform(_action.LightDir, Gi.ViewLightDir);
            if (_qualitatively)
            {
                commands.SetUniformMatrix4(_action.LightMatrix, Gi.MatrixViewDepthMap);
            }
        }

        private void _BindTextures(CommandList commands)
        {
            if (_small != null) commands.SetTexture(SlotSamplerSmall, _small);
            if (_big != null) commands.SetTexture(SlotSamplerBig, _big);
            if (_atlas != null) commands.SetTexture(SlotAtlas, _atlas);
            if (_lightMap != null) commands.SetTexture(SlotLightMap, _lightMap);
            if (_qualitatively && _depthTexture != null) commands.SetTexture(SlotDepthMap, _depthTexture);
        }

        /// <summary>
        /// Занести начальные юниформы для карты глубины (BindUniformBeginDepthMap оригинала)
        /// </summary>
        public void BindUniformBeginDepthMap(CommandList commands, ITexture small, ITexture big, ITexture atlas)
        {
            Commands = commands;
            _flagActionDepthMap = true;
            _action = _depthMap;
            commands.SetPipeline(_depthMap.Pipeline);
            commands.SetTexture(SlotSamplerSmall, small);
            commands.SetTexture(SlotSamplerBig, big);
            commands.SetTexture(SlotAtlas, atlas);
            commands.SetUniformMatrix4(_depthMap.View, Gi.MatrixViewDepthMap);
        }

        /// <summary>
        /// Обнулить для предметов
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindUniformItems()
        {
            Commands.SetUniform(_action.Depth, 0f);
            Commands.SetUniform(_action.Anim, 0f);
            Commands.SetUniform(_action.EyeMouth, 0);
        }

        /// <summary>
        /// Внести данные в юниформы
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UniformData(float x, float y, float z,
            float lightBlock, float lightSky, float depth, float anim, int eyeMouth)
        {
            Commands.SetUniform(_action.Pos, x, y, z);
            Commands.SetUniform(_action.Depth, depth);
            Commands.SetUniform(_action.Anim, anim);
            Commands.SetUniform(_action.EyeMouth, eyeMouth);
            if (!_flagActionDepthMap)
            {
                // Свет для карты теней не нужен
                Commands.SetUniform(_action.Light, lightBlock, lightSky);
            }
        }

        /// <summary>
        /// Внести данные в юниформы для претмета
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UniformData(float x, float y, float z, float lightBlock, float lightSky)
        {
            Commands.SetUniform(_action.Pos, x, y, z);
            if (!_flagActionDepthMap)
            {
                // Свет для карты теней не нужен
                Commands.SetUniform(_action.Light, lightBlock, lightSky);
            }
        }

        /// <summary>
        /// Внести матрицы костей (4x3) в юниформы
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UniformData(float[] elementTransforms)
            => Commands.SetUniformMatrix4x3(_action.ElementTransforms, elementTransforms, Ce.MaxAnimatedBones);

        /// <summary>
        /// Занести начальные юниформы для GUI (BindUniformBeginGui оригинала)
        /// </summary>
        public void BindUniformBeginGui()
        {
            Commands = Gui!.Commands;
            Gui.BindPipelines(_pipeGui);
            if (_small != null) Commands.SetTexture(SlotSamplerSmall, _small);
            if (_big != null) Commands.SetTexture(SlotSamplerBig, _big);
            if (_atlas != null) Commands.SetTexture(SlotAtlas, _atlas);
            Commands.SetUniformMatrix4(_guiView, Gi.Ortho);
            Commands.SetUniform(_guiBrightness, Gi.EntityBrightness);
            // Нужен когда хотим сущность продемонстрировать
            Commands.SetUniform(_guiDepth, -1f);
            UniformVolumeGui(true);
        }

        /// <summary>
        /// Занести юниформы для GUI анимированной сущности (BindUniformAnimationGui оригинала:
        /// текущий шейдер сущностей, Low или High, с ортогональной матрицей GUI)
        /// </summary>
        public void BindUniformAnimationGui(float x, float y, float scale, float depth, float anim, int eyeMouth)
        {
            Commands = Gui!.Commands;
            Program p = _action;
            Gui.BindPipelines(p.AnimationGui);
            _BindTextures(Commands);
            Commands.SetUniformMatrix4(p.View, Gi.Ortho);
            Commands.SetUniform(p.Brightness, Gi.EntityBrightness);
            // -5, -1, -5 нормализован
            Commands.SetUniform(p.LightDir, -.700140059f, -.140028015f, -.700140059f);
            Commands.SetUniform(p.Scale, -scale);
            Commands.SetUniform(p.Pos, x, y, 0);
            Commands.SetUniform(p.Depth, depth);
            Commands.SetUniform(p.Anim, anim);
            Commands.SetUniform(p.EyeMouth, eyeMouth);
            Commands.SetUniform(p.Light, .96875f, .96875f);
        }

        /// <summary>
        /// Внести позицию для претмета Gui
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UniformPosGui(float x, float y) => Commands.SetUniform(_guiPos, x, y);

        /// <summary>
        /// Внести значение объёмный ли предмет или нет
        /// </summary>
        public void UniformVolumeGui(bool volume = false)
        {
            if (volume)
            {
                // -5, -1, -5 нормализован
                Commands.SetUniform(_guiLightDir, -.700140059f, -.140028015f, -.700140059f);
                Commands.SetUniform(_guiScale, -(float)Gi.Si);
            }
            else
            {
                Commands.SetUniform(_guiLightDir, 0, 0, -1);
                Commands.SetUniform(_guiScale, (float)Gi.Si);
            }
        }

        #endregion

        public void Dispose()
        {
            _pipeGui[0].Dispose();
            _pipeGui[1].Dispose();
            _shaderGui.Dispose();
            _low.Dispose();
            _high.Dispose();
            _depthMap.Dispose();
        }

        private enum Kind { Low, High, DepthMap }

        /// <summary>
        /// Программа сущностей (Low, High или DepthMap) и её конвейеры
        /// </summary>
        private sealed class Program : IDisposable
        {
            public readonly IShader Shader;
            /// <summary>Мир: глубина, отсечение задних граней, смешивание по альфе (у DepthMap без цвета)</summary>
            public readonly IPipeline Pipeline;
            /// <summary>Сущность в GUI [глубина выключена, включена] (не у DepthMap)</summary>
            public readonly IPipeline[] AnimationGui = new IPipeline[2];
            public readonly UniformHandle View, Scale, Brightness, LightDir, Pos, Depth, Anim,
                EyeMouth, Light, ElementTransforms, LightMatrix;

            public Program(IGraphicsDevice device, string name, Kind kind)
            {
                string[] samplers = kind switch
                {
                    Kind.High => new[] { "sampler_small", "sampler_big", "atlas", "light_map", "depth_map" },
                    Kind.DepthMap => new[] { "sampler_small", "sampler_big", "atlas" },
                    _ => new[] { "sampler_small", "sampler_big", "atlas", "light_map" }
                };
                Shader = device.CreateShader(new ShaderDescription(name,
                    FileAssets.ReadStringToShader(name + ".vsh"),
                    FileAssets.ReadStringToShader(name + ".fsh"),
                    new[] { "v_position", "v_normal", "v_texCoord", "v_jointId", "v_clothId" },
                    samplers));
                View = Shader.GetUniform("view");
                Scale = Shader.GetUniform("scale");
                Brightness = Shader.GetUniform("brightness");
                LightDir = Shader.GetUniform("lightDir");
                Pos = Shader.GetUniform("pos");
                Depth = Shader.GetUniform("depth");
                Anim = Shader.GetUniform("anim");
                // У EntityDepthMap юниформ называется eyeLips, оригинал пишет eyeMouth: в тени глаза и рот
                // не отбрасываются, поведение то же
                EyeMouth = Shader.GetUniform("eyeMouth");
                Light = Shader.GetUniform("light");
                LightMatrix = Shader.GetUniform("lightMatrix");
                ElementTransforms = Shader.GetUniform("elementTransforms[0]");
                if (!ElementTransforms.IsValid) ElementTransforms = Shader.GetUniform("elementTransforms");

                Pipeline = device.CreatePipeline(new PipelineDescription(Shader, LayoutFloats, LayoutInts)
                {
                    Raster = RasterState.Default with { Cull = CullMode.Back },
                    Blend = BlendState.Alpha,
                    ColorWrite = kind != Kind.DepthMap
                });
                if (kind != Kind.DepthMap)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        AnimationGui[i] = device.CreatePipeline(new PipelineDescription(Shader, LayoutFloats, LayoutInts)
                        {
                            Raster = _GuiRaster(i == 1),
                            Blend = BlendState.Alpha
                        });
                    }
                }
            }

            public void Dispose()
            {
                Pipeline.Dispose();
                AnimationGui[0]?.Dispose();
                AnimationGui[1]?.Dispose();
                Shader.Dispose();
            }
        }
    }
}
