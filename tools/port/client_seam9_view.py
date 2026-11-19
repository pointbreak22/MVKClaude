"""Шов клиента (технический п. 1): состояние вида из статического Gi в объект ViewState окна.
Экраны и виджеты берут его у окна (window.View), HUD у игры (_game.View), шрифты и курсор у рендера (Render.View)."""
import os, re
from portlib import load, save, rep, fail, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

VIEW = ("Width|Height|Si|WindowsChatWidthMessage|WindowsChatHeightMessage|IsDrawOrto|ZoomDrawOrto|DebugOrtoNext|"
        "UpdateSizeInterface|UpOrtho|Ortho|MatrixViewEye|MatrixViewDepthMap|MatrixView|ViewLightDir|EntityBrightness|"
        "BlockBrightness")
VIEW_RE = re.compile(r"\bGi\.(" + VIEW + r")\b")

FILES = [
    (r"Gui\Controls\WidgetBase.cs", "window.View", 3),
    (r"Gui\Screens\ScreenBase.cs", "window.View", 4),
    (r"Gui\Screens\ScreenSplash.cs", "window.View", 2),
    (r"Gui\Screens\ScreenWorking.cs", "window.View", 2),
    (r"Gui\Screens\Window\ScreenChat.cs", "window.View", 2),
    (r"Gui\Screens\Window\ScreenWindow.cs", "window.View", 2),
    (r"Gui\Huds\HudBase.cs", "_game.View", 2),
    (r"Gui\Huds\HudDebug.cs", "_game.View", 2),
    (r"Gui\Huds\PartHubChat.cs", "_game.View", 7),
    (r"Gui\RenderSlot.cs", "_window.View", 3),
    (r"Gui\ToolTip.cs", "_window.View", 5),
    (r"Renderer\World\CursorRender.cs", "_worldRenderer.Render.View", 4),
]
for rel, accessor, n in FILES:
    t = load(rel, C)
    t, k = VIEW_RE.subn(accessor + r".\1", t)
    if k != n: fail(f"{rel}: Gi view {k}, expected {n}")
    save(rel, t, C)

# Шрифт без рендера (эталон шрифтов GoldenGen) считает в Si = 1, как статический Gi по умолчанию
rel = r"Renderer\Font\FontBase.cs"
t = load(rel, C)
t = rep(t, "_si = Gi.Si;", "_si = _render?.View.Si ?? 1;", rel, 1)
save(rel, t, C)

# Ничего из состояния вида не осталось в статике
for d, _, fs in os.walk(C):
    for f in fs:
        if f.endswith(".cs") and VIEW_RE.search(open(os.path.join(d, f), encoding="utf-8-sig").read()):
            fail(os.path.join(d, f) + ": Gi view left")
print("client_seam9_view ok")
