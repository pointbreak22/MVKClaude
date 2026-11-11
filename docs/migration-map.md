# Карта переноса

Статусы: ⬜ не начато, 🟨 в работе, ✅ перенесено и покрыто тестами.

| Оригинал (original/src) | Новый проект | Этап | Статус | Заметки |
|---|---|---|---|---|
| WinGL/Util (Glm, Mth, Clipboard) | Mvk.Core.Maths (Clipboard → Mvk.Platform) | 1 | ✅ | побитный паритет: `Golden/math.csv`; Clipboard ещё не перенесён |
| WinGL/OpenGL (GL, Shader, ShaderProgram) | Mvk.Rendering.OpenGL | 4 | ⬜ | заменяется Silk.NET.OpenGL |
| WinGL/Win32, Window.cs, Actions | Mvk.Platform | 4 | ⬜ | Silk.NET Windowing/Input, клавиши → InputAction |
| Vge/Json (кроме Bbmodel), NBT (кроме NBTTools) | Mvk.Core.Json, Mvk.Core.Nbt | 1 | ✅ | паритет байтов: `Golden/core.csv`; Bbmodel и NBTTools (зависят от блоков и предметов) → Engine |
| Vge/Util: ArrayFast, Conv, DoubleList, ListByte, ListFast, ListFlout, ListMessy, PointIntersection, Pole, Rand, RegTable, Sat3d, SizeDirectory, Vertex3d, WaitHandler, WeaverSpinLock, ComparisonDistance, CorrectTable, Sundry, Ticker | Mvk.Core.Util | 1 | ✅ | Rand побитно (`core.csv`); `Ce.Tps` и `Ce.MinDesiredBatchSize` стали параметром/константой |
| Vge/Util: AxisAlignedBB, AabbOutline, Frustum, MovingObjectPosition | Mvk.Engine | 2 | ⬜ | AxisAlignedBB зависит от `BlockPos` (World) |
| Vge/Util: Logger, Profiler, Options, OptionsFile, FileAssets | Mvk.Engine | 2 | ⬜ | связаны с `Ce`, `Gi`, событиями |
| Vge/Util: BufferedImage, BufferedFileImage | Mvk.Engine.Client | 5 | ⬜ | System.Drawing только Windows: заменить декодер PNG |
| Vge/World | Mvk.Engine | 2 | ⬜ | `BlockBase`, `WorldClient`, `ChunkProviderClient`, `GeneratingAtlas` ссылаются на Renderer: разрезать |
| Vge/Entity (кроме Render) | Mvk.Engine | 2 | ⬜ | |
| Vge/Entity/Render | Mvk.Engine.Client | 5 | ⬜ | |
| Vge/Item, Network, Management, Command, Event, Actions | Mvk.Engine | 2 | ⬜ | |
| Vge/Games (сервер) | Mvk.Engine | 2 | ⬜ | |
| Vge/Games (клиент), Realms | Mvk.Engine.Client | 5 | ⬜ | |
| Vge/Renderer | Mvk.Engine.Client (+ RHI) | 5 | ⬜ | 38 из 58 файлов напрямую вызывают GL |
| Vge/Gui | Mvk.Engine.Client | 5 | ⬜ | 10 из 37 файлов вызывают GL |
| Vge/Audio (Al, Alc) | Mvk.Platform (OpenAL) + Mvk.Engine.Client | 4–5 | ⬜ | DllImport → Silk.NET.OpenAL |
| Vge/Сe.cs (кириллическая «С» в имени) | Mvk.Engine/Ce.cs | 2 | ⬜ | переименовать файл |
| Mvk2/World, Entity, Item, Command, Packets, Util | Mvk.Game | 3 | ⬜ | golden-тесты генерации |
| Mvk2/Renderer, Gui, WindowMvk, DebugMvk | Mvk.Game.Client | 5 | ⬜ | |
| Mvk2/Games | Mvk.Game (+ Client) | 3/5 | ⬜ | |
| res/Assets | assets/ | 5 | ⬜ | копировать в вывод сборки |
