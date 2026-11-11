# GoldenGen: эталонные хэши со старого генератора

Собирает исходники оригинала (`original/src`, коммит `dfbd814`) **без изменений** в три сборки
(`Original/WinGL`, `Original/Vge`, `Original/Mvk2`), поднимает `GameServer` без окна и звука,
генерирует мир «Остров» и записывает хэши чанков.

Не входит в `MvkClaude.slnx`, потому что `original/` не хранится в git.

## Запуск
```
git clone https://github.com/SuperAnt30/Vge.git original   # если ещё нет; затем git -C original checkout dfbd814
cd tools/GoldenGen
dotnet build -c Release
dotnet bin/Release/net10.0/GoldenGen.dll ../../tests/Mvk.Game.Tests/Golden/island.csv 2
```
Аргументы: `[файл] [радиус] [seed1 seed2 ...]`. По умолчанию seed: 1, 2, 20241004, -987654321.

## Что в файле
`seed;chunkX;chunkZ;blocks;biomes;heightMapGen;light;blockEntities`
- `blocks` — SHA-256 по всем секциям `ChunkStorage.Data` (id + met + доп. жидкость);
- `biomes` — SHA-256 массива `Biome[256]`;
- `heightMapGen` — SHA-256 карты высот генерации;
- `light` — SHA-256 освещения секций сразу после генерации;
- `blockEntities` — количество блок-сущностей в чанке.

Проверяются 8 областей 5×5 чанков (центр координат, точка первого спавна, стороны острова),
итого 200 чанков на seed. Результат детерминирован: повторный запуск даёт тот же файл.

## Как это повторяет запуск игры
- Регистрация как в `WindowMvk` (блоки, предметы, сущности), но без атласа текстур.
- `AudioReg.Initialization(false)`: только ключи звуков, OpenAL не нужен.
- Реестры создаются заново перед каждым seed, как при каждом `WindowMain.GameLocalRun`.
