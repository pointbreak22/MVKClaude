"""Шов клиента 7 (этап 7): сетки чанков строятся несколькими потоками. У каждого потока свой ChunkMesher
(буферы вершин и рендеры блоков); перед построением WorldRenderer отдаёт чанку построитель своего потока.
Один чанк в пакете строит один поток, поэтому поле построителя чанка не делится между потоками.
Статический буфер альфа блоков чанка (_listAlphaBuffer) переезжает в ChunkMesher.AlphaBuffer."""
import os
from portlib import load, save, rep, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

rel = r"Renderer\World\ChunkRender.cs"
t = load(rel, C)
t = rep(t, """        /// <summary>
        /// Построитель сеток (буферы вершин и рендеры блоков), в оригинале статические поля Gi
        /// </summary>
        private readonly ChunkMesher _mesher;""", """        /// <summary>
        /// Построитель сеток (буферы вершин и рендеры блоков), в оригинале статические поля Gi.
        /// Свой у каждого потока построения сеток, задаётся перед Render и RenderAlpha (SetMesher)
        /// </summary>
        private ChunkMesher _mesher;

        /// <summary>
        /// Задать построитель сеток потока, который будет строить сетку этого чанка
        /// </summary>
        public void SetMesher(ChunkMesher mesher) => _mesher = mesher;""", rel, 1)
# Буфер альфа блоков был статическим (общим): теперь у построителя потока
t = rep(t, """        /// <summary>
        /// Массив буфера блоков альфы
        /// </summary>
        private static readonly ListFast<BlockBufferDistance> _listAlphaBuffer = new ListFast<BlockBufferDistance>();
""", "", rel, 1)
t = rep(t, "_listAlphaBuffer", "_mesher.AlphaBuffer", rel, 6)
save(rel, t, C)
print("client_seam7 ok")
