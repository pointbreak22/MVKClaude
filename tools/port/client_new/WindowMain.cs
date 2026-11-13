#nullable enable
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Mvk.Engine.Actions;
using Mvk.Engine.Audio;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Particle;
using Mvk.Engine.Event;
using Mvk.Engine.Games;
using Mvk.Engine.Gui.Screens;
using Mvk.Engine.Item;
using Mvk.Engine.Renderer;
using Mvk.Engine.Util;
using Mvk.Engine.World;
using Mvk.Engine.World.Block;
using Mvk.Engine.World.BlockEntity;
using Mvk.Rendering;

namespace Mvk.Engine
{
    /// <summary>
    /// Приложение клиента: экраны, игра, такты и кадры. Перенос логики WindowMain оригинала без окна ОС
    /// (WinGL.Window): окно, курсор, полноэкранный режим и закрытие делает хост платформы через IWindowHost,
    /// он же передаёт ввод и вызывает Update и Draw. Имена и порядок действий как в оригинале,
    /// чтобы перенесённые экраны (window.LScreen, window.Game, window.ScreenClose…) работали как раньше.
    /// </summary>
    public class WindowMain : IDisposable
    {
        #region Properties

        /// <summary>Версия OpenGL (описание устройства)</summary>
        public string VersionOpenGL { get; protected set; } = "";
        /// <summary>Версия</summary>
        public string Version { get; protected set; } = "";
        /// <summary>Координата мыши X</summary>
        public int MouseX { get; protected set; }
        /// <summary>Координата мыши Y</summary>
        public int MouseY { get; protected set; }
        /// <summary>Объект отладки</summary>
        public readonly Debug debug = new Debug();
        /// <summary>Отрисовка интерфейса</summary>
        public RenderMain Render { get; protected set; } = null!;
        /// <summary>Запуск экранов</summary>
        public LaunchScreen LScreen { get; protected set; } = null!;
        /// <summary>Игра, null если не запущена</summary>
        public GameBase? Game { get; protected set; }
        /// <summary>Хост клиента текущей игры (рендер мира и HUD)</summary>
        public GameClientHost? GameHost { get; private set; }
        /// <summary>Дельта последнего такта в мс</summary>
        public float DeltaTime { get; private set; }
        /// <summary>Дельта последнего кадра в мс</summary>
        public float DeltaTimeFrame { get; private set; }
        /// <summary>Загрузка ресурсов закончена</summary>
        public bool Ready { get; private set; }
        /// <summary>Открытый экран или null</summary>
        public ScreenBase? Screen { get; private set; }
        /// <summary>Общие объекты рендера</summary>
        public RenderContext Context { get; }
        /// <summary>Полноэкранный режим</summary>
        public bool FullScreen => _host.FullScreen;
        /// <summary>Звук окна</summary>
        public AudioBase Audio { get; protected set; }
        /// <summary>Ввод символов приходит в Windows-1251 (в оригинале при русской раскладке WinForms)</summary>
        public bool KeyCyrillic { get; protected set; }

        #endregion

        /// <summary>Шейдер блоков без теней (RenderMain оригинала)</summary>
        protected const string _shaderBlocksLow = "BlocksLow";
        /// <summary>Шейдер сущностей без теней (RenderMain оригинала)</summary>
        protected const string _shaderEntityLow = "EntityLow";

        protected readonly IWindowHost _host;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private bool _flagClose;
        private double _lastTickMs, _accumulatorMs, _lastFrameMs;
        /// <summary>Желаемое число кадров (Ticker.SetWishFrame оригинала), кадры задаёт хост</summary>
        private int _wishFrame = Ce.FpsOffside;
        /// <summary>Цвет очистки кадра (gl.ClearColor оригинала)</summary>
        private float _clearR, _clearG, _clearB, _clearA = 1;

        #region Initialized

        public WindowMain(IWindowHost host, IGraphicsDevice device)
        {
            _host = host;
            Context = new RenderContext(device);
            VersionOpenGL = device.Description;
            Audio = new AudioBase(host.Audio);
            LScreen = new LaunchScreen(this);
            Initialized();
        }

