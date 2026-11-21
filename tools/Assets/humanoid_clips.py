"""Свои клипы порта в Humanoid.bbmodel и Player.json (Zombie.json, Bandit.json):
- AttackSword: рубящий удар мечом дугой сверху (клинок вперёд), вместо тычка кулаком AttackRight;
- зеркальные клипы левой руки: для каждого клипа из MIRROR создаётся клип стороны Left (…Right → …Left, иначе
  имя + Left): кости Right↔Left (HoldItemOne↔HoldItemTwo), поворот по Y и Z с обратным знаком, смещение по X
  с обратным знаком. Записи в json сущностей — копия записи клипа с переставленными весами костей.
Запуск из корня репозитория: py tools/Assets/humanoid_clips.py (повторный запуск пересоздаёт клипы)."""
import copy, json, re, uuid

MODEL = r"assets/Mvk2Entities/Models/Humanoid.bbmodel"
ENTITIES = [r"assets/Mvk2Entities/Player.json", r"assets/Mvk2Entities/Zombie.json", r"assets/Mvk2Entities/Bandit.json"]
MIRROR = ["ShotRight", "ShotTwo", "ActionShovel", "ActionBlock", "AttackSword"]

d = json.load(open(MODEL, encoding="utf-8"))
anims = d["animations"]
by = {a["name"]: a for a in anims}
bones = {}


def walk(nodes):
    for n in nodes:
        if isinstance(n, dict):
            bones[n["name"]] = n["uuid"]
            walk(n.get("children", []))


walk(d["outliner"])


def side(name):
    if name == "HoldItemOne": return "HoldItemTwo"
    if name == "HoldItemTwo": return "HoldItemOne"
    if name.endswith("Right"): return name[:-5] + "Left"
    if name.endswith("Left"): return name[:-4] + "Right"
    return name


def left_name(code):
    return code[:-5] + "Left" if code.endswith("Right") else code + "Left"


def neg(v):
    v = str(v).strip()
    return v[1:] if v.startswith("-") else ("0" if v in ("0", "") else "-" + v)


def put(anim):
    old = by.get(anim["name"])
    if old in anims: anims.remove(old)
    anims.append(anim)
    by[anim["name"]] = anim


def kf(channel, time, x, y, z):
    return {"channel": channel, "data_points": [{"x": str(x), "y": str(y), "z": str(z)}], "uuid": str(uuid.uuid4()),
            "time": time, "color": -1, "interpolation": "linear", "bezier_linked": True,
            "bezier_left_time": [-0.1, -0.1, -0.1], "bezier_left_value": [0, 0, 0],
            "bezier_right_time": [0.1, 0.1, 0.1], "bezier_right_value": [0, 0, 0]}


# Рубящий удар мечом: замах вверх-назад с отведённой кистью, рубка дугой вниз-вперёд, возврат
sword = copy.deepcopy(by["AttackRight"])
sword["name"] = "AttackSword"
sword["uuid"] = str(uuid.uuid4())
sword["length"] = 0.5
keys = {
    "ArmRight": [(0, 0, 0, 0), (0.1, -150, -20, 25), (0.2, -55, 20, -10), (0.32, -25, 10, 0), (0.5, 0, 0, 0)],
    "ForearmRight": [(0, 0, 0, 0), (0.1, -45, 0, 0), (0.2, -10, 0, 0), (0.5, 0, 0, 0)],
    "HandRight": [(0, 0, 0, 0), (0.1, -35, 0, 0), (0.2, 25, 0, 0), (0.5, 0, 0, 0)],
    "Chest": [(0, 0, 0, 0), (0.1, 0, 25, 0), (0.2, 0, -15, 0), (0.5, 0, 0, 0)],
}
sword["animators"] = {bones[b]: {"name": b, "type": "bone", "keyframes": [kf("rotation", t, x, y, z) for t, x, y, z in ks]}
                      for b, ks in keys.items()}
put(sword)

for code in MIRROR:
    src = by[code]
    m = copy.deepcopy(src)
    m["name"] = left_name(code)
    m["uuid"] = str(uuid.uuid4())
    animators = {}
    for v in m["animators"].values():
        name = side(v["name"])
        v["name"] = name
        for k in v["keyframes"]:
            k["uuid"] = str(uuid.uuid4())
            for p in k["data_points"]:
                if k["channel"] == "rotation":
                    p["y"], p["z"] = neg(p["y"]), neg(p["z"])
                elif k["channel"] == "position":
                    p["x"] = neg(p["x"])
        animators[bones[name]] = v
    m["animators"] = animators
    put(m)

open(MODEL, "w", encoding="utf-8", newline="\n").write(json.dumps(d, ensure_ascii=False, separators=(",", ":")))

# Записи клипов в json сущностей
for path in ENTITIES:
    t = open(path, encoding="utf-8", newline="").read()
    nl = "\r\n" if "\r\n" in t else "\n"
    t = t.replace("\r\n", "\n")
    entries = {m.group(1): m.group(0) for m in re.finditer(
        r'        \{\n            Code: "(\w+)",\n(?:            .*\n|                .*\n)*?        \}', t)}
    add = []
    if "AttackSword" not in entries:
        e = entries["AttackRight"].replace('"AttackRight"', '"AttackSword"')
        entries["AttackSword"] = e
        add.append(e)
    for code in MIRROR:
        name = left_name(code)
        if name in entries: continue
        e = entries[code].replace(f'"{code}"', f'"{name}"')
        e = re.sub(r"(\w+)(: \d+)", lambda m2: side(m2.group(1)) + m2.group(2) if m2.group(1) in bones else m2.group(0), e)
        add.append(e)
    if add:
        i = t.rindex("\n    ]")
        t = t[:i] + "\n" + ",\n".join(add) + "," + t[i:]
    open(path, "w", encoding="utf-8", newline="").write(t.replace("\n", nl))
print("clips ok", [left_name(c) for c in MIRROR])
