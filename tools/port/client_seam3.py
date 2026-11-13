"""Шов клиента 3: рендер сущностей.
- Строка ошибки слоёв одежды берётся из ClientStrings (Sr в Engine внутренний).
- EntityRenderAnimation.DrawGui (сущность в окне инвентаря) переносится вместе с GUI:
  ему нужны Gi.Ortho и шейдер GUI сущностей, которых ещё нет.
- Атрибуты MethodImpl у пустых переопределений не трогаем: код логики анимации как в оригинале."""
import os
from portlib import load, save, rep, remove_member, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

rel = r"Entity\Render\EntityRenderAnimation.cs"
t = load(rel, C)
t = rep(t, "Sr.GetString(Sr.EntityClothingLayersObjectIsMissing,",
        "ClientStrings.GetString(ClientStrings.EntityClothingLayersObjectIsMissing,", rel)
t = remove_member(t, "public void DrawGui(float posX, float posY, float yaw, float pitch, float scale)", rel)
save(rel, t, C)
print("client_seam3 ok")
