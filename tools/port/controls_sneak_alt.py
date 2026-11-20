"""[ПОВЕДЕНИЕ] Присесть — Alt (Keys.Menu) вместо Shift (просьба пользователя).
- Options.ControlSneak по умолчанию Keys.Menu; в options.ini прежнее значение Shift (16, его писала игра сама —
  экрана переназначения клавиш нет) читается как Alt.
- В мире Alt больше не открывает меню паузы (было Escape или Alt): иначе Alt и приседал бы, и открывал меню.
  В открытом меню паузы Alt по-прежнему закрывает его (ScreenInGameMenu), это не мешает.
- Shift остаётся модификатором (Keyboard.KeyShift, IsKeyShift): не активировать блок вторым действием,
  перекладывание стаков в инвентаре, бросок с замахом (камень, динамит, яйцо) даже при взгляде на сущность."""
from portlib import load, save, rep

rel = r"Util\Options.cs"
t = load(rel)
t = rep(t, "        public static int ControlSneak = (int)Keys.ShiftKey;", "        public static int ControlSneak = (int)Keys.Menu;", rel, 1)
save(rel, t)

rel = r"Util\OptionsFile.cs"
t = load(rel)
t = rep(t, """                case "ControlSneak": Options.ControlSneak = int.Parse(value); return true;""",
"""                case "ControlSneak":
                    // [ПОВЕДЕНИЕ] Shift из прежних настроек — теперь Alt (присесть)
                    Options.ControlSneak = int.Parse(value);
                    if (Options.ControlSneak == (int)Keys.ShiftKey) Options.ControlSneak = (int)Keys.Menu;
                    return true;""", rel, 1)
if "using Mvk.Engine.Actions;" not in t:
    t = "using Mvk.Engine.Actions;\n" + t
save(rel, t)

rel = r"Actions\Keyboard.cs"
t = load(rel)
t = rep(t, "                if (keys == Keys.Escape || keys == Keys.Menu) _OnInGameMenu();",
        "                if (keys == Keys.Escape) _OnInGameMenu();", rel, 1)
save(rel, t)
print("controls_sneak_alt ok")
