"""[ПОВЕДЕНИЕ] Запись настройки PlayerSkin (свой скин персонажа) в файл настроек."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Util\OptionsFileMvk.cs"
t = load(rel, GAME)
t = rep(t, """            file.WriteLine("BlockPlacementPreview: " + (Options.BlockPlacementPreview ? "1" : "0"));""",
        """            file.WriteLine("BlockPlacementPreview: " + (Options.BlockPlacementPreview ? "1" : "0"));
            file.WriteLine("PlayerSkin: " + (Options.PlayerSkin ? "1" : "0"));""", rel, 1)
save(rel, t, GAME)
print("game_seam9_user_skin ok")
