"""Замена пространств имён оригинала на новые (общая для переноса Vge -> Mvk.Engine и Mvk2 -> Mvk.Game)."""
import re

USINGS = {
    "Vge.Util": ["Mvk.Core.Util", "Mvk.Engine.Util"],
    "Vge.NBT": ["Mvk.Core.Nbt", "Mvk.Engine.Nbt"],
    "Vge.Json": ["Mvk.Core.Json", "Mvk.Engine.Json"],
    "WinGL.Util": ["Mvk.Core.Maths"],
    "WinGL.Actions": ["Mvk.Engine.Actions"],
}

def read(p):
    b = open(p, "rb").read()
    if b.startswith(b"\xef\xbb\xbf"): b = b[3:]
    return b.decode("utf-8")

def convert(t):
    def using(m):
        ns = m.group(1)
        if ns in USINGS:
            return "\n".join("using " + n + ";" for n in USINGS[ns])
        if ns == "Vge": return "using Mvk.Engine;"
        if ns.startswith("Vge."): return "using Mvk.Engine." + ns[4:] + ";"
        if ns == "Mvk2": return "using Mvk.Game;"
        if ns.startswith("Mvk2."): return "using Mvk.Game." + ns[5:] + ";"
        return m.group(0)
    t = re.sub(r"^using ([\w.]+);", using, t, flags=re.M)
    t = re.sub(r"^namespace Vge\.NBT\b", "namespace Mvk.Engine.Nbt", t, flags=re.M)
    t = re.sub(r"^namespace Vge\b", "namespace Mvk.Engine", t, flags=re.M)
    t = re.sub(r"^namespace Mvk2\b", "namespace Mvk.Game", t, flags=re.M)
    # Полные имена в коде
    t = re.sub(r"\bVge\.Util\.", "Mvk.Engine.Util.", t)
    t = re.sub(r"\bVge\.(?=[A-Z])", "Mvk.Engine.", t)
    t = re.sub(r"\bMvk2\.(?=[A-Z])", "Mvk.Game.", t)
    t = re.sub(r"\bWinGL\.Util\.", "Mvk.Core.Maths.", t)
    # Файлы из тех же пространств, что переехали в Core, видели их типы без using
    extra = []
    for ns, core in [("Mvk.Engine.Util", "Mvk.Core.Util"), ("Mvk.Engine.Nbt", "Mvk.Core.Nbt"),
                     ("Mvk.Engine.Json", "Mvk.Core.Json"), ("Mvk.Game.Util", "Mvk.Core.Util")]:
        if re.search(r"^namespace " + re.escape(ns) + r"\b", t, flags=re.M) and ("using " + core + ";") not in t:
            extra.append("using " + core + ";")
    if extra:
        t = "\n".join(extra) + "\n" + t
    # Vge.Actions и WinGL.Actions сливаются в одно пространство: убрать повторные using
    seen = set(); out = []
    for line in t.split("\n"):
        key = line.strip().lstrip("﻿")
        if re.fullmatch(r"using [\w.]+;", key):
            if key in seen: continue
            seen.add(key)
        out.append(line)
    return "\n".join(out)
