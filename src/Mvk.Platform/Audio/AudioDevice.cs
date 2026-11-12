using System;
using System.Collections.Generic;
using Silk.NET.OpenAL;

namespace Mvk.Platform.Audio
{
    /// <summary>
    /// Звук через OpenAL (OpenAL Soft на всех платформах). Замена Vge.Audio.AudioBase/AudioSources.
    /// </summary>
    /// <remarks>
    /// Отличия от оригинала (docs/improvements.md, A3): PCM загружается в AL-буфер один раз при регистрации
    /// звука, а не при каждом проигрывании; источники не пересоздаются после окончания звука;
    /// устройство открывается один раз (оригинал открывал второе и не закрывал его).
    /// Слышимый результат тот же: позиция, громкость и высота задаются так же.
    /// </remarks>
    public sealed unsafe class AudioDevice : IDisposable
    {
        private const int AlcMonoSources = 0x1010;
        private const int AlcStereoSources = 0x1011;
        /// <summary>Предохранитель, как в оригинале: столько источников точно не нужно</summary>
        private const int MaxSources = 10000;

        private readonly ALContext _alc;
        private readonly AL _al;
        private readonly Device* _device;
        private readonly Context* _context;
        private readonly uint[] _sources;
        private readonly bool[] _busy;
        private readonly List<uint> _buffers = new();

        /// <summary>Сколько источников создано</summary>
        public int CountAll => _sources.Length;
        /// <summary>Сколько сейчас звучит (обновляется в Tick)</summary>
        public int CountPlaying { get; private set; }

        /// <summary>Открыть устройство вывода по умолчанию</summary>
        /// <param name="monoSourcesLimit">Сколько одновременных моно-звуков (в оригинале 256)</param>
        /// <param name="stereoSourcesLimit">Сколько одновременных стерео-звуков (в оригинале 4)</param>
        public AudioDevice(int monoSourcesLimit = 256, int stereoSourcesLimit = 4)
        {
            _alc = ALContext.GetApi(true);
            _al = AL.GetApi(true);
            _device = _alc.OpenDevice("");
            if (_device == null) throw new InvalidOperationException("Не удалось открыть звуковое устройство OpenAL");
            int* attrs = stackalloc int[] { AlcMonoSources, monoSourcesLimit, AlcStereoSources, stereoSourcesLimit, 0 };
            _context = _alc.CreateContext(_device, attrs);
            _alc.MakeContextCurrent(_context);
            _al.GetError();

            var list = new List<uint>();
            int limit = Math.Min(MaxSources, monoSourcesLimit + stereoSourcesLimit);
            while (list.Count < limit)
            {
                uint s = _al.GenSource();
                if (_al.GetError() != AudioError.NoError) break;
                list.Add(s);
            }
            _sources = list.ToArray();
            _busy = new bool[_sources.Length];
        }

        /// <summary>Загрузить звук в память звуковой карты. Возвращает номер для Play.</summary>
        public int AddSample(PcmSample sample)
        {
            uint buffer = _al.GenBuffer();
            BufferFormat format = sample.Channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16;
            fixed (byte* p = sample.Data)
            {
                _al.BufferData(buffer, format, p, sample.Data.Length, sample.SampleRate);
            }
            _buffers.Add(buffer);
            return _buffers.Count - 1;
        }

        /// <summary>
        /// Проиграть звук в точке мира. Если свободных источников нет, звук пропускается (как в оригинале).
        /// </summary>
        public bool Play(int sample, float x, float y, float z, float volume, float pitch)
        {
            if ((uint)sample >= (uint)_buffers.Count) return false;
            for (int i = 0; i < _sources.Length; i++)
            {
                if (_busy[i]) continue;
                uint s = _sources[i];
                _al.SetSourceProperty(s, SourceInteger.Buffer, (int)_buffers[sample]);
                _al.SetSourceProperty(s, SourceFloat.Pitch, pitch);
                _al.SetSourceProperty(s, SourceFloat.Gain, volume);
                _al.SetSourceProperty(s, SourceVector3.Position, x, y, z);
                _al.SourcePlay(s);
                _busy[i] = true;
                return true;
            }
            return false;
        }

        /// <summary>Позиция слушателя (игрока)</summary>
        public void SetListener(float x, float y, float z)
            => _al.SetListenerProperty(ListenerVector3.Position, x, y, z);

        /// <summary>Освободить источники, которые доиграли. Вызывать раз в такт.</summary>
        public void Tick()
        {
            int playing = 0;
            for (int i = 0; i < _sources.Length; i++)
            {
                if (!_busy[i]) continue;
                _al.GetSourceProperty(_sources[i], GetSourceInteger.SourceState, out int state);
                if (state == (int)SourceState.Playing) playing++;
                else
                {
                    _busy[i] = false;
                    _al.SetSourceProperty(_sources[i], SourceInteger.Buffer, 0);
                }
            }
            CountPlaying = playing;
        }

        public void Dispose()
        {
            foreach (uint s in _sources)
            {
                _al.SourceStop(s);
                _al.DeleteSource(s);
            }
            foreach (uint b in _buffers) _al.DeleteBuffer(b);
            _alc.MakeContextCurrent(null);
            _alc.DestroyContext(_context);
            _alc.CloseDevice(_device);
            _al.Dispose();
            _alc.Dispose();
        }
    }
}