        /// <summary>
        /// Дополнительная инициализация (наследник задаёт версию, экраны и загружает опции)
        /// </summary>
        protected virtual void Initialized()
        {
            Version = "Vge " + Assembly.GetExecutingAssembly().GetName().Version;
        }

        /// <summary>
        /// Окно готово (OnOpenGLInitialized + Begined оригинала): создать отрисовку и показать заставку
        /// </summary>
        public void Begin()
        {
            RenderInitialized();
            Begined();
        }

        /// <summary>Готовность к игре, все ресурсы загружены</summary>
        public void Readed() => Ready = true;

        /// <summary>
        /// В оригинале GL окна, им создавались сетки; здесь отрисовка интерфейса
        /// </summary>
        public RenderMain GetOpenGL() => Render;

        protected virtual void OptionsLoad() => new OptionsFile().Load();
        public virtual void OptionsSave() => new OptionsFile().Save();

        #endregion

        #region OnMouse

        public virtual void OnMouseMove(int x, int y)
        {
            MouseX = x;
            MouseY = y;
            if (Screen != null) Screen.OnMouseMove(x, y);
            else Game?.OnMouseMove(x, y);
        }

        public virtual void OnMouseDown(MouseButton button, int x, int y)
        {
            MouseX = x;
            MouseY = y;
            if (Screen != null) Screen.OnMouseDown(button, x, y);
            else Game?.OnMouseDown(button, x, y);
        }

        public virtual void OnMouseUp(MouseButton button, int x, int y)
        {
            MouseX = x;
            MouseY = y;
            if (Screen != null) Screen.OnMouseUp(button, x, y);
            else Game?.OnMouseUp(button, x, y);
        }

        public virtual void OnMouseWheel(int delta, int x, int y)
        {
            if (Screen != null) Screen.OnMouseWheel(delta, x, y);
            else Game?.OnMouseWheel(delta, x, y);
        }

        #endregion

        #region OnKey

        public virtual void OnKeyDown(Keys keys)
        {
            if (Screen != null) Screen.OnKeyDown(keys);
            else Game?.OnKeyDown(keys);
            // Принудительно, не вожно открыт ли Screen
            Game?.Key.OnKeyDownForcedly(keys);
            if (keys == Keys.F11) SetFullScreen(!FullScreen);
        }

        public virtual void OnKeyUp(Keys keys)
        {
            if (Screen != null) Screen.OnKeyUp(keys);
            else Game?.OnKeyUp(keys);
            // Принудительно, не вожно открыт ли Screen
            Game?.Key.OnKeyUpForcedly(keys);
        }

        public virtual void OnKeyPress(char key)
        {
            if (Screen != null)
            {
                if (KeyCyrillic)
                {
                    // Конвертируем кириллицу с Windows-1251 на Unicode
                    int id = key;
                    if (id > 191 && id < 256) key = (char)(id + 848);
                    else if (id == 168) key = (char)1025;
                    else if (id == 184) key = (char)1105;
                }
                Screen.OnKeyPress(key);
            }
        }

        #endregion

        #region On...

        /// <summary>
        /// Окно получило или потеряло фокус
        /// </summary>
        public virtual void OnActivate(bool active)
        {
            if (!active && Game != null)
            {
                // Если происходит деактивация окна, а мы находились в игре,
                // Выключаем вид от первого лица
                Game.MouseFirstPersonView(false);
            }
        }

        /// <summary>
        /// Задать цвет очистки следующих кадров (gl.ClearColor оригинала)
        /// </summary>
        public void ClearColor(float r, float g, float b, float a)
        {
            _clearR = r;
            _clearG = g;
            _clearB = b;
            _clearA = a;
        }

