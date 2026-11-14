"""Шов клиента 8 (этап 7, пулы буферов): построение сетки чанка без мусора в куче.
- Сетка секции хранилась копией ToArray и при каждой перестройке заменялась новым массивом:
  теперь массивы секции переиспользуются (ChunkSectionBuffer.Set, растут только при нехватке), число значений
  хранится отдельно.
- Каждый видимый альфа блок копировал свои вершины в два новых массива (ToArray) для сортировки по расстоянию:
  теперь вершины пишутся подряд в общий буфер построителя (ChunkMesher.AlphaScratch), а запись сортировки хранит
  смещения. Порядок сортировки тот же: ключ сравнения тот же (Distance), исходный порядок записей тот же.
- BlockBufferDistance реализует IComparable<T>: сортировка без упаковки структуры на каждое сравнение.
Вершины на GPU те же, байт в байт."""
import os, re
from portlib import load, save, rep, fail, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

# ---------- BufferFastFloat / BufferFastByte: добавление части массива ----------
rel = r"Renderer\World\BufferFastFloat.cs"
t = load(rel, C)
t = rep(t, """        /// <summary>
        /// Получить адрес буфера
        /// </summary>
        public override IntPtr ToBuffer()""", """        /// <summary>
        /// Добавить count значений массива с позиции start
        /// </summary>
        public void AddRange(float[] items, int start, int count)
        {
            Buffer.BlockCopy(items, start * sizeof(float), _buffer, Count * sizeof(float), count * sizeof(float));
            Count += count;
        }

        /// <summary>
        /// Получить адрес буфера
        /// </summary>
        public override IntPtr ToBuffer()""", rel, 1)
save(rel, t, C)

rel = r"Renderer\World\BufferFastByte.cs"
t = load(rel, C)
i = t.find("        public void AddRange(byte[] items)")
if i < 0: fail(rel + ": AddRange not found")
t = t[:i] + """        /// <summary>
        /// Добавить count значений массива с позиции start
        /// </summary>
        public void AddRange(byte[] items, int start, int count)
        {
            Buffer.BlockCopy(items, start, _buffer, Count, count);
            Count += count;
        }

""" + t[i:]
save(rel, t, C)

# ---------- ChunkSectionBuffer: массивы переиспользуются ----------
rel = r"Renderer\World\ChunkSectionBuffer.cs"
t = load(rel, C)
t = rep(t, """        public void Clear()
        {
            IsModifiedRender = false;
            BufferFloat = new float[0];
            BufferByte = new byte[0];
        }""", """        /// <summary>
        /// Сколько значений Float в буфере (массив может быть больше)
        /// </summary>
        public int CountFloat;
        /// <summary>
        /// Сколько значений Byte в буфере (массив может быть больше)
        /// </summary>
        public int CountByte;

        /// <summary>
        /// Скопировать сетку секции из буфера построителя, массивы растут только при нехватке
        /// </summary>
        public void Set(VertexBlockBuffer vertex)
        {
            CountFloat = vertex.BufferFloat.Count;
            CountByte = vertex.BufferByte.Count;
            if (BufferFloat == null || BufferFloat.Length < CountFloat) BufferFloat = new float[CountFloat];
            if (BufferByte == null || BufferByte.Length < CountByte) BufferByte = new byte[CountByte];
            vertex.BufferFloat.CopyTo(BufferFloat, CountFloat);
            vertex.BufferByte.CopyTo(BufferByte, CountByte);
        }

        /// <summary>
        /// Секция пуста (массивы остаются для следующей сетки)
        /// </summary>
        public void SetEmpty()
        {
            CountFloat = 0;
            CountByte = 0;
        }

        public void Clear()
        {
            IsModifiedRender = false;
            BufferFloat = new float[0];
            BufferByte = new byte[0];
            CountFloat = 0;
            CountByte = 0;
        }""", rel, 1)
save(rel, t, C)

# ---------- BufferFast: копия в массив ----------
rel = r"Renderer\World\BufferFast.cs"
t = load(rel, C)
t = rep(t, """        public T[] ToArray()
        {""", """        /// <summary>
        /// Скопировать первые count значений в массив
        /// </summary>
        public void CopyTo(T[] destination, int count) => Array.Copy(_buffer, destination, count);

        /// <summary>
        /// Весь внутренний массив (значимы первые Count), для копирования части без лишнего массива
        /// </summary>
        public T[] GetBufferAll() => _buffer;

        public T[] ToArray()
        {""", rel, 1)
save(rel, t, C)

