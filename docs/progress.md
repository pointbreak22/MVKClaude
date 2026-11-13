# Журнал работ

Новые записи сверху. Изменения поведения относительно оригинала помечаются `[ПОВЕДЕНИЕ]`.

## 2026-11-13: этап 5.2, пункт 1: небо острова на RHI
Сделано:
- `SkyRender` (Vge) → `src/Mvk.Engine.Client/Renderer/World/SkyRender.cs` (новый файл в
  `tools/port/client_new`): купол неба из 16 сегментов, шейдер `Sky` оригинала без изменений.
- `SkyIslandRender` (Mvk2) → `src/Mvk.Game.Client/Renderer/SkyIslandRender.cs`: закат и рассвет,
  солнце, луна с фазами, 1500 звёзд (сид 10842) с морганием, два слоя облаков из шума Перлина (сид 512),
  дождь и ливень (сид 200), брызги и звуки дождя, гром. Шейдеры `SkyElement`, `SkyStar`, `SkyClouds`
  оригинала без изменений. Смешивание и глубина как в оригинале описаны конвейерами RHI.
  `world.Rnd` расходуется в том же порядке, что и в оригинале.
- `SkyTexturesMvk`: текстуры `Sun.png` и `Moon.png` (в оригинале их грузил `LoadingMvk`).
- Небо выбирает клиент мода: `GameClientHost.SkyFactory` = `MvkClient.SkyFactory` (как
  `GameModClientMvk.CreateWorldSettings`: мир кошмаров с базовым небом, остальные с небом острова).
  `WorldRenderer.WorldStarted`/`WorldRenderInitSetting`, порядок проходов кадра как в оригинале:
  небо, сплошные блоки, облака и дождь, альфа-блоки.
- `Mvk.Desktop`: `--cmd "<команда чата>"` (после входа в мир, например `/time n`, `/weather s`)
  и `--look поворот,наклон` (градусы) для снимков без человека. Снимок PNG теперь RGB: альфа кадрового
  буфера (её пишет смешивание заката) давала в снимке прозрачную полосу, в окне её нет.
- Снимки: `docs/img/stage5-sky.png` (закат: солнце, звёзды, облака), `docs/img/stage5-rain.png` (ливень),
  `docs/img/stage5-sky-day.png` (день, взгляд вверх).
- Полная пересборка: 0 ошибок, 0 предупреждений. `dotnet test`: 235 из 235.
  `tools/port/run_client.py` воспроизводит `src/Mvk.Engine.Client` без расхождений.

Дальше:
- Пункт 2: рендер игрока и мобов. Пункт 3: изменение размера окна.
- Тени облаков (`DrawCloudsDepthMap`) ждут кадровых буферов в RHI.

## 2026-11-13: этап 5.1, мир на экране: локальная игра в окне, сетки чанков на RHI
Сделано:
- Тест `GameLocalHeadlessTests`: локальная игра (сервер и клиент в одном процессе) без окна.
  Игрок входит в мир острова и получает 25 чанков вокруг себя. Хост без окна
  `HeadlessGameClientHost` пригодится и для ботов.
- `src/Mvk.Engine.Client` собирается скриптами: `tools/port/run_client.py` = `port_client.py`
  (13 файлов построения сеток из `Vge/Renderer/World`: `ChunkRender`, `BlockRenderFull`,
  `BlockRenderLiquid`, `BlockSide`, буферы вершин, `TextureLightMap`) + `client_seam1.py`,
  `client_seam2.py` + новые файлы из `tools/port/client_new`.
- Новые файлы на RHI: `RenderContext` (общий индексный буфер квадов), `MeshBlocks` (2 вершинных
  буфера как в оригинале), `ChunkMesher`, `ShadersBlocks` (шейдер `BlocksLow` оригинала без
  изменений, 4 конвейера: сплошные, каркас, альфа, ближняя альфа со сдвигом глубины),
  `BlockAtlasTextures` (резкий атлас и размытый со своими мипмапами, как `BufferedImage`),
  `WorldRenderer` (поток построения сеток, очереди, привязка сеток в кадре, проходы как в оригинале),
  `GameClientHost`.
