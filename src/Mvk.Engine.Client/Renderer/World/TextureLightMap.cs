using Mvk.Rendering;
using System;
using Mvk.Core.Util;
using Mvk.Engine.Util;
using Mvk.Engine.World;

namespace Mvk.Engine.Renderer.World
{
    /// <summary>
    /// Объект работы текстуры для освещения
    /// </summary>
    public class TextureLightMap : System.IDisposable
    {
        /// <summary>
        /// Яркость фазы лун
        /// </summary>
        public readonly static float[] LightMoonPhase 
            = new float[] { 0, .16f, .32f, .48f, .8f, .48f, .32f, .16f };
        /// <summary>
        /// Яркость для текстур
        /// </summary>
        public readonly static float[] LightBrightnessTable = new float[16];

        
        /// <summary>
        /// Буфер карты 16 (x) * 16 (y) * 4 (RGBA)
        /// </summary>
        private readonly byte[] _buffer = new byte[1024];

        /// <summary>
        /// Параметр яркости солнца прошлого обнавления
        /// </summary>
        private float _sunLightPrev = -1f;
        /// <summary>
        /// Параметр яркости луны прошлого обнавления
        /// </summary>
        private float _moonLightPrev = -1f;
        /// <summary>
        /// Неба нет (true), прошлого обновления
        /// </summary>
        private bool _hasNoSkyPrev;

        private readonly IGraphicsDevice _device;
        /// <summary>
        /// Текстура карты света (пусто до первого Update)
        /// </summary>
        public ITexture Texture { get; private set; }

        public TextureLightMap(IGraphicsDevice device)
        {
            _device = device;
            float fm = 0f;
            for (int i = 0; i < 16; i++)
            {
                float f = 1f - i / 15f;
                LightBrightnessTable[i] = (1f - f) / (f * 3f + 1f) * (1f - fm) + fm;
            }
        }

        /// <summary>
        /// В каждом такте корректировка
        /// </summary>
        /// <param name="worldSettings">Настройки мира</param>
        public void Update(WorldSettings worldSettings)
        {
            if (_hasNoSkyPrev != worldSettings.HasNoSky)
            {
                // Скорее всего обновился мир, ибо сменилось небо
                _hasNoSkyPrev = worldSettings.HasNoSky;
                if (_hasNoSkyPrev)
                {
                    _GenTextureNotSky();
                    _UpdateLightmap();
                }
                else
                {
                    _sunLightPrev = worldSettings.Environment.GetSunLight();
                    _moonLightPrev = worldSettings.Environment.GetMoonLight();
                    _GenTextureSky(_sunLightPrev, _moonLightPrev);
                    _UpdateLightmap();
                }
            }
            else if (!_hasNoSkyPrev)
            {
                // Есть небо, проверяем смену яркости солнца
                float sunLight = worldSettings.Environment.GetSunLight();
                float moonLight = worldSettings.Environment.GetMoonLight();
                if (_sunLightPrev != sunLight || _moonLightPrev != moonLight)
                {
                    _sunLightPrev = sunLight;
                    _moonLightPrev = moonLight;
                    _GenTextureSky(_sunLightPrev, _moonLightPrev);
                    _UpdateLightmap();
                }
            }
        }

