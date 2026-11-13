"""Шов клиента 1: построение сеток чанков без глобального Gi и без GL.
- ChunkRender наследует логический ChunkClient из Engine (частицы обязательных блоков уже там).
- Буферы вершин и рендеры блоков (в оригинале статические поля Gi) живут в объекте ChunkMesher,
  которым владеет WorldRenderer: глобального состояния рендера больше нет.
- TextureLightMap: расчёт как в оригинале, загрузка в текстуру через RHI."""
import os, re
from portlib import load, save, rep, sub, remove_member, remove_field, add_using, fail, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

# ---------- ChunkRender ----------
rel = r"Renderer\World\ChunkRender.cs"
t = load(rel, C)
t = rep(t, "public class ChunkRender : ChunkBase", "public class ChunkRender : ChunkClient", rel)
t = remove_field(t, "public int Distance;", rel)
t = remove_field(t, "private readonly WorldClient _worldClient;", rel)
t = remove_field(t, "private readonly ListMessy<ushort>[] _listRequiredParticles;", rel)
t = remove_field(t, "private int _countRequiredParticles;", rel)
t = rep(t, """        public ChunkRender(WorldClient worldClient, int chunkPosX, int chunkPosY) 
            : base(worldClient, worldClient.ChunkPr.Settings, chunkPosX, chunkPosY)
        {""", """        /// <summary>
        /// Рендер мира: устройство для сеток и построитель сеток
        /// </summary>
        private readonly WorldRenderer _render;
        /// <summary>
        /// Построитель сеток (буферы вершин и рендеры блоков), в оригинале статические поля Gi
        /// </summary>
        private readonly ChunkMesher _mesher;

        public ChunkRender(WorldClient worldClient, WorldRenderer render, int chunkPosX, int chunkPosY) 
            : base(worldClient, chunkPosX, chunkPosY)
        {
            _render = render;
            _mesher = render.Mesher;""", rel)
t = rep(t, "            _worldClient = worldClient;\n", "", rel)
t = rep(t, "            _listRequiredParticles = new ListMessy<ushort>[NumberSections];\n", "", rel)
t = rep(t, "                _listRequiredParticles[index] = new ListMessy<ushort>();\n", "", rel)
t = rep(t, "new MeshBlocks(_worldClient.WorldRender.GetOpenGL())", "new MeshBlocks(_render.Context)", rel, count=2)
t = rep(t, "public bool ModifiedToRenderAlpha(int y)", "public override bool ModifiedToRenderAlpha(int y)", rel)
t = rep(t, "public void DisposeMesh()", "public override void DisposeMesh()", rel)
# В оригинале опечатка: вторая проверка тоже _meshDense
t = rep(t, """            if (_meshDense != null)
            {
                _meshAlpha.Dispose();
            }""", """            if (_meshAlpha != null)
            {
                _meshAlpha.Dispose();
            }""", rel)
t = rep(t, "public bool IsModifiedRender => _meshDense.IsModifiedRender;",
        "public bool IsModifiedRender => _meshDense != null && _meshDense.IsModifiedRender;", rel)
t = rep(t, "public bool IsModifiedRenderAlpha => _meshAlpha.IsModifiedRender;",
        "public bool IsModifiedRenderAlpha => _meshAlpha != null && _meshAlpha.IsModifiedRender;", rel)
t = rep(t, "        public override bool ModifiedToRenderAlpha(int y)\n        {\n            if (y >= 0",
        "        public override bool ModifiedToRenderAlpha(int y)\n        {\n            if (_meshAlpha != null && y >= 0", rel)
# Отрисовка записывается в список команд
t = rep(t, "public void DrawDense() => _meshDense.Draw();",
        "public void DrawDense(CommandList commands) => _meshDense.Draw(commands);", rel)
t = rep(t, "public void DrawAlpha() => _meshAlpha.Draw();",
        "public void DrawAlpha(CommandList commands) => _meshAlpha.Draw(commands);", rel)
# Gi -> ChunkMesher
t = rep(t, "Gi.Block.BlockRender", "_mesher.Of(Gi.Block)", rel)
for name in ["VertexDense", "VertexAlpha", "BlockRendFull", "BlockAlphaRendFull", "BlockLiquidAlphaRendFull"]:
    t = rep(t, "Gi." + name, "_mesher." + name, rel)
t = rep(t, "_worldClient.ChunkPrClient.GetChunkRender(", "(ChunkRender)_worldClient.ChunkPrClient.GetChunkClient(", rel)
# Уже есть в ChunkClient
t = remove_member(t, "public void RequiredRandomDisplayTick()", rel)
t = remove_member(t, "protected override void _SetRequiredRandomTick(", rel)
t = remove_member(t, "protected override void _UpSectionRequiredRandomTick(", rel)
t = rep(t, """" MD:" + _meshDense.Status + " MA:" + _meshAlpha.Status""",
        """" MD:" + _meshDense?.Status + " MA:" + _meshAlpha?.Status""", rel)
t = add_using(t, "Mvk.Rendering")
save(rel, t, C)

# ---------- TextureLightMap ----------
rel = r"Renderer\World\TextureLightMap.cs"
t = load(rel, C)
i = t.find("        /// <summary>\n        /// Обновить текстуру\n        /// </summary>\n        private void _UpdateLightmap()")
j = t.rfind("    }\n}")
if i < 0 or j < 0: fail(rel + ": _UpdateLightmap not found")
t = t[:i] + """        /// <summary>
        /// Загрузить карту света в текстуру 16x16.
        /// В оригинале GL_CLAMP: в core-профиле это ошибка, оставался REPEAT; координаты света
        /// всегда в центрах текселей (n/16 + 1/32), поэтому результат с ClampToEdge тот же.
        /// </summary>
        private void _UpdateLightmap()
        {
            if (Texture == null)
            {
                Texture = _device.CreateTexture(TextureDescription.Texture2D(16, 16,
                    new SamplerState(TextureFilter.Linear, TextureFilter.Linear, MipFilter.None, TextureWrap.ClampToEdge)));
            }
            Texture.SetData(_buffer, PixelFormat.Bgra8);
        }

        public void Dispose()
        {
            Texture?.Dispose();
            Texture = null;
        }
""" + t[j:]
t = rep(t, "public class TextureLightMap", "public class TextureLightMap : System.IDisposable", rel)
t = remove_field(t, "private uint _locationLightMap = 0;", rel)
t = rep(t, "        private readonly GL gl;\n", """        private readonly IGraphicsDevice _device;
        /// <summary>
        /// Текстура карты света (пусто до первого Update)
        /// </summary>
        public ITexture Texture { get; private set; }
""", rel)
t = rep(t, "public TextureLightMap(GL gl)", "public TextureLightMap(IGraphicsDevice device)", rel)
t = rep(t, "this.gl = gl;", "_device = device;", rel)
t = re.sub(r"^using WinGL(\.[\w.]+)?;\n", "", t, flags=re.M)
t = add_using(t, "Mvk.Rendering")
save(rel, t, C)

# ---------- остальные файлы: убрать using WinGL.OpenGL ----------
for d, _, fs in os.walk(os.path.join(C, "Renderer")):
    for f in fs:
        if not f.endswith(".cs"): continue
        r = os.path.relpath(os.path.join(d, f), C)
        t = load(r, C)
        u = re.sub(r"^using WinGL\.OpenGL;\n", "", t, flags=re.M)
        if u != t: save(r, u, C)
print("client_seam1 ok")