# ---------- BlockBufferDistance: смещения в общем буфере вместо своих массивов ----------
rel = r"Renderer\World\BlockBufferDistance.cs"
t = load(rel, C)
t = rep(t, """    public struct BlockBufferDistance : IComparable
    {
        /// <summary>
        /// Буфер для склейки рендера, Float данных
        /// </summary>
        public float[] BufferFloat;
        /// <summary>
        /// Буфер для склейки рендера, Byte данных
        /// </summary>
        public byte[] BufferByte;""", """    public struct BlockBufferDistance : IComparable, IComparable<BlockBufferDistance>
    {
        /// <summary>
        /// Начало Float данных блока в общем буфере построителя (ChunkMesher.AlphaScratch)
        /// </summary>
        public int StartFloat;
        /// <summary>
        /// Число Float данных блока
        /// </summary>
        public int CountFloat;
        /// <summary>
        /// Начало Byte данных блока в общем буфере построителя
        /// </summary>
        public int StartByte;
        /// <summary>
        /// Число Byte данных блока
        /// </summary>
        public int CountByte;""", rel, 1)
t = rep(t, """        public int CompareTo(object obj)""", """        public int CompareTo(BlockBufferDistance other) => Distance.CompareTo(other.Distance);

        public int CompareTo(object obj)""", rel, 1)
save(rel, t, C)

rel = r"Renderer\World\VertexBlockBuffer.cs"
t = load(rel, C)
t = rep(t, """        public void AddBlockBufferDistance(BlockBufferDistance blockBuffer)
        {
            BufferFloat.AddRange(blockBuffer.BufferFloat);
            BufferByte.AddRange(blockBuffer.BufferByte);
        }""", """        public void AddBlockBufferDistance(BlockBufferDistance blockBuffer, VertexBlockBuffer scratch)
        {
            BufferFloat.AddRange(scratch.BufferFloat.GetBufferAll(), blockBuffer.StartFloat, blockBuffer.CountFloat);
            BufferByte.AddRange(scratch.BufferByte.GetBufferAll(), blockBuffer.StartByte, blockBuffer.CountByte);
        }

        /// <summary>
        /// Дописать все вершины другого буфера
        /// </summary>
        public void Append(VertexBlockBuffer other)
        {
            BufferFloat.AddRange(other.BufferFloat.GetBufferAll(), 0, other.BufferFloat.Count);
            BufferByte.AddRange(other.BufferByte.GetBufferAll(), 0, other.BufferByte.Count);
        }""", rel, 1)
save(rel, t, C)

# ---------- ChunkRender ----------
rel = r"Renderer\World\ChunkRender.cs"
t = load(rel, C)
t = rep(t, """                        _sectionsBuffer[cbY].BufferFloat = _mesher.VertexDense.BufferFloat.ToArray();
                        _sectionsBuffer[cbY].BufferByte = _mesher.VertexDense.BufferByte.ToArray();""",
"""                        _sectionsBuffer[cbY].Set(_mesher.VertexDense);""", rel, 1)
t = rep(t, """                        if (_sectionsBuffer[cbY].BufferFloat.Length != 0)
                        {
                            _sectionsBuffer[cbY].BufferFloat = new float[0];
                            _sectionsBuffer[cbY].BufferByte = new byte[0];
                        }""", """                        _sectionsBuffer[cbY].SetEmpty();""", rel, 1)
t = rep(t, """                if (_sectionsBuffer[cbY].BufferFloat.Length != 0)
                {
                    _mesher.VertexDense.BufferFloat.AddRange(_sectionsBuffer[cbY].BufferFloat);
                    _mesher.VertexDense.BufferByte.AddRange(_sectionsBuffer[cbY].BufferByte);
                }""", """                if (_sectionsBuffer[cbY].CountFloat != 0)
                {
                    _mesher.VertexDense.BufferFloat.AddRange(_sectionsBuffer[cbY].BufferFloat, 0, _sectionsBuffer[cbY].CountFloat);
                    _mesher.VertexDense.BufferByte.AddRange(_sectionsBuffer[cbY].BufferByte, 0, _sectionsBuffer[cbY].CountByte);
                }""", rel, 1)
# Альфа блок: вершины в общий буфер, запись со смещениями
pattern = re.compile(r"""(?P<i>[ ]+)_mesher\.AlphaBuffer\.Add\(new BlockBufferDistance\(\)
(?P=i)\{
(?P=i)    BufferFloat = _mesher\.VertexAlpha\.BufferFloat\.ToArray\(\),
(?P=i)    BufferByte = _mesher\.VertexAlpha\.BufferByte\.ToArray\(\),
(?P=i)    Distance = Glm\.Distance\(posPlayer,
(?P=i)        new Vector3i\(cbX \| x, realY, cbZ \| z\)\)
(?P=i)\}\);
""")
t, n = pattern.subn(lambda m: f"""{m.group('i')}_mesher.AddAlphaBlock(Glm.Distance(posPlayer, new Vector3i(cbX | x, realY, cbZ | z)));
""", t)
if n != 2: fail(f"{rel}: alpha blocks {n}")
t = rep(t, "                _mesher.AlphaBuffer.Clear();", "                _mesher.ClearAlpha();", rel, 1)
t = rep(t, "                    _mesher.VertexAlpha.AddBlockBufferDistance(_mesher.AlphaBuffer[i]);",
        "                    _mesher.VertexAlpha.AddBlockBufferDistance(_mesher.AlphaBuffer[i], _mesher.AlphaScratch);", rel, 1)
save(rel, t, C)
print("client_seam8 ok")
