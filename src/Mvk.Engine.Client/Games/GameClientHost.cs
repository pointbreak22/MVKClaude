#nullable enable
using System;
using System.Collections.Generic;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Render;
using Mvk.Engine.Renderer;
using Mvk.Engine.Renderer.World.Entity;
using Mvk.Engine.Renderer.World;
using Mvk.Engine.World;
using Mvk.Engine.World.Chunk;
using Mvk.Rendering;

namespace Mvk.Engine.Games
{
    /// <summary>
    /// Хост клиента с рендером мира на RHI. Пока без экранов, HUD, частиц и звука:
    /// от хоста без окна отличается чанками с сетками и рендером мира.
    /// </summary>
    /// <remarks>
    /// В оригинале WorldRenderer создавался вместе с окном, до игры. Здесь он создаётся при первом
    /// обращении: WorldClient сообщает о старте мира (OnWorldStarting) раньше, чем GameBase зовёт OnGameStarting.
    /// Создавать его нужно в потоке окна (GameStarting вызывается оттуда).
    /// </remarks>
    public class GameClientHost : HeadlessGameClientHost
    {
        /// <summary>Общие объекты рендера</summary>
        public readonly RenderContext Context;
        /// <summary>Рендер мира, создаётся при запуске игры</summary>
        public WorldRenderer? WorldRender { get; private set; }

        private readonly string _shaderBlocksLow;
        private readonly string _shaderEntityLow;
        /// <summary>
        /// Рендеры сущностей, которые добавил мод (ключ вида рендера → создание)
        /// </summary>
        private readonly Dictionary<string, Func<EntityBase, EntitiesRenderer, ResourcesEntity, EntityRenderBase>> _entityRenders
            = new Dictionary<string, Func<EntityBase, EntitiesRenderer, ResourcesEntity, EntityRenderBase>>();

        public GameClientHost(GameModServer modServer, IGraphicsDevice device, string shaderBlocksLow, string shaderEntityLow)
            : base(modServer)
        {
            Context = new RenderContext(device);
            _shaderBlocksLow = shaderBlocksLow;
            _shaderEntityLow = shaderEntityLow;
        }

        private WorldRenderer _Render() => WorldRender ??= new WorldRenderer(Game, Context, _shaderBlocksLow, _shaderEntityLow);

        /// <summary>
        /// Зарегистрировать рендер сущности мода (в оригинале мод создавал его сам в InitRender сущности)
        /// </summary>
        public void RegisterEntityRender(string kind, Func<EntityBase, EntitiesRenderer, ResourcesEntity, EntityRenderBase> create)
            => _entityRenders[kind] = create;

        /// <summary>
        /// Создать рендер сущности по ключу вида (IEntityRenderFactory). Частицы пока без рендера.
        /// </summary>
        public override EntityRenderBase Create(string kind, EntityBase entity, ResourcesEntity resources)
        {
            EntitiesRenderer entities = _Render().Entities;
            if (_entityRenders.TryGetValue(kind, out var create)) return create(entity, entities, resources);
            return kind switch
            {
                EntityRenderKind.Client => new EntityRenderClient(entity, entities, resources),
                EntityRenderKind.Animation => new EntityRenderAnimation((EntityLiving)entity, entities, resources),
                EntityRenderKind.EyeMouth => new EntityRenderEyeMouth((EntityLiving)entity, entities, resources),
                EntityRenderKind.Item => new EntityRenderItem(entity, entities),
                _ => base.Create(kind, entity, resources)
            };
        }

        public override ChunkClient CreateChunk(WorldClient world, int chunkPosX, int chunkPosY)
            => new ChunkRender(world, _Render(), chunkPosX, chunkPosY);

        /// <summary>
        /// Окно показывает или прячет курсор (игра включает и выключает обзор мышью)
        /// </summary>
        public System.Action<bool>? CursorShowHandler { get; set; }

        public override void CursorShow(bool show) => CursorShowHandler?.Invoke(show);

        public override void OnGameStarting() => _Render();
        public override void OnWorldStarting() => _Render().Starting();

        /// <summary>
        /// Фабрика неба мира, её задаёт клиент мода (в оригинале небо создавал
        /// GameModClientMvk.CreateWorldSettings). Без фабрики рисуется базовое небо.
        /// </summary>
        public System.Func<GameBase, WorldRenderer, SkyRender>? SkyFactory { get; set; }

        /// <summary>
        /// Пришли настройки мира: создать небо мира и передать ему настройки
        /// (WorldStarted + WorldRenderInitSetting оригинала)
        /// </summary>
        public override void OnWorldSettings()
        {
            WorldRenderer render = _Render();
            render.WorldStarted(SkyFactory?.Invoke(Game, render) ?? new SkyRender(Game.Player, render));
            render.WorldRenderInitSetting();
        }
        public override void OnTick(float deltaTime) => WorldRender?.OnTick(deltaTime);
        public override void OnWorldStoping() => WorldRender?.Stoping();
        public override void OnModifyOverviewChunk() => WorldRender?.ModifyOverviewChunk();
        public override string ToDebugString() => WorldRender?.ToString() ?? "";

        public override void DisposeGame()
        {
            WorldRender?.Dispose();
            WorldRender = null;
        }

        /// <summary>
        /// Записать кадр мира
        /// </summary>
        public void Draw(CommandList commands, float timeIndex)
        {
            if (WorldRender != null) WorldRender.Draw(commands, timeIndex);
            else commands.Clear(0, 0, 0, 1);
        }
    }
}
