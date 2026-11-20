"""[ПОВЕДЕНИЕ] Баг оригинала: DamageLiving запоминал мир сервера в конструкторе. У игрока урон создаётся раньше, чем
ему назначен мир, поэтому любой урон игроку атакой или взрывом (DamageLiving._OnAttack, частицы урона) падал
с NullReferenceException. Теперь мир берётся у сущности в момент урона."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Entity\Damage\DamageLiving.cs"
t = load(rel, GAME)
t = rep(t, "        protected readonly WorldServer _worldServer;",
        "        protected WorldServer _worldServer => _entity.GetWorldServer();", rel, 1)
t = rep(t, """            _entity = entity;
            _worldServer = entity.GetWorldServer();""", """            _entity = entity;""", rel, 1)
save(rel, t, GAME)
print("game_seam8_fix_damage_world ok")
