#nullable enable
using System;
using Mvk.Engine.Games;
using Mvk.Engine.Renderer.Mesh;
using Mvk.Engine.Entity.Player;

namespace Mvk.Game.Client.Gui
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Полоски здоровья (красная, слева) и выносливости (жёлто-зелёная, справа) над слотами HUD
    /// в выживании; в творческом и наблюдателе скрыты. Значения приходят с сервера (здоровье и выносливость
    /// в метаданных игрока), показ плавно догоняет их. Буфер вершин заведён заранее.
    /// </summary>
    public sealed class HudVitals : IDisposable
    {
        private readonly GameBase _game;
        private readonly MeshGuiColor _mesh;
        /// <summary>
        /// 4 прямоугольника (фон и заливка двух полосок) по 4 вершины
        /// </summary>
        private readonly float[] _buffer = new float[4 * 4 * MeshGuiColor.VertexSize];
        private float _health = -1;
        private float _stamina = -1;

        public HudVitals(GameBase game, MeshGuiColor mesh)
        {
            _game = game;
            _mesh = mesh;
        }

        /// <summary>
        /// Собрать сетку полосок; false — рисовать нечего
        /// </summary>
        public bool Build(float deltaTime)
        {
            PlayerClientOwner player = _game.Player;
            if (player.CreativeMode || player.IsSpectator()) return false;
            float max = Math.Max(1, player.GetHealthMax());
            float health = Math.Clamp(player.GetHealth() / max, 0, 1);
            float stamina = Math.Clamp(player.GetStamina() / PlayerBase.StaminaMax, 0, 1);
            // Плавное догоняние значения
            float k = Math.Clamp(deltaTime / 120f, 0, 1);
            _health = _health < 0 ? health : _health + (health - _health) * k;
            _stamina = _stamina < 0 ? stamina : _stamina + (stamina - _stamina) * k;

            int si = _game.View.Si;
            int wc = _game.View.Width / 2;
            int y2 = _game.View.Height - 60 * si;
            int y1 = y2 - 5 * si;
            int left = wc - 216 * si, right = wc + 216 * si;
            int count = 0;
            count = _Rect(count, left, y1, wc - 4 * si, y2, .1f, .1f, .1f, .6f);
            count = _Rect(count, left, y1, left + (int)((wc - 4 * si - left) * _health), y2, .85f, .12f, .1f, .95f);
            count = _Rect(count, wc + 4 * si, y1, right, y2, .1f, .1f, .1f, .6f);
            count = _Rect(count, wc + 4 * si, y1, wc + 4 * si + (int)((right - wc - 4 * si) * _stamina), y2, .7f, .85f, .2f, .95f);
            _mesh.Reload(_buffer, count);
            return true;
        }

        private int _Rect(int n, int x1, int y1, int x2, int y2, float r, float g, float b, float a)
        {
            n = _Vertex(n, x1, y1, r, g, b, a);
            n = _Vertex(n, x1, y2, r, g, b, a);
            n = _Vertex(n, x2, y2, r, g, b, a);
            return _Vertex(n, x2, y1, r, g, b, a);
        }

        private int _Vertex(int n, float x, float y, float r, float g, float b, float a)
        {
            _buffer[n] = x;
            _buffer[n + 1] = y;
            _buffer[n + 2] = 0;
            _buffer[n + 3] = 0;
            _buffer[n + 4] = r;
            _buffer[n + 5] = g;
            _buffer[n + 6] = b;
            _buffer[n + 7] = a;
            return n + MeshGuiColor.VertexSize;
        }

        public void Draw() => _mesh.Draw();

        public void Dispose() => _mesh.Dispose();
    }
}
