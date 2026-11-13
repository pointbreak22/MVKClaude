#nullable enable
using System;
using System.Runtime.CompilerServices;
using Mvk.Engine.Entity.Render;
using Mvk.Engine.Renderer.Mesh;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.World.Entity
{
    /// <summary>
    /// Объект рисует слои конкретной сущности (одежда и предметы в руках), если такие имеются
    /// </summary>
    public class EntityLayerRender : IDisposable
    {
        /// <summary>
        /// Сетка сущности
        /// </summary>
        private readonly MeshEntity _mesh;
        /// <summary>
        /// Объект рендера всех сущностей
        /// </summary>
        private readonly EntitiesRenderer _entities;

        public EntityLayerRender(EntitiesRenderer entities)
        {
            _entities = entities;
            _mesh = new MeshEntity(entities.Context, BufferUsage.Dynamic);
        }

        /// <summary>
        /// Пополнить сетку полигонами
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRangeBuffer(VertexEntityBuffer buffer)
            => _entities.Buffer.AddRange(buffer);

        /// <summary>
        /// Перезаписать полигоны. Загрузка на GPU откладывается до отрисовки: смена одежды приходит
        /// в такте (пакеты сервера), а буферы GPU пишет только проход кадра.
        /// </summary>
        public void Reload()
        {
            _pendingFloat = _entities.Buffer.ToArrayFloat();
            _pendingInt = _entities.Buffer.ToArrayInt();
            _pendingCount = _entities.Buffer.CountVertices;
            _entities.Buffer.Clear();
        }

        /// <summary>
        /// Полигоны, ожидающие загрузки на GPU
        /// </summary>
        private float[]? _pendingFloat;
        private int[] _pendingInt = Array.Empty<int>();
        private int _pendingCount;

        /// <summary>
        /// Прорисовать сетку
        /// </summary>
        public void MeshDraw()
        {
            if (_pendingFloat != null)
            {
                _mesh.Reload(_pendingFloat, _pendingInt, _pendingCount);
                _pendingFloat = null;
                _pendingInt = Array.Empty<int>();
            }
            _mesh.Draw(_entities.ShsEntity.Commands);
        }

        /// <summary>
        /// Выгрузить сетку с GPU
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _mesh.Dispose();
    }
}
