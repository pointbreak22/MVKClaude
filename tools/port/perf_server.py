"""Производительность сервера: имена секций профайлера без аллокаций в такте (docs/improvements.md R20).
Profiler включён всегда, а имя секции нужно только для записи в лог, когда секция дольше порога.
В оригинале имя собиралось строкой на каждый активный чанк ("TickChunk " + X + ":" + Y) и на каждого моба
("Ai " + alias) в каждом такте: около 25 КБ мусора на такт. Теперь части имени запоминаются,
строка собирается только для лога; текст в логе тот же."""
from portlib import load, save, rep

rel = r"Util\Profiler.cs"
t = load(rel)
t = rep(t, """        private string _profilingSection;
""", """        private string _profilingSection;
        /// <summary>
        /// Части имени секции, строка собирается только для лога:
        /// 0 только имя, 1 имя и строка, 2 имя и два числа через двоеточие
        /// </summary>
        private int _sectionParts;
        private string _sectionText;
        private int _sectionX, _sectionY;
""", rel, 1)
t = rep(t, """        public void StartSection(string name)
        {
            _profilingSection = name;
            _profilingEnabled = true;""", """        public void StartSection(string name)
        {
            _profilingSection = name;
            _sectionParts = 0;
            _profilingEnabled = true;""", rel, 1)
t = rep(t, """        /// <summary>
        /// Закрыть проверку по времени
        /// </summary>
        public void EndSection(""", """        /// <summary>
        /// Начало секции с именем name + text, строка не собирается без записи в лог
        /// </summary>
        public void StartSection(string name, string text)
        {
            StartSection(name);
            _sectionParts = 1;
            _sectionText = text;
        }

        /// <summary>
        /// Начало секции с именем name + x + ":" + y, строка не собирается без записи в лог
        /// </summary>
        public void StartSection(string name, int x, int y)
        {
            StartSection(name);
            _sectionParts = 2;
            _sectionX = x;
            _sectionY = y;
        }

        /// <summary>
        /// Полное имя секции для лога
        /// </summary>
        private string _SectionName() => _sectionParts switch
        {
            1 => _profilingSection + _sectionText,
            2 => _profilingSection + _sectionX + ":" + _sectionY,
            _ => _profilingSection
        };

        /// <summary>
        /// Закрыть проверку по времени
        /// </summary>
        public void EndSection(""", rel, 1)
t = rep(t, "Log.Log(Srl.SomethingIsTooLong, _prefix, _profilingSection, time);",
        "Log.Log(Srl.SomethingIsTooLong, _prefix, _SectionName(), time);", rel, 1)
t = rep(t, """                    _profilingSection, _stopwatch.ElapsedTicks / (float)_timerFrequency);""",
        """                    _SectionName(), _stopwatch.ElapsedTicks / (float)_timerFrequency);""", rel, 1)
save(rel, t)

rel = r"World\WorldServer.cs"
t = load(rel)
t = rep(t, 'Filer.StartSection("TickChunk " + chunk.X + ":" + chunk.Y);',
        'Filer.StartSection("TickChunk ", chunk.X, chunk.Y);', rel, 1)
save(rel, t)

rel = r"Entity\EntityMob.cs"
t = load(rel)
t = rep(t, '_worldServer.Filer.StartSection("Ai " + Ce.Entities.EntitiesAlias[IndexEntity]);',
        '_worldServer.Filer.StartSection("Ai ", Ce.Entities.EntitiesAlias[IndexEntity]);', rel, 1)
save(rel, t)
print("perf_server ok")
