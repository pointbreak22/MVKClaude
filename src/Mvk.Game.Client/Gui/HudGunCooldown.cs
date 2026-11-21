#nullable enable
using System;
using Mvk.Engine.Games;
using Mvk.Engine.Item;
using Mvk.Engine.Renderer.Mesh;
using Mvk.Game.Entity.Inventory;
using Mvk.Game.Entity.List;
using Mvk.Game.Item;
using Mvk.Game.Item.List;

namespace Mvk.Game.Client.Gui
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] Откат оружия в HUD, как у умения в Dota: тёмный сектор над значком оружия в слоте, убывающий
    /// по часовой стрелке от 12 часов по ходу перезарядки ствола или паузы после выстрела. Время считает сервер
    /// (GunReload); клиент повторяет те же правила по стаку и инвентарю: отсчёт идёт за стаком оружия
    /// (при переносе в другой слот продолжается), новый стак с сервера (выстрел, заряд ствола) начинает его заново.
    /// Сетка пересобирается каждый кадр, только пока есть откат; буфер вершин заведён заранее (без аллокаций).
    /// </summary>
    public sealed class HudGunCooldown : IDisposable
    {
        private const int _Slots = GunReload.SlotCount;
        /// <summary>
        /// Секторов на слот: между углами квадрата слота (верх делится 12 часами надвое)
        /// </summary>
        private const int _Sectors = 5;

        private readonly GameBase _game;
        private readonly InventoryPlayerMvk _inventory;
        private readonly MeshGuiColor _mesh;
        private readonly float[] _buffer = new float[_Slots * _Sectors * 4 * MeshGuiColor.VertexSize];

        private readonly ItemStack?[] _stacks = new ItemStack?[_Slots];
        private readonly ItemStack?[] _stacksPrev = new ItemStack?[_Slots];
        /// <summary>
        /// Тактов перезарядки текущего ствола
        /// </summary>
        private readonly int[] _ticks = new int[_Slots];
        private readonly int[] _ticksPrev = new int[_Slots];
        /// <summary>
        /// Тактов паузы после выстрела осталось
        /// </summary>
        private readonly int[] _pause = new int[_Slots];
        private readonly int[] _pausePrev = new int[_Slots];
        private uint _tickPrev;

        public HudGunCooldown(GameBase game, InventoryPlayerMvk inventory, MeshGuiColor mesh)
        {
            _game = game;
            _inventory = inventory;
            _mesh = mesh;
            _tickPrev = game.TickCounterClient;
        }

        /// <summary>
        /// Обновить отсчёт по тактам клиента и собрать сетку секторов.
        /// cx, cy — центр значка слота 0, step — шаг слотов кармана, leftX — центр значка левой руки, half — полуразмер.
        /// Возвращает, есть ли что рисовать
        /// </summary>
        public bool Build(float timeIndex, int cx, int cy, int step, int leftX, int half, int limitPocket)
        {
            uint tick = _game.TickCounterClient;
            int passed = (int)Math.Min(tick - _tickPrev, 40u);
            _tickPrev = tick;
            bool blocking = _game.Player is PlayerClientOwnerMvk mvk && mvk.BlockingSent;

            Array.Copy(_stacks, _stacksPrev, _Slots);
            Array.Copy(_ticks, _ticksPrev, _Slots);
            Array.Copy(_pause, _pausePrev, _Slots);
            int count = 0;
            for (int slot = 0; slot < _Slots; slot++)
            {
                ItemStack? stack = _inventory.GetStackInSlot(slot);
                if (stack != _stacksPrev[slot])
                {
                    int from = _Moved(stack, slot);
                    _ticks[slot] = from >= 0 ? _ticksPrev[from] : 0;
                    _pause[slot] = from >= 0 ? _pausePrev[from] : 0;
                    // Меньше стволов, чем было, — выстрел: пауза после него
                    ItemStack? prev = _stacksPrev[slot];
                    if (from < 0 && stack?.Item is ItemGun shot && prev?.Item == shot && stack.Met < prev.Met)
                    {
                        _pause[slot] = shot.ShotPause;
                    }
                    _stacks[slot] = stack;
                }
                _pause[slot] = Math.Max(0, _pause[slot] - passed);
                if (stack?.Item is not ItemGun gun) continue;
                float left = 0;
                if (gun.IsReloading(stack, _inventory))
                {
                    if (!(gun.Two && blocking)) _ticks[slot] = Math.Min(_ticks[slot] + passed, gun.ReloadTicks);
                    left = 1f - Math.Clamp((_ticks[slot] + timeIndex) / gun.ReloadTicks, 0, 1);
                }
                else
                {
                    _ticks[slot] = 0;
                }
                if (_pause[slot] > 0 && gun.ShotPause > 0)
                {
                    left = Math.Max(left, Math.Clamp((_pause[slot] - timeIndex) / gun.ShotPause, 0, 1));
                }
                if (left <= 0 || (slot < InventoryPlayerMvk.PocketCount && slot >= limitPocket)) continue;
                int x = slot == InventoryPlayerMvk.PocketCount ? leftX : cx + slot * step;
                count = _Sector(count, x, cy, half, left);
            }
            _mesh.Reload(_buffer, count);
            return count > 0;
        }

        /// <summary>
        /// Слот, из которого переехал стак оружия (тот же стак или такой же, пропавший оттуда), или -1
        /// </summary>
        private int _Moved(ItemStack? stack, int slot)
        {
            if (stack?.Item is not ItemGun) return -1;
            for (int i = 0; i < _Slots; i++)
            {
                ItemStack? prev = _stacksPrev[i];
                if (i == slot || prev == null) continue;
                if (prev == stack) return i;
                if (prev.Item == stack.Item && prev.Met == stack.Met && prev.ItemDamaged == stack.ItemDamaged
                    && _inventory.GetStackInSlot(i) != prev) return i;
            }
            return -1;
        }

        /// <summary>
        /// Тёмный сектор доли left круга: от угла (1 - left) по часовой до 12 часов, вписан в квадрат слота
        /// </summary>
        private int _Sector(int count, int cx, int cy, int half, float left)
        {
            float a0 = (1f - left) * MathF.Tau;
            for (int s = 0; s < _Sectors; s++)
            {
                // Границы по углам квадрата (45°, 135°, 225°, 315° от 12 часов): край сектора лежит на одной стороне
                float b0 = s == 0 ? 0 : (s * 2 - 1) * MathF.PI / 4;
                float b1 = s == _Sectors - 1 ? MathF.Tau : (s * 2 + 1) * MathF.PI / 4;
                float from = Math.Max(a0, b0);
                if (from >= b1) continue;
                count = _Vertex(count, cx, cy);
                count = _Vertex(count, cx, cy);
                // Обход как у прямоугольников GUI (иначе грань отсекается)
                count = _Edge(count, cx, cy, half, b1);
                count = _Edge(count, cx, cy, half, from);
            }
            return count;
        }

        private int _Edge(int count, int cx, int cy, int half, float angle)
        {
            float dx = MathF.Sin(angle), dy = -MathF.Cos(angle);
            float k = half / Math.Max(Math.Abs(dx), Math.Abs(dy));
            return _Vertex(count, cx + dx * k, cy + dy * k);
        }

        private int _Vertex(int count, float x, float y)
        {
            _buffer[count] = x;
            _buffer[count + 1] = y;
            _buffer[count + 2] = 0;
            _buffer[count + 3] = 0;
            _buffer[count + 4] = 0;
            _buffer[count + 5] = 0;
            _buffer[count + 6] = 0;
            _buffer[count + 7] = .6f;
            return count + MeshGuiColor.VertexSize;
        }

        public void Draw() => _mesh.Draw();

        public void Dispose() => _mesh.Dispose();
    }
}
