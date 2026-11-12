# Журнал работ

Новые записи сверху. Изменения поведения относительно оригинала помечаются `[ПОВЕДЕНИЕ]`.

## 2026-11-12: этап 3, Mvk2 → Mvk.Game, генерация острова побитно как в оригинале
Сделано:
- `src/Mvk.Game` собирается скриптами: `tools/port/run_game.py` = `port_game.py` (105 файлов логики
  Mvk2, 35 клиентских файлов оставлены на этап 5: Gui, Renderer, Realms, окно, рендер игрока)
  + `game_new/` + швы `game_seam*.py` + `cleanup.py`. Общая замена пространств имён в `portconv.py`.
- `game_seam1`: `GameModClientMvk` без окна (небо, HUD, экраны чата/инвентаря/хранилища переопределит
  клиентский наследник); рендер игрока-владельца через фабрику, вид `EntityRenderKindMvk.Owner`.
- `game_seam2`: пакет склада идёт открытому экрану через `IGameClientHost`; снимок карты биомов
  в PNG (F6, System.Drawing) убран из логики, вернётся в клиенте.
- Сборка всего решения с нуля: 0 ошибок, 0 предупреждений.
- GoldenGen: генерация вынесена в общий `IslandGolden.cs` (тот же код компилируется против оригинала
  и против нового кода). Эталон после рефакторинга пересобран: файл побайтно тот же.
- `IslandParityTests`: новый `Mvk.Game` на `Mvk.Engine` генерирует те же 800 чанков (4 seed × 8 областей):
  блоки, биомы, карта высот, освещение, блок-сущности. `dotnet test`: 223 из 223.

Дальше:
- Режимы игры (выживание в бесконечном мире, на острове), тестовые миры из меню оригинала.
- Этап 4: платформа и RHI (окно Silk.NET, ввод, OpenGL).

## 2026-11-12: этап 2, PNG паритет и сборка без предупреждений
Сделано:
- Эталон `tests/Mvk.Engine.Tests/Golden/png.csv` (GoldenGen, режим `png`): все 178 PNG из ассетов
  оригинала, декодированные GDI+ (размер и SHA-256 пикселей BGRA). `ImageParityTests`: StbImageSharp
  даёт побайтно те же пиксели для всех 178 файлов. Значит атлас блоков и текстуры моделей совпадут.
- Исправление прошлого шага: `ChunkClient.ModifiedToRender` скрывал виртуальный метод `ChunkBase`
  (CS0114), из-за чего клиентские сетки не узнали бы о перерендере. Убран, используется метод базы.
  Прошлая запись «0 предупреждений» была по инкрементальной сборке; полная показала 7.
- `tools/port/fix_warnings.py`: предупреждения из оригинала (`throw ex` → `throw`, `ReadExactly`
  в RegionFile, явный 0 у `FeatureValun._rangeY`). Полная пересборка: 0 предупреждений, 0 ошибок.
- `dotnet test`: 219 из 219.

Отложено:
- Прогон `GameLocal` без окна в тестах: базовый `GameModClient.CreateWorldSettings` возвращает null,
  мир без настроек из `Mvk2` не запускается. Сделаю на этапе 3 вместе с контентом `Mvk2`.

## 2026-11-12: этап 2, Engine компилируется (логика клиента отделена от окна)
Сделано:
- `src/Mvk.Engine` собирается скриптами `tools/port` из оригинала (299 файлов) и компилируется:
  0 ошибок, 0 предупреждений. Ни одной ссылки на окно, OpenGL, System.Drawing.
- Шов клиента (ADR 0003, швы 3/4/5/8, `seam4.py`): `GameBase` больше не наследует окно (`Warp`),
  всё отображение идёт через `IGameClientHost` (экраны, HUD, чат, рендер мира, частицы, звук, курсор).
  `GameBase(host, modClient)`, `GameLocal(host, …)`, `GameNet(host, …)`, `GameModClient()` без окна.
- Логический чанк клиента `ChunkClient` (частицы обязательных блоков, дистанция); сетки добавит
  наследник в клиенте, его создаёт `IGameClientHost.CreateChunk`.
- `Keys`, `MouseButton` → `Mvk.Engine.Actions`; `ColorsBiom` → `Mvk.Engine.World`;
  `RenderFigure` → `Mvk.Engine.Util`; `Gi` остался в Engine без буферов вершин и рендеров блоков.
