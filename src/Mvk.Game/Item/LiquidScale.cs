using System;

namespace Mvk.Game.Item
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Согласованные шкалы жидкости (known-issues п. 2 и 3).
    /// Уровень блока жидкости 1..15: met 0 — источник (15), иначе met — уровень (чем больше, тем полнее).
    /// Ведро 1..7 (литров вдвое больше), Met 0 у ведра из творческого инвентаря считается полным, как в оригинале,
    /// где такое ведро выливало источник.
    /// Источник даёт полное ведро, полное ведро выливает источник; текущий уровень и неполное ведро пересчитываются
    /// пропорционально. Из текущей жидкости полного ведра не набрать (не больше 6), иначе вылив источника
    /// из ведра, набранного в потоке, создавал бы воду из ничего.
    /// </summary>
    public static class LiquidScale
    {
        /// <summary>Полное ведро</summary>
        public const int BucketFull = 7;
        /// <summary>Уровень источника</summary>
        public const int LevelSource = 15;

        /// <summary>Уровень блока 1..15 по met жидкости (met 0 — источник)</summary>
        public static int LevelOfMet(int met) => met == 0 ? LevelSource : met;

        /// <summary>Met жидкости по уровню блока 1..15</summary>
        public static int MetOfLevel(int level) => level >= LevelSource ? 0 : level;

        /// <summary>Количество в ведре 1..7 по Met ведра (0 — полное)</summary>
        public static int AmountOfBucket(int met) => met == 0 || met > BucketFull ? BucketFull : met;

        /// <summary>
        /// Сколько ведра в блоке уровня level: источник — полное ведро, текущая жидкость пропорционально, от 1 до 6
        /// </summary>
        public static int BucketOfLevel(int level)
            => level >= LevelSource ? BucketFull : Math.Clamp((level * BucketFull + LevelSource / 2) / LevelSource, 1, BucketFull - 1);

        /// <summary>
        /// Уровень блока для количества ведра 1..7: полное ведро — источник, иначе пропорционально
        /// </summary>
        public static int LevelOfBucket(int amount)
            => amount >= BucketFull ? LevelSource : Math.Max(1, (amount * LevelSource + BucketFull / 2) / BucketFull);

        /// <summary>
        /// Набрать жидкость из блока уровня level в ведро, где уже есть have (0 — пустое ведро).
        /// Ведро берёт только то, что влезает; возвращает новое количество в ведре и остаток уровня в блоке
        /// (0 — блок жидкости убрать). Если ведро полное, ничего не меняется.
        /// </summary>
        public static (int Bucket, int RestLevel) Scoop(int have, int level)
        {
            if (have >= BucketFull) return (have, level);
            int inBlock = BucketOfLevel(level);
            int take = Math.Min(BucketFull - have, inBlock);
            int rest = take == inBlock ? 0 : Math.Max(0, level - LevelOfBucket(take));
            return (have + take, rest);
        }
    }
}
