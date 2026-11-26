"""Без изменения поведения: убран отладочный вывод урона в консоль из оригинала (DamageBase.OnAttack), при многих
мобах он нагружал консоль на каждом ударе."""
from portlib import load, save, rep

rel = r"Entity\DamageBase.cs"
t = load(rel)
t = rep(t, """            Console.Write("OnAttack " + damage);\n""", "", rel, 1)
t = rep(t, """            Console.WriteLine(" => " + damage);\n""", "", rel, 1)
save(rel, t)
print("quiet_damage ok")
