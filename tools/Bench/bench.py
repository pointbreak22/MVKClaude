"""
Замер этапа 7: загрузка мира и кадр порта (Mvk.Desktop --bench --novsync), несколько запусков, медиана.

Запуск из корня репозитория:
    dotnet build src/Hosts/Mvk.Desktop -c Release -o logs/portbin
    py tools/Bench/bench.py [запусков, по умолчанию 3] [метка] [ключи Mvk.Desktop, например --mesh-workers 1] [saved]

saved: мир создаётся одним запуском заранее, замеряется загрузка сохранённого мира (чанки читаются с диска,
генерации нет, упор в построение сеток).

Каждый запуск: новый мир (зерно 2, слот 0), взгляд 30,-20, без вертикальной синхронизации, без звука.
Числа: время до последней сетки после входа в мир, время этапов на чанк (рельеф, декор, свет, сетка),
кадр в потоке окна и кадров в секунду на готовом мире. Журналы в logs/bench/<метка>-<n>.txt,
сводка дописывается в logs/bench/summary.txt.
"""
import os
import re
import shutil
import statistics
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PORT = os.path.join(ROOT, "logs", "portbin", "Mvk2.exe")
OUT = os.path.join(ROOT, "logs", "bench")
# Дополнительные ключи Mvk.Desktop из командной строки после метки (например --mesh-workers 1)
EXTRA = []


def run_once(path, fresh=True):
    if fresh:
        shutil.rmtree(os.path.join(ROOT, "Games", "1"), ignore_errors=True)
    with open(path, "w", encoding="utf-8") as f:
        subprocess.run([PORT, "--bench", "60", "--novsync", "--look", "30,-20"] + EXTRA, cwd=ROOT,
                       stdout=f, stderr=subprocess.STDOUT, timeout=180, encoding="utf-8")
    text = open(path, encoding="utf-8", errors="replace").read()
    r = {}
    m = re.search(r"Мир загружен за ([\d.]+) с", text)
    if m: r["load_s"] = float(m.group(1))
    m = re.search(r"Кадр: ([\d.]+) мс в потоке окна, ([\d.]+) кадров/с", text)
    if m:
        r["frame_ms"] = float(m.group(1))
        r["fps"] = float(m.group(2))
    m = re.search(r"Память при загрузке: выделено ([\d.]+) МБ, сборок Gen0 (\d+), Gen1 (\d+), Gen2 (\d+)", text)
    if m:
        r["alloc_mb"] = float(m.group(1))
        r["gen0"] = int(m.group(2))
        r["gen1"] = int(m.group(3))
        r["gen2"] = int(m.group(4))
    m = re.search(r"Перестройка всех сеток: (\d+) сеток за ([\d.]+) с", text)
    if m:
        r["remesh_n"] = int(m.group(1))
        r["remesh_s"] = float(m.group(2))
    m = re.search(r"Свёртка сеток: (\S+)", text)
    if m: r["mesh_digest"] = m.group(1)
    for stage in ["Relief", "Decoration", "HeightMap", "SideLight", "Mesh", "MeshAlpha", "Pack", "Unpack"]:
        m = re.search(stage + r": (\d+) шт, (\d+) мс, ([\d.]+) мс/шт, ([\d.]+) КБ/шт", text)
        if m:
            r[stage + "_n"] = int(m.group(1))
            r[stage + "_ms"] = float(m.group(3))
            r[stage + "_kb"] = float(m.group(4))
    return r


def main():
    runs = int(sys.argv[1]) if len(sys.argv) > 1 else 3
    label = sys.argv[2] if len(sys.argv) > 2 else "run"
    global EXTRA
    EXTRA = sys.argv[3:]
    saved = len(sys.argv) > 3 and sys.argv[3] == "saved"
    os.makedirs(OUT, exist_ok=True)
    if saved:
        run_once(os.path.join(OUT, f"{label}-create.txt"))
    results = [run_once(os.path.join(OUT, f"{label}-{i + 1}.txt"), not saved) for i in range(runs)]
    keys = sorted({k for r in results for k in r})
    lines = [f"== {label}: {runs} запусков, медиана"]
    for k in keys:
        values = [r[k] for r in results if k in r]
        if isinstance(values[0], str):
            lines.append(f"  {k}: {', '.join(values)}")
            continue
        lines.append(f"  {k}: {statistics.median(values):g}  (все: {', '.join(f'{v:g}' for v in values)})")
    text = "\n".join(lines)
    print(text)
    with open(os.path.join(OUT, "summary.txt"), "a", encoding="utf-8") as f:
        f.write(text + "\n")


if __name__ == "__main__":
    main()
