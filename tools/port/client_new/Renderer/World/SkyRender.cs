#nullable enable
using System;
using Mvk.Core.Maths;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Util;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.World
{
    /// <summary>
    /// Объект отвечает за прорисовку неба. Перенос SkyRender оригинала на RHI:
    /// купол из 16 сегментов (верх цвета неба, середина цвета тумана, низ цвета низа).
    /// </summary>
    public class SkyRender : IDisposable
    {
        /// <summary>
        /// Раскладка вершин неба (MeshSky): позиция 3 float и признак цвета 1 float
        /// </summary>
        public static readonly VertexBufferLayout LayoutSky = new VertexBufferLayout(16,
            new VertexAttribute(0, VertexFormat.Float3, 0),
            new VertexAttribute(1, VertexFormat.Float1, 12));

        /// <summary>
        /// Состояние неба в оригинале: Render.DepthOff (без теста и записи глубины), отсечение
        /// задних граней остаётся включённым с прохода сплошных блоков прошлого кадра
        /// </summary>
        public static readonly RasterState RasterSky = RasterState.Default with
        {
            Cull = CullMode.Back, DepthTest = false, DepthWrite = false, DepthFunc = CompareFunc.Always
        };
        /// <summary>
        /// Небо каркасом (Debug.IsDrawVoxelLine): в оригинале глубина при этом не выключается
        /// </summary>
        public static readonly RasterState RasterSkyLine = RasterState.Default with { Fill = FillMode.Wireframe };

        /// <summary>
        /// Объект рендера мира
        /// </summary>
        protected readonly WorldRenderer _worldRenderer;
        /// <summary>
        /// Объект игрока на клиенте
        /// </summary>
        protected readonly PlayerClientOwner _player;
        /// <summary>
        /// Графическое устройство
        /// </summary>
        protected readonly IGraphicsDevice _device;

        /// <summary>
        /// Шейдер неба
        /// </summary>
        protected readonly IShader _shSky;
        /// <summary>
        /// Конвейер неба
        /// </summary>
        protected readonly IPipeline _pipeSky;
        /// <summary>
        /// Конвейер неба каркасом
        /// </summary>
        protected readonly IPipeline _pipeSkyLine;
        /// <summary>
        /// Униформы шейдера неба
        /// </summary>
        protected readonly UniformHandle _uSkyView, _uSkyColor, _uSkyColorDown, _uSkyColorFog;

        /// <summary>
        /// Сетка неба
        /// </summary>
        private readonly IGpuBuffer _mesh;
        /// <summary>
        /// Количество вершин сетки неба
        /// </summary>
        private int _meshCount;
        /// <summary>
        /// Количество сегментов окружности
        /// </summary>
        protected int _segments = 16;
        /// <summary>
        /// Верхняя высота
        /// </summary>
        protected int _heightUp = 128;
        /// <summary>
        /// Средняя высота, примерно для глаз
        /// </summary>
        protected int _heightCenter = 8;
        /// <summary>
        /// Нижняя высота
        /// </summary>
        protected int _heightDown = -16;
        /// <summary>
        /// Обзор в блоках
        /// </summary>
        protected int _overviewBlock;

        private Vector3 _colorSky;
        private Vector3 _colorFog;
        protected Vector3 _colorDown;

        public SkyRender(PlayerClientOwner player, WorldRenderer worldRenderer)
        {
            _worldRenderer = worldRenderer;
            _player = player;
            _device = worldRenderer.Context.Device;
            _overviewBlock = _player.OverviewChunk * 16;
            _mesh = _device.CreateBuffer(BufferKind.Vertex, BufferUsage.Static);
            _ReloadMesh();

            _shSky = _device.CreateShader(new ShaderDescription("Sky",
                FileAssets.ReadStringToShader("Sky.vsh"), FileAssets.ReadStringToShader("Sky.fsh"),
                new[] { "v_position", "v_fog" }));
            _uSkyView = _shSky.GetUniform("view");
            _uSkyColor = _shSky.GetUniform("color");
            _uSkyColorDown = _shSky.GetUniform("colorDown");
            _uSkyColorFog = _shSky.GetUniform("colorfog");
            _pipeSky = _device.CreatePipeline(new PipelineDescription(_shSky, LayoutSky)
            {
                Raster = RasterSky,
                Blend = BlendState.Alpha
            });
            _pipeSkyLine = _device.CreatePipeline(new PipelineDescription(_shSky, LayoutSky)
            {
                Raster = RasterSkyLine,
                Blend = BlendState.Alpha
            });
        }

        /// <summary>
        /// Инициализация настроек мира
        /// </summary>
        public virtual void InitSetting() { }

        private void _ReloadMesh()
        {
            float[] buffer = _GenBuffer();
            _meshCount = buffer.Length / 4;
            _mesh.SetData<float>(buffer);
        }

        private float[] _GenBuffer()
        {
            float[] list = new float[_segments * 72];
            int c = 0;
            float fob = _overviewBlock + 32;
            float angleStep = Glm.Pi360 / _segments;
            float x, z, x0, z0, currentAngle;
            float up = _heightUp, center = _heightCenter, down = _heightDown * 4;
            x0 = z0 = 0;

            // Генерация вершин по окружности
            for (int i = 0; i <= _segments; i++)
            {
                currentAngle = angleStep * i;
                x = fob * Glm.Cos(currentAngle);
                z = fob * Glm.Sin(currentAngle);

                if (i > 0)
                {
                    // Up
                    _V(list, ref c, x, up, z, 0);
                    _V(list, ref c, 0, up, 0, 0);
                    _V(list, ref c, x0, up, z0, 0);
                    // Up side
                    _V(list, ref c, x, center, z, 2);
                    _V(list, ref c, x, up, z, 0);
                    _V(list, ref c, x0, up, z0, 0);

                    _V(list, ref c, x0, center, z0, 2);
                    _V(list, ref c, x, center, z, 2);
                    _V(list, ref c, x0, up, z0, 0);
                    // Down side
                    _V(list, ref c, x, down, z, 1);
                    _V(list, ref c, x, center, z, 2);
                    _V(list, ref c, x0, center, z0, 2);

                    _V(list, ref c, x0, down, z0, 1);
                    _V(list, ref c, x, down, z, 1);
                    _V(list, ref c, x0, center, z0, 2);
                    // Down
                    _V(list, ref c, x0, down, z0, 1);
                    _V(list, ref c, 0, down, 0, 1);
                    _V(list, ref c, x, down, z, 1);
                }

                x0 = x;
                z0 = z;
            }

            return list;
        }

        private static void _V(float[] list, ref int c, float x, float y, float z, float fog)
        {
            list[c++] = x;
            list[c++] = y;
            list[c++] = z;
            list[c++] = fog;
        }

        /// <summary>
        /// Изменён обзор чанков
        /// </summary>
        public virtual void ModifyOverviewChunk(int overviewBlock)
        {
            _overviewBlock = overviewBlock;
            _ReloadMesh();
        }

        /// <summary>
        /// Игровой такт
        /// </summary>
        public virtual void Update()
        {
            _colorSky = _worldRenderer.ColorSky;
            _colorFog = _worldRenderer.ColorFog;
            _colorDown = _colorFog;
        }

        /// <summary>
        /// Прорисовка неба
        /// </summary>
        public virtual void DrawSky(CommandList commands, float timeIndex)
        {
            commands.SetPipeline(Debug.IsDrawVoxelLine ? _pipeSkyLine : _pipeSky);
            commands.SetUniformMatrix4(_uSkyView, Gi.MatrixView);
            commands.SetUniform(_uSkyColor, _colorSky.X, _colorSky.Y, _colorSky.Z, 1f);
            commands.SetUniform(_uSkyColorDown, _colorDown.X, _colorDown.Y, _colorDown.Z, 1f);
            commands.SetUniform(_uSkyColorFog, _colorFog.X, _colorFog.Y, _colorFog.Z, 1f);
            commands.SetVertexBuffer(0, _mesh);
            commands.Draw(_meshCount);

            _DrawAddElementSky(commands, timeIndex);
        }

        /// <summary>
        /// Прорисовка облака неба и дождь
        /// </summary>
        public virtual void DrawCloudsRain(CommandList commands, float timeIndex) { }

        /// <summary>
        /// Прорисовка облаков для карты теней
        /// </summary>
        public virtual void DrawCloudsDepthMap(CommandList commands, float timeIndex) { }

        /// <summary>
        /// Дополнительные элементы неба, рисуются в состоянии неба (без глубины или каркасом)
        /// </summary>
        protected virtual void _DrawAddElementSky(CommandList commands, float timeIndex) { }

        public virtual void Dispose()
        {
            _mesh.Dispose();
            _pipeSky.Dispose();
            _pipeSkyLine.Dispose();
            _shSky.Dispose();
        }
    }
}
