"""Перенос клиентской части Vge (рендер мира) в src/Mvk.Engine.Client.
Переносятся файлы без прямых вызовов GL (построение сеток, буферы, математика света).
Файлы с GL (меши, шейдеры, рендер мира) написаны заново на RHI и лежат в client_new/.
Запуск: python port_client.py"""
import os
from portconv import convert, read
root = r"E:\Projects\Games\MvkClaude"
src = os.path.join(root, r"original\src\Vge")
dst = os.path.join(root, r"src\Mvk.Engine.Client")

FILES = [
    # Построение сеток чанков
    "Renderer/World/BlockBufferDistance.cs", "Renderer/World/BlockRenderFull.cs",
    "Renderer/World/BlockRenderLiquid.cs", "Renderer/World/BlockSide.cs",
    "Renderer/World/BufferFast.cs", "Renderer/World/BufferFastByte.cs", "Renderer/World/BufferFastFloat.cs",
    "Renderer/World/BufferSlot.cs", "Renderer/World/ChunkSectionBuffer.cs",
    "Renderer/World/VertexBlockBuffer.cs", "Renderer/World/ColorsLights.cs",
    "Renderer/World/ChunkRender.cs",
    # Карта освещения: расчёт переносится, загрузка в текстуру заменяется швом
    "Renderer/World/TextureLightMap.cs",
    # Рендер сущностей: логика анимации и юниформы через ShadersEntity и IEntityRender клиента
    "Entity/Render/EntityRenderAbstract.cs", "Entity/Render/EntityRenderClient.cs",
    "Entity/Render/EntityRenderAnimation.cs", "Entity/Render/EntityRenderEyeMouth.cs",
    "Entity/Render/EntityRenderItem.cs",
    # Шрифты, чат и HUD: рисуют через RenderMain, MeshGuiColor и MeshGuiLine клиента (client_new)
    "Renderer/Font/EnumFontFX.cs", "Renderer/Font/FontBase.cs", "Renderer/Font/FontStyle.cs",
    "Renderer/Font/Symbol.cs", "Renderer/Font/TransferText.cs", "Realms/ChatList.cs",
    "Gui/Huds/HudBase.cs", "Gui/Huds/HudDebug.cs", "Gui/Huds/PartHubChat.cs",
    # Предмет в GUI (сетка MeshGuiEntity и шейдер EntityGui клиента)
    "Renderer/World/ItemRender.cs",
    # Курсор блока, сетка чанка (F3+G) и хитбоксы (F3+B) на MeshLine и шейдере Line клиента
    "Renderer/World/CursorRender.cs", "Renderer/World/Entity/HitboxEntityRender.cs",
    # Экраны, виджеты и загрузка ресурсов: окно приложения WindowMain и Warp написаны заново (client_new)
    "Realms/Loading.cs", "Gui/RenderSlot.cs", "Gui/ToolTip.cs",
    "Gui/Controls/Button.cs", "Gui/Controls/ButtonClose.cs", "Gui/Controls/ButtonIcon.cs",
    "Gui/Controls/ButtonRemove.cs", "Gui/Controls/ButtonThin.cs", "Gui/Controls/ButtonWide.cs",
    "Gui/Controls/CheckBox.cs", "Gui/Controls/ControlIcon.cs", "Gui/Controls/ControlSlot.cs",
    "Gui/Controls/EnumAlight.cs", "Gui/Controls/EnumAlightVert.cs", "Gui/Controls/Label.cs",
    "Gui/Controls/LineHorizontal.cs", "Gui/Controls/Slider.cs", "Gui/Controls/TextBox.cs",
    "Gui/Controls/WidgetBase.cs",
    "Gui/Screens/EnumScreenParent.cs", "Gui/Screens/LaunchScreen.cs", "Gui/Screens/ScreenBase.cs",
    "Gui/Screens/ScreenInGameMenu.cs", "Gui/Screens/ScreenMainMenu.cs", "Gui/Screens/ScreenProcess.cs",
    "Gui/Screens/ScreenSplash.cs", "Gui/Screens/ScreenWorking.cs",
    "Gui/Screens/Window/ScreenChat.cs", "Gui/Screens/Window/ScreenCreateGame.cs",
    "Gui/Screens/Window/ScreenMultiplayer.cs", "Gui/Screens/Window/ScreenNotification.cs",
    "Gui/Screens/Window/ScreenOptions.cs", "Gui/Screens/Window/ScreenSingle.cs",
    "Gui/Screens/Window/ScreenWindow.cs", "Gui/Screens/Window/ScreenYesNo.cs",
]

n = 0
for rel in FILES:
    t = convert(read(os.path.join(src, rel))).replace("\r\n", "\n").replace("\n", "\r\n")
    o = os.path.join(dst, rel)
    os.makedirs(os.path.dirname(o), exist_ok=True)
    open(o, "wb").write(b"\xef\xbb\xbf" + t.encode("utf-8"))
    n += 1
print("port_client ported", n)