        /// <summary>
        /// Записать кадр (OnOpenGlDraw оригинала) и отправить на устройство
        /// </summary>
        public void Draw(CommandList commands, int width, int height)
        {
            double now = _clock.Elapsed.TotalMilliseconds;
            DeltaTimeFrame = (float)(now - _lastFrameMs);
            _lastFrameMs = now;
            if (GameHost != null) GameHost.DeltaTimeFrame = DeltaTimeFrame;

            commands.Reset();
            commands.SetViewport(0, 0, width, height);
            commands.Clear(_clearR, _clearG, _clearB, _clearA);
            Render.BeginFrame(commands);
            float interpolation = (float)(_accumulatorMs / Ce.TickTime);
            if (Game == null || GameHost == null)
            {
                Screen?.Draw(interpolation);
            }
            else
            {
                // Есть игра
                float timeIndex = Game.IsGamePaused ? 1 : interpolation;
                GameHost.Draw(commands, timeIndex);
                if (Screen != null)
                {
                    Render.BeginFrame(commands);
                    Render.DepthOff();
                    Screen.Draw(timeIndex);
                    Render.DepthOn();
                }
            }
            if (Ce.IsDebugDraw)
            {
                DrawDebug();
            }
            Context.Device.Submit(commands);
        }

        protected virtual void DrawDebug() { }

        /// <summary>
        /// Изменён размер окна
        /// </summary>
        public virtual void OnResized(int width, int height)
        {
            if (width <= 0 || height <= 0) return;
            Gi.Width = width;
            Gi.Height = height;
            UpdateSizeInterface();
            Gi.UpOrtho();
            Screen?.Resized();
            Game?.OnResized(width, height);
        }

        #endregion

        /// <summary>
        /// Изменить размер интерфейса
        /// </summary>
        public virtual void UpdateSizeInterface()
        {
            Gi.UpdateSizeInterface();
            Render?.UpdateSizeInterface();
            Game?.OnResized(Gi.Width, Gi.Height);
        }

        #region WindowOverride

        /// <summary>
        /// Задать режим экрана
        /// </summary>
        public virtual void SetFullScreen(bool fullScreen)
        {
            _host.SetFullScreen(fullScreen);
            Options.FullScreen = fullScreen;
        }

        /// <summary>
        /// Включить или выключить вертикальную синхронизацию
        /// </summary>
        public virtual void SetVSync(bool on) => _host.SetVSync(on);

        /// <summary>
        /// Показать заставку
        /// </summary>
        public virtual void Begined() => LScreen.Splash();

        /// <summary>
        /// Создать отрисовку интерфейса
        /// </summary>
        protected virtual void RenderInitialized() => Render = new RenderMain(Context);

        /// <summary>
        /// Выход из приложения
        /// </summary>
        public void Exit() => Close();

        /// <summary>
        /// Закрыть приложение: сначала остановить игру (сервер сохраняет мир)
        /// </summary>
        public void Close()
        {
            if (Game != null)
            {
                _flagClose = true;
                Game.GameStoping(ClientStrings.ClosingTheApplication, false);
            }
            else
            {
                _host.Close();
            }
        }

        /// <summary>
        /// Желаемое число кадров в секунду (в оригинале Ticker; здесь кадры задаёт хост)
        /// </summary>
        public void SetWishFrame(int frame) => _wishFrame = frame;

        /// <summary>Желаемое число кадров в секунду</summary>
        public int WishFrame => _wishFrame;

        /// <summary>
        /// Показать или скрыть курсор (захват мыши в игре)
        /// </summary>
        public void CursorShow(bool show) => _host.CursorShow(show);

        /// <summary>
        /// Задать позицию курсора
        /// </summary>
        public void SetCursorPosition(int x, int y) => _host.SetCursorPosition(x, y);

        /// <summary>
        /// Текст из буфера обмена (Clipboard.GetText WinForms оригинала)
        /// </summary>
        public string ClipboardGetText() => _host.ClipboardGetText() ?? "";

        /// <summary>
        /// Положить текст в буфер обмена
        /// </summary>
        public void ClipboardSetText(string text) => _host.ClipboardSetText(text);

        #endregion

        #region Screen

        /// <summary>
        /// Создание нового экрана
        /// </summary>
        public void ScreenCreate(ScreenBase screen, bool dispose = true)
        {
            if (dispose) Screen?.Dispose();
            Screen = screen;
            Screen.Initialize();
            // Если находимся в игре, то выключаем управление вида от первого лица
            Game?.MouseFirstPersonView(false);
        }

        /// <summary>
        /// Запуск от родителя
        /// </summary>
        public void ScreenLaunchFromParent(ScreenBase screen, EnumScreenParent enumParent = EnumScreenParent.None)
        {
            Screen?.Dispose();
            Screen = screen;
            Screen.LaunchFromParent(enumParent);
            Screen.Resized();
        }

