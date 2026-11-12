"""Предупреждения компилятора, унаследованные от оригинала. Правки без изменения поведения на целых данных."""
import re
from portlib import load, save, rep, fail

# CA2200: catch (Exception ex) { throw ex; } терял стек вызовов. throw; сохраняет его, логика та же.
for rel, n in [(r"Nbt\NBTTools.cs", 4), (r"Network\ReadPacket.cs", 1)]:
    t = load(rel)
    t, k = re.subn(r"catch \(Exception ex\)(\s*\{(?:\s*//[^\n]*)*\s*)throw ex;", r"catch (Exception)\1throw;", t)
    if k != n: fail(f"{rel}: expected {n} rethrow, got {k}")
    save(rel, t)

# CA2022: Read без проверки прочитанного. На целом файле то же, на обрезанном исключение
# (его ловит catch ниже и пишет в лог), а не тихие нули в чанке.
rel = r"World\RegionFile.cs"; t = load(rel)
save(rel, rep(t, "stream.Read(_buffers[i], 0, size);", "stream.ReadExactly(_buffers[i], 0, size);", rel, 1))

# CS0649: _rangeY нигде не задаётся (в оригинале тоже), всегда 0. Явно, чтобы было видно: см. improvements E8.
rel = r"World\Gen\Feature\FeatureValun.cs"; t = load(rel)
save(rel, rep(t, "private readonly byte _rangeY;",
              "private readonly byte _rangeY = 0; // в оригинале не задаётся нигде, всегда 0 (improvements E8)", rel, 1))
print("fix_warnings ok")
