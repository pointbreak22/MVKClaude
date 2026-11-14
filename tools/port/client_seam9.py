"""Шов клиента 9 (этап 7): свёртка вершин сеток чанков для замера (MeshDigest, только при --bench).
Проверяет, что оптимизации построения сетки дают те же вершины байт в байт."""
import os
from portlib import load, save, rep, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

rel = r"Renderer\World\ChunkRender.cs"
t = load(rel, C)
t = rep(t, """            _meshDense.SetBuffer(_mesher.VertexDense);""", """            _meshDense.SetBuffer(_mesher.VertexDense);
            if (MeshDigest.Enabled) MeshDigest.Add(CurrentChunkX, CurrentChunkY, false, _mesher.VertexDense);""", rel, 1)
t = t.replace("""                _meshAlpha.SetBufferClear();""", """                _meshAlpha.SetBufferClear();
                if (MeshDigest.Enabled) MeshDigest.AddEmpty(CurrentChunkX, CurrentChunkY, true);""")
if t.count("MeshDigest.AddEmpty") != 2: raise SystemExit(rel + ": SetBufferClear count")
t = rep(t, """            _meshAlpha.SetBuffer(_mesher.VertexAlpha);""", """            _meshAlpha.SetBuffer(_mesher.VertexAlpha);
            if (MeshDigest.Enabled) MeshDigest.Add(CurrentChunkX, CurrentChunkY, true, _mesher.VertexAlpha);""", rel, 1)
save(rel, t, C)
print("client_seam9 ok")
