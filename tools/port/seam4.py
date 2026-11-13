"""Шов 3/4/5/8 (ADR 0003): логика клиента (GameBase, WorldClient, игрок, чанк) отвязана от окна и рендера.
Всё, что раньше шло в WindowMain/WorldRenderer/Hud/ChatList, теперь идёт через IGameClientHost."""
import os, re
from portlib import load, save, rep, sub, remove_member, remove_field, add_using, fail, ORIG

def from_orig(src_rel, dst_rel, ns_old, ns_new):
    t = load(src_rel, ORIG)
    t = rep(t, "namespace " + ns_old, "namespace " + ns_new, src_rel)
    t = re.sub(r"^using WinGL\.Util;", "using Mvk.Core.Maths;", t, flags=re.M)
    save(dst_rel, t)

# A. Клавиши и кнопки мыши (были в WinGL) — это данные ввода, не окно
from_orig(r"WinGL\Actions\Keys.cs", r"Actions\Keys.cs", "WinGL.Actions", "Mvk.Engine.Actions")
from_orig(r"WinGL\Actions\MouseButton.cs", r"Actions\MouseButton.cs", "WinGL.Actions", "Mvk.Engine.Actions")
# N. Цвета биомов и геометрия прямоугольников — данные, без GL
from_orig(r"Vge\Renderer\World\ColorsBiom.cs", r"World\ColorsBiom.cs", "Vge.Renderer.World", "Mvk.Engine.World")
from_orig(r"Vge\Renderer\RenderFigure.cs", r"Util\RenderFigure.cs", "Vge.Renderer", "Mvk.Engine.Util")
for rel in [r"Util\Options.cs", r"Games\GameBase.cs", r"Games\GameModClient.cs"]:
    save(rel, add_using(load(rel), "Mvk.Engine.Actions"))

# B. Gi: остаётся состояние вида (размеры, Si, матрицы), уходят буферы вершин и рендеры блоков
rel = "Gi.cs"; t = load(rel)
for f in ["public readonly static VertexBlockBuffer VertexDense", "public readonly static VertexBlockBuffer VertexAlpha",
          "public readonly static BlockRenderFull BlockRendFull", "public readonly static BlockRenderFull BlockAlphaRendFull",
          "public readonly static BlockRenderLiquid BlockLiquidAlphaRendFull",
          "public static float DarkeningSideDown", "public static float DarkeningSideA", "public static float DarkeningSideB"]:
    t = remove_field(t, f, rel)
t = sub(t, r"\n[ \t]*//public readonly static float\[\] LightPoles[^\n]*", "", rel)
t = sub(t, r"\n[ \t]*public readonly static float\[\] LightPoles = new float\[\] \{.*?\}; // С Diffuse", "", rel, re.S)
t = sub(t, r"\n[ \t]*/// <summary>\n[ \t]*/// Затемнение стороны от стороны блока\n[ \t]*/// </summary>", "", rel)
save(rel, t)

# C. ChunkRender -> ChunkClient (логическая часть клиентского чанка), меш создаёт хост
for rel in [r"World\WorldClient.cs", r"Entity\Player\PlayerClientOwner.cs", r"World\Chunk\ChunkProviderClient.cs"]:
    t = load(rel)
    t = re.sub(r"\bGetChunkRender\b", "GetChunkClient", t)
    t = re.sub(r"\bChunkRender\b", "ChunkClient", t)
    save(rel, t)
rel = r"World\Chunk\ChunkProviderClient.cs"; t = load(rel)
t = rep(t, "new ChunkClient(_worldClient, chx, chy)", "_worldClient.Game.Host.CreateChunk(_worldClient, chx, chy)", rel, 1)
save(rel, t)

# D. GameBase
rel = r"Games\GameBase.cs"; t = load(rel)
t = rep(t, "public abstract class GameBase : Warp", "public abstract class GameBase : IDisposable", rel)
t = rep(t, "        public readonly GameModClient ModClient;\n",
        "        public readonly GameModClient ModClient;\n"
        "        /// <summary>\n        /// Хост клиента: окно, экраны, рендер мира, звук (реализует Mvk.Engine.Client)\n        /// </summary>\n"
        "        public readonly IGameClientHost Host;\n", rel)
