"""Шов клиента 5: экраны и виджеты на WindowMain клиента.
- RenderFigure в оригинале лежал в Vge.Renderer, в порте в Mvk.Engine.Util.
- Буфер обмена WinForms (Clipboard) через окно платформы: WindowMain.ClipboardGetText/SetText.
- Чат: в оригинале ChatList в игроке и Hud в игре; в порте оба у хоста клиента (window.GameHost).
- Опции: рендеры блоков в оригинале статические в Gi, в порте у построителя сеток рендера мира;
  ModifyShadow вызывается у рендера мира текущей игры.
- TextBox: CS0168 неиспользуемая переменная исключения (в оригинале то же предупреждение)."""
import os
from portlib import load, save, rep, add_using, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

n = 0
for d, _, fs in os.walk(os.path.join(C, "Gui")):
    for f in fs:
        if not f.endswith(".cs"): continue
        rel = os.path.relpath(os.path.join(d, f), C)
        t = load(rel, C)
        u = t
        if "RenderFigure" in u and "using Mvk.Engine.Util;" not in u:
            u = add_using(u, "Mvk.Engine.Util")
        u = u.replace("Clipboard.SetText(", "window.ClipboardSetText(")
        u = u.replace("Clipboard.GetText()", "window.ClipboardGetText()")
        if u != t:
            save(rel, u, C); n += 1
print("client_seam5 files", n)

rel = r"Gui\Screens\Window\ScreenChat.cs"
t = load(rel, C)
t = rep(t, "window.Game.Player.Chat", "window.GameHost.ChatList", rel)
t = rep(t, "window.Game.Hud.ChatOn();", "window.GameHost.Hud?.ChatOn();", rel, 1)
t = rep(t, "window.Game?.Hud.ChatOff();", "window.GameHost?.Hud?.ChatOff();", rel, 1)
save(rel, t, C)

rel = r"Gui\Screens\Window\ScreenOptions.cs"
t = load(rel, C)
t = rep(t, """                Gi.BlockRendFull.InitAmbientOcclusion();
                Gi.BlockAlphaRendFull.InitAmbientOcclusion();
                Gi.BlockLiquidAlphaRendFull.InitAmbientOcclusion();
""", """                if (window.GameHost?.WorldRender != null)
                {
                    window.GameHost.WorldRender.Mesher.BlockRendFull.InitAmbientOcclusion();
                    window.GameHost.WorldRender.Mesher.BlockAlphaRendFull.InitAmbientOcclusion();
                    window.GameHost.WorldRender.Mesher.BlockLiquidAlphaRendFull.InitAmbientOcclusion();
                }
""", rel, 1)
t = rep(t, "window.Game.WorldRender.ModifyShadow();", "window.GameHost?.WorldRender?.ModifyShadow();", rel, 1)
save(rel, t, C)

rel = r"Gui\Controls\TextBox.cs"
t = load(rel, C)
t = rep(t, "catch(Exception ex)", "catch(Exception)", rel, 1)
save(rel, t, C)
print("client_seam5 ok")