- `Mvk.Game.Client/MvkClient.Initialize`: пути, настройки и реестры, как окно оригинала до игры.
- `Mvk.Desktop` теперь запускает игру (`--slot`, `--seed`): такт 20 раз в секунду, кадр с долей
  времени до такта, клавиши Silk переводятся в `Keys` оригинала, обзор мышью по щелчку.
  Тестовая сцена этапа 4 осталась (`--test-scene`). `--screenshot файл.png --shot-after 10`
  ждёт входа в мир и построения сеток, сохраняет кадр. Снимок: `docs/img/stage5-world.png`
  (песок и вода острова, туман).
- Полная пересборка: 0 ошибок, 0 предупреждений. `dotnet test`: 235 из 235.

Не сделано на этом шаге (дальше по этапу 5):
- Небо, облака, сущности и игрок, частицы, курсор блока, тени (нужны кадровые буферы в RHI).
- GUI: шрифты, HUD, меню, экраны загрузки и паузы, чат; выбор режима игры (остров и бесконечный мир).
- Звук в игре (`AudioDevice` через `IGameClientHost.PlaySound`).
- Изменение размера окна (сейчас матрица проекции считается по размеру при старте и при движении камеры).

## 2026-11-12: этап 4, платформа и RHI, тестовая сцена с кубами
Сделано:
- RHI в `Mvk.Rendering` (ADR 0002, модель как у Vulkan): `IGpuBuffer`, `ITexture` (2D и массив 2D,
  вход RGBA или BGRA, мипмапы свои или от драйвера), `IShader` (location входов и слоты текстур
  задаются описанием), неизменяемый `IPipeline` (раскладки вершин по слотам, топология, глубина,
  отсечение, смешивание, сдвиг глубины, каркас), `CommandList` (запись без GL, память переиспользуется
  между кадрами), `IGraphicsDevice.Submit` и `ReadPixels`.
- Бэкенд `Mvk.Rendering.OpenGL` на Silk.NET: проигрывает список команд, помнит состояние GL и не
  повторяет вызовы, VAO кешируются по (конвейер, буферы). Поддержаны два вершинных буфера на меш
  (float и int через `glVertexAttribIPointer`), как у `MeshBlocks` оригинала.
- `Mvk.Platform`: `InputActionMap` (действия вместо клавиш, несколько привязок на действие,
  переназначение), `InputDevice` (клавиатура, мышь, захват курсора, события для GUI),
  `AudioDevice` (OpenAL Soft через Silk.NET) и `PcmSample.DecodeOgg` (NVorbis 0.10.5, как в оригинале).
- Эталон `tests/Mvk.Platform.Tests/Golden/audio.csv` (GoldenGen, режим `ogg`): все 51 звук ассетов
  декодируются побайтно как `AudioSample.LoadOgg` оригинала.
- `Mvk.Desktop`: тестовая сцена `TestScene` (25 кубов с текстурами блоков из массива текстур
  с мипмапами, прозрачное стекло, каркасный режим F3, 2D-картинка поверх, звук по клику).
  Сцена работает только через интерфейсы RHI. `--screenshot файл.png` рисует кадр и сохраняет PNG:
  так рендер проверяется без человека (снимок: `docs/img/stage4.png`).
- Новый проект тестов `Mvk.Platform.Tests` (11 тестов: звук, действия ввода, список команд).
- Полная пересборка: 0 ошибок, 0 предупреждений. `dotnet test`: 234 из 234.

Не сделано на этом этапе (перенесено):
- Перевод клавиш Silk.NET в `Mvk.Engine.Actions.Keys` и набор игровых действий: этап 5, вместе
  с клиентом (Platform не может ссылаться на Engine).
- Цели рендера (framebuffer для теней) и uniform-массивы (кости моделей): добавлю в RHI на этапе 5,
  когда появятся их потребители.
- Буфер обмена (`WinGL.Util.Clipboard`): этап 5, вместе с текстовыми полями.

Дальше:
- Этап 5: клиент (мешинг чанков, рендер мира, GUI, экраны) на RHI. Шейдеры оригинала GLSL 330 как есть.

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
