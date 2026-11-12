using System;
using System.Threading;
using Mvk.Platform.Audio;

namespace Mvk.Platform.Tests
{
    public class AudioDeviceTests
    {
        /// <summary>
        /// Беззвучно проигрывает короткий звук через настоящий OpenAL: источник занят, пока звук идёт, потом свободен.
        /// На машине без звукового устройства (сборочный сервер) проверка пропускается.
        /// </summary>
        [Fact]
        public void PlaysAndFreesSource()
        {
            AudioDevice audio;
            try
            {
                audio = new AudioDevice(8, 2);
            }
            catch (Exception)
            {
                return;
            }
            using (audio)
            {
                Assert.True(audio.CountAll > 0);
                // 50 мс тишины, моно, 8 кГц
                int sample = audio.AddSample(new PcmSample(1, 8000, new byte[800]));
                Assert.True(audio.Play(sample, 1, 2, 3, 0f, 1f));
                Assert.False(audio.Play(sample + 1, 0, 0, 0, 0f, 1f));
                audio.Tick();
                Assert.Equal(1, audio.CountPlaying);

                for (int i = 0; i < 100 && audio.CountPlaying > 0; i++)
                {
                    Thread.Sleep(20);
                    audio.Tick();
                }
                Assert.Equal(0, audio.CountPlaying);
            }
        }
    }
}
