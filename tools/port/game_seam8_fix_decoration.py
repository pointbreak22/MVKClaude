"""[ПОВЕДЕНИЕ] Исправление бага оригинала (docs/known-issues.md п. 4):
ChunkProviderGenerateIsland.Decoration после цикла по 9 соседним чанкам вызывал DecorationsColumnAfter
для биома последнего соседа из AreaOne9priority (переменная biome перезаписывалась в цикле), а не для
биома самого чанка. На границе биомов чанк получил бы декор чужого биома.
Сейчас баг скрытый: список декора After пуст у всех биомов, а зерно генератора задаётся заново для каждого
чанка, поэтому генерация не меняется (Golden/island.csv совпадает). Правка нужна, чтобы декор After,
добавленный в биом, ставился в своём биоме."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"World\Gen\ChunkProviderGenerateIsland.cs"
t = load(rel, GAME)
t = rep(t, """            biome.DecorationsColumnAfter(chunk);""", """            // Декор после областей по биому самого чанка (в оригинале шёл биом последнего соседа из цикла)
            _biomes[chunk.Biome[136]].DecorationsColumnAfter(chunk);""", rel, 1)
save(rel, t, GAME)
print("game_seam8_fix_decoration ok")
