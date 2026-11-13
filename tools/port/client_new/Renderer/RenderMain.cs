#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mvk.Core.Maths;
using Mvk.Engine.Renderer.Font;
using Mvk.Engine.Renderer.Mesh;
using Mvk.Engine.Util;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer
{
    /// <summary>
    /// Отрисовка интерфейса на RHI. Перенос GUI-части RenderMain оригинала: шейдеры GuiColor и GuiLine,
    /// текстуры GUI по номерам, основной шрифт, включение и выключение глубины.
    /// Методы с теми же именами, что в оригинале, пишут в список команд текущего кадра (Commands),
    /// поэтому перенесённый код GUI вызывает их как раньше.
    /// </summary>
    public class RenderMain : IDisposable
    {
        /// <summary>Общие объекты рендера</summary>
        public readonly RenderContext Context;
        /// <summary>Текстуры GUI по номерам</summary>
        public readonly TextureMap Texture;
        /// <summary>Основной шрифт</summary>
        public FontBase? FontMain { get; private set; }

        /// <summary>Список команд текущего кадра</summary>
        public CommandList Commands { get; private set; } = new CommandList();
        /// <summary>Номер кадра, растёт в BeginFrame и при Flush</summary>
        public long Frame { get; private set; }

        /// <summary>Номера текстур виджетов и частиц (TextureIndex оригинала)</summary>
        protected uint _textureWidgets, _textureParticles;

        private readonly IShader _shGuiColor, _shGuiLine;
        private readonly UniformHandle _colorProjview, _colorPos, _lineProjview, _linePos;
        /// <summary>Конвейеры: [глубина выключена, включена]</summary>
        private readonly IPipeline[] _pipeColor = new IPipeline[2];
        private readonly IPipeline[] _pipeLine = new IPipeline[2];
        /// <summary>Матрица 2D окна (Ortho2D окна оригинала)</summary>
        private readonly float[] _ortho2D = new float[16];
        /// <summary>Глубина включена (в оригинале после мира тест и запись глубины включены)</summary>
        private bool _depth = true;
        /// <summary>
        /// Конвейеры последнего выбранного шейдера [глубина выключена, включена]. В оригинале глубина —
        /// общее состояние GL и действует на любой шейдер, в том числе сущностей в GUI.
        /// </summary>
        private IPipeline[]? _current;

        public RenderMain(RenderContext context)
        {
            Context = context;
            Texture = new TextureMap(this);
            IGraphicsDevice device = context.Device;
            _shGuiColor = device.CreateShader(new ShaderDescription("GuiColor",
                FileAssets.ReadStringToShader("GuiColor.vsh"), FileAssets.ReadStringToShader("GuiColor.fsh"),
                new[] { "v_position", "v_texCoord", "v_color" }, new[] { "u_texture0" }));
            _colorProjview = _shGuiColor.GetUniform("projview");
            _colorPos = _shGuiColor.GetUniform("pos");
            _shGuiLine = device.CreateShader(new ShaderDescription("GuiLine",
                FileAssets.ReadStringToShader("GuiLine.vsh"), FileAssets.ReadStringToShader("GuiLine.fsh"),
                new[] { "v_position", "v_color" }, Array.Empty<string>()));
            _lineProjview = _shGuiLine.GetUniform("projview");
            _linePos = _shGuiLine.GetUniform("pos");
            for (int i = 0; i < 2; i++)
            {
                // Состояние GL оригинала после мира: отсечение задних граней, LEQUAL, смешивание по альфе
                RasterState raster = RasterState.Default with
                {
                    Cull = CullMode.Back, DepthTest = i == 1, DepthWrite = i == 1,
                    DepthFunc = i == 1 ? CompareFunc.LessEqual : CompareFunc.Always
                };
                _pipeColor[i] = device.CreatePipeline(new PipelineDescription(_shGuiColor, MeshGuiColor.Layout)
                {
                    Raster = raster, Blend = BlendState.Alpha
                });
                _pipeLine[i] = device.CreatePipeline(new PipelineDescription(_shGuiLine, MeshGuiLine.Layout)
                {
                    Raster = raster, Blend = BlendState.Alpha, Topology = PrimitiveTopology.Lines
                });
            }
        }

        /// <summary>
        /// Начать GUI-проход кадра в списке команд (после мира)
        /// </summary>
        public void BeginFrame(CommandList commands)
        {
            Commands = commands;
            Frame++;
            _depth = true;
            _current = null;
            Glm.Ortho(0, Gi.Width, Gi.Height, 0, 0, 1).ConvArray(_ortho2D);
        }

        /// <summary>
        /// Отправить записанное и продолжить запись в тот же список (состояние бэкенда сохраняется)
        /// </summary>
        public void Flush()
        {
            Context.Device.Submit(Commands);
            Commands.Reset();
            Frame++;
            if (_current != null) Commands.SetPipeline(_current[_depth ? 1 : 0]);
        }

        /// <summary>
        /// Изменён размер интерфейса: размеры шрифтов (UpdateSizeInterface окна оригинала)
        /// </summary>
        public virtual void UpdateSizeInterface() => FontMain?.UpdateSizeInterface();

        #region ShaderBind

        /// <summary>Матрица 2D окна</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float[] GetOrtho2D() => _ortho2D;

        public void ShaderBindGuiLine() => ShaderBindGuiLine(0, 0);

        public void ShaderBindGuiLine(float posX, float posY)
        {
            BindPipelines(_pipeLine);
            Commands.SetUniformMatrix4(_lineProjview, _ortho2D);
            Commands.SetUniform(_linePos, posX, posY);
        }

        public void ShaderBindGuiColor() => ShaderBindGuiColor(0, 0);

        public void ShaderBindGuiColor(float posX, float posY)
        {
            BindPipelines(_pipeColor);
            Commands.SetUniformMatrix4(_colorProjview, _ortho2D);
            Commands.SetUniform(_colorPos, posX, posY);
        }

        /// <summary>
        /// Выбрать шейдер: пара конвейеров [без глубины, с глубиной], берётся по текущему состоянию глубины
        /// </summary>
        public void BindPipelines(IPipeline[] pipelines)
        {
            _current = pipelines;
            Commands.SetPipeline(pipelines[_depth ? 1 : 0]);
        }

        /// <summary>
        /// Выключить тест и запись глубины
        /// </summary>
        public void DepthOff()
        {
            _depth = false;
            if (_current != null) Commands.SetPipeline(_current[0]);
        }

        /// <summary>
        /// Включить тест и запись глубины
        /// </summary>
        public void DepthOn()
        {
            _depth = true;
            if (_current != null) Commands.SetPipeline(_current[1]);
        }

        /// <summary>
        /// Сам объект: в оригинале GetOpenGL отдавал GL, им создавались сетки GUI
        /// </summary>
        public RenderMain GetOpenGL() => this;

        /// <summary>
        /// Шейдер сущностей (в оригинале RenderMain.ShsEntity), задаёт хост при создании рендера мира
        /// </summary>
        public Mvk.Engine.Renderer.Shaders.ShadersEntity? ShsEntity { get; set; }

        #endregion

        #region Texture

        /// <summary>
        /// Привязать текстуру RHI к слоту 0 (карта света в отладке и прочие текстуры не из TextureMap)
        /// </summary>
        public void BindTexture(ITexture texture) => Commands.SetTexture(0, texture);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindTextureWidgets() => Texture.BindTexture(_textureWidgets);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindTextureParticles() => Texture.BindTexture(_textureParticles);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CreateTextureFontMain(BufferedImage buffered) => FontMain = new FontBase(buffered, 1, this);

        /// <summary>
        /// Загрузить картинки GUI из папки текстур (шаги Loading оригинала) и создать текстуры
        /// </summary>
        public void LoadTextures()
        {
            Dictionary<string, BufferedImage> buffereds = new Dictionary<string, BufferedImage>();
            _LoadImages(buffereds);
            AtFinishLoading(buffereds);
        }

        /// <summary>
        /// Шаги загрузки картинок (Loading._Steps оригинала): основной шрифт, виджеты, частицы
        /// </summary>
        protected virtual void _LoadImages(Dictionary<string, BufferedImage> buffereds)
        {
            CreateTextureFontMain(_FileToBufferedImage(buffereds, EnumTexture.FontMain.ToString()));
            _FileToBufferedImage(buffereds, EnumTexture.Widgets.ToString());
            _FileToBufferedImage(buffereds, EnumTexture.Particles.ToString());
        }

        /// <summary>
        /// Картинка из папки текстур в словарь по ключу (имя файла без .png)
        /// </summary>
        protected static BufferedImage _FileToBufferedImage(Dictionary<string, BufferedImage> buffereds, string key)
        {
            BufferedImage buffered = BufferedFileImage.FileToBufferedImage(Options.PathTextures + key + ".png");
            buffereds[key] = buffered;
            return buffered;
        }

        /// <summary>
        /// Загрузка закончена: картинки в текстуры (AtFinishLoading оригинала)
        /// </summary>
        public virtual void AtFinishLoading(Dictionary<string, BufferedImage> buffereds)
        {
            if (FontMain != null && buffereds.TryGetValue(EnumTexture.FontMain.ToString(), out BufferedImage? font))
            {
                FontMain.CreateMesh(Texture.SetTexture(font));
            }
            if (buffereds.TryGetValue(EnumTexture.Widgets.ToString(), out BufferedImage? widgets))
            {
                _textureWidgets = Texture.SetTexture(widgets);
            }
            if (buffereds.TryGetValue(EnumTexture.Particles.ToString(), out BufferedImage? particles))
            {
                _textureParticles = Texture.SetTexture(particles);
            }
        }

        #endregion

        public virtual void Dispose()
        {
            for (int i = 0; i < 2; i++)
            {
                _pipeColor[i].Dispose();
                _pipeLine[i].Dispose();
            }
            _shGuiColor.Dispose();
            _shGuiLine.Dispose();
            Texture.Dispose();
        }
    }

    /// <summary>
    /// Ключи текстур GUI движка (TextureIndex.cs оригинала)
    /// </summary>
    public enum EnumTexture
    {
        /// <summary>Шрифт стандартный</summary>
        FontMain,
        /// <summary>Основной виджет</summary>
        Widgets,
        /// <summary>Текстура частичек</summary>
        Particles
    }
}
