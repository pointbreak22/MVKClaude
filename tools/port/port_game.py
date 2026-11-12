"""Перенос контента Mvk2 (логика, сервер, генерация) в src/Mvk.Game с заменой пространств имён.
Клиент (окно, GUI, рендер, экраны загрузки) остаётся на этап 5 (Mvk.Game.Client)."""
import os, re, sys, fnmatch
from portconv import convert, read
root = r"E:\Projects\Games\MvkClaude"
src = os.path.join(root, r"original\src\Mvk2")
dst = os.path.join(root, r"src\Mvk.Game")

client = ["Program.cs", "WindowMvk.cs", "Gui/*", "Renderer/*", "Realms/*", "Entity/Render/*"]

ported = skipped = 0
for d, _, fs in os.walk(src):
    if "\\bin" in d or "\\obj" in d: continue
    for f in fs:
        if not f.endswith(".cs"): continue
        p = os.path.join(d, f)
        rel = os.path.relpath(p, src).replace("\\", "/")
        if rel.startswith("Properties/") or any(fnmatch.fnmatch(rel, c) for c in client):
            skipped += 1; continue
        t = convert(read(p)).replace("\r\n", "\n").replace("\n", "\r\n")
        o = os.path.join(dst, rel)
        os.makedirs(os.path.dirname(o), exist_ok=True)
        open(o, "wb").write(b"\xef\xbb\xbf" + t.encode("utf-8"))
        ported += 1
print("ported", ported, "skipped", skipped)
