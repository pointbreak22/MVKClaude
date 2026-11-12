using System;
using System.Collections.Generic;
using System.IO;
using Mvk.Core.Maths;
using Mvk.Engine.Util;
using Mvk.Rendering;

namespace Mvk.Desktop
{
    /// <summary>
    /// Тестовая сцена этапа 4: кубы с текстурами блоков оригинала.
    /// Проверяет весь путь RHI: массив текстур с мипмапами, два вершинных буфера (float и int, как MeshBlocks),
    /// индексы, непрозрачный и прозрачный конвейеры, каркасный режим, 2D-текстуру поверх сцены.
    /// Работает только через интерфейсы Mvk.Rendering и не знает про OpenGL.
    /// </summary>
    internal sealed class TestScene : IDisposable
    {
        private const string BlockVertex = @"#version 330 core
in vec3 v_position;
in vec2 v_texCoord;
in int v_face;
in int v_light;
out vec3 a_uvw;
out float a_light;
uniform mat4 projView;
uniform mat4 model;
uniform int layerTop;
uniform int layerBottom;
uniform int layerSide;
void main()
{
    int layer = v_face == 0 ? layerTop : (v_face == 1 ? layerBottom : layerSide);
    a_uvw = vec3(v_texCoord, float(layer));
    a_light = float(v_light) / 255.0;
    gl_Position = projView * model * vec4(v_position, 1.0);
}";

        private const string BlockFragment = @"#version 330 core
in vec3 a_uvw;
in float a_light;
out vec4 f_color;
uniform sampler2DArray blocks;
uniform float alpha;
void main()
{
    vec4 c = texture(blocks, a_uvw);
    if (c.a < 0.1) discard;
    f_color = vec4(c.rgb * a_light, c.a * alpha);
}";

        private const string GuiVertex = @"#version 330 core
in vec2 v_position;
in vec2 v_texCoord;
out vec2 a_uv;
uniform mat4 projView;
void main()
{
    a_uv = v_texCoord;
    gl_Position = projView * vec4(v_position, 0.0, 1.0);
}";

        private const string GuiFragment = @"#version 330 core
in vec2 a_uv;
out vec4 f_color;
uniform sampler2D image;
void main()
{
    f_color = texture(image, a_uv);
}";

        /// <summary>Блок сцены: слои текстур сверху, снизу, сбоку</summary>
        private readonly record struct Cube(int Top, int Bottom, int Side);

        private readonly IGraphicsDevice _device;
        private readonly CommandList _commands = new CommandList();

        private readonly IShader _blockShader;
        private readonly IShader _guiShader;
        private readonly IPipeline _opaque;
        private readonly IPipeline _wireframe;
        private readonly IPipeline _translucent;
        private readonly IPipeline _gui;
        private readonly IGpuBuffer _cubeFloats;
        private readonly IGpuBuffer _cubeInts;
        private readonly IGpuBuffer _cubeIndices;
        private readonly IGpuBuffer _guiVertices;
        private readonly ITexture _blocks;
        private readonly ITexture _sun;

        private readonly UniformHandle _uProjView, _uModel, _uTop, _uBottom, _uSide, _uAlpha, _uGuiProjView;

        private readonly List<Cube> _cubes = new();
        private readonly Cube _glass;
        private readonly float[] _guiData = new float[24];

        /// <summary>Каркасный режим (F3)</summary>
        public bool Wireframe { get; set; }
        /// <summary>Угол поворота камеры вокруг сцены, радианы</summary>
        public float CameraYaw { get; set; } = 0.6f;