t = remove_member(t, "public HudBase Hud { get; protected set; }", rel)
t = remove_field(t, "public readonly WorldRenderer WorldRender;", rel)
t = rep(t, "public GameBase(WindowMain window, GameModClient gameModClient) : base(window)\n        {\n",
        "public GameBase(IGameClientHost host, GameModClient gameModClient)\n        {\n            Host = host;\n", rel)
t = rep(t, "WorldRender = new WorldRenderer(this);", "Host.Attach(this);", rel)
t = rep(t, "=> window.DeltaTimeFrame;", "=> Host.DeltaTimeFrame;", rel)
t = rep(t, "public float DeltaTime => window.DeltaTime;",
        "public float DeltaTime => Host.DeltaTime;\n\n"
        "        /// <summary>\n        /// Сохранить опции\n        /// </summary>\n"
        "        public void OptionsSave() => Host.OptionsSave();", rel)
t = rep(t, "window.LScreen.InGameMenu();", "Host.OpenInGameMenu();", rel)
t = rep(t, "WorldRender.GameStarting();", "Host.OnGameStarting();", rel)
t = rep(t, "window.ScreenClose();\n            Hud = ModClient.CreateHud();", "Host.ScreenClose();\n            Host.OnStartedGame();", rel)
t = rep(t, "window.CursorShow(", "Host.CursorShow(", rel)
t = rep(t, "window.SetCursorPosition(", "Host.SetCursorPosition(", rel)
t = sub(t, r"public override void (On\w+)\(", r"public virtual void \1(", rel)
t = rep(t, "WorldRender.OnTick(deltaTime);", "Host.OnTick(deltaTime);", rel)
t = sub(t, r"\n[ \t]*// Тут индикация если имеется\n[ \t]*Hud\?\.OnTick\(deltaTime\);\n", "\n", rel)
t = rep(t, """            if (Hud != null)
            {
                Hud.OnResized(width, height);
            }""", "            Host.OnResized(width, height);", rel)
t = remove_member(t, "public override void Draw(float timeIndex)", rel)
t = rep(t, "window.PlaySound(", "Host.PlaySound(", rel)
t = sub(t, r"public override void Dispose\(\)\n\s*\{.*?WorldRender\.Dispose\(\);\n\s*\}",
        "public virtual void Dispose() => Host.DisposeGame();", rel, re.S)
t = rep(t, "WorldRender.ToString(), // 6", "Host.ToDebugString(), // 6", rel)
t = rep(t, "WorldRender.Stoping();", "Host.OnWorldStoping();", rel, 1)
for m in re.finditer(r".*(\bwindow\b|WorldRender|\bHud\b).*", t): fail(rel + ": still uses " + m.group(0).strip()) if not m.group(0).strip().startswith("//") else None
save(rel, t)

# J. GameLocal
rel = r"Games\GameLocal.cs"; t = load(rel)
t = rep(t, "public GameLocal(WindowMain window,", "public GameLocal(IGameClientHost host,", rel)
t = rep(t, ": base(window, gameModClient)", ": base(host, gameModClient)", rel)
t = rep(t, "window.CreateGameModServer()", "host.CreateGameModServer()", rel)
t = rep(t, "window.LScreen.Process(", "Host.ShowProcess(", rel)
t = rep(t, "if (window.Screen != null && window.Screen is ScreenWorking screen)", "if (Host.IsLoadingScreen)", rel)
t = rep(t, "screen.ServerBegin(packet.Value);", "Host.ServerLoadingBegin(packet.Value);", rel)
t = rep(t, "screen.ServerStep();", "Host.ServerLoadingStep();", rel)
save(rel, t)

# K. GameNet
rel = r"Games\GameNet.cs"; t = load(rel)
t = rep(t, "public GameNet(WindowMain window,", "public GameNet(IGameClientHost host,", rel)
t = rep(t, ": base(window, gameMod)", ": base(host, gameMod)", rel)
t = rep(t, "window.LScreen.Process(", "Host.ShowProcess(", rel)
t = rep(t, "WorldRender.Stoping();", "Host.OnWorldStoping();", rel, 2)
save(rel, t)

