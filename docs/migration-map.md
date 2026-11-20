# Карта переноса

Статусы: ⬜ не начато, 🟨 в работе, ✅ перенесено. Паритет всего переноса: `docs/parity-checklist.md` (этап 6).

| Оригинал (original/src) | Новый проект | Этап | Статус | Заметки |
|---|---|---|---|---|
| WinGL/Util (Glm, Mth, Clipboard) | Mvk.Core.Maths (Clipboard → Mvk.Platform) | 1 | ✅ | побитный паритет: `Golden/math.csv`; Clipboard ещё не перенесён |
| WinGL/OpenGL (GL, Shader, ShaderProgram) | Mvk.Rendering (RHI) + Mvk.Rendering.OpenGL | 4 | ✅ | не перенос, а замена: RHI + бэкенд на Silk.NET.OpenGL; проверка снимком тестовой сцены (`Mvk.Desktop --screenshot`) |
| WinGL/Win32, Window.cs, Actions | Mvk.Platform | 4 | ✅ | окно, ввод через `InputActionMap`, перевод клавиш, буфер обмена, полноэкранный режим, раскладка мыши (`WorldMouseActions`) |
| Vge/Json (кроме Bbmodel), NBT (кроме NBTTools) | Mvk.Core.Json, Mvk.Core.Nbt | 1 | ✅ | паритет байтов: `Golden/core.csv`; Bbmodel и NBTTools (зависят от блоков и предметов) → Engine |
| Vge/Util: ArrayFast, Conv, DoubleList, ListByte, ListFast, ListFlout, ListMessy, PointIntersection, Pole, Rand, RegTable, Sat3d, SizeDirectory, Vertex3d, WaitHandler, WeaverSpinLock, ComparisonDistance, CorrectTable, Sundry, Ticker | Mvk.Core.Util | 1 | ✅ | Rand побитно (`core.csv`); `Ce.Tps` и `Ce.MinDesiredBatchSize` стали параметром/константой |
| Vge/Util: AxisAlignedBB, AabbOutline, Frustum, MovingObjectPosition | Mvk.Engine | 2 | ✅ | компилируется; AxisAlignedBB зависит от `BlockPos` (World) |
| Vge/Util: Logger, Profiler, Options, OptionsFile, FileAssets | Mvk.Engine | 2 | ✅ | компилируется; связаны с `Ce`, `Gi`, событиями |
| Vge/Util: BufferedImage, BufferedFileImage | Mvk.Engine | 2 | ✅ | PNG через StbImageSharp (RGBA→BGRA); побайтный паритет с GDI+ на 178 ассетах: `Golden/png.csv` |
| Vge/World | Mvk.Engine | 2 | ✅ | `ChunkRender` → логический `ChunkClient` + сетки в клиенте; `ColorsBiom` → `Mvk.Engine.World`; `BlockBase`, `WorldClient`, `ChunkProviderClient`, `GeneratingAtlas` ссылаются на Renderer: разрезать |
| Vge/Entity (кроме Render) | Mvk.Engine | 2 | ✅ | компилируется; рендер через `IEntityRenderFactory` |
| Vge/Entity/Render | Mvk.Engine.Client | 5 | ✅ | 5.2: `EntitiesRenderer`, `Entity/Render/*` (анимация костей, одежда, рука), 5.5 частицы |
| Vge/Item, Network, Management, Command, Event, Actions | Mvk.Engine | 2 | ✅ | компилируется; `WinGL/Actions` (Keys, MouseButton) → `Mvk.Engine.Actions` |
| Vge/Games (сервер) | Mvk.Engine | 2 | ✅ | компилируется |
| Vge/Games (клиент), Realms | логика: Mvk.Engine; окно и экраны: Mvk.Engine.Client | 2/5 | ✅ | `GameBase` без `Warp`, отображение через `IGameClientHost` (ADR 0003); ChatList, Loading → клиент |
| Vge/Renderer | Mvk.Engine.Client (+ RHI) | 5 | ✅ | 5.1: сетки чанков, `WorldRenderer`, `ShadersBlocks`, карта света, атлас (`tools/port/run_client.py`); 5.2: `SkyRender`, рендер сущностей (`EntitiesRenderer`, `Entity/Render/*`); 5.3: шрифты, `ChatList`, `Gui/Huds`, `Gui/Screens`, `Gui/Controls`, `Loading`, `WindowMain` (без окна ОС), `RenderMain` GUI; 5.4–5.7: `AudioBase`, `ParticlesRenderer`, `CursorRender`, `ShadowMapping` |
| Vge/Gui | Mvk.Engine.Client | 5 | ✅ | 5.3: все экраны, элементы, HUD, чат через RHI (`client_seam4/5.py`) |
| Vge/Audio (Al, Alc, AudioSample, AudioSource, AudioSources) | Mvk.Platform.Audio (`AudioDevice`, `PcmSample`) | 4 | ✅ | DllImport → Silk.NET.OpenAL; декодирование OGG побайтно: `Golden/audio.csv` (51 звук) |
| Vge/Audio (AudioBase, реестр звуков) | Mvk.Engine + Mvk.Engine.Client | 5 | ✅ | 5.4: `AudioBase` через `IWindowAudio`, `DesktopAudio` на OpenAL |
| Vge/Сe.cs (кириллическая «С» в имени), Gi.cs | Mvk.Engine/Ce.cs, Gi.cs | 2 | ✅ | файл переименован; из `Gi` убраны буферы вершин и рендеры блоков, палитра → `ColorPalette`, затемнение → `BlockShading`, состояние вида → `ViewState` (Mvk.Engine/ViewState.cs) |
| Mvk2/World, Entity, Item, Command, Packets, Util, Games (логика), DebugMvk (без снимка биомов) | Mvk.Game | 3 | ✅ | `run_game.py`; генерация острова побитно: `IslandParityTests` против `Golden/island.csv` |
| Mvk2/Renderer, Gui, WindowMvk, DebugMvk | Mvk.Game.Client | 5 | ✅ | 5.2: `SkyIslandRender`, `SkyTexturesMvk`, `EntityRenderOwner`; 5.3: `RenderMvk`, `HudMvk`, `RenderSlots`, `WindowMvk`, все `Gui/*`, `LoadingMvk`, `DebugMvkClient` (F6) |
| Mvk2/Games | Mvk.Game (+ Client) | 3/5 | ✅ | `GameModClientMvk`, `GameModServerMvk` (run_game.py), клиентская часть в `Mvk.Game.Client` |
| res/Assets | assets/ | 6 | ✅ | байт в байт (`.gitattributes`: `-text`), `Mvk.Desktop` копирует в вывод сборки; правки только `[ПОВЕДЕНИЕ]` |
| (инструмент) | tools/OrigShot | 6 | ✅ | кадры оригинала без изменений для сравнения с портом, `compare.py` |
| (новое) | src/Mvk.Rendering.Vulkan | 10 | 🟨 | бэкенд RHI на Vulkan: устройство, цепочка показа, буферы, текстуры, шейдеры GLSL 330 → SPIR-V, конвейеры, карта теней; кадры в полёте — дальше |

