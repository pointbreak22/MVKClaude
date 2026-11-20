"""[ПОВЕДЕНИЕ] Свет факела в руке (hand_light.py): невидимый светящийся блок TorchLight (как воздух, свет 14)
регистрируется последним, чтобы id прежних блоков не сдвигались; движок ставит его в клетку головы игрока."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"World\Block\BlocksRegMvk.cs"
t = load(rel, GAME)
t = rep(t, """            Torch = BlocksReg.RegisterBlockClass("Torch", materials.Rope);
""", """            Torch = BlocksReg.RegisterBlockClass("Torch", materials.Rope);
            // [ПОВЕДЕНИЕ] Свет факела в руке: как воздух, но светит (своего json нет, как у воздуха)
            BlockTorchLight torchLight = new BlockTorchLight(materials.Air);
            torchLight.InitAirAlias("TorchLight");
            BlocksReg.Table.Add("TorchLight", torchLight);
            Mvk.Engine.World.HandLight.Block = torchLight;
""", rel, 1)
save(rel, t, GAME)
print("game light ok")
