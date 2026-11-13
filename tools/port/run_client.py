"""Пересобирает src/Mvk.Engine.Client с нуля: перенос из original/src/Vge (port_client.py),
новые файлы client_new/, швы client_seam*.py."""
import os, shutil, subprocess, sys
root = r"E:\Projects\Games\MvkClaude"
client = os.path.join(root, r"src\Mvk.Engine.Client")
g = os.path.join(root, r"tools\port")
for name in os.listdir(client):
    if name in ("Mvk.Engine.Client.csproj", "bin", "obj"): continue
    p = os.path.join(client, name)
    shutil.rmtree(p) if os.path.isdir(p) else os.remove(p)
def run(script, *args):
    r = subprocess.run([sys.executable, os.path.join(g, script), *args])
    if r.returncode != 0: sys.exit("FAILED " + script)
run("port_client.py")
new = os.path.join(g, "client_new")
if os.path.isdir(new):
    for d, _, fs in os.walk(new):
        for f in fs:
            rel = os.path.relpath(os.path.join(d, f), new)
            os.makedirs(os.path.dirname(os.path.join(client, rel)), exist_ok=True)
            shutil.copy(os.path.join(d, f), os.path.join(client, rel))
for s in sorted(n for n in os.listdir(g) if n.startswith("client_seam") and n.endswith(".py")):
    run(s)
print("run_client ok")
