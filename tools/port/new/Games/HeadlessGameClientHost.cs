using System.Collections.Generic;
using Mvk.Core.Maths;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Render;
using Mvk.Engine.Network;
using Mvk.Engine.World;
using Mvk.Engine.World.Chunk;

namespace Mvk.Engine.Games
{
    /// <summary>
    /// Хост клиента без окна, рендера и звука: для тестов и ботов.
    /// Клиент с таким хостом получает чанки и сущности, тикает мир, но ничего не рисует.
    /// </summary>
    public class HeadlessGameClientHost : IGameClientHost, IEntityRenderFactory
    {
        private readonly GameModServer _modServer;

        /// <summary>Игра, привязанная к хосту</summary>
        public GameBase Game { get; private set; }
        /// <summary>Игрок на сервере, игра запущена</summary>
        public bool Started { get; private set; }
        /// <summary>Сообщения чата, пришедшие клиенту</summary>
        public readonly List<string> Chat = new List<string>();
        /// <summary>Сколько шагов загрузки сделал сервер</summary>
        public int LoadingSteps { get; private set; }

        /// <param name="modServer">Серверный мод для локальной игры</param>
        public HeadlessGameClientHost(GameModServer modServer) => _modServer = modServer;

        public float DeltaTimeFrame { get; set; } = 16f;
        public float DeltaTime { get; set; } = 50f;
        public virtual void CursorShow(bool show) { }
        public virtual void SetCursorPosition(int x, int y) { }
        public void OptionsSave() { }

        public void OpenInGameMenu() { }
        public void ScreenClose() { }
        public void ShowProcess(string text) { }
        /// <summary>Экран загрузки «открыт», пока игра не началась: иначе клиент пропустит пакет старта</summary>
        public bool IsLoadingScreen => !Started;
        public void ServerLoadingBegin(int count) { }
        public void ServerLoadingStep() => LoadingSteps++;
        public void AcceptNetworkPackage(IPacket packet) { }
        public void ChatMessage(string message) => Chat.Add(message);
        public GameModServer CreateGameModServer() => _modServer;

        public void Attach(GameBase game) => Game = game;
        public IEntityRenderFactory EntityRenders => this;
        public virtual ChunkClient CreateChunk(WorldClient world, int chunkPosX, int chunkPosY)
            => new ChunkClient(world, chunkPosX, chunkPosY);
        public virtual void SpawnParticle(ushort particleId, Vector3 pos, Vector3 motion, int parameter) { }
        public virtual void PlaySound(int key, float posX, float posY, float posZ, float volume, float pitch) { }

        public virtual void OnGameStarting() { }
        public virtual void OnStartedGame() => Started = true;
        public virtual void OnTick(float deltaTime) { }
        public virtual void OnWorldStarting() { }
        public virtual void OnWorldStoping() { }
        public virtual void OnWorldSettings() { }
        public virtual void OnRespawnInWorld() { }
        public virtual void OnModifyOverviewChunk() { }
        public void DebugToggle(EnumDebugToggle toggle) { }
        public virtual string ToDebugString() => "headless";
        public virtual void DisposeGame() { }

        /// <summary>Пустой рендер: сущности получают объект без отображения</summary>
        public virtual EntityRenderBase Create(string kind, EntityBase entity, ResourcesEntity resources)
            => new EntityRenderBase();
    }
}
