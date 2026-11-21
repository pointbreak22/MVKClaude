"""Запуск: py tools/Assets/item_models.py. Свои объёмные модели предметов порта (по образцу кирки и лопаты: кубы в единицах 1/16 блока, хват у начала оси Y)
и общая текстура-палитра Items/Tools/PortPalette.png (32x32, клетки 4x4 единицы UV)."""
import os, re
from PIL import Image

R = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "assets")
COLORS = {
    "wood": (139, 94, 52), "darkwood": (92, 60, 32), "steel": (178, 184, 194), "lightsteel": (220, 225, 232),
    "black": (40, 40, 44), "red": (190, 40, 30), "gold": (212, 170, 60), "orange": (240, 130, 30),
    "yellow": (255, 220, 90), "white": (235, 235, 230), "stone": (120, 120, 112), "leather": (110, 70, 40),
    "darksteel": (95, 100, 110), "rope": (180, 145, 95), "paper": (220, 200, 160), "grey": (150, 150, 150),
}
NAMES = list(COLORS)

im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
px = im.load()
for i, n in enumerate(NAMES):
    cx, cy = i % 4, i // 4
    for y in range(8):
        for x in range(8):
            r, g, b = COLORS[n]
            k = 1.0 if (x + y) % 3 else .93  # лёгкая фактура
            px[cx * 8 + x, cy * 8 + y] = (int(r * k), int(g * k), int(b * k), 255)
im.save(os.path.join(R, r"Mvk2Textures\Items\Tools\PortPalette.png"))


def uv(color):
    i = NAMES.index(color)
    cx, cy = i % 4, i // 4
    return f"[{cx * 4 + 1}, {cy * 4 + 1}, {cx * 4 + 3}, {cy * 4 + 3}]"


def el(f, t, color, rotate=None, origin=None):
    faces = ",\n".join(f'                {{ Texture:"T0", Side:"{s}", Uv:{uv(color)}}}'
                       for s in ("North", "East", "South", "West", "Up", "Down"))
    rot = f"\n            Rotate: {list(rotate)},\n            Origin: {list(origin)}," if rotate else ""
    return f"""        {{   From: {list(f)},
            To: {list(t)},{rot}
            Faces: [
{faces}
            ]
        }}"""


