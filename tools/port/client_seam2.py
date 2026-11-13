"""Шов клиента 2: строки и предупреждения.
- Sr.OutOfRangeArray и Sr.ItIsImpossibleToCompareTwoObjects ушли в Mvk.Core (CoreStrings, internal),
  клиенту даём свои ClientStrings (tools/port/client_new/ClientStrings.cs).
- CS0168: неиспользуемая переменная light2 в TextureLightMap (в оригинале то же предупреждение).
"""
import os, re
from portlib import load, save, fail, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

n = 0
for d, _, fs in os.walk(os.path.join(C, "Renderer")):
    for f in fs:
        if not f.endswith(".cs"): continue
        r = os.path.relpath(os.path.join(d, f), C)
        t = load(r, C)
        u = t.replace("Sr.GetString(Sr.OutOfRangeArray,", "ClientStrings.GetString(ClientStrings.OutOfRangeArray,")
        u = u.replace("Sr.ItIsImpossibleToCompareTwoObjects", "ClientStrings.ItIsImpossibleToCompareTwoObjects")
        if u != t:
            save(r, u, C); n += 1
if n != 4: fail(f"client_seam2: expected 4 files with Sr strings, got {n}")

rel = r"Renderer\World\TextureLightMap.cs"
t = load(rel, C)
t, k = re.subn(r"^[ \t]*float light2;\n", "", t, flags=re.M)
if k != 2: fail(f"{rel}: expected 2 light2, got {k}")
save(rel, t, C)
print("client_seam2 ok")