- Отладочная карта чанков: Engine отдаёт вершины (`Debug.TakeChunksVertices`), меш рисует клиент.
- `dotnet test`: 41 из 41.

Дальше:
- Тест-хост `IGameClientHost` без окна и прогон `GameLocal` в тестах (headless-клиент).
- Golden атласа: байты PNG через StbImageSharp против GDI+.
- Этап 3: `Mvk2` (мир, генерация) → `Mvk.Game`, проверка по `island.csv`.

## 2026-11-12: этап 1, Json, NBT, утилиты Core
Сделано:
- В `Mvk.Core` перенесены Json (без Bbmodel), NBT (без NBTTools) и самодостаточные утилиты `Vge/Util`
  (списки, Rand, Pole, RegTable, Sundry, Ticker и др., полный список в `docs/migration-map.md`).
- Тексты ошибок, нужные Core, вынесены в `CoreStrings` (текст как в `Vge/SR.cs`).
- Новый эталон `tests/Mvk.Core.Tests/Golden/core.csv` (GoldenGen, режим `core`): последовательности Rand
  для 7 сидов, байты записи и чтения NBT, разбор Json. `CoreParityTests`: новый код совпадает побайтно.
- Улучшения (U1–U7 в `docs/improvements.md`): NBT без аллокаций на каждое число, `ReadExactly`,
  сравнение в `ListFast`/`ListMessy` без боксинга, `ChekComment` без `Substring`, nullable без предупреждений.
- Сборка без предупреждений. `dotnet test`: 41 из 41.

Дальше:
- Этап 2 (Engine): `Ce`, Logger/Profiler, AxisAlignedBB/Frustum, мир, чанки, блоки, сущности, сеть.

## 2026-11-12: этап 1, математика Core
Сделано:
- `tools/GoldenGen` собирает оригинал (WinGL, Vge, Mvk2) под net10 без правок и снимает эталоны:
  `tests/Mvk.Game.Tests/Golden/island.csv` (4 сида × 8 областей, 800 чанков, 55 разных биомов)
  и `tests/Mvk.Core.Tests/Golden/math.csv` (Sin/Cos/Tan/WrapAngleToPi/Floor/Ceiling/Round/Sqrt/
  Normalize/Ray/Mat4).
- `WinGL/Util` (Glm, векторы, Mat*) и `Mth` перенесены в `Mvk.Core.Maths`.
  `MathParityTests` подтверждают: результаты побитно совпадают со старым кодом.
- Найден скрытый порядок инициализации: в оригинале таблицу синусов заполняет `WinGL.Window`,
  без окна `Glm.Sin` возвращает 0. В новом Core таблица заполняется статическим инициализатором.
- Улучшения записаны в `docs/improvements.md`.
- `dotnet test`: 24 из 24.

Дальше:
- Json, NBT, самодостаточные части `Vge/Util` → `Mvk.Core` с тестами.

## 2026-11-12: этап 0, каркас
Сделано:
- Решение `MvkClaude.slnx` на .NET 10.0.302 (`global.json`), общие настройки в `Directory.Build.props`,
  версии пакетов централизованно в `Directory.Packages.props` (Silk.NET 2.23.0).
- Проекты: Core, Engine, Engine.Client, Rendering, Rendering.OpenGL, Platform, Game, Game.Client,
  хосты Desktop и Server, тесты Core/Engine/Game.
- Заготовка RHI: `IGraphicsDevice`, `GraphicsBackend`; `OpenGLDevice` на Silk.NET.OpenGL.
- `Mvk.Desktop` открывает окно OpenGL 3.3 и очищает экран белым (как загрузчик оригинала).
- `ArchitectureTests`: Core, Engine, Game и Server не ссылаются на графику, платформу и клиент.
- Оригинал склонирован в `original/` (коммит `dfbd814`), исключён из git.
- `dotnet build`: 0 предупреждений, 0 ошибок. `dotnet test`: 5 из 5.

Дальше:
- Прототип Silk.NET на Android.
- Снять golden-хэши чанков со старого генератора.
- Этап 1: перенос `WinGL/Util` и `Vge/Util`, `NBT`, `Json` в `Mvk.Core`.
