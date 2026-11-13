"""Шов клиента 6: курсор блока, сетка чанка и хитбоксы на линиях RenderMain.
- MeshLine создаётся от RenderMain (в оригинале от GL) с видом буфера RHI вместо GL_*_DRAW.
- HitboxEntityRender получает RenderMain вместо GL.
- В CursorRender лишний using System.Configuration оригинала (не используется, в .NET нет без пакета)."""
import os
from portlib import load, save, rep, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

rel = r"Renderer\World\CursorRender.cs"
t = load(rel, C)
t = rep(t, "using System.Configuration;\n", "", rel, 1)
t = rep(t, "GL.GL_DYNAMIC_DRAW", "BufferUsage.Dynamic", rel, 2)
t = rep(t, "GL.GL_STATIC_DRAW", "BufferUsage.Static", rel, 1)
t = rep(t, "using Mvk.Engine.Renderer.Mesh;", "using Mvk.Engine.Renderer.Mesh;\nusing Mvk.Rendering;", rel, 1)
save(rel, t, C)

rel = r"Renderer\World\Entity\HitboxEntityRender.cs"
t = load(rel, C)
t = rep(t, "public HitboxEntityRender(GL gl)", "public HitboxEntityRender(RenderMain gl)", rel, 1)
t = rep(t, "GL.GL_STREAM_DRAW", "BufferUsage.Dynamic", rel, 1)  # GL_STREAM_DRAW: в RHI ближайший вид Dynamic
t = rep(t, "using Mvk.Engine.Renderer.Mesh;", "using Mvk.Engine.Renderer.Mesh;\nusing Mvk.Rendering;", rel, 1)
save(rel, t, C)
print("client_seam6 ok")
