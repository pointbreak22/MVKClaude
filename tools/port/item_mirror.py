"""[ПОВЕДЕНИЕ] Предмет в левой руке зеркален правой (просьба пользователя 21.11): сетка предмета для кости левой руки
отражается по X (позиция и нормаль), порядок вершин каждого квада обращается, чтобы грани смотрели наружу."""
from portlib import load, save, rep

rel = r"Entity\Render\VertexEntityBuffer.cs"
t = load(rel)
t = rep(t, """        /// <summary>
        /// Создать буфер для предмета с масштабом
        /// </summary>""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Зеркальная по X копия сетки (предмет в левой руке): позиция и нормаль по X с обратным знаком,
        /// вершины квада 1 и 3 меняются местами (обход граней сохраняется)
        /// </summary>
        public VertexEntityBuffer MirrorX()
        {
            float[] bufferFloat = new float[BufferFloat.Length];
            int[] bufferInt = new int[BufferInt.Length];
            int quads = BufferFloat.Length / 32;
            for (int q = 0; q < quads; q++)
            {
                for (int v = 0; v < 4; v++)
                {
                    int from = v == 1 ? 3 : v == 3 ? 1 : v;
                    int src = (q * 4 + from) * 8, dst = (q * 4 + v) * 8;
                    Array.Copy(BufferFloat, src, bufferFloat, dst, 8);
                    bufferFloat[dst] = -bufferFloat[dst];
                    bufferFloat[dst + 3] = -bufferFloat[dst + 3];
                    Array.Copy(BufferInt, (q * 4 + from) * SizeInt, bufferInt, (q * 4 + v) * SizeInt, SizeInt);
                }
            }
            return new VertexEntityBuffer(bufferFloat, bufferInt);
        }

        /// <summary>
        /// Создать буфер для предмета с масштабом
        /// </summary>""", rel, 1)
save(rel, t)
print("item_mirror ok")
