"""Убрать using клиентских пространств имён из Engine: оставшиеся ссылки покажет компилятор по именам типов."""
import os, re
import sys
eng = sys.argv[1] if len(sys.argv) > 1 else r"E:\Projects\Games\MvkClaude\src\Mvk.Engine"
n = 0
for d, _, fs in os.walk(eng):
    if "\\bin" in d or "\\obj" in d: continue
    for f in fs:
        if not f.endswith(".cs"): continue
        p = os.path.join(d, f)
        t = open(p, "rb").read().decode("utf-8-sig")
        u = re.sub(r"^using Mvk\.Engine\.(Renderer|Gui)(\.[\w.]+)?;\r?\n", "", t, flags=re.M)
        u = re.sub(r"^using WinGL(\.[\w.]+)?;\r?\n", "", u, flags=re.M)
        if u != t:
            open(p, "wb").write(b"\xef\xbb\xbf" + u.encode("utf-8")); n += 1
print("cleanup ok", n)
