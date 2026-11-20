"""Звуки порта в wav (свои синтезированные: выстрел, взрыв): ключ Sounds.json с расширением .wav берётся как есть,
без «.ogg» (SampleReg). Декодирует хост (PcmSample.DecodeWav). Звуки оригинала остаются в ogg."""
from portlib import load, save, rep

rel = r"Audio\SampleReg.cs"
t = load(rel)
t = rep(t, """        public SampleReg(string key)
        {""", """        public SampleReg(string key)
        {
            if (key.EndsWith(".wav"))
            {
                // Звук порта в wav: путь с расширением, ключ без пути и расширения
                Path = key;
                string name = key.Substring(0, key.Length - 4);
                Key = name.Contains("/") ? name.Substring(name.LastIndexOf("/") + 1) : name;
                return;
            }""", rel, 1)
save(rel, t)
print("item_sound_wav ok")
