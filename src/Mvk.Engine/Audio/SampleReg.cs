#nullable enable
namespace Mvk.Engine.Audio
{
    /// <summary>
    /// Структура семпла для регистрации
    /// </summary>
    public struct SampleReg
    {
        public readonly string Key;
        public readonly string Path;

        public SampleReg(string key)
        {
            if (key.EndsWith(".wav"))
            {
                // Звук порта в wav: путь с расширением, ключ без пути и расширения
                Path = key;
                string name = key.Substring(0, key.Length - 4);
                Key = name.Contains("/") ? name.Substring(name.LastIndexOf("/") + 1) : name;
                return;
            }
            if (key.Contains("/"))
            {
                int index = key.LastIndexOf("/") + 1;
                Key = key.Substring(index, key.Length - index);
                Path = key + ".ogg";
            }
            else
            {
                Key = key;
                Path = key + ".ogg";
            }
        }

        public override string ToString()
        {
            return Key;
        }
    }
}
