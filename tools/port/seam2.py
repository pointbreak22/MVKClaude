import os, re, sys
dst = r"E:\Projects\Games\MvkClaude\src\Mvk.Engine"

def edit(rel, pairs, regex=False):
    p = os.path.join(dst, rel)
    t = open(p, "rb").read().decode("utf-8-sig").replace("\r\n", "\n")
    for a, b in pairs:
        n = re.sub(a, b, t) if regex else t.replace(a, b)
        if n == t:
            print("NOT FOUND in", rel, ":", a[:70]); sys.exit(1)
        t = n
    open(p, "wb").write(b"\xef\xbb\xbf" + t.replace("\n", "\r\n").encode("utf-8"))

edit(r"World\Block\BlockBase.cs", [
    ("public BlockRenderFull BlockRender;", "public EnumBlockRender RenderType;"),
    ("=> BlockRender = Gi.BlockRendFull;", "=> RenderType = EnumBlockRender.Full;"),
])
edit(r"World\Block\List\BlockAlpha.cs", [("=> BlockRender = Gi.BlockAlphaRendFull;", "=> RenderType = EnumBlockRender.AlphaFull;")])
edit(r"World\Block\List\BlockLiquid.cs", [("=> BlockRender = Gi.BlockLiquidAlphaRendFull;", "=> RenderType = EnumBlockRender.LiquidAlpha;")])
edit(r"World\Block\QuadSide.cs", [(r"\bGi\.LightPoles\b", "BlockShading.LightPoles")], regex=True)
edit(r"World\Block\SideLiquid.cs", [(r"\bGi\.LightPoles\b", "BlockShading.LightPoles")], regex=True)
edit(r"Entity\Particle\EntityCubeFX.cs", [(r"\bGi\.Color(Red|Green|Blue)\b", r"ColorPalette.Color\1")], regex=True)
for rel in [r"World\Block\BlockBase.cs"]:
    p = os.path.join(dst, rel)
    t = open(p, "rb").read().decode("utf-8-sig")
    t = t.replace("using Mvk.Engine.Renderer.World;\r\n", "")
    open(p, "wb").write(b"\xef\xbb\xbf" + t.encode("utf-8"))

print("seam2 ok")
