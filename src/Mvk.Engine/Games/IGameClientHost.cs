using Mvk.Core.Maths;
using Mvk.Engine.Entity.Render;
using Mvk.Engine.Network;
using Mvk.Engine.World;
using Mvk.Engine.World.Chunk;

namespace Mvk.Engine.Games
{
    /// <summary>
    /// Хост логики клиента: окно, экраны, рендер мира, звук. Реализует Engine.Client
    /// (в оригинале GameBase сам наследовал окно Warp и владел WorldRenderer/HUD).
    /// Логика клиента вызывает хост только через этот интерфейс, поэтому может работать без окна (тесты, боты).
    /// </summary>
    public interface IGameClientHost
    {
        #region Окно и время

        /// <summary>
        /// Дельта последнего кадра в mc
        /// </summary>
        float DeltaTimeFrame { get; }
        /// <summary>
        /// Дельта последнего тика в mc
        /// </summary>
        float DeltaTime { get; }
        /// <summary>
        /// Показать или скрыть курсор
        /// </summary>
        void CursorShow(bool show);
        /// <summary>
        /// Задать позицию курсора
        /// </summary>
        void SetCursorPosition(int x, int y);
        /// <summary>
        /// Записать настройки
        /// </summary>
        void OptionsSave();

        #endregion

        #region Экраны

        /// <summary>
        /// Открыть меню в игре (Esc)
        /// </summary>
        void OpenInGameMenu();
        /// <summary>
        /// Закрыть текущий экран (загрузки)
        /// </summary>
        void ScreenClose();
        /// <summary>
        /// Показать экран процесса с текстом (сохранение, выход)
        /// </summary>
        void ShowProcess(string text);
        /// <summary>
        /// Открыт ли экран загрузки сервера (только тогда принимаются шаги загрузки и старт игры)
        /// </summary>
        bool IsLoadingScreen { get; }
        /// <summary>
        /// Сервер начал загрузку, всего шагов
        /// </summary>
        void ServerLoadingBegin(int count);
        /// <summary>
        /// Сервер сделал шаг загрузки
        /// </summary>
        void ServerLoadingStep();
        /// <summary>
        /// Передать пакет открытому экрану (окна инвентаря и т.п.)
        /// </summary>
        void AcceptNetworkPackage(IPacket packet);
        /// <summary>
        /// Пришло сообщение чата
        /// </summary>
        void ChatMessage(string message);
        /// <summary>
        /// Создать серверный мод для локальной игры
        /// </summary>
        GameModServer CreateGameModServer();

        #endregion

        #region Мир для отображения

        /// <summary>
        /// Привязать игру к хосту (клиент создаёт рендер мира)
        /// </summary>
        void Attach(GameBase game);
        /// <summary>
        /// Фабрика рендеров сущностей
        /// </summary>
        IEntityRenderFactory EntityRenders { get; }
        /// <summary>
        /// Создать чанк клиента (клиент создаёт чанк с сетками)
        /// </summary>
        ChunkClient CreateChunk(WorldClient world, int chunkPosX, int chunkPosY);
        /// <summary>
        /// Заспавнить частицу
        /// </summary>
        void SpawnParticle(ushort particleId, Vector3 pos, Vector3 motion, int parameter);
        /// <summary>
        /// Проиграть звук, координаты уже относительно слушателя
        /// </summary>
        void PlaySound(int key, float posX, float posY, float posZ, float volume, float pitch);

        /// <summary>Игра запускается (WorldRender.GameStarting)</summary>
        void OnGameStarting();
        /// <summary>Игрок на сервере, игра запущена (создание HUD)</summary>
        void OnStartedGame();
        /// <summary>Игровой такт отображения (WorldRender.OnTick, Hud.OnTick)</summary>
        void OnTick(float deltaTime);
        /// <summary>Мир клиента создан (WorldRender.Starting)</summary>
        void OnWorldStarting();
        /// <summary>Мир клиента останавливается или ошибка сети (WorldRender.Stoping)</summary>
        void OnWorldStoping();
        /// <summary>Пришли настройки мира (WorldRender.WorldRenderInitSetting)</summary>
        void OnWorldSettings();
        /// <summary>Игрок возродился в мире (WorldRender.RespawnInWorld)</summary>
        void OnRespawnInWorld();
        /// <summary>Изменился обзор чанков (WorldRender.ModifyOverviewChunk)</summary>
        void OnModifyOverviewChunk();
        /// <summary>Переключить отладочное отображение (F3+клавиша)</summary>
        void DebugToggle(EnumDebugToggle toggle);
        /// <summary>Отладочный текст рендера мира</summary>
        string ToDebugString();
        /// <summary>Освободить ресурсы отображения игры</summary>
        void DisposeGame();

        #endregion
    }

    /// <summary>
    /// Переключатели отладочного отображения
    /// </summary>
    public enum EnumDebugToggle
    {
        /// <summary>F3+G курсор чанка</summary>
        ChunkCursor,
        /// <summary>F3+B хитбоксы сущностей</summary>
        HitboxEntities
    }
}
