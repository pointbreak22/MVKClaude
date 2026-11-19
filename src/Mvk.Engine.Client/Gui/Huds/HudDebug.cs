using Mvk.Engine.Util;
using Mvk.Engine.Games;
using Mvk.Engine.Renderer;
using Mvk.Engine.Renderer.Mesh;

namespace Mvk.Engine.Gui.Huds
{
    /// <summary>
    /// Отладочная индикация Heads-Up Display
    /// </summary>
    public class HudDebug : HudBase
    {
        /// <summary>
        /// Текстура карты света
        /// </summary>
        private readonly MeshGuiColor _meshLightMap;

        public HudDebug(GameBase game) : base(game)
        {
            _meshLightMap = new MeshGuiColor(GetOpenGL());
        }

        /// <summary>
        /// Метод для прорисовки кадра
        /// </summary>
        /// <param name="timeIndex">коэффициент времени от прошлого TPS клиента в диапазоне 0 .. 1</param>
        public override void Draw(float timeIndex)
        {
            base.Draw(timeIndex);
#if DEBUG

            // Отладка Карты света
            _meshLightMap.Reload(RenderFigure.Rectangle(_game.View.Width - 144, 16, _game.View.Width - 16, 144, 0, 0, 1, 1));
            Render.BindTexture(Host.WorldRender.LightMap.Texture);
            Render.ShaderBindGuiColor();
            _meshLightMap.Draw();
            
#endif
        }

        public override void Dispose()
        {
            base.Dispose();
            _meshLightMap.Dispose();
        }
    }
}
