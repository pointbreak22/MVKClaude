"""Общие помощники для скриптов швов: правка файлов Engine с проверкой, что замена нашлась."""
import os, re, sys
ROOT = r"E:\Projects\Games\MvkClaude"
ENG = os.path.join(ROOT, r"src\Mvk.Engine")
ORIG = os.path.join(ROOT, r"original\src")

def load(rel, base=ENG):
    return open(os.path.join(base, rel), "rb").read().decode("utf-8-sig").replace("\r\n", "\n")

def save(rel, t, base=ENG):
    p = os.path.join(base, rel)
    os.makedirs(os.path.dirname(p), exist_ok=True)
    open(p, "wb").write(b"\xef\xbb\xbf" + t.replace("\n", "\r\n").encode("utf-8"))

def fail(msg):
    print("FAIL:", msg); sys.exit(1)

def rep(t, a, b, rel="", count=None):
    """Заменить строку a на b; если не найдено, остановиться"""
    if a not in t: fail(f"{rel}: not found: {a[:80]!r}")
    if count is not None and t.count(a) != count: fail(f"{rel}: expected {count}, got {t.count(a)}: {a[:60]!r}")
    return t.replace(a, b)

def sub(t, pattern, b, rel="", flags=0):
    n, k = re.subn(pattern, b, t, flags=flags)
    if k == 0: fail(f"{rel}: regex not found: {pattern[:80]!r}")
    return n

def remove_member(t, signature, rel=""):
    """Удалить член класса по началу сигнатуры вместе с doc-комментарием и атрибутами над ним"""
    i = t.find(signature)
    if i < 0: fail(f"{rel}: member not found: {signature[:80]!r}")
    line_start = t.rfind("\n", 0, i) + 1
    # подняться по строкам комментариев/атрибутов
    start = line_start
    while True:
        prev_end = start - 1
        prev_start = t.rfind("\n", 0, prev_end) + 1
        line = t[prev_start:prev_end].strip()
        if line.startswith("///") or line.startswith("[") and line.endswith("]"):
            start = prev_start
        else:
            break
    # тело: либо { ... } , либо => ... ;
    j = i
    brace = t.find("{", i); arrow = t.find("=>", i); semi = t.find(";", i)
    if arrow >= 0 and (brace < 0 or arrow < brace) and semi >= 0 and (brace < 0 or semi < brace):
        end = semi + 1
    else:
        depth = 0; k = brace
        while True:
            c = t[k]
            if c == "{": depth += 1
            elif c == "}":
                depth -= 1
                if depth == 0: break
            k += 1
        end = k + 1
    # съесть перевод строки и одну пустую строку после
    if t[end:end + 1] == "\n": end += 1
    if t[end:end + 1] == "\n": end += 1
    return t[:start] + t[end:]

def add_using(t, ns):
    u = "using " + ns + ";"
    if u in t: return t
    return u + "\n" + t

def remove_field(t, line_text, rel=""):
    """Удалить однострочный член (поле) вместе с doc-комментарием и атрибутами над ним"""
    i = t.find(line_text)
    if i < 0: fail(f"{rel}: field not found: {line_text[:80]!r}")
    start = t.rfind("\n", 0, i) + 1
    end = t.find("\n", i) + 1
    while True:
        prev_end = start - 1
        if prev_end <= 0: break
        prev_start = t.rfind("\n", 0, prev_end) + 1
        line = t[prev_start:prev_end].strip()
        if line.startswith("///") or (line.startswith("[") and line.endswith("]")):
            start = prev_start
        else:
            break
    return t[:start] + t[end:]
