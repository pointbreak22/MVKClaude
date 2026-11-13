#nullable enable
using System;
using Mvk.Engine.Util;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer
{
    /// <summary>
    /// Карта теней на RHI, перенос ShadowMapping оригинала: кадровый буфер с текстурой глубины 4096x4096,
    /// ортогональная проекция ±64 блока от солнца, проход в карту теней для ближних 45 чанков.
    /// </summary>
    public sealed class ShadowMapping : IDisposable
    {
        /// <summary>
        /// Количество чанков для карты теней
        /// </summary>
        public const int CountChunkShadowMap = 45;
        /// <summary>
        /// Размер ортогональной проекции карты теней в блоках
        /// </summary>
        public const int SizeOrthShadowMap = 64;
        /// <summary>
        /// Размер текстуры карты теней
        /// </summary>
        public const int SizeTextureShadowMap = 4096;

        /// <summary>
        /// Включены ли тени (Options.Shadow)
        /// </summary>
        public bool IsShadow { get; private set; }
        /// <summary>
        /// Текстура глубины карты теней, null пока тени ни разу не включались
        /// </summary>
        public ITexture? DepthTexture { get; private set; }

        private readonly IGraphicsDevice _device;
        private IRenderTarget? _target;

        public ShadowMapping(IGraphicsDevice device)
        {
            _device = device;
            ReloadInit();
        }

        /// <summary>
        /// Изменена опция теней: яркость блоков и сущностей как в оригинале
        /// </summary>
        public void ReloadInit()
        {
            IsShadow = Options.Shadow;
            Gi.BlockBrightness = IsShadow ? 0.3f : 0.15f;
            Gi.EntityBrightness = 0.3f;
            if (IsShadow && _target == null)
            {
                // CLAMP_TO_BORDER с белой рамкой: вне карты глубина 1, тени нет
                DepthTexture = _device.CreateTexture(TextureDescription.Depth(SizeTextureShadowMap, SizeTextureShadowMap,
                    new SamplerState(TextureFilter.Nearest, TextureFilter.Nearest, MipFilter.None,
                        TextureWrap.ClampToBorder, true)));
                _target = _device.CreateRenderTarget(DepthTexture);
            }
        }

        /// <summary>
        /// Начать проход в карту теней: кадровый буфер теней, его размер и очистка глубины.
        /// Возвращает false, если тени выключены.
        /// </summary>
        public bool RenderSceneBegin(CommandList commands)
        {
            if (IsShadow && _target != null)
            {
                commands.SetRenderTarget(_target);
                commands.SetViewport(0, 0, SizeTextureShadowMap, SizeTextureShadowMap);
                commands.Clear(1, 1, 1, 1, false, true);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Вернуться к рисованию в окно
        /// </summary>
        public void RenderSceneEnd(CommandList commands, int width, int height)
        {
            commands.SetRenderTarget(null);
            commands.SetViewport(0, 0, width, height);
        }

        public void Dispose()
        {
            _target?.Dispose();
            _target = null;
            DepthTexture?.Dispose();
            DepthTexture = null;
        }
    }
}