        /// <summary>
        /// Закрыть скрин
        /// </summary>
        public void ScreenClose()
        {
            Screen?.Dispose();
            Screen = null;
            // Если находимся в игре, то включаем управление вида от первого лица
            Game?.MouseFirstPersonView(true);
        }

        #endregion

        #region Game

        protected virtual AllWorlds _CreateAllWorlds() => new AllWorlds();

        /// <summary>Сообщения регистров о шагах (в оригинале window.LScreen.Process)</summary>
        protected IProgress<string> _Progress() => new ScreenProgress(this);

        protected virtual void _InitializationBlocks() => BlocksReg.Initialization(_Progress());
        protected virtual void _InitializationItems() => ItemsReg.Initialization(_Progress());
        protected virtual void _InitializationEntities() => EntitiesReg.Initialization(_Progress());
        protected virtual void _InitializationBlocksEntity() => BlocksEntityReg.Initialization(_Progress());
        protected virtual void _InitializationEntitiesFX() => EntitiesFXReg.Initialization(_Progress());

        /// <summary>
        /// Инициализация блоков, предметов, атласа и сущностей перед каждой игрой, как в оригинале.
        /// Атлас блоков загружает на GPU рендер мира (BlockAtlasTextures).
        /// </summary>
        private void _InitializationBlocksAtlasEntities()
        {
            _InitializationBlocks();
            _InitializationItems();
            _InitializationEntities();
            _InitializationBlocksEntity();
            EntitiesReg.TextureManagerRun();
            _InitializationEntitiesFX();
        }

        /// <summary>
        /// Запустить сетевую игру
        /// </summary>
        public void GameNetRun(string ipAddress, int port)
        {
            if (Game == null)
            {
                _InitializationBlocksAtlasEntities();
                LScreen.Process(L.T("Connection") + Ce.Ellipsis);
                GameHost = _CreateGameHost();
                Game = new GameNet(GameHost, ipAddress, port, _CreateGameModClient());
                _GameRun();
            }
        }

        /// <summary>
        /// Запустить локальную игру
        /// </summary>
        public void GameLocalRun(GameSettings gameSettings)
        {
            if (Game == null)
            {
                _InitializationBlocksAtlasEntities();
                LScreen.Working(L.T("LoadingTheWorld"));
                GameHost = _CreateGameHost();
                Game = new GameLocal(GameHost, gameSettings, _CreateAllWorlds(), _CreateGameModClient());
                _GameRun();
            }
        }

        /// <summary>
        /// Хост клиента для новой игры: рендер мира, HUD и связь игры с окном
        /// </summary>
        protected virtual GameClientHost _CreateGameHost()
            => new GameClientHost(CreateGameModServer(), Render, _shaderBlocksLow, _shaderEntityLow, this);

        /// <summary>
        /// Подключить к хосту клиента части мода: небо, HUD, рендеры сущностей (в оригинале их создавал
        /// клиент мода, знающий окно)
        /// </summary>
        public virtual void SetupHost(GameClientHost host) { }

        private void _GameRun()
        {
            Game!.Stoped += Game_Stoped;
            Game.Error += Game_Error;
            Game.ServerTextDebug += Game_ServerTextDebug;
            Game.TagDebug += Game_TagDebug;
            SetWishFrame(Options.Fps);
            Game.GameStarting();
        }

        protected virtual GameModClient _CreateGameModClient() => new GameModClient();
        public virtual GameModServer CreateGameModServer() => new GameModServer();

        /// <summary>
        /// Остановить игру
        /// </summary>
        public void GameStoping() => Game?.GameStoping(ClientStrings.TheUserStoppedTheGame, false);

        private void Game_ServerTextDebug(object? sender, StringEventArgs e) => debug.Server = e.Text;
        private void Game_TagDebug(object? sender, StringEventArgs e) => debug.SetTag(e);

