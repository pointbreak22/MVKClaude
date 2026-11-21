"""[ПОВЕДЕНИЕ] Здоровье и выносливость игрока (PlayerVitals): такт после перезарядки оружия (game_seam9_gun_reload),
трата выносливости на удар и бросок. Имя файла — после gun_reload по алфавиту."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\List\PlayerServerMvk.cs"
t = load(rel, GAME)
t = rep(t, """            Guns.Update(this);
        }""", """            Guns.Update(this);
            Vitals.Update(this);
        }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Здоровье и выносливость
        /// </summary>
        public readonly Player.PlayerVitals Vitals = new Player.PlayerVitals();

        /// <summary>
        /// [ПОВЕДЕНИЕ] Удар и бросок тратят выносливость, без неё слабее
        /// </summary>
        public override float StaminaFactor(StaminaAction action) => Vitals.Action(this, action);""", rel, 1)
save(rel, t, GAME)
print("game_seam9_hvitals ok")