MODELS = {
    "Knife": [
        el((-1, 0, -1), (1, 6, 1), "darkwood"), el((-2, 6, -1.5), (2, 7, 1.5), "darksteel"),
        el((-.5, 7, -1.5), (.5, 17, 1.5), "steel"), el((-.5, 17, -1), (.5, 19, .5), "lightsteel")],
    "Spear": [
        el((-.75, -10, -.75), (.75, 30, .75), "wood"), el((-1, 30, -1), (1, 32, 1), "darksteel"),
        el((-.5, 32, -2), (.5, 38, 2), "steel"), el((-.5, 38, -1), (.5, 41, 1), "lightsteel")],
    "SwordIron": [
        el((-1.5, -1, -1.5), (1.5, 0, 1.5), "gold"), el((-1, 0, -1), (1, 6, 1), "leather"),
        el((-4, 6, -1.5), (4, 7.5, 1.5), "gold"), el((-.6, 7.5, -2), (.6, 26, 2), "steel"),
        el((-.6, 26, -1), (.6, 28, 1), "lightsteel")],
    "Musket": [
        # Кремнёвый пистолет: рукоять в ладони (у начала осей), ствол над кистью вперёд (+Y), курок сбоку
        el((-1, -2.5, -4.5), (1, .5, 1.5), "wood"), el((-1.2, -3, -5.5), (1.2, .5, -4.5), "gold"),
        el((-1, -1.5, 1.5), (1, 7, 3), "darkwood"), el((-.75, -1, 3), (.75, 14, 4.5), "darksteel"),
        el((-1, 13, 2.75), (1, 14, 4.75), "gold"), el((-1, 6.5, 2.75), (1, 7.5, 4.75), "gold"),
        el((1, -.5, 2.5), (1.6, 2.5, 4.5), "steel"), el((1, -1.5, 4), (1.6, -.5, 6), "darksteel"),
        el((-.3, .5, -.5), (.3, 2.5, 1.5), "gold"), el((-.2, 1, .5), (.2, 1.5, 1.5), "black")],
    "DoubleBarrel": [
        el((-1.5, -12, -2), (1.5, 4, 2), "wood"), el((-2, 4, -2), (2, 9, 2), "lightsteel"),
        el((-1, 9, -.1), (1, 36, 1.9), "black"), el((-1, 9, -2.1), (1, 36, -.1), "black"),
        el((-1.5, 10, -2.6), (1.5, 20, 2.6), "wood"), el((-.3, 2, -3), (.3, 4, -2), "black")],
    "Bow": [
        el((-1, -2.5, -1), (1, 2.5, 1), "leather"), el((-.75, 2.5, -.5), (.75, 8, 1), "wood"),
        el((-.75, 8, .5), (.75, 12, 2), "wood"), el((-.6, 12, 1.5), (.6, 14, 3), "darkwood"),
        el((-.75, -8, -.5), (.75, -2.5, 1), "wood"), el((-.75, -12, .5), (.75, -8, 2), "wood"),
        el((-.6, -14, 1.5), (.6, -12, 3), "darkwood"), el((-.2, -13, 2.9), (.2, 13, 3.2), "white")],
    "Shield": [
        el((3, -5, -8), (4, 11, 8), "wood"), el((2.8, -5.5, -8.5), (4.2, -4.5, 8.5), "darksteel"),
        el((2.8, 10.5, -8.5), (4.2, 11.5, 8.5), "darksteel"), el((2.8, -5.5, -8.5), (4.2, 11.5, -7.5), "darksteel"),
        el((2.8, -5.5, 7.5), (4.2, 11.5, 8.5), "darksteel"), el((4, 1, -2), (5, 5, 2), "steel"),
        el((2, 1, -1), (3, 5, 1), "leather")],
    "Torch": [
        el((-1, 0, -1), (1, 12, 1), "wood"), el((-1.3, 11, -1.3), (1.3, 12, 1.3), "darkwood"),
        el((-1.2, 12, -1.2), (1.2, 15, 1.2), "orange"), el((-.7, 15, -.7), (.7, 16.5, .7), "yellow")],
    "Flint": [
        el((-3, 0, -1), (3, 1.5, 1), "darksteel"), el((-3, 1.5, -1), (-2, 3, 1), "darksteel"),
        el((-1.5, 1.5, -1.2), (1.5, 4.5, 1.2), "stone")],
    "Hook": [
        el((-.75, 0, -.75), (.75, 14, .75), "darksteel"), el((-1.2, -1, -1.2), (1.2, 0, 1.2), "rope"),
        el((.75, 12, -.5), (4, 13.5, .5), "darksteel"), el((3, 13.5, -.5), (4, 17, .5), "steel"),
        el((-4, 12, -.5), (-.75, 13.5, .5), "darksteel"), el((-4, 13.5, -.5), (-3, 17, .5), "steel"),
        el((-.5, 12, .75), (.5, 13.5, 4), "darksteel"), el((-.5, 13.5, 3), (.5, 17, 4), "steel")],
    "Arrow": [
        # Стрела по длине +Y: древко, наконечник, оперение двумя перекрещёнными пластинами
        el((-.4, 0, -.4), (.4, 20, .4), "wood"), el((-.9, 20, -.3), (.9, 22, .3), "steel"),
        el((-.5, 22, -.2), (.5, 23.5, .2), "lightsteel"), el((-1.5, 1, -.1), (1.5, 5, .1), "white"),
        el((-.1, 1, -1.5), (.1, 5, 1.5), "red")],
    "Dynamite": [
        el((-1.5, 0, -1.5), (1.5, 9, 1.5), "red"), el((-1.6, 2, -1.6), (1.6, 3, 1.6), "black"),
        el((-1.6, 6, -1.6), (1.6, 7, 1.6), "black"), el((-.3, 9, -.3), (.3, 12, .3), "paper")],
}

for name, els in MODELS.items():
    body = ",\n".join(els)
    open(os.path.join(R, rf"Mvk2Items\Shapes\Tools\Port{name}.json"), "w", encoding="utf-8", newline="\n").write(
        "{\n    Texture: {\n        T0: \"Items/Tools/PortPalette\"\n    },\n    Elements: [\n" + body + "\n    ]\n}\n")
print("ok", list(MODELS))
