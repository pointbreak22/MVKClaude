"""Шов клиента 4: шрифты, чат и HUD на RenderMain клиента.
- Сетки GUI создаются от RenderMain (в оригинале от GL окна): game.GetOpenGL() -> GetOpenGL() WarpRenderer.
- _game.Render -> Render (WarpRenderer берёт его у хоста клиента).
- Чат: в оригинале ChatList жил в игроке (Player.Chat), в порте его держит хост клиента (ChatList),
  логика игрока не знает о шрифтах.
- FontBase.CreateMesh без GL: сетка от RenderMain шрифта.
- HudDebug: карта света в отладке берётся текстурой RHI рендера мира."""
import os
from portlib import load, save, rep, add_using, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

rel = r"Renderer\Font\FontBase.cs"
t = load(rel, C)
t = rep(t, "Sr.GetString(Sr.TheSymbolIsNotInTheList,", "ClientStrings.GetString(ClientStrings.TheSymbolIsNotInTheList,", rel, 4)
t = rep(t, "public void CreateMesh(GL gl, uint index)", "public void CreateMesh(uint index)", rel, 1)
t = rep(t, "_mesh = new MeshGuiColor(gl);", "_mesh = new MeshGuiColor(_render);", rel, 1)
save(rel, t, C)

for rel in [r"Gui\Huds\HudBase.cs", r"Gui\Huds\HudDebug.cs", r"Gui\Huds\PartHubChat.cs"]:
    t = load(rel, C)
    # RenderFigure в оригинале лежал в Vge.Renderer, в порте в Mvk.Engine.Util
    if "RenderFigure" in t: t = add_using(t, "Mvk.Engine.Util")
    t = rep(t, "(game.GetOpenGL())", "(GetOpenGL())", rel)
    if "_game.Render." in t:
        t = t.replace("_game.Render.", "Render.")
    save(rel, t, C)

rel = r"Gui\Huds\PartHubChat.cs"
t = load(rel, C)
t = rep(t, "_chat = game.Player.Chat;", "_chat = Host.ChatList;", rel, 1)
save(rel, t, C)

rel = r"Gui\Huds\HudDebug.cs"
t = load(rel, C)
t = rep(t, "Render.LightMap.BindTexture2dGui();", "Render.BindTexture(Host.WorldRender.LightMap.Texture);", rel, 1)
save(rel, t, C)
print("client_seam4 ok")