        public TestScene(IGraphicsDevice device, string assetsDir)
        {
            _device = device;
            string blocksDir = Path.Combine(assetsDir, "Mvk2Textures", "Blocks");

            // Массив текстур 16x16, как слои будущего атласа
            string[] names =
            {
                "Stone", "Granite", "Limestone", "Cobblestone", "Sand", "Gravel", "Clay", "Loam", "Humus",
                "Bedrock", "GrassTop", "GrassSide", "Ore/OreCoal", "Ore/OreIron", "Ore/OreGold", "Ore/OreDiamond",
                "Ore/OreEmerald", "Ore/OreRuby", "Ore/OreSapphire", "Ore/OreSulfur", "BrolUp", "BrolSide",
                "BrolDown", "Glass", "GlassTranslucent", "Debug"
            };
            var index = new Dictionary<string, int>();
            _blocks = device.CreateTexture(new TextureDescription(16, 16, names.Length, 5, SamplerState.PointMipRepeat));
            for (int i = 0; i < names.Length; i++)
            {
                BufferedImage image = BufferedFileImage.FileToBufferedImage(Path.Combine(blocksDir, names[i] + ".png"));
                if (image.Width != 16 || image.Height != 16)
                    throw new InvalidDataException(names[i] + ": ожидалась текстура 16x16");
                _blocks.SetData(image.Buffer, PixelFormat.Bgra8, 0, i);
                index[names[i]] = i;
            }
            _blocks.GenerateMipmaps();

            BufferedImage sun = BufferedFileImage.FileToBufferedImage(Path.Combine(assetsDir, "Mvk2Textures", "Sun.png"));
            _sun = device.CreateTexture(TextureDescription.Texture2D(sun.Width, sun.Height, SamplerState.PointClamp));
            _sun.SetData(sun.Buffer, PixelFormat.Bgra8);

            _blockShader = device.CreateShader(new ShaderDescription("TestBlock", BlockVertex, BlockFragment,
                new[] { "v_position", "v_texCoord", "v_face", "v_light" }, new[] { "blocks" }));
            _uProjView = _blockShader.GetUniform("projView");
            _uModel = _blockShader.GetUniform("model");
            _uTop = _blockShader.GetUniform("layerTop");
            _uBottom = _blockShader.GetUniform("layerBottom");
            _uSide = _blockShader.GetUniform("layerSide");
            _uAlpha = _blockShader.GetUniform("alpha");

            _guiShader = device.CreateShader(new ShaderDescription("TestGui", GuiVertex, GuiFragment,
                new[] { "v_position", "v_texCoord" }, new[] { "image" }));
            _uGuiProjView = _guiShader.GetUniform("projView");

            // Раскладка как у MeshBlocks: слот 0 float (pos3, uv2), слот 1 int
            var floats = new VertexBufferLayout(20,
                new VertexAttribute(0, VertexFormat.Float3, 0),
                new VertexAttribute(1, VertexFormat.Float2, 12));
            var ints = new VertexBufferLayout(8,
                new VertexAttribute(2, VertexFormat.Int1, 0),
                new VertexAttribute(3, VertexFormat.Int1, 4));
            RasterState solid = RasterState.Default with { Cull = CullMode.Back };
            _opaque = device.CreatePipeline(new PipelineDescription(_blockShader, floats, ints) { Raster = solid });
            _wireframe = device.CreatePipeline(new PipelineDescription(_blockShader, floats, ints)
            {
                Raster = RasterState.Default with { Fill = FillMode.Wireframe }
            });
            _translucent = device.CreatePipeline(new PipelineDescription(_blockShader, floats, ints)
            {
                Raster = solid with { DepthWrite = false },
                Blend = BlendState.Alpha
            });
            _gui = device.CreatePipeline(new PipelineDescription(_guiShader,
                new VertexBufferLayout(16,
                    new VertexAttribute(0, VertexFormat.Float2, 0),
                    new VertexAttribute(1, VertexFormat.Float2, 8)))
            {
                Raster = RasterState.NoDepth,
                // Солнце оригинала без альфы на чёрном фоне: рисуется сложением, как в SkyIslandRender
                Blend = BlendState.Additive
            });

            BuildCube(out float[] cubeFloats, out int[] cubeInts, out ushort[] cubeIndices);
            _cubeFloats = device.CreateBuffer(BufferKind.Vertex, BufferUsage.Static);
            _cubeFloats.SetData<float>(cubeFloats);
            _cubeInts = device.CreateBuffer(BufferKind.Vertex, BufferUsage.Static);
            _cubeInts.SetData<int>(cubeInts);
            _cubeIndices = device.CreateBuffer(BufferKind.Index, BufferUsage.Static);
            _cubeIndices.SetData<ushort>(cubeIndices);
            _guiVertices = device.CreateBuffer(BufferKind.Vertex, BufferUsage.Dynamic);

            // 5x5 блоков: первый ряд трава, дальше камни, руды и бревно
            int loam = index["Loam"];
            _cubes.Add(new Cube(index["GrassTop"], loam, index["GrassSide"]));
            _cubes.Add(new Cube(index["BrolUp"], index["BrolDown"], index["BrolSide"]));
            foreach (string name in new[]
            {
                "Stone", "Granite", "Limestone", "Cobblestone", "Sand", "Gravel", "Clay", "Loam", "Humus", "Bedrock",
                "Ore/OreCoal", "Ore/OreIron", "Ore/OreGold", "Ore/OreDiamond", "Ore/OreEmerald", "Ore/OreRuby",
                "Ore/OreSapphire", "Ore/OreSulfur", "Glass", "Debug"
            })
            {
                int i = index[name];
                _cubes.Add(new Cube(i, i, i));
            }
            while (_cubes.Count < 25) _cubes.Add(_cubes[_cubes.Count - 22]);
            int glass = index["GlassTranslucent"];
            _glass = new Cube(glass, glass, glass);
        }

