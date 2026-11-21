"""[ПОВЕДЕНИЕ] Шов клиента: полоски здоровья над мобами (HealthBarRender) после сущностей и предметов."""
import os
from portlib import load, save, rep, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

rel = r"Renderer\World\Entity\EntitiesRenderer.cs"
t = load(rel, C)
t = rep(t, """        private HitboxEntityRender? _hitbox;""", """        private HitboxEntityRender? _hitbox;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Полоски здоровья над мобами
        /// </summary>
        private HealthBarRender? _healthBars;""", rel, 1)
t = rep(t, """            if (IsHitBox)
            {
                // Есть хит бокс сущности""", """            // [ПОВЕДЕНИЕ] Полоски здоровья над мобами
            _healthBars ??= new HealthBarRender(Render);
            _healthBars.Draw(_game, _arrayChunkRender, timeIndex);

            if (IsHitBox)
            {
                // Есть хит бокс сущности""", rel, 1)
t = rep(t, """            _hitbox?.Dispose();""", """            _hitbox?.Dispose();
            _healthBars?.Dispose();""", rel, 1)
save(rel, t, C)
print("client health bar ok")
