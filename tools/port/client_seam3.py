"""Шов клиента 3: рендер сущностей.
- Mat4 значимый тип: Copy(m) родительской кости заменён присваиванием.
- Строка ошибки слоёв одежды берётся из ClientStrings (Sr в Engine внутренний).
- Атрибуты MethodImpl у пустых переопределений не трогаем: код логики анимации как в оригинале."""
import os
from portlib import load, save, rep, remove_member, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

rel = r"Entity\Render\EntityRenderAnimation.cs"
t = load(rel, C)
t = rep(t, "Sr.GetString(Sr.EntityClothingLayersObjectIsMissing,",
        "ClientStrings.GetString(ClientStrings.EntityClothingLayersObjectIsMissing,", rel)
# Mat4 стал значимым типом (improvements R19): копия родительской матрицы кости явным присваиванием,
# в оригинале Copy(m) писал в общий массив копии m
t = rep(t, "_bonesTransforms[bone.ParentIndex].Copy(_bonesTransforms[i]);",
        "_bonesTransforms[i] = _bonesTransforms[bone.ParentIndex];", rel)
save(rel, t, C)
print("client_seam3 ok")
