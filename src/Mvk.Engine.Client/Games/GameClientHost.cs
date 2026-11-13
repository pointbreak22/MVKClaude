#nullable enable
using System;
using System.Collections.Generic;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Render;
using Mvk.Engine.Gui.Huds;
using Mvk.Engine.Realms;
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

        /// <param name="renderFactory">Отрисовка интерфейса мода (RenderMvk), по умолчанию RenderMain движка</param>
        public GameClientHost(GameModServer modServer, IGraphicsDevice device, string shaderBlocksLow, string shaderEntityLow,
            Func<RenderContext, RenderMain>? renderFactory = null)
            : base(modServer)
        {
            Context = new RenderContext(device);
            Render = renderFactory?.Invoke(Context) ?? new RenderMain(Context);
            _shaderBlocksLow = shaderBlocksLow;
            _shaderEntityLow = shaderEntityLow;
        }

        private WorldRenderer _Render()
        {
            if (WorldRender == null)
            {
                WorldRender = new WorldRenderer(Game, Context, _shaderBlocksLow, _shaderEntityLow);
                // Предметы и сущности в GUI рисуются через RenderMain (в оригинале общий RenderMain окна)
                WorldRender.Entities.Gui = Render;
                Render.ShsEntity = WorldRender.Entities.ShsEntity;
            }
            return WorldRender;
        }

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

        /// <summary>
        /// Отрисовка интерфейса: шрифты, текстуры GUI, шейдеры GuiColor и GuiLine.
        /// Мод задаёт своего наследника (RenderMvk) фабрикой в конструкторе.
        /// </summary>
        public RenderMain Render { get; }
        /// <summary>
        /// Строки чата для HUD (в оригинале Player.Chat), создаётся с основным шрифтом при запуске игры
        /// </summary>
        public ChatList? ChatList { get; private set; }
        /// <summary>
        /// HUD, создаётся когда игрок вошёл в мир (Hud оригинала)
        /// </summary>
        public HudBase? Hud { get; private set; }
        /// <summary>
        /// Фабрика HUD мода (GameModClient.CreateHud оригинала). Без неё HudDebug, как в движке оригинала.
        /// </summary>
        public System.Func<GameBase, HudBase>? HudFactory { get; set; }
        /// <summary>Картинки GUI загружены</summary>
        private bool _texturesLoaded;

        public override void OnGameStarting()
        {
            _Render();
            if (!_texturesLoaded)
            {
                // Шаги загрузки картинок оригинала (Loading, LoadingMvk) до входа в мир
                Render.LoadTextures();
                _texturesLoaded = true;
            }
            ChatList = new ChatList(Ce.ChatLineTimeLife, Render.FontMain);
        }

        /// <summary>
        /// Игрок вошёл в мир: создать HUD (GameBase.OnStartedGame оригинала: Hud = ModClient.CreateHud())
        /// </summary>
        public override void OnStartedGame()
        {
            base.OnStartedGame();
            Hud?.Dispose();
            Hud = HudFactory != null ? HudFactory(Game) : new HudDebug(Game);
        }

        /// <summary>
        /// Сообщение чата в строки HUD (PlayerClientOwner оригинала: Chat.AddMessage)
        /// </summary>
        public override void ChatMessage(string message)
        {
            base.ChatMessage(message);
            ChatList?.AddMessage(message, Gi.WindowsChatWidthMessage, Gi.Si);
        }

        /// <summary>
        /// Изменён размер окна: размер шрифтов (UpdateSizeInterface окна оригинала) и HUD
        /// </summary>
        public override void OnResized(int width, int height)
        {
            Render.UpdateSizeInterface();
            Hud?.OnResized(width, height);
        }
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
        public override void OnTick(float deltaTime)
        {
            WorldRender?.OnTick(deltaTime);
            Hud?.OnTick(deltaTime);
        }
        public override void OnWorldStoping() => WorldRender?.Stoping();
        public override void OnModifyOverviewChunk() => WorldRender?.ModifyOverviewChunk();
        public override string ToDebugString() => WorldRender?.ToString() ?? "";

        public override void DisposeGame()
        {
            Hud?.Dispose();
            Hud = null;
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
            // Интерфейс поверх мира (GameBase.Draw оригинала: Hud.Draw после WorldRender.Draw)
            if (Hud != null)
            {
                Render.BeginFrame(commands);
                Hud.Draw(timeIndex);
            }
        }
    }
}
