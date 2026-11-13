#nullable enable
using System;
using Mvk.Engine.Util;

namespace Mvk.Engine.Audio
{
    /// <summary>
    /// Звук окна, перенос AudioBase оригинала: загрузка всех звуков по шагам (для заставки),
    /// проигрывание с громкостью из опций, такт освобождения источников и отладочная строка.
    /// Устройство вывода (OpenAL) даёт платформа через IWindowAudio; без него звука нет, логика та же.
    /// </summary>
    public class AudioBase
    {
        private readonly IWindowAudio? _device;

        /// <summary>Строка отладки: звучит / всего источников</summary>
        public string StrDebug { get; private set; } = "";

        public AudioBase(IWindowAudio? device) => _device = device;

        /// <summary>
        /// Инициализация звука: количество звуков и лимиты одновременных моно и стерео источников
        /// </summary>
        public void Initialize(int count, int monoSourcesLimit, int stereoSourcesLimit)
            => _device?.Initialize(count, monoSourcesLimit, stereoSourcesLimit);

        /// <summary>
        /// Загрузить все звуки из AudioIndexs, по шагу на звук (событие Step для заставки)
        /// </summary>
        public void InitializeSample()
        {
            int count = AudioIndexs.Count();
            for (int i = 0; i < count; i++)
            {
                _device?.LoadSample(i, Options.PathSounds + AudioIndexs.GetPath(i));
                OnStep();
            }
            AudioIndexs.ClearPath();
        }

        /// <summary>
        /// Игровой такт: освободить доигравшие источники
        /// </summary>
        public void Tick()
        {
            if (_device == null) return;
            _device.Tick();
            StrDebug = string.Format("{0}/{1}", _device.CountProcessing, _device.CountAll);
        }

        /// <summary>
        /// Проиграть звук, координаты относительно слушателя
        /// </summary>
        public void PlaySound(int key, float posX, float posY, float posZ, float volume, float pitch)
        {
            if (Options.SoundVolume > 0)
            {
                _device?.Play(key, posX, posY, posZ, volume * Options.SoundVolumeFloat, pitch);
            }
        }

        /// <summary>
        /// Выгрузить звуки
        /// </summary>
        public void Clear() => _device?.Clear();

        #region Event

        /// <summary>Шаг загрузки звука</summary>
        public event EventHandler? Step;
        protected void OnStep() => Step?.Invoke(this, EventArgs.Empty);

        #endregion
    }

    /// <summary>
    /// Устройство вывода звука платформы (OpenAL на ПК)
    /// </summary>
    public interface IWindowAudio
    {
        /// <summary>Открыть устройство, count звуков, лимиты моно и стерео источников</summary>
        void Initialize(int count, int monoSourcesLimit, int stereoSourcesLimit);
        /// <summary>Загрузить звук ogg под номером index</summary>
        void LoadSample(int index, string fileName);
        /// <summary>Проиграть звук, если он загружен и есть свободный источник</summary>
        void Play(int index, float x, float y, float z, float volume, float pitch);
        /// <summary>Освободить доигравшие источники</summary>
        void Tick();
        /// <summary>Сколько источников звучит</summary>
        int CountProcessing { get; }
        /// <summary>Сколько всего источников</summary>
        int CountAll { get; }
        /// <summary>Выгрузить звуки и закрыть устройство</summary>
        void Clear();
    }
}
