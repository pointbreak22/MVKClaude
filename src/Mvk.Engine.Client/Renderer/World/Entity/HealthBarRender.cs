#nullable enable
using System;
using Mvk.Core.Maths;
using Mvk.Core.Util;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Games;
using Mvk.Engine.Renderer.Mesh;
using Mvk.Engine.Util;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.World.Entity
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Полоска здоровья над живыми сущностями: всегда у враждебных (EntityLiving.HealthBarAlways),
    /// у остальных — когда здоровье не полное. Полоска из горизонтальных линий в мире (с глубиной), повёрнута
    /// к камере по повороту игрока; закрашенная часть — доля здоровья (зелёная → красная). Здоровье приходит
    /// с сервера в метаданных сущности. Буфер вершин заведён заранее.
    /// </summary>
    public sealed class HealthBarRender : IDisposable
    {
        /// <summary>
        /// Линий по высоте полоски
        /// </summary>
        private const int _Lines = 7;
        private const float _HalfWidth = .4f;
        private const float _Step = .011f;
        /// <summary>
        /// Дальность показа, блоков
        /// </summary>
        private const float _Distance = 24;

        private readonly RenderMain _render;
        private readonly MeshLine _mesh;
        /// <summary>
        /// На каждую линию высоты две линии (закрашенная и пустая часть), по 2 вершины
        /// </summary>
        private readonly float[] _buffer = new float[_Lines * 2 * 2 * MeshLine.VertexSize];

        public HealthBarRender(RenderMain render)
        {
            _render = render;
            _mesh = new MeshLine(render, BufferUsage.Dynamic);
        }

        public void Draw(GameBase game, ArrayFast<ChunkRender> chunks, float timeIndex)
        {
            PlayerClientOwner player = game.Player;
            float px = player.PosFrameX, py = player.PosFrameY, pz = player.PosFrameZ;
            Vector3 forward = Glm.Ray(player.GetRotationFrameYaw(timeIndex), 0);
            float len = MathF.Sqrt(forward.X * forward.X + forward.Z * forward.Z);
            if (len < .001f) return;
            float rx = -forward.Z / len * _HalfWidth, rz = forward.X / len * _HalfWidth;
            for (int i = 0; i < chunks.Count; i++)
            {
                ChunkRender chunk = chunks[i];
                for (int j = 0; j < chunk.ListEntities.Count; j++)
                {
                    if (chunk.ListEntities.GetAt(j) is not EntityLiving living || living.Id == player.Id
                        || living is PlayerBase) continue;
                    float health = living.GetHealth(), max = living.GetHealthMax();
                    if (health <= 0 || max <= 0 || (!living.HealthBarAlways && health >= max)) continue;
                    float x = living.GetPosFrameX(timeIndex) - px;
                    float y = living.GetPosFrameY(timeIndex) - py;
                    float z = living.GetPosFrameZ(timeIndex) - pz;
                    if (x * x + y * y + z * z > _Distance * _Distance) continue;
                    _Build(Math.Clamp(health / max, 0, 1), rx, rz);
                    _render.ShaderBindLine(game.View.MatrixView, x, y + living.Size.GetHeight() + .3f, z, 1f);
                    _mesh.Reload(_buffer);
                    _mesh.Draw();
                }
            }
        }

        /// <summary>
        /// Линии полоски: доля part закрашена от левого края
        /// </summary>
        private void _Build(float part, float rx, float rz)
        {
            float r = part > .5f ? (1 - part) * 2 : 1, g = part > .5f ? 1 : part * 2;
            float mx = -rx + rx * 2 * part, mz = -rz + rz * 2 * part;
            int n = 0;
            for (int k = 0; k < _Lines; k++)
            {
                float y = k * _Step;
                n = _Vertex(n, -rx, y, -rz, r, g, 0);
                n = _Vertex(n, mx, y, mz, r, g, 0);
                n = _Vertex(n, mx, y, mz, .12f, .12f, .12f);
                n = _Vertex(n, rx, y, rz, .12f, .12f, .12f);
            }
        }

        private int _Vertex(int n, float x, float y, float z, float r, float g, float b)
        {
            _buffer[n] = x;
            _buffer[n + 1] = y;
            _buffer[n + 2] = z;
            _buffer[n + 3] = r;
            _buffer[n + 4] = g;
            _buffer[n + 5] = b;
            _buffer[n + 6] = 1;
            return n + MeshLine.VertexSize;
        }

        public void Dispose() => _mesh.Dispose();
    }
}
