using System;
using Mvk.Engine.Audio;
using Mvk.Platform.Audio;

namespace Mvk.Desktop
{
    /// <summary>
    /// Звук ПК для окна Малювек: OpenAL Soft (Mvk.Platform.AudioDevice), ogg декодирует NVorbis, как в оригинале.
    /// Звуки загружаются в память звуковой карты один раз (improvements A3), номера как в AudioIndexs.
    /// </summary>
    internal sealed class DesktopAudio : IWindowAudio
    {
        private AudioDevice? _device;
        /// <summary>Номер звука AudioIndexs → номер буфера устройства, -1 если не загружен</summary>
        private int[] _map = Array.Empty<int>();

        public int CountProcessing => _device?.CountPlaying ?? 0;
        public int CountAll => _device?.CountAll ?? 0;

        public void Initialize(int count, int monoSourcesLimit, int stereoSourcesLimit)
        {
            _device?.Dispose();
            try
            {
                _device = new AudioDevice(monoSourcesLimit, stereoSourcesLimit);
            }
            catch (Exception e)
            {
                // Без звуковой карты игра идёт без звука (в оригинале падала)
                Console.Error.WriteLine("Звук отключён: " + e.Message);
                _device = null;
            }
            _map = new int[count];
            Array.Fill(_map, -1);
        }

        public void LoadSample(int index, string fileName)
        {
            if (_device == null) return;
            PcmSample sample = PcmSample.DecodeOgg(fileName);
            // Пустой звук в оригинале не проигрывался (sample.Size > 0)
            if (sample.Data.Length > 0) _map[index] = _device.AddSample(sample);
        }

        public void Play(int index, float x, float y, float z, float volume, float pitch)
        {
            if (_device != null && (uint)index < (uint)_map.Length && _map[index] >= 0)
            {
                _device.Play(_map[index], x, y, z, volume, pitch);
            }
        }

        public void Tick() => _device?.Tick();

        public void Clear()
        {
            _device?.Dispose();
            _device = null;
            _map = Array.Empty<int>();
        }
    }
}
