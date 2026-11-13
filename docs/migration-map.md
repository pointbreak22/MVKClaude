# Карта переноса

Статусы: ⬜ не начато, 🟨 в работе, ✅ перенесено и покрыто тестами.

| Оригинал (original/src) | Новый проект | Этап | Статус | Заметки |
|---|---|---|---|---|
| WinGL/Util (Glm, Mth, Clipboard) | Mvk.Core.Maths (Clipboard → Mvk.Platform) | 1 | ✅ | побитный паритет: `Golden/math.csv`; Clipboard ещё не перенесён |
| WinGL/OpenGL (GL, Shader, ShaderProgram) | Mvk.Rendering (RHI) + Mvk.Rendering.OpenGL | 4 | ✅ | не перенос, а замена: RHI + бэкенд на Silk.NET.OpenGL; проверка снимком тестовой сцены (`Mvk.Desktop --screenshot`) |
| WinGL/Win32, Window.cs, Actions | Mvk.Platform | 4 | 🟨 | окно, `InputDevice`, `InputActionMap` готовы; перевод клавиш в `Engine.Actions.Keys`, буфер обмена, полноэкранный режим → этап 5 |
| Vge/Json (кроме Bbmodel), NBT (кроме NBTTools) | Mvk.Core.Json, Mvk.Core.Nbt | 1 | ✅ | паритет байтов: `Golden/core.csv`; Bbmodel и NBTTools (зависят от блоков и предметов) → Engine |
| Vge/Util: ArrayFast, Conv, DoubleList, ListByte, ListFast, ListFlout, ListMessy, PointIntersection, Pole, Rand, RegTable, Sat3d, SizeDirectory, Vertex3d, WaitHandler, WeaverSpinLock, ComparisonDistance, CorrectTable, Sundry, Ticker | Mvk.Core.Util | 1 | ✅ | Rand побитно (`core.csv`); `Ce.Tps` и `Ce.MinDesiredBatchSize` стали параметром/константой |
| Vge/Util: AxisAlignedBB, AabbOutline, Frustum, MovingObjectPosition | Mvk.Engine | 2 | 🟨 | компилируется; AxisAlignedBB зависит от `BlockPos` (World) |
| Vge/Util: Logger, Profiler, Options, OptionsFile, FileAssets | Mvk.Engine | 2 | 🟨 | компилируется; связаны с `Ce`, `Gi`, событиями |
| Vge/Util: BufferedImage, BufferedFileImage | Mvk.Engine | 2 | ✅ | PNG через StbImageSharp (RGBA→BGRA); побайтный паритет с GDI+ на 178 ассетах: `Golden/png.csv` |
| Vge/World | Mvk.Engine | 2 | 🟨 | `ChunkRender` → логический `ChunkClient` + сетки в клиенте; `ColorsBiom` → `Mvk.Engine.World`; `BlockBase`, `WorldClient`, `ChunkProviderClient`, `GeneratingAtlas` ссылаются на Renderer: разрезать |
| Vge/Entity (кроме Render) | Mvk.Engine | 2 | 🟨 | компилируется; рендер через `IEntityRenderFactory` |
| Vge/Entity/Render | Mvk.Engine.Client | 5 | ⬜ | |
| Vge/Item, Network, Management, Command, Event, Actions | Mvk.Engine | 2 | 🟨 | компилируется; `WinGL/Actions` (Keys, MouseButton) → `Mvk.Engine.Actions` |
| Vge/Games (сервер) | Mvk.Engine | 2 | 🟨 | компилируется |
| Vge/Games (клиент), Realms | логика: Mvk.Engine; окно и экраны: Mvk.Engine.Client | 2/5 | 🟨 | `GameBase` без `Warp`, отображение через `IGameClientHost` (ADR 0003); ChatList, Loading → клиент |
| Vge/Renderer | Mvk.Engine.Client (+ RHI) | 5 | 🟡 | 5.1: сетки чанков, `WorldRenderer`, `ShadersBlocks`, карта света, атлас (`tools/port/run_client.py`); 5.2: `SkyRender`, рендер сущностей (`EntitiesRenderer`, `Entity/Render/*`); 5.3: шрифты, `ChatList`, `Gui/Huds`, `Gui/Screens`, `Gui/Controls`, `Loading`, `WindowMain` (без окна ОС), `RenderMain` GUI; 5.4–5.7: `AudioBase`, `ParticlesRenderer`, `CursorRender`, `ShadowMapping` |
| Vge/Gui | Mvk.Engine.Client | 5 | ⬜ | 10 из 37 файлов вызывают GL |
| Vge/Audio (Al, Alc, AudioSample, AudioSource, AudioSources) | Mvk.Platform.Audio (`AudioDevice`, `PcmSample`) | 4 | ✅ | DllImport → Silk.NET.OpenAL; декодирование OGG побайтно: `Golden/audio.csv` (51 звук) |
| Vge/Audio (AudioBase, реестр звуков) | Mvk.Engine + Mvk.Engine.Client | 5 | ⬜ | связать ключи звуков Engine с `AudioDevice` через `IGameClientHost` |
| Vge/Сe.cs (кириллическая «С» в имени), Gi.cs | Mvk.Engine/Ce.cs, Gi.cs | 2 | 🟨 | файл переименован; из `Gi` убраны буферы вершин и рендеры блоков, палитра → `ColorPalette`, затемнение → `BlockShading` |
| Mvk2/World, Entity, Item, Command, Packets, Util, Games (логика), DebugMvk (без снимка биомов) | Mvk.Game | 3 | ✅ | `run_game.py`; генерация острова побитно: `IslandParityTests` против `Golden/island.csv` |
| Mvk2/Renderer, Gui, WindowMvk, DebugMvk | Mvk.Game.Client | 5 | 🟡 | 5.2: `SkyIslandRender`, `SkyTexturesMvk`, `EntityRenderOwner`; 5.3: `RenderMvk`, `HudMvk`, `RenderSlots`, `WindowMvk`, все `Gui/*`, `LoadingMvk`, `DebugMvkClient` (F6) |
| Mvk2/Games | Mvk.Game (+ Client) | 3/5 | ⬜ | |
| res/Assets | assets/ | 5 | ⬜ | копировать в вывод сборки |
