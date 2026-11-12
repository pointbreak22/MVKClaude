"""Mvk2: остатки окна и System.Drawing в логике.
- пакет склада уходит открытому экрану через хост;
- снимок карты биомов в PNG (F6, отладка) рисует System.Drawing: переедет в Mvk.Game.Client (этап 5)."""
import os, re
from portlib import load, save, rep, sub, remove_member, fail, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")
def L(rel): return load(rel, GAME)
def S(rel, t): save(rel, t, GAME)

rel = r"Entity\List\PlayerClientOwnerMvk.cs"; t = L(rel)
S(rel, rep(t, "_game.ModClient.Screen?.AcceptNetworkPackage(packet);", "_game.Host.AcceptNetworkPackage(packet);", rel, 1))

rel = "DebugMvk.cs"; t = L(rel)
for sig in ["public static void ScreenFileBiomeArea(GameBase game)", "private static void _FileArea(long seed, int id, GameBase game)",
            "private static Color _ConvertBiomeHeight(int index)", "private static Color _ConvertBiome(EnumBiomeIsland biome)"]:
    t = remove_member(t, sig, rel)
for u in ["using System.Drawing;\n", "using System.Diagnostics;\n", "using Mvk.Game.World.Gen.Layer;\n",
          "using Mvk.Engine.World.Gen.Layer;\n", "using Mvk.Engine.Games;\n"]:
    t = rep(t, u, "", rel)
if "Bitmap" in t or "Color." in t: fail(rel + ": System.Drawing still used")
S(rel, t)
print("game_seam2 ok")