        /// <summary>Куб со стороной 1 с центром в нуле: 6 граней по 4 вершины</summary>
        private static void BuildCube(out float[] floats, out int[] ints, out ushort[] indices)
        {
            // Начало грани, ось U, ось V (нормаль = U x V наружу), номер грани для шейдера, яркость
            (Vector3 o, Vector3 u, Vector3 v, int face, int light)[] faces =
            {
                (new Vector3(-.5f, .5f, .5f), new Vector3(1, 0, 0), new Vector3(0, 0, -1), 0, 255),
                (new Vector3(-.5f, -.5f, -.5f), new Vector3(1, 0, 0), new Vector3(0, 0, 1), 1, 128),
                (new Vector3(-.5f, -.5f, .5f), new Vector3(1, 0, 0), new Vector3(0, 1, 0), 2, 204),
                (new Vector3(.5f, -.5f, -.5f), new Vector3(-1, 0, 0), new Vector3(0, 1, 0), 2, 204),
                (new Vector3(.5f, -.5f, .5f), new Vector3(0, 0, -1), new Vector3(0, 1, 0), 2, 153),
                (new Vector3(-.5f, -.5f, -.5f), new Vector3(0, 0, 1), new Vector3(0, 1, 0), 2, 153)
            };
            floats = new float[6 * 4 * 5];
            ints = new int[6 * 4 * 2];
            indices = new ushort[6 * 6];
            float[] us = { 0, 1, 1, 0 };
            float[] vs = { 0, 0, 1, 1 };
            for (int f = 0; f < 6; f++)
            {
                for (int k = 0; k < 4; k++)
                {
                    Vector3 p = faces[f].o + faces[f].u * us[k] + faces[f].v * vs[k];
                    int n = f * 4 + k;
                    floats[n * 5] = p.X;
                    floats[n * 5 + 1] = p.Y;
                    floats[n * 5 + 2] = p.Z;
                    floats[n * 5 + 3] = us[k];
                    floats[n * 5 + 4] = 1f - vs[k]; // строка 0 картинки это верх грани
                    ints[n * 2] = faces[f].face;
                    ints[n * 2 + 1] = faces[f].light;
                }
                // Тот же порядок индексов, что в MeshBase оригинала: 0,1,2, 0,2,3
                int b = f * 4;
                int i = f * 6;
                indices[i] = (ushort)b; indices[i + 1] = (ushort)(b + 1); indices[i + 2] = (ushort)(b + 2);
                indices[i + 3] = (ushort)b; indices[i + 4] = (ushort)(b + 2); indices[i + 5] = (ushort)(b + 3);
            }
        }

