"""Перенос скина Майнкрафта (64x64) на модель персонажа игры (Humanoid.bbmodel, развёртка 128x256).

Первый (базовый) слой скина раскладывается на кубы модели: каждая грань куба модели получает
соответствующую грань части скина (голова, тело, руки, ноги) с учётом положения куба внутри части
(плечо, предплечье и кисть — верх, середина и низ руки скина; бедро и голень — верх и низ ноги и т. д.).
Результат — текстура UserSkin.png в Humanoid.bbmodel (персонаж «Свой скин», выбор в настройках игры).

Запуск из корня репозитория: py tools/Assets/skin_convert.py [скин.png]
По умолчанию берётся assets/Mvk2Entities/Skins/user_skin.png."""
import base64, copy, io, json, sys, uuid
from PIL import Image

MODEL = r"assets/Mvk2Entities/Models/Humanoid.bbmodel"
SKIN = sys.argv[1] if len(sys.argv) > 1 else r"assets/Mvk2Entities/Skins/user_skin.png"
NAME = "UserSkin.png"

# Части скина Майнкрафта: смещение развёртки (u, v) и размер (ширина, высота, глубина)
MC = {
    "head": ((0, 0), (8, 8, 8)),
    "body": ((16, 16), (8, 12, 4)),
    "rarm": ((40, 16), (4, 12, 4)),
    "larm": ((32, 48), (4, 12, 4)),
    "rleg": ((0, 16), (4, 12, 4)),
    "lleg": ((16, 48), (4, 12, 4)),
}
# Кости модели -> часть скина
BONE_PART = {
    "Head": "head",
    "Body": "body", "Chest": "body", "Hip": "body", "Neck": "body",
    "ArmRight": "rarm", "ForearmRight": "rarm", "HandRight": "rarm",
    "ArmLeft": "larm", "ForearmLeft": "larm", "HandLeft": "larm",
    "LegRight": "rleg", "ShinRight": "rleg", "FootRight": "rleg",
    "LegLeft": "lleg", "ShinLeft": "lleg", "FootLeft": "lleg",
}
# Кубы, задающие границы части в модели
PART_CUBES = {
    "head": ["head"],
    "body": ["neck", "stomach", "chest", "hip"],
    "rarm": ["shoulderRight", "forearmRight", "handRight"],
    "larm": ["shoulderLeft", "forearmLeft", "handLeft"],
    "rleg": ["thighRight", "shinRight"],
    "lleg": ["thighLeft", "shinLeft"],
}


def box_faces(u, v, w, h, d):
    """Грани развёртки куба в соглашении Blockbench (как и у скина Майнкрафта)"""
    return {
        "north": [u + d, v + d, u + d + w, v + d + h],
        "east": [u, v + d, u + d, v + d + h],
        "south": [u + 2 * d + w, v + d, u + 2 * d + 2 * w, v + d + h],
        "west": [u + d + w, v + d, u + 2 * d + w, v + d + h],
        "up": [u + d + w, v + d, u + d, v],
        "down": [u + 2 * d + w, v, u + d + w, v + d],
    }


def axes(face, f, t):
    """Координаты модели (горизонталь, вертикаль) точки грани с долями s, t по кубу f..t"""
    x1, y1, z1 = f
    x2, y2, z2 = t
    if face == "north":
        return lambda s, q: (x2 - s * (x2 - x1), y2 - q * (y2 - y1))
    if face == "south":
        return lambda s, q: (x1 + s * (x2 - x1), y2 - q * (y2 - y1))
    if face == "east":
        return lambda s, q: (z2 - s * (z2 - z1), y2 - q * (y2 - y1))
    if face == "west":
        return lambda s, q: (z1 + s * (z2 - z1), y2 - q * (y2 - y1))
    if face == "up":
        return lambda s, q: (x2 - s * (x2 - x1), z2 - q * (z2 - z1))
    return lambda s, q: (x2 - s * (x2 - x1), z1 + q * (z2 - z1))


