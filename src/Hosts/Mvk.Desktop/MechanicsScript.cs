using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Mvk.Desktop
{
    /// <summary>
    /// Сценарий механики для сравнения с оригиналом (этап 6): нажатия клавиш по номеру такта
    /// и след игрока на каждом такте. Тот же файл подключён в tools/OrigShot, поэтому без зависимостей
    /// от движка: клавиши передаются именами, состояние числами.
    /// Формат: "0:W+;10:Space+;12:Space-;60:W-;80:end" (такт:Клавиша+ нажать, Клавиша- отпустить, end конец).
    /// MouseLeft и MouseRight — кнопки мыши игры оригинала (левая ломает, правая ставит) по центру экрана,
    /// "такт:/команда" — команда чата серверу (например "5:/summon 3 4").
    /// </summary>
    public sealed class MechanicsScript
    {
        private readonly List<(int Tick, string Key, bool Down)> _events = new List<(int, string, bool)>();
        private readonly int _end;
        private readonly StreamWriter _trace;
        private int _tick;

        /// <summary>Сценарий дошёл до end, след записан</summary>
        public bool Finished { get; private set; }

        public MechanicsScript(string script, string tracePath)
        {
            _end = -1;
            foreach (string part in script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string[] kv = part.Split(':');
                int tick = int.Parse(kv[0], CultureInfo.InvariantCulture);
                string action = kv[1];
                if (action == "end") _end = tick;
                else if (action.StartsWith('/')) _events.Add((tick, action, true));
                else _events.Add((tick, action.Substring(0, action.Length - 1), action[^1] == '+'));
            }
            if (_end < 0) throw new ArgumentException("В сценарии нет end: " + script);
            _trace = new StreamWriter(tracePath) { NewLine = "\n" };
            _trace.WriteLine("tick;x;y;z;mx;my;mz;onGround;target;entities");
        }

        /// <summary>
        /// Перед тактом: нажать и отпустить клавиши этого такта
        /// </summary>
        public void BeforeTick(Action<string, bool> key)
        {
            if (Finished) return;
            foreach (var e in _events)
            {
                if (e.Tick == _tick) key(e.Key, e.Down);
            }
        }

        /// <summary>
        /// После такта: записать состояние игрока, на end закрыть след
        /// </summary>
        /// <param name="target">Блок под прицелом: "x,y,z:id" или "-"</param>
        /// <param name="entities">Сущности мира клиента: <see cref="EntitiesDigest"/></param>
        public void AfterTick(float x, float y, float z, float mx, float my, float mz, bool onGround, string target,
            string entities)
        {
            if (Finished) return;
            _trace.WriteLine(string.Join(";", _tick.ToString(CultureInfo.InvariantCulture),
                _F(x), _F(y), _F(z), _F(mx), _F(my), _F(mz), onGround ? "1" : "0", target, entities));
            if (_tick >= _end)
            {
                Finished = true;
                _trace.Dispose();
            }
            _tick++;
        }

        /// <summary>
        /// Свёртка сущностей: "число:хэш" по id и позициям, порядок списка не важен
        /// </summary>
        public static string EntitiesDigest(List<(int Id, float X, float Y, float Z)> entities)
        {
            entities.Sort((a, b) => a.Id.CompareTo(b.Id));
            ulong hash = 14695981039346656037UL;
            foreach (var e in entities)
            {
                hash = (hash ^ (uint)e.Id) * 1099511628211UL;
                hash = (hash ^ (uint)BitConverter.SingleToInt32Bits(e.X)) * 1099511628211UL;
                hash = (hash ^ (uint)BitConverter.SingleToInt32Bits(e.Y)) * 1099511628211UL;
                hash = (hash ^ (uint)BitConverter.SingleToInt32Bits(e.Z)) * 1099511628211UL;
            }
            return entities.Count.ToString(CultureInfo.InvariantCulture) + ":" + hash.ToString("x16");
        }

        private static string _F(float v) => v.ToString("R", CultureInfo.InvariantCulture);
    }
}