        /// <summary>Нарисовать кадр</summary>
        /// <param name="time">Время сцены в секундах (задаёт вращение блоков)</param>
        public void Render(int width, int height, float time)
        {
            if (width <= 0 || height <= 0) return;
            CommandList c = _commands;
            c.Reset();
            c.SetViewport(0, 0, width, height);
            c.Clear(0.53f, 0.74f, 0.93f, 1f);

            Mat4 proj = Glm.Perspective(Glm.Radians(60f), (float)width / height, 0.1f, 100f);
            var eye = new Vector3(Glm.Sin(CameraYaw) * 9f, 6f, Glm.Cos(CameraYaw) * 9f);
            Mat4 view = Glm.LookAt(eye, new Vector3(0, 0, 0), new Vector3(0, 1, 0));
            Mat4 projView = proj * view;

            c.SetPipeline(Wireframe ? _wireframe : _opaque);
            c.SetVertexBuffer(0, _cubeFloats);
            c.SetVertexBuffer(1, _cubeInts);
            c.SetIndexBuffer(_cubeIndices, IndexFormat.UInt16);
            c.SetTexture(0, _blocks);
            c.SetUniform(_uProjView, projView);
            c.SetUniform(_uAlpha, 1f);
            for (int i = 0; i < _cubes.Count; i++)
            {
                int gx = i % 5 - 2;
                int gz = i / 5 - 2;
                DrawCube(c, _cubes[i], gx * 1.6f, 0f, gz * 1.6f, time * 0.5f + i * 0.4f, 1f);
            }

            // Прозрачное стекло рисуется после непрозрачного и не пишет глубину
            if (!Wireframe)
            {
                c.SetPipeline(_translucent);
                c.SetUniform(_uProjView, projView);
                c.SetUniform(_uAlpha, 0.75f);
            }
            DrawCube(c, _glass, 0f, 1.7f, 0f, -time * 0.5f, 1.4f);

            // Картинка в углу: 2D-текстура, динамический буфер, без глубины
            float s = Math.Min(width, height) * 0.2f;
            FillQuad(_guiData, 16f, 16f, s, s);
            _guiVertices.SetData<float>(_guiData);
            c.SetPipeline(_gui);
            c.SetVertexBuffer(0, _guiVertices);
            c.SetTexture(0, _sun);
            c.SetUniform(_uGuiProjView, Glm.Ortho(0, width, height, 0));
            c.Draw(6);

            _device.Submit(c);
        }

        private void DrawCube(CommandList c, Cube cube, float x, float y, float z, float angle, float scale)
        {
            Mat4 model = Mat4.Identity();
            model.Translate(x, y, z);
            model.RotateY(angle);
            if (scale != 1f) model.Scale(scale, scale, scale);
            c.SetUniform(_uModel, model);
            c.SetUniform(_uTop, cube.Top);
            c.SetUniform(_uBottom, cube.Bottom);
            c.SetUniform(_uSide, cube.Side);
            c.DrawIndexed(36);
        }

        /// <summary>Два треугольника: x, y, u, v на вершину</summary>
        private static void FillQuad(float[] d, float x, float y, float w, float h)
        {
            ReadOnlySpan<float> q = stackalloc float[]
            {
                x, y, 0, 0,
                x, y + h, 0, 1,
                x + w, y + h, 1, 1,
                x, y, 0, 0,
                x + w, y + h, 1, 1,
                x + w, y, 1, 0
            };
            q.CopyTo(d);
        }

        public void Dispose()
        {
            _opaque.Dispose();
            _wireframe.Dispose();
            _translucent.Dispose();
            _gui.Dispose();
            _cubeFloats.Dispose();
            _cubeInts.Dispose();
            _cubeIndices.Dispose();
            _guiVertices.Dispose();
            _blocks.Dispose();
            _sun.Dispose();
            _blockShader.Dispose();
            _guiShader.Dispose();
        }
    }
}
