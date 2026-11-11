"""Карта зависимостей Vge: какие типы клиентских папок используют серверные файлы."""
import os, re, sys, collections
src = r"E:\Projects\Games\MvkClaude\original\src\Vge"
client_dirs = ["Renderer", "Gui", "Audio", "Realms", os.path.join("Entity", "Render")]
client_files = {"WindowMain.cs", "Debug.cs", "L.cs", "Warp.cs", "AssetsTexture.cs"}

def read(p):
    return open(p, "rb").read().decode("utf-8-sig", errors="replace")

def is_client(rel):
    return any(rel.startswith(d + os.sep) for d in client_dirs) or rel in client_files

types = {}  # type name -> rel file
for d, _, fs in os.walk(src):
    for f in fs:
        if not f.endswith(".cs"): continue
        p = os.path.join(d, f); rel = os.path.relpath(p, src)
        for m in re.finditer(r"\b(?:class|struct|interface|enum)\s+(\w+)", read(p)):
            types.setdefault(m.group(1), rel)

client_types = {t: r for t, r in types.items() if is_client(r)}
uses = collections.defaultdict(set)
for d, _, fs in os.walk(src):
    for f in fs:
        if not f.endswith(".cs"): continue
        p = os.path.join(d, f); rel = os.path.relpath(p, src)
        if is_client(rel) or rel.startswith(("Json", "NBT")): continue
        t = re.sub(r"//.*", "", read(p))
        for name in set(re.findall(r"\b[A-Z]\w+\b", t)):
            if name in client_types and types.get(name) != rel:
                uses[rel].add(name)
for rel in sorted(uses):
    print(rel, ":", ", ".join(sorted(uses[rel])))
print("server files using client types:", len(uses))
