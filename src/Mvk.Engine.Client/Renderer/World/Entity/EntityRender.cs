#nullable enable
using System;
using System.Runtime.CompilerServices;
using Mvk.Engine.Entity.Render;
using Mvk.Engine.Renderer.Mesh;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.World.Entity
{
    /// <summary>
    /// Объект рисует сущности: одна неизменная сетка на тип сущности или предмета
    /// </summary>
    public class EntityRender : IEntityRender, IDisposable
    {
        /// <summary>
        /// Сетка сущности
        /// </summary>
        private readonly MeshEntity _mesh;
        /// <summary>
        /// Объект рендера всех сущностей, через него список команд прохода
        /// </summary>
        private readonly EntitiesRenderer _entities;

        public EntityRender(EntitiesRenderer entities, VertexEntityBuffer buffers)
        {
            _entities = entities;
            _mesh = new MeshEntity(entities.Context, BufferUsage.Static);
            _mesh.Reload(buffers);
        }

        /// <summary>
        /// Прорисовать сетку
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MeshDraw() => _mesh.Draw(_entities.ShsEntity.Commands);

        /// <summary>
        /// Выгрузить сетку с GPU
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _mesh.Dispose();
    }
}
