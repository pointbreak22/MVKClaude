"""
Прогон механики: одни и те же сценарии нажатий в оригинале (OrigShot) и в порте (Mvk.Desktop),
след игрока на каждом такте (позиция, скорость, на земле, блок под прицелом) должен совпасть побитно.

Запуск из корня репозитория (сборки как для compare.py, см. README.md):
    py tools/OrigShot/mechanics.py            все сценарии
    py tools/OrigShot/mechanics.py walk-swim  один

След в logs/shots/mech-<сценарий>-orig.csv и -port.csv. Мир каждый раз новый, зерно 2, игрок на точке
появления; сценарий начинается через 8 секунд после входа в мир (чанки вокруг уже пришли).
"""
import os
import shutil
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ORIG = os.path.join(ROOT, "tools", "OrigShot", "bin", "Release", "net10.0", "OrigShot.exe")
PORT = os.path.join(ROOT, "logs", "portbin", "Mvk2.exe")
OUT = os.path.join(ROOT, "logs", "shots")

# Сценарий: имя, взгляд (градусы), нажатия по тактам, [команды чата до начала], [последнее состояние сущностей
# обязано совпасть: по умолчанию да]
SCENARIOS = [
    # Шаг к воде, прыжок, падение в воду, плавание
    ("walk-swim", "30,0", "0:W+;20:Space+;22:Space-;100:W-;140:end"),
    # Бег с прыжками в сторону острова
    ("sprint-jump", "270,0", "0:ControlKey+;0:W+;10:Space+;12:Space-;40:Space+;42:Space-;80:W-;80:ControlKey-;110:end"),
    # Крадучись
    ("sneak", "270,0", "0:ShiftKey+;0:W+;60:W-;60:ShiftKey-;80:end"),
    # Вбок, назад, вбок
    ("strafe", "270,0", "0:A+;30:A-;30:S+;60:S-;60:D+;90:D-;110:end"),
    # Ломать блоки под ногами пустой рукой в творческом режиме (в выживании песок рукой не ломается),
    # первый щелчок, как у человека, включает обзор мышью; игрок падает в яму
    ("dig", "30,-70", "0:D2+;1:D2-;2:MouseLeft+;3:MouseLeft-;5:MouseLeft+;120:MouseLeft-;150:end", ["/gamemode c"]),
    # Поставить блок из творческого режима нельзя без предмета: правая кнопка на блок пустой рукой
    ("use", "30,-70", "0:D2+;1:D2-;2:MouseRight+;3:MouseRight-;5:MouseRight+;40:MouseRight-;60:end", ["/gamemode c"]),
    # Прыжок на месте в воде
    ("swim-up", "30,0", "0:W+;40:W-;40:Space+;100:Space-;140:end"),
    # Три курицы перед игроком (отладочная /summon, в OrigShot та же команда), 25 секунд ИИ
    ("mobs", "270,0", "5:/summon 3 4;500:end"),
    # Курицы вплотную, игрок идёт на них (столкновения, толчки). Толчок зависит от того, в какой такт сервера
    # придёт позиция игрока: два запуска оригинала тоже расходятся (191 из 197 состояний), конец не сравнивается
    ("mobs-push", "270,0", "5:/summon 3 2;40:W+;120:W-;300:end", (), False),
]


def _run(exe, args, cwd, log):
    with open(log, "w", encoding="utf-8", errors="replace") as f:
        subprocess.run([exe] + args, cwd=cwd, stdout=f, stderr=subprocess.STDOUT, timeout=180)


def run(name, look, script, commands=(), strict_end=True):
    os.makedirs(OUT, exist_ok=True)
    orig = os.path.join(OUT, f"mech-{name}-orig.csv")
    port = os.path.join(OUT, f"mech-{name}-port.csv")
    common = ["--script", script, "--shot-after", "8", "--look", look]
    for c in commands:
        common += ["--cmd", c]
    _run(ORIG, common + ["--trace", orig], os.path.dirname(ORIG), os.path.join(OUT, f"mech-{name}-orig.txt"))
    shutil.rmtree(os.path.join(ROOT, "Games", "1"), ignore_errors=True)
    _run(PORT, common + ["--trace", port], ROOT, os.path.join(OUT, f"mech-{name}-port.txt"))
    a = open(orig, encoding="utf-8").read().splitlines() if os.path.exists(orig) else []
    b = open(port, encoding="utf-8").read().splitlines() if os.path.exists(port) else []
    if not a or not b:
        print(f"{name}: нет следа (оригинал {len(a)}, порт {len(b)} строк)")
        return False
    if len(a) != len(b):
        print(f"{name}: разная длина {len(a)} и {len(b)}")
        return False
    # Игрок: побитно по тактам (столбцы до блока под прицелом включительно)
    for i, (x, y) in enumerate(zip(a, b)):
        if x.split(";")[:9] != y.split(";")[:9]:
            print(f"{name}: расхождение в строке {i}:\n  оригинал {x}\n  порт     {y}")
            return False
    # Сущности считает сервер в своём потоке: пакет приходит на такт раньше или позже даже между двумя
    # запусками оригинала. Сравниваются последовательности разных состояний (без повторов подряд):
    # общая подпоследовательность и последнее состояние
    ea = _distinct([x.split(";")[9] for x in a[1:]])
    eb = _distinct([y.split(";")[9] for y in b[1:]])
    common = _lcs(ea, eb)
    share = common / max(len(ea), len(eb))
    same_end = ea[-1] == eb[-1]
    print(f"{name}: игрок совпадает, {len(a) - 1} тактов; сущности: состояний {len(ea)} и {len(eb)}, "
          f"общих {common} ({share:.0%}), последнее {'совпадает' if same_end else 'разное'}")
    # Хаотичные сценарии (толчки) расходятся и у оригинала с самим собой: порог ниже, конец не обязателен
    return share >= .9 and same_end if strict_end else share >= .75


def _distinct(values):
    out = []
    for v in values:
        if not out or out[-1] != v:
            out.append(v)
    return out


def _lcs(a, b):
    prev = [0] * (len(b) + 1)
    for x in a:
        cur = [0]
        for j, y in enumerate(b):
            cur.append(prev[j] + 1 if x == y else max(prev[j + 1], cur[j]))
        prev = cur
    return prev[-1]


def main():
    names = sys.argv[1:]
    ok = True
    for scenario in SCENARIOS:
        name, look, script = scenario[:3]
        if names and name not in names:
            continue
        ok &= run(name, look, script, *scenario[3:])
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
