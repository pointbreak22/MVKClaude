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
]

n = 0
for rel in FILES:
    t = convert(read(os.path.join(src, rel))).replace("\r\n", "\n").replace("\n", "\r\n")
    o = os.path.join(dst, rel)
    os.makedirs(os.path.dirname(o), exist_ok=True)
    open(o, "wb").write(b"\xef\xbb\xbf" + t.encode("utf-8"))
    n += 1
print("port_client ported", n)