        /// <summary>
        /// Генерируем текстуру с небом
        /// </summary>
        /// <param name="sunLight">Яркость солнца, 0.0 - 1.0</param>
        /// <param name="moonLight">Значение яркости луны, можно использовать фазу луны 0.0 - 0.5</param>
        private void _GenTextureSky(float sunLight, float moonLight)
        {
            for (int i = 0; i < 256; ++i)
            {
                float ls = sunLight < moonLight && moonLight > 0
                    ? LightBrightnessTable[i / 16] * moonLight + .05f
                    : LightBrightnessTable[i / 16] * sunLight * .95f + .05f;

                //float lb = MvkStatic.LightBrightnessTable[i % 16] * 1.5f;
                float lb = (i % 16) / 14f;

                //if (молния)
                //{
                //    ls = MvkStatic.LightBrightnessTable[i / 16];
                //}

                float ls2 = ls * (sunLight * .65f + .35f);
                float ls3 = ls * (sunLight * .65f + .35f);
                float lb2 = lb * ((lb * .6f + .4f) * .6f + .4f);
                float lb3 = lb * (lb * lb * .6f + .4f);
                float cr = ls2 + lb;
                float cg = ls3 + lb2;
                float cb = ls + lb3;
                cr = cr * .96f + .03f;
                cg = cg * .96f + .03f;
                cb = cb * .96f + .03f;
                float light;

                //if (this.bossColorModifier > 0.0F)
                //{ // босс
                //light = 1f;// this.bossColorModifierPrev + (this.bossColorModifier - this.bossColorModifierPrev) * p_78472_1_;
                //cr = cr * (1.0F - light) + cr * 0.7F * light;
                //cg = cg * (1.0F - light) + cg * 0.6F * light;
                //cb = cb * (1.0F - light) + cb * 0.6F * light;
                //}

                //if (var2.provider.getDimensionId() == 1)
                //{ // другой мир, всегда темно (ад)
                //    cr = 0.22F + lb * 0.75F;
                //    cg = 0.28F + lb2 * 0.75F;
                //    cb = 0.25F + lb3 * 0.75F;
                //}

                //cr = 0.32F + lb * 0.75F;
                //cg = 0.28F + lb2 * 0.75F;
                //cb = 0.25F + lb3 * 0.75F;


                //if (this.mc.thePlayer.isPotionActive(Potion.nightVision))
                //{ // зелье ночного виденья
                //light = 1f;// this.func_180438_a(this.mc.thePlayer, p_78472_1_);
                //light2 = 1.0F / cr;

                //if (light2 > 1f / cg) light2 = 1f / cg;
                //if (light2 > 1f / cb) light2 = 1f / cb;

                //cr = cr * (1f - light) + cr * light2 * light;
                //cg = cg * (1f - light) + cg * light2 * light;
                //cb = cb * (1f - light) + cb * light2 * light;
                //}

                if (cr > 1f) cr = 1f;
                if (cg > 1f) cg = 1f;
                if (cb > 1f) cb = 1f;

                light = .5f;// this.mc.gameSettings.gammaSetting; // яркость экрана  .1 - .5
                float cr2 = 1f - cr;
                float cg2 = 1f - cg;
                float cb2 = 1f - cb;
                cr2 = 1f - cr2 * cr2 * cr2 * cr2;
                cg2 = 1f - cg2 * cg2 * cg2 * cg2;
                cb2 = 1f - cb2 * cb2 * cb2 * cb2;
                cr = cr * (1f - light) + cr2 * light;
                cg = cg * (1f - light) + cg2 * light;
                cb = cb * (1f - light) + cb2 * light;
                cr = cr * .96f + .03f;
                cg = cg * .96f + .03f;
                cb = cb * .96f + .03f;

                if (cr > 1f) cr = 1f;
                if (cg > 1f) cg = 1f;
                if (cb > 1f) cb = 1f;

                if (cr < 0f) cr = 0f;
                if (cg < 0f) cg = 0f;
                if (cb < 0f) cb = 0f;

                _buffer[i * 4] = (byte)(cb * 255);
                _buffer[i * 4 + 1] = (byte)(cg * 255);
                _buffer[i * 4 + 2] = (byte)(cr * 255);
                _buffer[i * 4 + 3] = 255;
            }
        }

