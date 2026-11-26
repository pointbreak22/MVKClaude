"""Без изменения поведения: убран отладочный вывод здоровья и части тела в консоль из оригинала (DamageLiving)."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\Damage\DamageLiving.cs"
t = load(rel, GAME)
t = rep(t, """            Console.WriteLine("GetHealth" + _entity.GetHealth());\n""", "", rel, 1)
t = rep(t, """            Console.Write(" " + enumBody);\n""", "", rel, 1)
save(rel, t, GAME)
print("game_seam9_quiet_damage ok")