        protected virtual void Game_Stoped(object? sender, GameStopEventArgs e)
        {
            Game!.Dispose();
            Game = null;
            GameHost = null;
            SetWishFrame(Ce.FpsOffside);
            debug.Server = e.Notification;
            if (_flagClose)
            {
                // Если закрытие игры из-за закрытия приложения, повторяем закрытие приложения
                Close();
            }
            else
            {
                // Чистка мусора
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (e.IsWarning)
                {
                    // Окно оповещения
                    LScreen.Notification(e.Notification);
                }
                else
                {
                    // Меню
                    LScreen.MainMenu();
                }
            }
        }

        private void Game_Error(object? sender, ThreadExceptionEventArgs e)
        {
            Logger.Crash(e.Exception, "WindowMain");
            _host.Crash(e.Exception);
        }

        #endregion

        #region Ticker

        /// <summary>
        /// Прокрутить такты по реальному времени (Ticker оригинала: 20 тактов в секунду)
        /// </summary>
        public void Update()
        {
            double now = _clock.Elapsed.TotalMilliseconds;
            _accumulatorMs += now - _lastTickMs;
            _lastTickMs = now;
            // Если машина не успевает, не копим такты бесконечно
            if (_accumulatorMs > Ce.TickTime * 10) _accumulatorMs = Ce.TickTime * 10;
            while (_accumulatorMs >= Ce.TickTime)
            {
                _accumulatorMs -= Ce.TickTime;
                DeltaTime = Ce.TickTime;
                if (GameHost != null) GameHost.DeltaTime = DeltaTime;
                OnTick();
            }
        }

        protected virtual void OnTick()
        {
            Audio.Tick();
            debug.Audio = Audio.StrDebug;
            if (Screen != null)
            {
                Screen.OnTick(DeltaTime);
            }
            if (Game == null)
            {
                debug.Client = "null";
            }
            else
            {
                debug.Client = Game.ToString();
                Game.OnTick(DeltaTime);
            }
        }

        /// <summary>Время с запуска в мс</summary>
        public long Time() => _clock.ElapsedMilliseconds;
        /// <summary>Время с запуска в тиках таймера</summary>
        public long TimeTicks() => _clock.ElapsedTicks;

        #endregion

        #region Sound

        /// <summary>
        /// Звук в координатах относительно игрока (их считает GameBase.PlaySound)
        /// </summary>
        public virtual void PlaySound(int key, float posX, float posY, float posZ, float volume, float pitch)
            => Audio.PlaySound(key, posX, posY, posZ, volume, pitch);

        /// <summary>
        /// Звук клика GUI
        /// </summary>
        public virtual void SoundClick(float volume) { }

        #endregion

        public virtual void Dispose()
        {
            Ready = false;
            Screen?.Dispose();
            Game?.Dispose();
            Render?.Dispose();
            Audio.Clear();
            Context.Dispose();
        }

        /// <summary>
        /// Сообщения шагов регистров на экран процесса
        /// </summary>
        private sealed class ScreenProgress : IProgress<string>
        {
            private readonly WindowMain _window;
            public ScreenProgress(WindowMain window) => _window = window;
            public void Report(string value) => _window.LScreen.Process(value);
        }
    }

    /// <summary>
    /// Окно платформы для WindowMain: то, что в оригинале делал WinGL.Window
    /// </summary>
    public interface IWindowHost
    {
        /// <summary>Полноэкранный режим</summary>
        bool FullScreen { get; }
        /// <summary>Задать полноэкранный режим</summary>
        void SetFullScreen(bool fullScreen);
        /// <summary>Вертикальная синхронизация</summary>
        void SetVSync(bool on);
        /// <summary>Показать или скрыть курсор (при скрытом мышь захвачена для обзора)</summary>
        void CursorShow(bool show);
        /// <summary>Задать позицию курсора</summary>
        void SetCursorPosition(int x, int y);
        /// <summary>Закрыть окно и выйти</summary>
        void Close();
        /// <summary>Ошибка игры (в оригинале MessageBoxCrash)</summary>
        void Crash(Exception exception);
        /// <summary>Текст из буфера обмена или null</summary>
        string? ClipboardGetText();
        /// <summary>Положить текст в буфер обмена</summary>
        void ClipboardSetText(string text);
        /// <summary>Устройство звука или null (без звуковой карты, снимки)</summary>
        IWindowAudio? Audio { get; }
    }
}
