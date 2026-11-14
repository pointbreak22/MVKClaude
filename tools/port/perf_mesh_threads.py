"""Этап 7: сетки чанков строятся несколькими потоками (WorldRenderer + JobScheduler). Общего изменяемого
состояния в построении сетки два, оба переводятся на поток, результат не меняется:
- Gi.Block (текущий блок, им пользуется только построение сеток) — [ThreadStatic];
- DestroyBlockProgress.GetVertices писал позиции трещин разрушения в общий массив вершин шага:
  теперь копия вершин шага во временный массив своего потока (Vertex3d — структура)."""
from portlib import load, save, rep

rel = "Gi.cs"
t = load(rel)
t = rep(t, """    /// <summary>
    /// Выбранный объект блока для рендера
    /// </summary>
    public static BlockBase Block;""", """    /// <summary>
    /// Выбранный объект блока для рендера (у каждого потока построения сеток свой)
    /// </summary>
    [ThreadStatic]
    public static BlockBase Block;""", rel, 1)
if "using System;" not in t:
    t = "using System;\n" + t.lstrip("﻿")
save(rel, t)

rel = r"World\Block\DestroyBlockProgress.cs"
t = load(rel)
t = rep(t, """        public Vertex3d[] GetVertices(int progress, Vertex3d[] vertices)
        {
            Vertex3d[] ds = _vertices[progress - 1];
            ds[0].SetPosition(vertices[0]);
            ds[1].SetPosition(vertices[1]);
            ds[2].SetPosition(vertices[2]);
            ds[3].SetPosition(vertices[3]);
            return ds;
        }""", """        public Vertex3d[] GetVertices(int progress, Vertex3d[] vertices)
        {
            Vertex3d[] step = _vertices[progress - 1];
            // Свой массив у каждого потока построения сеток: вершины шага (текстура) с позициями грани
            Vertex3d[] ds = _threadVertices ??= new Vertex3d[4];
            for (int i = 0; i < 4; i++)
            {
                ds[i] = step[i];
                ds[i].SetPosition(vertices[i]);
            }
            return ds;
        }

        /// <summary>
        /// Вершины разрушения потока построения сеток (результат GetVertices до следующего вызова в том же потоке)
        /// </summary>
        [ThreadStatic]
        private static Vertex3d[] _threadVertices;""", rel, 1)
if "using System;" not in t:
    t = "using System;\n" + t.lstrip("﻿")
save(rel, t)
print("perf_mesh_threads ok")
