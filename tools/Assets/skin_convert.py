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


def paint(po, W, H, so, e, part, boxes, keep_alpha):
    """Разложить часть скина so на грани куба e"""
    (mu, mv), (mw, mh, md) = MC[part]
    src = box_faces(mu, mv, mw, mh, md)
    pf, pt = boxes[part]
    for face, fd in e.get("faces", {}).items():
        uv = fd.get("uv")
        if not uv:
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
                po[px, py] = c if keep_alpha else (c[0], c[1], c[2], 255)


def put_texture(d, name, image, template="Robinson.png"):
    """Внести или заменить текстуру модели"""
    buf = io.BytesIO()
    image.save(buf, "PNG")
    tex = [t for t in d["textures"] if t["name"] == template][0]
    new = copy.deepcopy(tex)
    new.update({"name": name, "uuid": str(uuid.uuid4()), "path": "", "relative_path": "",
                "source": "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode()})
    d["textures"] = [t for t in d["textures"] if t["name"] != name]
    new["id"] = str(len(d["textures"]))
    d["textures"].append(new)


# Одежда из второго слоя: группа слоя модели -> часть скина
LAYER_PART = {
    "L-JacketB1": "body", "L-JacketFRC": "rarm", "L-JacketARC": "rarm", "L-JacketFLC": "larm", "L-JacketALC": "larm",
    "L-PantsB1": "body", "L-PantsH1": "body", "L-PantsR1": "rleg", "L-PantsSR1": "rleg",
    "L-PantsL1": "lleg", "L-PantsSL1": "lleg",
    "L-ShoesSR1": "rleg", "L-ShoesFR1": "rleg", "L-ShoesSL1": "lleg", "L-ShoesFL1": "lleg",
    "L-HoodU": "head",
}
# Второй слой скина: область -> где лежит такая же часть первого слоя
OVERLAY = [((32, 0, 64, 16), (0, 0)), ((16, 32, 40, 48), (16, 16)), ((40, 32, 56, 48), (40, 16)),
           ((48, 48, 64, 64), (32, 48)), ((0, 32, 16, 48), (0, 16)), ((0, 48, 16, 64), (16, 48))]
HOOD_FROM, HOOD_TO, HOOD_UV = [-7, 46.5, -6.5], [7, 60.5, 7.5], [48, 220]
# Слот лица: маска — пластина на всё лицо, очки — полоса на уровне глаз (группа, от, до, смещение развёртки)
FACE_CUBES = [("L-MaskU", [-7, 47, -7], [7, 60, -6], [0, 128]),
              ("L-GlassesU", [-7, 53, -7.5], [7, 56, -6.5], [32, 128])]


def ensure_group(d, name, f_, t_, uv, parent="_Hat"):
    """Добавить группу слоя с одним кубом в группу parent (если её ещё нет)"""
    def find(nodes, n_):
        for n in nodes:
            if isinstance(n, dict):
                if n["name"] == n_:
                    return n
                r = find(n.get("children", []), n_)
                if r:
                    return r
        return None
    if find(d["outliner"], name):
        return
    hat = find(d["outliner"], parent)
    lhat = find(d["outliner"], "L-Hat")
    cube = copy.deepcopy([e for e in d["elements"] if e["uuid"] == lhat["children"][0]][0])
    size = [t_[i] - f_[i] for i in range(3)]
    f = box_faces(uv[0], uv[1], size[0], size[1], size[2])
    cube.update({"from": f_, "to": t_, "rotation": [0, 0, 0], "origin": [0, 47, 0],
                 "uv_offset": uv, "uuid": str(uuid.uuid4()), "box_uv": True, "visibility": True})
    for face in cube["faces"]:
        cube["faces"][face]["uv"] = [int(round(x)) for x in f[face]]
    d["elements"].append(cube)
    group = copy.deepcopy({k: v for k, v in lhat.items() if k != "children"})
    group.update({"name": name, "uuid": str(uuid.uuid4()), "children": [cube["uuid"]], "visibility": True})
    hat["children"].append(group)


def ensure_hood(d):
    """Куб капюшона (группа L-HoodU в _Hat головы), чуть больше головы"""
    def find(nodes, name):
        for n in nodes:
            if isinstance(n, dict):
                if n["name"] == name:
                    return n
                r = find(n.get("children", []), name)
                if r:
                    return r
        return None
    if find(d["outliner"], "L-HoodU"):
        return
    hat = find(d["outliner"], "_Hat")
    lhat = find(d["outliner"], "L-Hat")
    cube = copy.deepcopy([e for e in d["elements"] if e["uuid"] == lhat["children"][0]][0])
    size = [HOOD_TO[i] - HOOD_FROM[i] for i in range(3)]
    f = box_faces(HOOD_UV[0], HOOD_UV[1], size[0], size[1], size[2])
    cube.update({"from": HOOD_FROM, "to": HOOD_TO, "rotation": [0, 0, 0], "origin": [0, 47, 0],
                 "uv_offset": HOOD_UV, "uuid": str(uuid.uuid4()), "box_uv": True, "visibility": True})
    for face in cube["faces"]:
        cube["faces"][face]["uv"] = [int(round(x)) for x in f[face]]
    d["elements"].append(cube)
    group = copy.deepcopy({k: v for k, v in lhat.items() if k != "children"})
    group.update({"name": "L-HoodU", "uuid": str(uuid.uuid4()), "children": [cube["uuid"]], "visibility": True})
    hat["children"].append(group)


def main():
    d = json.load(open(MODEL, encoding="utf-8"))
    skin = Image.open(SKIN).convert("RGBA")
    if skin.size != (64, 64):
        raise SystemExit("Нужен скин 64x64")
    W, H = d["resolution"]["width"], d["resolution"]["height"]
    ensure_hood(d)
    for name, f_, t_, uv in FACE_CUBES:
        ensure_group(d, name, f_, t_, uv)
    elements = {e["uuid"]: e for e in d["elements"]}
    cube_bone, cube_group = {}, {}

    def walk(nodes, bone, group):
        for n in nodes:
            if isinstance(n, str):
                cube_bone[n] = bone
                cube_group[n] = group
            else:
                name = n["name"]
                walk(n.get("children", []), group if name.startswith(("L-", "_")) else name, name)
    walk(d["outliner"], "", "")
    by_name = {e["name"]: e for e in d["elements"] if not e["name"].startswith("L-")}
    boxes = {}
    for part, names in PART_CUBES.items():
        f = [min(by_name[n]["from"][i] for n in names) for i in range(3)]
        t = [max(by_name[n]["to"][i] for n in names) for i in range(3)]
        boxes[part] = (f, t)

    # Первый слой — текстура персонажа UserSkin
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    po, so = out.load(), skin.load()
    for uid, e in elements.items():
        part = BONE_PART.get(cube_bone.get(uid, ""))
        if part is None or e["name"].startswith("L-") or e["name"] == "cube":
            continue
        paint(po, W, H, so, e, part, boxes, False)
    put_texture(d, NAME, out)

    # Второй слой поверх первого — одежда (куртка, штаны, обувь); капюшон — только второй слой
    full = skin.copy()
    over = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    for (x1, y1, x2, y2), (bx, by) in OVERLAY:
        region = skin.crop((x1, y1, x2, y2))
        full.alpha_composite(region, (bx, by))
        over.paste(region, (bx, by))
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    pl = layer.load()
    for uid, e in elements.items():
        part = LAYER_PART.get(cube_group.get(uid, ""))
        if part is None or "Armor" in cube_group.get(uid, ""):
            continue
        hood = cube_group[uid] == "L-HoodU"
        paint(pl, W, H, (over if hood else full).load(), e, part, boxes, True)
    # Слот лица: маска — лицо первого слоя (на скине пользователя это маска), очки — второй слой на уровне глаз;
    # только передняя грань, остальные прозрачны
    face_names = {n for n, _, _, _ in FACE_CUBES}
    for uid, e in elements.items():
        name = cube_group.get(uid, "")
        if name not in face_names:
            continue
        front = {"north": e["faces"]["north"]}
        paint(pl, W, H, (skin if name == "L-MaskU" else over).load(), {**e, "faces": front}, "head", boxes, True)
    put_texture(d, "LayerUserSkin.png", layer, "LayerBase.png")
    open(MODEL, "w", encoding="utf-8", newline="\n").write(json.dumps(d, ensure_ascii=False, separators=(",", ":")))
    print("skin ok", [t["name"] for t in d["textures"]])


main()
