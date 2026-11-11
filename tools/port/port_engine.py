"""РџРµСЂРµРЅРѕСЃ СЃРµСЂРІРµСЂРЅРѕР№ С‡Р°СЃС‚Рё Vge РІ src/Mvk.Engine СЃ Р·Р°РјРµРЅРѕР№ РїСЂРѕСЃС‚СЂР°РЅСЃС‚РІ РёРјС‘РЅ.
Р—Р°РїСѓСЃРє: python port_engine.py [--dry]"""
import os, re, sys, fnmatch
root = r"E:\Projects\Games\MvkClaude"
src = os.path.join(root, r"original\src\Vge")
dst = os.path.join(root, r"src\Mvk.Engine")

# РЈР¶Рµ РІ Mvk.Core
in_core = ["Json/IJson.cs", "Json/JsonArray.cs", "Json/JsonCompound.cs", "Json/JsonKeyValue.cs",
           "Json/JsonRead.cs", "Json/JsonValue.cs", "NBT/*.cs"]
core_util = ["ArrayFast", "Conv", "DoubleList", "ListByte", "ListFast", "ListFlout", "ListMessy",
             "PointIntersection", "Pole", "Rand", "RegTable", "Sat3d", "SizeDirectory", "Vertex3d",
             "WaitHandler", "WeaverSpinLock", "ComparisonDistance", "CorrectTable", "Sundry", "Ticker"]
in_core += ["Util/" + n + ".cs" for n in core_util]
in_core.remove("NBT/*.cs"); in_core += ["NBT/" + n for n in os.listdir(os.path.join(src, "NBT")) if n != "NBTTools.cs"]

# РљР»РёРµРЅС‚ (СЌС‚Р°Рї 5, Mvk.Engine.Client)
client = ["Renderer/*", "Gui/*", "WindowMain.cs", "Warp.cs", "AssetsTexture.cs",
          "Audio/Al.cs", "Audio/Alc.cs", "Audio/AudioBase.cs", "Audio/AudioSample.cs", "Audio/AudioSource.cs",
          "Audio/AudioSources.cs",
          "Realms/ChatList.cs", "Realms/Loading.cs",
          "Entity/Render/EntityRenderAbstract.cs", "Entity/Render/EntityRenderAnimation.cs",
          "Entity/Render/EntityRenderClient.cs", "Entity/Render/EntityRenderEyeMouth.cs",
          "Entity/Render/EntityRenderItem.cs", "Entity/Render/EntityRenderParticle.cs",
          "Gi.cs",
          "Util/BufferedFileImage.cs"]
# Р›РѕРіРёРєР° РєР»РёРµРЅС‚Р° РѕСЃС‚Р°С‘С‚СЃСЏ РІ Engine (headless-РєР»РёРµРЅС‚), СЂРµРЅРґРµСЂ СѓС…РѕРґРёС‚ Р·Р° РёРЅС‚РµСЂС„РµР№СЃС‹: СЃРј. docs/adr/0003

def match(rel, pats):
    return any(fnmatch.fnmatch(rel, p) for p in pats)

def read(p):
    b = open(p, "rb").read()
    if b.startswith(b"\xef\xbb\xbf"): b = b[3:]
    return b.decode("utf-8")

USINGS = {
    "Vge.Util": ["Mvk.Core.Util", "Mvk.Engine.Util"],
    "Vge.NBT": ["Mvk.Core.Nbt", "Mvk.Engine.Nbt"],
    "Vge.Json": ["Mvk.Core.Json", "Mvk.Engine.Json"],
    "WinGL.Util": ["Mvk.Core.Maths"],
}

def convert(t):
    def using(m):
        ns = m.group(1)
        if ns in USINGS:
            return "\n".join("using " + n + ";" for n in USINGS[ns])
        if ns == "Vge": return "using Mvk.Engine;"
        if ns.startswith("Vge."): return "using Mvk.Engine." + ns[4:] + ";"
        return m.group(0)
    t = re.sub(r"^using ([\w.]+);", using, t, flags=re.M)
    t = re.sub(r"^namespace Vge\.NBT\b", "namespace Mvk.Engine.Nbt", t, flags=re.M)
    t = re.sub(r"^namespace Vge\b", "namespace Mvk.Engine", t, flags=re.M)
    # РџРѕР»РЅС‹Рµ РёРјРµРЅР° РІ РєРѕРґРµ
    t = re.sub(r"\bVge\.Util\.", "Mvk.Engine.Util.", t)
    t = re.sub(r"\bVge\.(?=[A-Z])", "Mvk.Engine.", t)
    t = re.sub(r"\bWinGL\.Util\.", "Mvk.Core.Maths.", t)
    # Р¤Р°Р№Р»С‹ РёР· С‚РµС… Р¶Рµ РїСЂРѕСЃС‚СЂР°РЅСЃС‚РІ, С‡С‚Рѕ РїРµСЂРµРµС…Р°Р»Рё РІ Core, РІРёРґРµР»Рё РёС… С‚РёРїС‹ Р±РµР· using
    extra = []
    for ns, core in [("Mvk.Engine.Util", "Mvk.Core.Util"), ("Mvk.Engine.Nbt", "Mvk.Core.Nbt"),
                     ("Mvk.Engine.Json", "Mvk.Core.Json")]:
        if re.search(r"^namespace " + re.escape(ns) + r"\b", t, flags=re.M) and ("using " + core + ";") not in t:
            extra.append("using " + core + ";")
    if extra:
        t = "\n".join(extra) + "\n" + t
    return t

dry = "--dry" in sys.argv
ported = skipped = 0
for d, _, fs in os.walk(src):
    for f in fs:
        if not f.endswith(".cs"): continue
        p = os.path.join(d, f)
        rel = os.path.relpath(p, src).replace("\\", "/")
        if match(rel, in_core) or match(rel, client):
            skipped += 1; continue
        out_rel = rel.replace("NBT/", "Nbt/")
        if re.fullmatch(r".e\.cs", out_rel): out_rel = "Ce.cs"  # РІ РѕСЂРёРіРёРЅР°Р»Рµ В«РЎВ» РєРёСЂРёР»Р»РёС‡РµСЃРєР°СЏ
        if rel == "SR.cs": out_rel = "Sr.cs"
        if rel == "SRL.cs": out_rel = "Srl.cs"
        t = convert(read(p)).replace("\r\n", "\n").replace("\n", "\r\n")
        if not dry:
            o = os.path.join(dst, out_rel)
            os.makedirs(os.path.dirname(o), exist_ok=True)
            open(o, "wb").write(b"\xef\xbb\xbf" + t.encode("utf-8"))
        ported += 1
print("ported", ported, "skipped", skipped)

