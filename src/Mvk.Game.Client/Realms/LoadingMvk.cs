// Перенос из Mvk2 (original/src/Mvk2) без изменения логики; код оригинала без аннотаций nullable
#nullable disable
using Mvk.Game.Client;
using Mvk.Game.Client.Renderer;
using System;
using Mvk.Engine.Audio;
using Mvk.Engine.Realms;
using Mvk.Core.Util;
using Mvk.Engine.Util;

namespace Mvk.Game.Client.Realms
{
    /// <summary>
    /// Объект который запускает текстуры, звук, и формирует всё для игры
    /// </summary>
    public class LoadingMvk : Loading
    {
        /// <summary>
        /// Объект окна малювек
        /// </summary>
        private readonly WindowMvk _window;
        /// <summary>
        /// Количество текстур
        /// </summary>
        private readonly int _countTexture = Enum.GetNames(typeof(EnumTextureMvk)).Length;

        public LoadingMvk(WindowMvk window) : base(window)
        {
            _window = window;
            AudioReg.Initialization(true);
        }

        /// <summary>
        /// Максимальное количество шагов
        /// </summary>
        public override int GetMaxCountSteps() => base.GetMaxCountSteps() + _countTexture;

        /// <summary>
        /// Этот метод как раз и реализует список загрузок
        /// </summary>
        protected override void _Steps()
        {
            // Загружаем  по умолчанию
            base._Steps();
            _StepsTextures();
            // Шаги звуков (_StepsSample оригинала) вернутся вместе со звуком в игре
        }

        /// <summary>
        /// Шаги текстур
        /// </summary>
        private void _StepsTextures()
        {
            RenderMvk renderMvk = _window.Render as RenderMvk;
            // Шрифты
            renderMvk.CreateTextureFontSmall(
                _FileToBufferedImage(EnumTextureMvk.FontSmall.ToString(),
                Options.PathTextures + EnumTextureMvk.FontSmall.ToString() + ".png"));
            OnStep();

            renderMvk.CreateTextureFontLarge(
                _FileToBufferedImage(EnumTextureMvk.FontLarge.ToString(), 
                Options.PathTextures + EnumTextureMvk.FontLarge.ToString() + ".png"));
            OnStep();

            // Остальные после шрифтов
            for (int i = 2; i < _countTexture; i++)
            {
                string name = ((EnumTextureMvk)i).ToString();
                _FileToBufferedImage(name, Options.PathTextures + name + ".png");
                OnStep();
            }
        }
    }
}
