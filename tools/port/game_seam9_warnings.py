"""Mvk2: предупреждения из оригинала. Отладочные поля, которым присваивают, но не читают
(их чтение закомментировано в оригинале): оставлены как есть, предупреждение подавлено точечно."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

for rel, line in [(r"Entity\AI\Move\EntityAIFindEatGrass.cs", "        private bool _isSapling = false;\n"),
                  (r"World\Gen\ChunkProviderGenerateIsland.cs", "        private int iMin = int.MaxValue;\n        private int iMax = int.MinValue;\n")]:
    t = load(rel, GAME)
    t = rep(t, line, "#pragma warning disable CS0414 // отладочное поле оригинала, чтение закомментировано\n"
            + line + "#pragma warning restore CS0414\n", rel, 1)
    save(rel, t, GAME)
print("game_seam9_warnings ok")