        /// <summary>
        /// Генерируем текстуру без неба
        /// </summary>
        private void _GenTextureNotSky()
        {
            for (int i = 0; i < 256; ++i)
            {
                float lb = (i % 16) / 14f;
                float lb2 = lb * ((lb * .6f + .4f) * .6f + .4f);
                float lb3 = lb * (lb * lb * .6f + .4f);

                float light;

                //if (this.bossColorModifier > 0.0F)
                //{ // босс
                //light = 1f;// this.bossColorModifierPrev + (this.bossColorModifier - this.bossColorModifierPrev) * p_78472_1_;
                //cr = cr * (1.0F - light) + cr * 0.7F * light;
                //cg = cg * (1.0F - light) + cg * 0.6F * light;
                //cb = cb * (1.0F - light) + cb * 0.6F * light;
                //}

                //if (var2.provider.getDimensionId() == 1)
                //{ // другой мир, всегда темно (ад)
                float cr = 0.22F + lb * 0.75F;
                float cg = 0.28F + lb2 * 0.75F;
                float cb = 0.25F + lb3 * 0.75F;
                //}

                // На край смахивает
                //cr = 0.32F + lb * 0.75F;
                //cg = 0.28F + lb2 * 0.75F;
                //cb = 0.25F + lb3 * 0.75F;


                //if (this.mc.thePlayer.isPotionActive(Potion.nightVision))
                { // зелье ночного виденья
                    //light = .3f;// this.func_180438_a(this.mc.thePlayer, p_78472_1_);
                    //light2 = 1.0F / cr;

                    //if (light2 > 1f / cg) light2 = 1f / cg;
                    //if (light2 > 1f / cb) light2 = 1f / cb;

                    //cr = cr * (1f - light) + cr * light2 * light;
                    //cg = cg * (1f - light) + cg * light2 * light;
                    //cb = cb * (1f - light) + cb * light2 * light;
                }

                if (cr > 1f) cr = 1f;
                if (cg > 1f) cg = 1f;
                if (cb > 1f) cb = 1f;

                light = .1f;// this.mc.gameSettings.gammaSetting; // яркость экрана  .1 - .5
                float cr2 = 1f - cr;
                float cg2 = 1f - cg;
                float cb2 = 1f - cb;
                cr2 = 1f - cr2 * cr2 * cr2 * cr2;
                cg2 = 1f - cg2 * cg2 * cg2 * cg2;
                cb2 = 1f - cb2 * cb2 * cb2 * cb2;
                cr = cr * (1f - light) + cr2 * light;
                cg = cg * (1f - light) + cg2 * light;
                cb = cb * (1f - light) + cb2 * light;
                cr = cr * .96f + .03f;
                cg = cg * .96f + .03f;
                cb = cb * .96f + .03f;

                if (cr > 1f) cr = 1f;
                if (cg > 1f) cg = 1f;
                if (cb > 1f) cb = 1f;

                if (cr < 0f) cr = 0f;
                if (cg < 0f) cg = 0f;
                if (cb < 0f) cb = 0f;

                _buffer[i * 4] = (byte)(cb * 255);
                _buffer[i * 4 + 1] = (byte)(cg * 255);
                _buffer[i * 4 + 2] = (byte)(cr * 255);
                _buffer[i * 4 + 3] = 255;
            }
        }

        /// <summary>
        /// Загрузить карту света в текстуру 16x16.
        /// В оригинале GL_CLAMP: в core-профиле это ошибка, оставался REPEAT; координаты света
        /// всегда в центрах текселей (n/16 + 1/32), поэтому результат с ClampToEdge тот же.
        /// </summary>
        private void _UpdateLightmap()
        {
            if (Texture == null)
            {
                Texture = _device.CreateTexture(TextureDescription.Texture2D(16, 16,
                    new SamplerState(TextureFilter.Linear, TextureFilter.Linear, MipFilter.None, TextureWrap.ClampToEdge)));
            }
            Texture.SetData(_buffer, PixelFormat.Bgra8);
        }

        public void Dispose()
        {
            Texture?.Dispose();
            Texture = null;
        }
    }
}
