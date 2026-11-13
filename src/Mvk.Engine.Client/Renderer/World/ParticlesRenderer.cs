#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mvk.Core.Maths;
using Mvk.Engine.Entity.Particle;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Entity.Render;
using Mvk.Engine.Games;
using Mvk.Engine.Util;
using Mvk.Engine.World.Block;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.World
{
    /// <summary>
    /// Частицы на RHI. Перенос ParticlesRenderer оригинала: списки объёмных (до 1000) и плоских (до 4000)
    /// частиц, их такт (физика и жизнь), три общие сетки (квад, спрайт, куб) и шейдеры EntityParticle2d/3d.
    /// В оригинале юниформы каждой частицы искались по имени; здесь хэндлы получены один раз.
    /// </summary>
    public sealed class ParticlesRenderer : IDisposable
    {
        /// <summary>
        /// Максимальное количество частиц 3д
        /// </summary>
        private const int _MaxAmount3d = 1000;
        /// <summary>
        /// Максимальное количество частиц 2д
        /// </summary>
        private const int _MaxAmount2d = 4000;

        /// <summary>Сетка 2д-квада и куба: позиция (3 float) и признак (1 float), как MeshParticle оригинала</summary>
        private static readonly VertexBufferLayout _layout31 = new VertexBufferLayout(16,
            new VertexAttribute(0, VertexFormat.Float3, 0), new VertexAttribute(1, VertexFormat.Float1, 12));
        /// <summary>Сетка спрайта: позиция (2 float) и текстура (2 float), как MeshParticleTexture оригинала</summary>
        private static readonly VertexBufferLayout _layout22 = new VertexBufferLayout(16,
            new VertexAttribute(0, VertexFormat.Float2, 0), new VertexAttribute(1, VertexFormat.Float2, 8));

        private readonly GameBase _game;
        private readonly WorldRenderer _worldRenderer;
        private readonly RenderContext _context;
        private readonly List<EntityFX> _list3d = new List<EntityFX>(_MaxAmount3d);
        private readonly List<EntityFX> _list2d = new List<EntityFX>(_MaxAmount2d);

        private readonly IGpuBuffer _meshQuad, _meshSprite, _meshCube;
        private readonly int _countQuad, _countSprite, _countCube;
        private readonly IShader _shader2d, _shader3d;
        /// <summary>Квад и спрайт рисуются 2д-шейдером с разными раскладками вершин, куб 3д-шейдером</summary>
        private readonly IPipeline _pipeQuad, _pipeSprite, _pipeCube;
        private readonly UniformHandle _u2Param, _u2View, _u2Rotate, _u2Pos, _u2Color, _u2Uv, _u2Scale, _u2Light;
        private readonly UniformHandle _u3Param, _u3View, _u3Pos, _u3Color, _u3Scale, _u3Light;
        /// <summary>Матрица поворота спрайтов к камере (BindUniformParticle2d оригинала)</summary>
        private Mat4 _rotate = Mat4.Identity();

        private CommandList? _commands;
        private IPipeline? _bound;

        /// <summary>Фабрика рендеров сущностей (хост клиента), ею создаются частицы</summary>
        public IEntityRenderFactory? RenderFactory { get; set; }
        /// <summary>Отрисовка интерфейса: текстура частиц (Particles.png)</summary>
        public RenderMain? Gui { get; set; }

        public ParticlesRenderer(GameBase game, WorldRenderer worldRenderer)
        {
            _game = game;
            _worldRenderer = worldRenderer;
            _context = worldRenderer.Context;
            IGraphicsDevice device = _context.Device;

            float[] rectangle = _Rectangle(-.125f, .125f);
            float[] cube = _Cube(-.125f, 0, .125f, .25f);
            _meshQuad = _Buffer(device, rectangle);
            _meshSprite = _Buffer(device, rectangle);
            _meshCube = _Buffer(device, cube);
            _countQuad = _countSprite = rectangle.Length / 4 / 4 * 6;
            _countCube = cube.Length / 4 / 4 * 6;
            _context.Quads.Ensure(cube.Length / 4 / 4);

            _shader2d = device.CreateShader(new ShaderDescription("EntityParticle2d",
                FileAssets.ReadStringToShader("EntityParticle2d.vsh"), FileAssets.ReadStringToShader("EntityParticle2d.fsh"),
                new[] { "v_position", "v_texCoord" }, new[] { "u_texture0", "light_map" }));
            _shader3d = device.CreateShader(new ShaderDescription("EntityParticle3d",
                FileAssets.ReadStringToShader("EntityParticle3d.vsh"), FileAssets.ReadStringToShader("EntityParticle3d.fsh"),
                new[] { "v_position", "v_color" }, new[] { "light_map" }));
            _u2Param = _shader2d.GetUniform("param");
            _u2View = _shader2d.GetUniform("view");
            _u2Rotate = _shader2d.GetUniform("rotateMatrix");
            _u2Pos = _shader2d.GetUniform("pos");
            _u2Color = _shader2d.GetUniform("color");
            _u2Uv = _shader2d.GetUniform("uv");
            _u2Scale = _shader2d.GetUniform("scale");
            _u2Light = _shader2d.GetUniform("light");
            _u3Param = _shader3d.GetUniform("param");
            _u3View = _shader3d.GetUniform("view");
            _u3Pos = _shader3d.GetUniform("pos");
            _u3Color = _shader3d.GetUniform("color");
            _u3Scale = _shader3d.GetUniform("scale");
            _u3Light = _shader3d.GetUniform("light");

            // Состояние оригинала после прохода сущностей: глубина, отсечение задних граней, смешивание по альфе
            RasterState raster = RasterState.Default with { Cull = CullMode.Back };
            _pipeQuad = device.CreatePipeline(new PipelineDescription(_shader2d, _layout31) { Raster = raster, Blend = BlendState.Alpha });
            _pipeSprite = device.CreatePipeline(new PipelineDescription(_shader2d, _layout22) { Raster = raster, Blend = BlendState.Alpha });
            _pipeCube = device.CreatePipeline(new PipelineDescription(_shader3d, _layout31) { Raster = raster, Blend = BlendState.Alpha });
        }

        private static IGpuBuffer _Buffer(IGraphicsDevice device, float[] data)
        {
            IGpuBuffer buffer = device.CreateBuffer(BufferKind.Vertex, BufferUsage.Static);
            buffer.SetData<float>(data);
            return buffer;
        }

        #region Figure

        private static float[] _Rectangle(float f1, float f2) => new float[] {
                f1, f1, 0, 0,
                f2, f1, 1, 0,
                f2, f2, 1, 1,
                f1, f2, 0, 1
            };

        private static float[] _Cube(float w1, float h1, float w2, float h2)
        {
            float s1 = BlockShading.DarkeningSideB;
            float s2 = BlockShading.DarkeningSideA;
            float s3 = BlockShading.DarkeningSideDown;
            return new float[]
            {
                w1, h2, w2, 1,
                w2, h2, w2, 1,
                w2, h2, w1, 1,
                w1, h2, w1, 1,

                w1, h1, w1, s3,
                w2, h1, w1, s3,
                w2, h1, w2, s3,
                w1, h1, w2, s3,

                w1, h1, w2, s1,
                w2, h1, w2, s1,
                w2, h2, w2, s1,
                w1, h2, w2, s1,

                w2, h1, w1, s1,
                w1, h1, w1, s1,
                w1, h2, w1, s1,
                w2, h2, w1, s1,

                w2, h1, w2, s2,
                w2, h1, w1, s2,
                w2, h2, w1, s2,
                w2, h2, w2, s2,

                w1, h1, w1, s2,
                w1, h1, w2, s2,
                w1, h2, w2, s2,
                w1, h2, w1, s2
            };
        }

        #endregion

        /// <summary>
        /// Объект игрока для клиента
        /// </summary>
        public PlayerClientOwner Player
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _game.Player;
        }

        /// <summary>
        /// Заспавнить частицу
        /// </summary>
        public void Spawn(ushort particleId, Vector3 pos, Vector3 motion, int parameter)
        {
            EntityFX entity = Ce.EntitiesFX.CreateEntityClient(particleId, _game.World, RenderFactory!, parameter);
            entity.InitRun(pos, motion);
            if (entity.IsCube)
            {
                while (_list3d.Count >= _MaxAmount3d) _list3d.RemoveAt(0);
                _list3d.Add(entity);
            }
            else
            {
                while (_list2d.Count >= _MaxAmount2d) _list2d.RemoveAt(0);
                _list2d.Add(entity);
            }
        }

        /// <summary>
        /// Метод для прорисовки кадра
        /// </summary>
        public void Draw(CommandList commands, float timeIndex)
        {
            _commands = commands;
            _bound = null;
            ITexture lightMap = _worldRenderer.LightMap.Texture;

            // BindUniformParticle3d оригинала
            commands.SetTexture(0, lightMap);
            _Bind(_pipeCube);
            commands.SetUniformMatrix4(_u3View, Gi.MatrixView);
            int count = _list3d.Count;
            for (int i = 0; i < count; i++)
            {
                _list3d[i].Render.Draw(timeIndex, _game.DeltaTimeFrame);
            }

            // Текстура частиц и BindUniformParticle2d оригинала
            ITexture? particles = Gui?.TextureParticles;
            if (particles != null) commands.SetTexture(0, particles);
            commands.SetTexture(1, lightMap);
            float pitch = _game.Player.RotationFramePitch;
            if (_game.Player.ViewCamera == EnumViewCamera.Front) pitch += Glm.Pi;
            _rotate = Mat4.Identity();
            _rotate.RotateY(-_game.Player.RotationFrameYaw);
            _rotate.RotateX(pitch);
            _bound = null;
            count = _list2d.Count;
            for (int i = 0; i < count; i++)
            {
                _list2d[i].Render.Draw(timeIndex, _game.DeltaTimeFrame);
            }
        }

        private void _Bind(IPipeline pipeline)
        {
            if (_bound == pipeline) return;
            _bound = pipeline;
            _commands!.SetPipeline(pipeline);
            if (pipeline != _pipeCube)
            {
                // Юниформы кадра 2д-шейдера: вид и поворот к камере
                _commands.SetUniformMatrix4(_u2View, Gi.MatrixView);
                _commands.SetUniform(_u2Rotate, in _rotate);
            }
        }

        /// <summary>
        /// Нарисовать одну частицу (юниформы EntityRenderParticle оригинала и сетка по виду частицы)
        /// </summary>
        public void DrawParticle(EntityFX entity, float timeIndex, float lightBlock, float lightSky)
        {
            CommandList commands = _commands!;
            float x = entity.GetPosFrameX(timeIndex) - _game.Player.PosFrameX;
            float y = entity.GetPosFrameY(timeIndex) - _game.Player.PosFrameY;
            float z = entity.GetPosFrameZ(timeIndex) - _game.Player.PosFrameZ;
            Vector4 color = entity.Color;
            if (entity.IsCube)
            {
                _Bind(_pipeCube);
                commands.SetUniform(_u3Pos, x, y, z);
                commands.SetUniform(_u3Color, color.X, color.Y, color.Z, color.W);
                commands.SetUniform(_u3Light, lightBlock, lightSky);
                commands.SetUniform(_u3Scale, entity.Scale);
                commands.SetUniform(_u3Param, entity.Param);
                _DrawMesh(commands, _meshCube, _countCube);
                return;
            }
            bool sprite = entity.TypeDraw == EnumParticleDraw.Sprite;
            _Bind(sprite ? _pipeSprite : _pipeQuad);
            commands.SetUniform(_u2Pos, x, y, z);
            commands.SetUniform(_u2Color, color.X, color.Y, color.Z, color.W);
            commands.SetUniform(_u2Light, lightBlock, lightSky);
            commands.SetUniform(_u2Scale, entity.Scale);
            commands.SetUniform(_u2Param, entity.Param);
            if (entity.IsSprite)
            {
                Vector4 uv = entity.Uv;
                commands.SetUniform(_u2Uv, uv.X, uv.Y, uv.Z, uv.W);
            }
            if (sprite) _DrawMesh(commands, _meshSprite, _countSprite);
            else _DrawMesh(commands, _meshQuad, _countQuad);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _DrawMesh(CommandList commands, IGpuBuffer mesh, int countIndices)
        {
            commands.SetVertexBuffer(0, mesh);
            commands.SetIndexBuffer(_context.Quads.Buffer, IndexFormat.UInt32);
            commands.DrawIndexed(countIndices);
        }

        /// <summary>
        /// Игровой такт: физика частиц и их жизнь
        /// </summary>
        public void OnTick(float deltaTime)
        {
            _Update(_list3d, deltaTime);
            _Update(_list2d, deltaTime);
        }

        private void _Update(List<EntityFX> list, float deltaTime)
        {
            int count = list.Count - 1;
            for (int i = count; i >= 0; i--)
            {
                if (list[i].IsDead)
                {
                    list.RemoveAt(i);
                }
                else
                {
                    list[i].UpdateClient(_game.World, deltaTime);
                    list[i].Update();
                }
            }
        }

        /// <summary>
        /// Очистить частицы
        /// </summary>
        public void Clear()
        {
            _list3d.Clear();
            _list2d.Clear();
        }

        public void Dispose()
        {
            _pipeQuad.Dispose();
            _pipeSprite.Dispose();
            _pipeCube.Dispose();
            _shader2d.Dispose();
            _shader3d.Dispose();
            _meshQuad.Dispose();
            _meshSprite.Dispose();
            _meshCube.Dispose();
            Clear();
        }

        public override string ToString() => "Fx2d:" + _list2d.Count + " Fx3d:" + _list3d.Count;
    }
}