def norm(face, f, t, h, v):
    """Доли s, t точки (h, v) на грани части f..t (обратно к axes)"""
    x1, y1, z1 = f
    x2, y2, z2 = t
    c = lambda a: max(0.0, min(1.0, a))
    if face == "north":
        return c((x2 - h) / (x2 - x1)), c((y2 - v) / (y2 - y1))
    if face == "south":
        return c((h - x1) / (x2 - x1)), c((y2 - v) / (y2 - y1))
    if face == "east":
        return c((z2 - h) / (z2 - z1)), c((y2 - v) / (y2 - y1))
    if face == "west":
        return c((h - z1) / (z2 - z1)), c((y2 - v) / (y2 - y1))
    if face == "up":
        return c((x2 - h) / (x2 - x1)), c((z2 - v) / (z2 - z1))
    return c((x2 - h) / (x2 - x1)), c((v - z1) / (z2 - z1))


def main():
    d = json.load(open(MODEL, encoding="utf-8"))
    skin = Image.open(SKIN).convert("RGBA")
    if skin.size != (64, 64):
        raise SystemExit("Нужен скин 64x64")
    W, H = d["resolution"]["width"], d["resolution"]["height"]
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    po, so = out.load(), skin.load()
    elements = {e["uuid"]: e for e in d["elements"]}
    cube_bone = {}

    def walk(nodes, bone):
        for n in nodes:
            if isinstance(n, str):
                cube_bone[n] = bone
            else:
                walk(n.get("children", []), n["name"])
    walk(d["outliner"], "")
    by_name = {e["name"]: e for e in d["elements"] if not e["name"].startswith("L-")}
    boxes = {}
    for part, names in PART_CUBES.items():
        f = [min(by_name[n]["from"][i] for n in names) for i in range(3)]
        t = [max(by_name[n]["to"][i] for n in names) for i in range(3)]
        boxes[part] = (f, t)
    for uid, e in elements.items():
        part = BONE_PART.get(cube_bone.get(uid, ""))
        if part is None or e["name"].startswith("L-") or e["name"] == "cube":
            continue
        (mu, mv), (mw, mh, md) = MC[part]
        src = box_faces(mu, mv, mw, mh, md)
        pf, pt = boxes[part]
        for face, fd in e.get("faces", {}).items():
            uv = fd.get("uv")
            if not uv or fd.get("texture") is None:
                continue
            to_model = axes(face, e["from"], e["to"])
            su = src[face]
            u0, u1 = sorted((uv[0], uv[2]))
            v0, v1 = sorted((uv[1], uv[3]))
            for py in range(int(v0), int(round(v1))):
                for px in range(int(u0), int(round(u1))):
                    if not (0 <= px < W and 0 <= py < H):
                        continue
                    # Доли пикселя на грани в направлении развёртки
                    s = ((px + .5) - uv[0]) / (uv[2] - uv[0]) if uv[2] != uv[0] else .5
                    q = ((py + .5) - uv[1]) / (uv[3] - uv[1]) if uv[3] != uv[1] else .5
                    h, v = to_model(s, q)
                    sp, tp = norm(face, pf, pt, h, v)
                    sx = su[0] + sp * (su[2] - su[0])
                    sy = su[1] + tp * (su[3] - su[1])
                    sx = int(min(max(sx - (.5 if su[2] < su[0] else 0), 0), 63.999))
                    sy = int(min(max(sy - (.5 if su[3] < su[1] else 0), 0), 63.999))
                    c = so[sx, sy]
                    po[px, py] = (c[0], c[1], c[2], 255)
    buf = io.BytesIO()
    out.save(buf, "PNG")
    tex = [t for t in d["textures"] if t["name"] == "Robinson.png"][0]
    new = copy.deepcopy(tex)
    new.update({"name": NAME, "uuid": str(uuid.uuid4()), "path": "", "relative_path": "",
                "source": "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode()})
    d["textures"] = [t for t in d["textures"] if t["name"] != NAME]
    new["id"] = str(len(d["textures"]))
    d["textures"].append(new)
    open(MODEL, "w", encoding="utf-8", newline="\n").write(json.dumps(d, ensure_ascii=False, separators=(",", ":")))
    print("skin ok", [t["name"] for t in d["textures"]])


main()