## Из старого проекта автора Mvk (SuperAnt30/Mvk, `original-mvk/`, только чтение)

Берутся идеи и устройство механик, код пишется свой; ассеты старого проекта не переносятся (лицензии нет,
происхождение графики и звуков неясно), вместо них свои заглушки.

| Старый Mvk | Порт | Статус |
|---|---|---|
| `MvkServer/Entity/List/EntityThrowable.cs`, `EntityExplosives.cs` (бросок, полёт, попадание лучом) | `Mvk.Engine/Entity/List/EntityProjectile.cs` (из `tools/port/new`), `Mvk.Game/Entity/List/EntityProjectileMvk.cs` (из `tools/port/game_new`), сущность `Projectile` | ✅ попадание считает сервер |
| `MvkServer/Item/List/ItemUniExplosives.cs`, `EntityExplosives.cs`, `World/Explosion.cs` (взрыв лучами) | `Mvk.Game/Item/List/ItemDynamite.cs`, `Entity/List/EntityDynamiteMvk.cs`, `World/ExplosionMvk.cs` (из `tools/port/game_new`), ассеты-заглушки `assets/port-assets.txt` | ✅ взрыв считает сервер |
| броня старого Mvk (шлем, нагрудник, поножи, ботинки; только слоты `armorInventory`, без защиты) | одежда Mvk2 с защитой: `ItemCloth.Protection` (шов `item_armor.py`), `HelmetIron`/`ChestplateIron`/`LeggingsIron`/`BootsIron` (`assets/Mvk2Items/Cloth`), снижение урона в `DamageLiving` (шов `game_seam8_projectile_armor.py`) | ✅ защиту считает сервер; вид на модели — слои существующей одежды |
| (новое) факел | блок `Torch` (`assets/Mvk2Blocks/Torch.json`, форма `Shapes/Torch.json`), предмет `Torch` (`ItemsForBlock`), шов `game_seam8_projectile_torch.py` | ✅ свет 14, без огня |
| (новое) меч, мотыга, серверный износ | `Mvk.Game/Item/List/Tool/ItemSword.cs`, `ItemHoe.cs` (из `tools/port/game_new`), `Mvk.Engine/Item/ItemWear.cs` (из `tools/port/new`), швы `item_wear.py`, `game_seam8_projectile_sword.py` | ✅ |
| (новое) износ брони | шов `game_seam8_projectile_armorwear.py`, `MaxDamage` в `assets/Mvk2Items/Cloth/*Iron.json` | ✅ |
| (новое) щит | `Mvk.Game/Item/List/ItemShield.cs` (game_new), пакет `PacketC0BBlocking` (new), швы `item_shield.py`, `game_seam8_projectile_sword_shield.py` | ✅ блок считает сервер |
| (новое) лук и стрелы | `Mvk.Game/Item/List/ItemBow.cs`, `ItemArrow.cs`, `Item/ItemAmmo.cs`, `Entity/List/EntityArrowMvk.cs` (game_new), шов `game_seam8_projectile_sword_shield_bow.py` | ✅ выстрел и расход — сервер |
| (новое) огнестрел | `Mvk.Game/Item/List/ItemGun.cs`, `ItemAmmoPart.cs`, `Entity/List/EntityBulletMvk.cs` (game_new), шов `game_seam8_projectile_sword_shield_bow_gun.py`; wav-звуки: шов `item_sound_wav.py`, `PcmSample.DecodeWav`, `assets/Mvk2Sounds/Shot.wav` | ✅ всё на сервере |
| (новое) звук взрыва | `assets/Mvk2Sounds/Explode.wav` (синтез), `ExplosionMvk` | ✅ |
| (новое) модели брони | `assets/Mvk2Entities/Layers/Base.json` (слои `Armor*`), текстура `LayerArmor.png` в `Models/Humanoid.bbmodel`, `NameLayer` в `Cloth/*Iron.json` | ✅ |
| (новое) две руки в выживании | `HandManager` (руки, клик и удержание), `InventoryBase.HandLeft`, пакет `PacketC0CHandControl` (new), швы `controls_two_hands.py`, `game_seam8_projectile_torch_hands.py` | ✅ этап 8.1 |
| (новое) добыча на сервере | `World/BlockHits.cs` (new), `ItemBase.DigAcceleration`, швы `mining_server.py`, `game_seam8_projectile_torch_hands_mining.py` | ✅ этап 8.2 |
| (новое) двуручность, сила удара, крит | `InventoryBase.HandBusy`, `ItemBase.Forearm`/`DigStrength`/`CritMultiplier`, швы `combat_two_hands.py`, `game_seam8_projectile_torch_hands_mining_combat.py` | ✅ этап 8.3 |
| (новое) щит в любой руке, «крест» | `game_new/Entity/Player/HandManagerMvkHands.cs`, швы `controls_shield_hands.py`, `game_seam8_projectile_torch_hands_mining_combat_shield.py` | ✅ этап 8.4 |
| (новое) единый бросок | `ItemBase.OnThrow`, C0C `ThrowBegin`/`ThrowEnd`, `game_new/Entity/List/EntityTorchMvk.cs`, швы `throw_hands.py`, `game_seam8_projectile_torch_hands_mining_combat_shield_throw.py` | ✅ |
