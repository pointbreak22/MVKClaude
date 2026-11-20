#nullable enable
using System.Collections.Generic;
using Mvk.Engine.World.Block;

namespace Mvk.Engine.World
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Добыча «часть за удар» на сервере: каждый удар рукой или инструментом снимает часть прочности блока,
    /// прогресс общий для всех ударов (обеих рук, других игроков). Видимая стадия трещин (0..7) хранится в байте
    /// разрушения блока, как в оригинале; в нём же пауза спада (биты 5-6), её обновляет каждый удар. Спад трещин без
    /// ударов делает прежний тик мира (WorldServer._TickBlocks). Дробный счёт ударов держится здесь и сверяется
    /// со стадией: если трещины зажили или их добавил другой, счёт берётся от видимой стадии.
    /// </summary>
    public sealed class BlockHits
    {
        /// <summary>
        /// Предел учёта: больше блоков с неполными трещинами учёт сбрасывает (счёт восстановится по стадии)
        /// </summary>
        private const int _MaxEntries = 1024;

        private readonly Dictionary<(int, int, int), float> _hits = new Dictionary<(int, int, int), float>();

        /// <summary>
        /// Удар по блоку силой strength; need — сколько ударов силы 1 нужно на разрушение.
        /// Возвращает true, если блок разрушен (ломает вызывающий)
        /// </summary>
        public bool Hit(WorldServer world, BlockPos pos, float need, float strength)
        {
            (int, int, int) key = (pos.X, pos.Y, pos.Z);
            if (need <= strength)
            {
                _hits.Remove(key);
                return true;
            }
            byte destroy = world.GetBlockDestroy(pos);
            int stage = (destroy & 127) == 0 ? 0 : destroy & 15;
            if (!_hits.TryGetValue(key, out float hits) || (int)(hits * 8 / need) != stage)
            {
                hits = stage * need / 8f;
            }
            hits += strength;
            if (hits >= need)
            {
                _hits.Remove(key);
                return true;
            }
            if (_hits.Count >= _MaxEntries && !_hits.ContainsKey(key)) _hits.Clear();
            _hits[key] = hits;
            int newStage = (int)(hits * 8 / need);
            world.SetBlockDestroy(pos, (byte)(newStage | 96), newStage != stage || destroy == 0);
            return false;
        }

        /// <summary>
        /// Блок сломан или заменён: забыть счёт
        /// </summary>
        public void Forget(BlockPos pos) => _hits.Remove((pos.X, pos.Y, pos.Z));
    }
}
