"""Шов 6/7: реестры без окна, атлас и картинки без System.Drawing и без видеокарты."""
import os, re, sys, shutil
root = r"E:\Projects\Games\MvkClaude"
dst = os.path.join(root, r"src\Mvk.Engine")

def load(rel):
    return open(os.path.join(dst, rel), "rb").read().decode("utf-8-sig").replace("\r\n", "\n")
def save(rel, t):
    open(os.path.join(dst, rel), "wb").write(b"\xef\xbb\xbf" + t.replace("\n", "\r\n").encode("utf-8"))

def cut(t, start, end_marker_after, rel):
    """Вырезать от строки start (включая doc-комментарий перед ней) до end_marker_after включительно."""
    i = t.find(start)
    if i < 0: print("NOT FOUND", rel, start); sys.exit(1)
    # захватить summary-комментарий над методом
    j = t.rfind("        /// <summary>", 0, i)
    if j >= 0 and t[j:i].count("\n") <= 4: i = j
    k = t.find(end_marker_after, i)
    if k < 0: print("END NOT FOUND", rel, end_marker_after); sys.exit(1)
    return t[:i] + t[k + len(end_marker_after):]

# Реестры: прогресс загрузки через IProgress<string> вместо окна
for rel, key in [(r"Entity\EntitiesReg.cs", "CreateModelsEntities"), (r"Entity\Particle\EntitiesFXReg.cs", "CreateItems"),
                 (r"Item\ItemsReg.cs", "CreateItems"), (r"World\Block\BlocksReg.cs", "CreateBlocks"),
                 (r"World\BlockEntity\BlocksEntityReg.cs", "CreateBlocksEntity")]:
    t = load(rel)
    a = "public static void Initialization(WindowMain window = null)"
    if a not in t: print("NOT FOUND", rel); sys.exit(1)
    t = t.replace(a, "public static void Initialization(IProgress<string> progress = null)")
    t = re.sub(r"if \(window != null\)\s*\{\s*window\.LScreen\.Process\((L\.T\(\"\w+\"\))\);\s*window\.DrawFrame\(\);\s*\}",
               r"progress?.Report(\1);", t)
    if "window" in re.sub(r"//.*|///.*", "", t).replace("IProgress", ""):
        print("window still used in", rel)
    if "using System;" not in t: t = "using System;\n" + t
    save(rel, t)

# BlocksReg: загрузка атласа в видеокарту уходит в клиент
t = load(r"World\Block\BlocksReg.cs")
t = cut(t, "public static void InitializationAtlas(WindowMain window)", "=> BlockItemAtlas.EndImage(window.Render.Texture);\n", "BlocksReg")
save(r"World\Block\BlocksReg.cs", t)

# EntitiesReg / ShapeBase: SetImageTexture2dArray (загрузка текстур моделей) уходит в клиент
t = load(r"Entity\EntitiesReg.cs")
i = t.find("public static void SetImageTexture2dArray(TextureMap textureMap")
j = t.rfind("        /// <summary>", 0, i)
depth = 0; k = t.find("{", i)
while True:
    c = t[k]
    if c == "{": depth += 1
    elif c == "}":
        depth -= 1
        if depth == 0: break
    k += 1
t = t[:j] + t[k + 1:].lstrip("\n")
save(r"Entity\EntitiesReg.cs", t)

t = load(r"Entity\Shape\ShapeBase.cs")
i = t.find("public void SetImageTexture2dArray(TextureMap textureMap")
j = t.rfind("        /// <summary>", 0, i)
depth = 0; k = t.find("{", i)
while True:
    c = t[k]
    if c == "{": depth += 1
    elif c == "}":
        depth -= 1
        if depth == 0: break
    k += 1
t = t[:j] + t[k + 1:].lstrip("\n")
save(r"Entity\Shape\ShapeBase.cs", t)

# GeneratingAtlas: только раскладка и пиксели, без GL и без System.Drawing
t = load(r"World\GeneratingAtlas.cs")
for u in ["using System.Drawing;\n", "using System.Drawing.Imaging;\n", "using System.Runtime.InteropServices;\n", "using Mvk.Engine.Renderer;\n"]:
    t = t.replace(u, "")
t = t.replace("""        /// <summary>
        /// Id текстуры атласа размытых блоков
        /// </summary>
        public uint _idAtlasBlurry;
        /// <summary>
        /// Id текстуры атласа блоков с чёткой резкостью
        /// </summary>
        public uint _idAtlasSharpness;
""", """        /// <summary>
        /// Размер стороны атласа в пикселях
        /// </summary>
        public int Size => _height;
        /// <summary>
        /// Пиксели атласа (B, G, R, A построчно) для загрузки в видеокарту клиентом
        /// </summary>
        public byte[] Buffer => _buffer;
        /// <summary>
        /// Маска занятых ячеек атласа (для отладки)
        /// </summary>
        public bool[,] Mask => _mask;
""")
t = cut(t, "public void EndImage(TextureMap textureMap)", "            Clear();\n        }\n", "atlas EndImage")
t = cut(t, "public void DebugSpritePng(string alias, SpriteData sprite)", "            Marshal.FreeHGlobal(ptr);\n        }\n", "atlas Debug")
t = cut(t, "public void DeleteTexture(TextureMap textureMap)", "                _idAtlasBlurry = 0;\n            }\n        }\n", "atlas Delete")
save(r"World\GeneratingAtlas.cs", t)


print("seam6 ok")
