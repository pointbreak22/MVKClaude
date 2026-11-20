"""Технический шаг Nullable 2: #nullable enable во всех остальных папках Mvk.Engine (после Network и Util).
Правки строк сняты циклом «сборка — предупреждения — правка» и лежат в nullable_engine_rest.json
(файл, номер строки, строка до, строка после); шов проверяет, что строка до совпадает, иначе останавливается.
Правила: поле или свойство, которое задаётся после конструктора, — `= null!`; событие без подписчиков — `?`;
необязательный параметр `= null` — `T?`; возврат, присваивание, аргумент и разыменование, где оригинал
полагается на не-null, — `!`; `Equals(object?)`, `CompareTo(object?)`, обработчики событий `object? sender`.
Поведение не меняется: `!` и `null!` только снимают предупреждения. Места с `null!` — список того, где
значение на деле бывает null (кандидаты на `T?` при следующей чистке, docs/improvements.md R37)."""
import json, os
from portlib import load, save, ENG, fail

HERE = os.path.dirname(os.path.abspath(__file__))

for d, _, fs in os.walk(ENG):
    if os.sep + "obj" in d or os.sep + "bin" in d: continue
    for f in fs:
        if not f.endswith(".cs"): continue
        rel = os.path.relpath(os.path.join(d, f), ENG)
        t = load(rel)
        if not t.startswith("#nullable"):
            save(rel, "#nullable enable\n" + t)

edits = json.load(open(os.path.join(HERE, "nullable_engine_rest.json"), encoding="utf-8"))
count = 0
for rel, changes in edits.items():
    lines = load(rel).split("\n")
    for ln, old, new in changes:
        if lines[ln - 1] != old:
            fail(f"{rel}:{ln}: ожидалось {old.strip()[:80]!r}, в файле {lines[ln - 1].strip()[:80]!r}")
        lines[ln - 1] = new
        count += 1
    save(rel, "\n".join(lines))
print(f"nullable_engine_rest ok ({count} строк)")
