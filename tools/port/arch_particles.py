"""Технический п. 3: частицы клиента (списки, создание, физика и жизнь в такте) из рендера в Engine (ParticleList).
Такт частиц вызывает игра (GameBase.OnTick) прямо перед тактом хоста, где раньше их считал рендер мира;
между ними рендер обновлял только карту света, поэтому порядок и результат те же."""
from portlib import load, save, rep, add_using

rel = r"Games\GameBase.cs"
t = load(rel)
t = rep(t, """        public WorldClient World { get; private set; }""", """        public WorldClient World { get; private set; }
        /// <summary>
        /// Частицы клиента: создание, физика и жизнь (рендер частиц только рисует их)
        /// </summary>
        public readonly ParticleList Particles = new ParticleList();""", rel, 1)
t = rep(t, """                // Обновить рендоровский мир
                Host.OnTick(deltaTime);""", """                // Физика частиц и их жизнь (в оригинале в такте рендера мира, перед ним только карта света)
                Particles.Update(World, deltaTime);
                // Обновить рендоровский мир
                Host.OnTick(deltaTime);""", rel, 1)
t = add_using(t, "Mvk.Engine.Entity.Particle")
save(rel, t)
print("arch_particles ok")
