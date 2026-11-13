"""
Сравнение кадров оригинала и порта (этап 6).

Запуск из корня репозитория:
    py tools/OrigShot/compare.py            все сцены из SCENES
    py tools/OrigShot/compare.py shore      одна сцена

Перед запуском собрать обе стороны в Release (в Debug оба клиента рисуют отладочную карту света в углу):
    dotnet build tools/OrigShot -c Release
    dotnet build src/Hosts/Mvk.Desktop -c Release -o logs/portbin
(порт в отдельную папку: bin/Release может быть занят открытой игрой).
Для каждой сцены: оригинал снимает tools/OrigShot, порт снимает Mvk.Desktop
с тем же размером клиентской области, зерном, взглядом и командами.
Мир каждый раз создаётся с нуля, погода ясная (/weather c, смена облаков плавная, поэтому ждём 12 с),
кадр снимается при ветре около нуля (--calm): иначе волна воды даёт точки на дальней воде, как и в оригинале. Результат в logs/shots/: <сцена>-orig.png, <сцена>-port.png и
<сцена>-cmp.png (оригинал | порт | разница x4). В консоль: средняя разница и доля пикселей с разницей > 24.
"""
import os
import shutil
import subprocess
import sys

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ORIG = os.path.join(ROOT, "tools", "OrigShot", "bin", "Release", "net10.0", "OrigShot.exe")
PORT = os.path.join(ROOT, "logs", "portbin", "Mvk2.exe")
OUT = os.path.join(ROOT, "logs", "shots")

# Сцена: имя, взгляд (градусы), команды чата
SCENES = [
    ("shore", "30,-20", ["/weather c"]),
    ("down", "120,-60", ["/weather c"]),
    ("far", "200,0", ["/weather c"]),
    ("night", "30,-10", ["/weather c", "/time n"]),
    ("evening", "270,5", ["/weather c", "/time 11500"]),
]


def _run(exe, args, cwd, log):
    with open(log, "w", encoding="utf-8", errors="replace") as f:
        subprocess.run([exe] + args, cwd=cwd, stdout=f, stderr=subprocess.STDOUT, timeout=180)


def shoot(name, look, commands, shot_after="12"):
    os.makedirs(OUT, exist_ok=True)
    orig_png = os.path.join(OUT, name + "-orig.png")
    port_png = os.path.join(OUT, name + "-port.png")
    cmd_args = []
    for c in commands:
        cmd_args += ["--cmd", c]
    _run(ORIG, ["--screenshot", orig_png, "--shot-after", shot_after, "--calm", "--look", look] + cmd_args,
         os.path.dirname(ORIG), os.path.join(OUT, name + "-orig.txt"))
    w, h = open(orig_png + ".size").read().split()
    # Мир порта тоже с нуля: слот 0 лежит в Games/1 рядом с рабочей папкой
    shutil.rmtree(os.path.join(ROOT, "Games", "1"), ignore_errors=True)
    _run(PORT, ["--screenshot", port_png, "--shot-after", shot_after, "--calm", "--look", look, "--size", f"{w}x{h}"] + cmd_args,
         ROOT, os.path.join(OUT, name + "-port.txt"))
    return orig_png, port_png


def compare(name, orig_png, port_png):
    a = np.asarray(Image.open(orig_png).convert("RGB")).astype(np.int16)
    b = np.asarray(Image.open(port_png).convert("RGB")).astype(np.int16)
    if a.shape != b.shape:
        print(f"{name}: размеры разные {a.shape} и {b.shape}")
        return
    d = np.abs(a - b).max(axis=2)
    mean = float(np.abs(a - b).mean())
    share = float((d > 24).mean()) * 100
    diff = np.clip(d * 4, 0, 255).astype(np.uint8)
    diff_rgb = np.stack([diff, diff // 3, diff // 3], axis=2)
    h, w = d.shape
    out = Image.new("RGB", (w * 3, h))
    out.paste(Image.fromarray(a.astype(np.uint8)), (0, 0))
    out.paste(Image.fromarray(b.astype(np.uint8)), (w, 0))
    out.paste(Image.fromarray(diff_rgb), (w * 2, 0))
    out.save(os.path.join(OUT, name + "-cmp.png"))
    print(f"{name}: средняя разница {mean:.2f}, пикселей с разницей > 24: {share:.2f}%")


def main():
    names = sys.argv[1:]
    for name, look, commands in SCENES:
        if names and name not in names:
            continue
        orig_png, port_png = shoot(name, look, commands)
        compare(name, orig_png, port_png)


if __name__ == "__main__":
    main()