# L. GameModClient: без окна, экран и HUD создаёт клиент
rel = r"Games\GameModClient.cs"; t = load(rel)
t = remove_field(t, "protected readonly WindowMain _window;", rel)
t = rep(t, "public GameModClient(WindowMain window) => _window = window;", "public GameModClient() { }", rel)
t = remove_member(t, "public ScreenBase Screen => _window.Screen;", rel)
t = remove_member(t, "public virtual HudBase CreateHud() => new HudDebug(Game);", rel)
save(rel, t)

# E. WorldClient
rel = r"World\WorldClient.cs"; t = load(rel)
t = remove_field(t, "public readonly WorldRenderer WorldRender;", rel)
t = sub(t, r"\n[ \t]*WorldRender = game\.WorldRender;", "", rel)
t = rep(t, "Game.WorldRender.WorldRenderInitSetting();", "Game.Host.OnWorldSettings();", rel)
t = rep(t, "WorldRender.Starting();", "Game.Host.OnWorldStarting();", rel)
t = rep(t, "WorldRender.Stoping();", "Game.Host.OnWorldStoping();", rel)
t = sub(t, r"\n[ \t]*ParticlesRenderer particles = Game\.WorldRender\.Particles;", "", rel)
t = rep(t, "particles.Spawn(", "Game.Host.SpawnParticle(", rel, 2)
save(rel, t)

# F/G. Игрок: чат и рендеры сущностей у хоста
rel = r"Entity\Player\PlayerClientOwner.cs"; t = load(rel)
t = remove_field(t, "public readonly ChatList Chat;", rel)
t = sub(t, r"\n[ \t]*Chat = new ChatList\([^\n]*", "", rel)
t = rep(t, "Chat.AddMessage(packet.Message, Gi.WindowsChatWidthMessage,  Gi.Si);", "_game.Host.ChatMessage(packet.Message);", rel)
t = rep(t, "_game.WorldRender.Entities", "_game.Host.EntityRenders", rel)
t = rep(t, "_game.WorldRender.RespawnInWorld();", "_game.Host.OnRespawnInWorld();", rel)
t = rep(t, "_game.WorldRender.ModifyOverviewChunk();", "_game.Host.OnModifyOverviewChunk();", rel)
save(rel, t)
rel = r"Entity\Player\PlayerClient.cs"; t = load(rel)
save(rel, rep(t, "_game.WorldRender.Entities", "_game.Host.EntityRenders", rel))

# H. Пакеты
rel = r"Network\ProcessClientPackets.cs"; t = load(rel)
t = rep(t, "Game.WorldRender.Entities", "Game.Host.EntityRenders", rel)
t = rep(t, "Game.ModClient.Screen.AcceptNetworkPackage(packet);", "Game.Host.AcceptNetworkPackage(packet);", rel)
save(rel, t)

# I. Клавиатура: отладочные переключатели у хоста
rel = r"Actions\Keyboard.cs"; t = load(rel)
t = rep(t, "_game.WorldRender.ChunkCursorHiddenShow();", "_game.Host.DebugToggle(EnumDebugToggle.ChunkCursor);", rel)
t = rep(t, "_game.WorldRender.HitboxEntitiesHiddenShow();", "_game.Host.DebugToggle(EnumDebugToggle.HitboxEntities);", rel)
save(rel, t)

# M. Debug: карта чанков отдаёт вершины, меш и шейдер у клиента
rel = "Debug.cs"; t = load(rel)
t = remove_field(t, "private static MeshGuiColor _meshChunks;", rel)
t = remove_member(t, "public static void DrawChunks(WindowMain window)", rel)
t = rep(t, "        private static bool _RenderChuks(List<float> vs)",
        "        /// <summary>\n        /// Вершины отладочной карты чанков (x, y, u, v, r, g, b, a), если изменились, иначе null\n        /// </summary>\n"
        "        public static float[] TakeChunksVertices()\n        {\n"
        "            if (_renderChunks && !_flagBlockDraw)\n            {\n"
        "                List<float> vs = new List<float>();\n"
        "                if (_RenderChuks(vs))\n                {\n                    _renderChunks = false;\n"
        "                    return vs.ToArray();\n                }\n            }\n            return null;\n        }\n\n"
        "        private static bool _RenderChuks(List<float> vs)", rel)
save(rel, t)
print("seam4 ok")
