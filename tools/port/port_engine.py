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
          "Util/BufferedFileImage.cs"]
# Р›РѕРіРёРєР° РєР»РёРµРЅС‚Р° РѕСЃС‚Р°С‘С‚СЃСЏ РІ Engine (headless-РєР»РёРµРЅС‚), СЂРµРЅРґРµСЂ СѓС…РѕРґРёС‚ Р·Р° РёРЅС‚РµСЂС„РµР№СЃС‹: СЃРј. docs/adr/0003

def match(rel, pats):
    return any(fnmatch.fnmatch(rel, p) for p in pats)

from portconv import USINGS, convert, read

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

