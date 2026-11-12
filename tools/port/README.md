# tools/port — воспроизводимый перенос Vge → Mvk.Engine

Перенос делается скриптами, а не руками, чтобы его можно было повторить с нуля
(например, если в оригинале что-то поменяется) и чтобы каждое изменение было видно.

```
python tools/port/run_all.py          # очистить src/Mvk.Engine и собрать заново
powershell -File tools/port/build_errs.ps1   # сводка ошибок сборки по типам
python tools/port/deps.py             # какие серверные файлы Vge используют клиентские типы
```

Порядок в `run_all.py`:
1. `port_engine.py` — копирует файлы `original/src/Vge` (кроме уже перенесённых в Core и клиентских),
   меняет пространства имён `Vge.*` → `Mvk.Engine.*`, `WinGL.Util` → `Mvk.Core.Maths`.
2. `new/` — новые файлы движка (интерфейсы швов, замены System.Drawing).
3. Швы по порядку (см. `docs/adr/0003-engine-split.md`):
   - `seam1.py` — рендер сущностей через `IEntityRenderFactory`;
   - `seam2.py` — блок хранит `EnumBlockRender`, затемнение сторон и палитра вынесены из `Gi`;
   - `seam6.py` — реестры получают `IProgress<string>` вместо окна, атлас без GL и System.Drawing;
   - `seam4.py` — логика клиента (`GameBase`, `WorldClient`, игрок, `ChunkClient`) без окна:
     всё отображение через `IGameClientHost`;
   - `cleanup.py` — убирает using клиентских пространств имён (Renderer, Gui, WinGL).

Когда перенос Engine закончится, скрипты останутся как история: дальше код правится в `src/` напрямую.
