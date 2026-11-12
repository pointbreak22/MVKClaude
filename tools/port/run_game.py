"""Пересобирает src/Mvk.Game с нуля: перенос из original/src/Mvk2, новые файлы game_new/, швы game_seam*.py."""
import os, shutil, subprocess, sys
root = r"E:\Projects\Games\MvkClaude"
game = os.path.join(root, r"src\Mvk.Game")
g = os.path.join(root, r"tools\port")
for name in os.listdir(game):
    if name in ("Mvk.Game.csproj", "bin", "obj"): continue
    p = os.path.join(game, name)
    shutil.rmtree(p) if os.path.isdir(p) else os.remove(p)
def run(script, *args):
    r = subprocess.run([sys.executable, os.path.join(g, script), *args])
    if r.returncode != 0: sys.exit("FAILED " + script)
run("port_game.py")
new = os.path.join(g, "game_new")
if os.path.isdir(new):
    for d, _, fs in os.walk(new):
        for f in fs:
            rel = os.path.relpath(os.path.join(d, f), new)
            os.makedirs(os.path.dirname(os.path.join(game, rel)), exist_ok=True)
            shutil.copy(os.path.join(d, f), os.path.join(game, rel))
for s in sorted(n for n in os.listdir(g) if n.startswith("game_seam") and n.endswith(".py")):
    run(s)
run("cleanup.py", game)
print("run_game ok")
