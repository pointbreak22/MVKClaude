"""[ПОВЕДЕНИЕ] Этап 7, решение пользователя 14.11.2026 (docs/improvements.md R26): пакет загрузки чанков сервера
получает весь такт вместо трети (Ce.MaxBatchChunksTime). Мир тот же (порядок рельефа и ChunkPresent не меняется,
свёртка сеток совпадает), меняется только темп сервера при загрузке: мир загружается за 3.9 с вместо 5.1 с."""
import re
from portlib import load, save, fail

rel = "Ce.cs"
t = load(rel)
# В имени TickTime оригинала есть невидимые символы, поэтому через регулярное выражение
pattern = re.compile(r"(    public const int MaxBatchChunksTime = Tick\W*Time) / 3;[^\n]*")
t, n = pattern.subn(r"\1; // [ПОВЕДЕНИЕ] весь такт, в оригинале треть (R26)", t)
if n != 1: fail(rel + f": MaxBatchChunksTime {n}")
save(rel, t)
print("fix_generation_budget ok")
