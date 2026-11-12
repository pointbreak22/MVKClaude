using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Mvk.Engine.Actions;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Event;
using Mvk.Engine.Network;
using Mvk.Engine.Network.Packets;
using Mvk.Engine.Network.Packets.Server;
using Mvk.Core.Util;
using Mvk.Engine.Util;
using Mvk.Engine.World;
using Mvk.Core.Maths;

namespace Mvk.Engine.Games
{
    /// <summary>
    /// Абстрактный класс игры
    /// </summary>
    public abstract class GameBase : IDisposable
    {
        /// <summary>
        /// Объект лога
        /// </summary>
        public readonly Logger Log;
        /// <summary>
        /// Объект сыщик
        /// </summary>
        public readonly Profiler Filer;
        /// <summary>
        /// Объект нажатия клавиатуры
        /// </summary>
        public readonly Keyboard Key;
        /// <summary>
        /// Объект игрового мода для клиентской части, этот объект наследуется другими проектами
        /// </summary>
        public readonly GameModClient ModClient;
        /// <summary>
        /// Хост клиента: окно, экраны, рендер мира, звук (реализует Mvk.Engine.Client)
        /// </summary>
        public readonly IGameClientHost Host;

        /// <summary>
        /// Локальная игра
        /// </summary>
        public bool IsLoacl { get; protected set; } = true;
        /// <summary>
        /// Пауза в игре
        /// </summary>
        public bool IsGamePaused { get; protected set; } = false;
        /// <summary>
        /// Объект игрока для клиента
        /// </summary>
        public readonly PlayerClientOwner Player;
        /// <summary>
        /// Клиентский объект мира
        /// </summary>
        public WorldClient World { get; private set; }
        /// <summary>
        /// Список игроков во всех мирах, для чата 
        /// </summary>
        public readonly Dictionary<int, string> Players = new Dictionary<int, string>();

        /// <summary>
        /// Увеличивается каждый тик
        /// </summary>
        //public uint TickCounter { get; private set; } = 0;
        /// <summary>
        /// Флаг запущена ли игра, надо для сервера
        /// </summary>
        public bool FlagGameStarted { get; private set; }

        /// <summary>
        /// Счётчик тиков без синхронизации с сервером, отсчёт от запуска программы
        /// </summary>
        public uint TickCounterClient { get; private set; }
        /// <summary>
        /// Оповещение остановки, если "" её ещё не было
        /// </summary>
        protected string _stopNotification = "";
        /// <summary>
        /// Надо ли выводить окно оповещении 
        /// </summary>
        protected bool _stopIsWarning = false;
        /// <summary>
        /// Остановка из потока
        /// </summary>
        protected bool _isStop = false;
        /// <summary>
        /// Флаг, можно ли уже использовать тик
        /// </summary>
        protected bool _flagTick = false;
        /// <summary>
        /// Объект работы с пакетами
        /// </summary>
        protected readonly ProcessClientPackets _packets;

        /// <summary>
        /// Объект времени с момента запуска проекта
        /// </summary>
        private readonly Stopwatch _stopwatch = new Stopwatch();
       
        /// <summary>
        /// Флаг было ли уже остановление
        /// </summary>
        private bool _flagStoped = false;
        /// <summary>
        /// Имеется ли упровление мышкой вида от первого лица
        /// </summary>
        private bool _isMouseFirstPersonView;
        /// <summary>
        /// Флаг первого запуска упровление мышкой вида от первого лица
        /// </summary>
        private bool _flagFirstMouseFPV;

        public GameBase(IGameClientHost host, GameModClient gameModClient)
        {
            Host = host;
            ModClient = gameModClient;
            ModClient.Init(this);
            Ce.InitClient();
            Log = new Logger("Logs");
            Filer = new Profiler(Log, "[Client] ");
            _packets = new ProcessClientPackets(this);
            Player = ModClient.CreatePlayerClientOwner();
            Key = new Keyboard(this);
            Key.InGameMenu += _Key_InGameMenu;
            Host.Attach(this);
        }

        /// <summary>
        /// Дельта последнего кадра в mc
        /// </summary>
        public float DeltaTimeFrame => Host.DeltaTimeFrame;
        /// <summary>
        /// Дельта последнего тика в mc
        /// </summary>
        public float DeltaTime => Host.DeltaTime;

        /// <summary>
        /// Сохранить опции
        /// </summary>
        public void OptionsSave() => Host.OptionsSave();

        /// <summary>
        /// Активация окна меню
        /// </summary>
        private void _Key_InGameMenu(object sender, EventArgs e)
        {
            SetGamePauseSingle(true);
            Host.OpenInGameMenu();
        }

        /// <summary>
        /// Псевдоним игрока
        /// </summary>
        public virtual string ToLoginPlayer() => Options.Nickname;
        /// <summary>
        /// токен игрока
        /// </summary>
        public virtual string ToTokenPlayer() => Options.Token;

        #region StartStopPause

        /// <summary>
        /// Запуск игры и миров, после всех сетевых проверок, только для сети
        /// </summary>
        public virtual void GameStartingNet() { }

        /// <summary>
        /// Запуск игры и миров, по сети миры создаём чуть позже, после получения от сервера инфу о блоках и сущностях
        /// </summary>
        public virtual void GameStarting()
        {
            // Инициализация после старта игры, когда уже все блоки и сущности загружены
            ModClient.InitAfterStartGame();

            World = new WorldClient(this);
            World.TagDebug += World_TagDebug;
            Player.TagDebug += World_TagDebug;
            Player.WorldStarting();
            _stopwatch.Start();
            // Рендер мира информируем, что готово всё
            Host.OnGameStarting();
            //Ce.IsDebugDrawChunks = 
            // TODO::2025-09-19 включение F3
            //Ce.IsDebugDraw = true;
            FlagGameStarted = true;
        }

        private void World_TagDebug(object sender, StringEventArgs e) => _OnTagDebug(e);

        /// <summary>
        /// Остановка игры
        /// </summary>
        public virtual void GameStoping(string notification, bool isWarning)
        {
            World.Stoping();
        }

        /// <summary>
        /// Задать паузу для одиночной игры
        /// </summary>
        public virtual void SetGamePauseSingle(bool value) { }

        /// <summary>
        /// Получить истину запущена ли сеть
        /// </summary>
        public virtual bool IsRunNet() => true;

        /// <summary>
        /// Игрок на сервере
        /// </summary>
        public void PlayerOnTheServer(int id, string uuid)
        {
            Player.PlayerOnTheServer(id, uuid);

            // Отправим обзор 
            Player.SetOverviewChunk(Options.OverviewChunk, false);
            // Запущена игра
            StartedGame();

            World.SpawnEntityInWorld(Player);
            _flagTick = true;
        }

        /// <summary>
        /// Получили пакет загрузки от сервера
        /// </summary>
        public virtual void PacketLoadingGame(PacketS02LoadingGame packet) { }

        /// <summary>
        /// Запущена игра
        /// </summary>
        protected void StartedGame()
        {
            // Закрываем скрин загрузки
            Host.ScreenClose();
            Host.OnStartedGame();
        }

        #endregion

        #region OnMouse

        /// <summary>
        /// Включить или выключить вид от первого лица
        /// </summary>
        public void MouseFirstPersonView(bool on)
        {
            if (_isMouseFirstPersonView != on)
            {
                _isMouseFirstPersonView = on;
                Host.CursorShow(!on);
                if (on)
                {
                    // Включить вид от первого лица
                    _flagFirstMouseFPV = true;
                }
                else
                {
                    // Выключить вид от первого лица
                    Player.ActionStop();
                }
            }
        }

        public virtual void OnMouseMove(int x, int y)
        {
            if (_isMouseFirstPersonView)
            {
                int centerX = Gi.Width / 2;
                int centerY = Gi.Height / 2;

                if (_flagFirstMouseFPV)
                {
                    _flagFirstMouseFPV = false;
                }
                else
                {
                    // Передаём откланение от центра
                    Player.MouseMove(x - centerX, y - centerY);
                }
                // Смещаем курсор в центр
                Host.SetCursorPosition(centerX, centerY);
            }
        }

        public virtual void OnMouseDown(MouseButton button, int x, int y)
        {
            if (_isMouseFirstPersonView)
            {
                Player.Control((int)button << 20, true);
            }
            else
            {
                // Включить вид от первого лица
                MouseFirstPersonView(true);
            }
        }

        public virtual void OnMouseUp(MouseButton button, int x, int y)
        {
            if (_isMouseFirstPersonView)
            {
                Player.Control((int)(button) << 20, false);
            }
        }

        /// <summary>
        /// Вращение колёсика мыши
        /// </summary>
        public virtual void OnMouseWheel(int delta, int x, int y)
        {
            if (_isMouseFirstPersonView)
            {
                if (delta > 0) Player.Inventory.SlotLess();
                else if (delta < 0) Player.Inventory.SlotMore();
            }
        }

        #endregion

        #region OnKey

        /// <summary>
        /// Клавиша нажата
        /// </summary>
        public virtual void OnKeyDown(Keys keys)
        {
            Key.OnKeyDown(keys);
            if (_isMouseFirstPersonView)
            {
                Player.Control((int)keys, true);
            }
            ModClient.OnKeyDown(keys);
        }

        /// <summary>
        /// Клавиша отпущена
        /// </summary>
        public virtual void OnKeyUp(Keys keys)
        {
            Key.OnKeyUp(keys);
            if (_isMouseFirstPersonView)
            {
                Player.Control((int)keys, false);
            }
        }

        /// <summary>
        /// Нажата клавиша в char формате
        /// </summary>
        public virtual void OnKeyPress(char key) => Key.OnKeyPress(key);

        #endregion

        #region TickDraw

        /// <summary>
        /// Игровой такт
        /// </summary>
        /// <param name="deltaTime">Дельта последнего тика в mc</param>
        public virtual void OnTick(float deltaTime)
        {
            // почерёдно получаем пакеты с сервера
            //Filer.StartSection("Packets.Update");
            _packets.Update();
            //Filer.EndSection(10);
            if (_flagTick && !IsGamePaused)
            {
                TickCounterClient++;
                //TickCounter++;
                //Filer.StartSection("Player.Update");
                // Обновить игрока
                //Player.Update();
                // Filer.EndStartSection("World.Update", 10);
                // Обновить мир
                if (World.Settings != null) // Покуда не пришёл пакет данных мира, нечего его обновлять
                {
                    World.UpdateClient();
                }
                //Filer.EndStartSection("WorldRender.Update", 10);
               
                // Обновить рендоровский мир
                Host.OnTick(deltaTime);
                
                // Filer.EndSection(5);


                // Прошла минута, или 1800 тактов
                if (TickCounterClient % 1800 == 0)
                {
                    Log.Save();
                }

                if (TickCounterClient % 150 == 0)
                {
                    // Раз в 5 секунды перепинговка
                    TrancivePacket(new Packet00PingPong(Time()));
                }
            }

            // Тест
            //   TrancivePacket(new PacketC04PlayerPosition(new Vector3(1, 2.5f, 3.5f), true, false, true));
        }

        /// <summary>
        /// Получить время в милисекундах с момента запуска проекта
        /// </summary>
        public long Time() => _stopwatch.ElapsedMilliseconds;

        /// <summary>
        /// Получить время в тактах объекта Stopwatch с момента запуска проекта
        /// </summary>
        public long ElapsedTicks() => _stopwatch.ElapsedTicks;

        /// <summary>
        /// Задать время с сервера
        /// </summary>
       // public void SetTickCounter(uint time) => TickCounter = time;

        #endregion

        #region Effect, sound particles
        
        /// <summary>
        /// Проиграть звуковой эффект в глобальных координатах мира
        /// </summary>
        public virtual void PlaySound(int key, float posX, float posY, float posZ, float volume, float pitch)
        {
            posX -= Player.PosX;
            posY -= Player.PosY + Player.GetEyeHeight();
            posZ -= Player.PosZ;

            float c = Glm.Cos(Player.RotationYaw);
            float s = Glm.Sin(Player.RotationYaw);
            float x2 = posX * c + posZ * s;
            posZ = posZ * c - posX * s;
            posX = x2;

            if (volume > 3)
            {
                // Слышимость в 4 раза лучше
                posX *= .03f;
                posY *= .03f;
                posZ *= .03f;
            }
            else
            {
                // Слышимость в 2 раза лучше кроме высоты
                posX *= .1f;
                posZ *= .1f;
            }
            //Console.WriteLine(posX + " " + posY + " " + posZ + " v:" + volume);
            Host.PlaySound(key, posX, posY, posZ, volume, pitch);
        }

        /// <summary>
        /// Заспавнить частицу
        /// </summary>
        public virtual void SpawnParticle(int key, int count, 
            Vector3 pos, Vector3 offset, float motion, int parameter)
        {
            if (count == 1)
            {
               // ClientMain.EffectRender.SpawnParticle(particle, pos, offset * motion, items);
            }
            else
            {
                Vector3 of;
                Vector3 m = new Vector3(0);
                for (int i = 0; i < count; i++)
                {
                    of = new Vector3(
                        (World.Rnd.NextFloat() - .5f) * offset.X,
                        (World.Rnd.NextFloat() - .5f) * offset.Y,
                        (World.Rnd.NextFloat() - .5f) * offset.Z);
                    if (motion > 0)
                    {
                        m = new Vector3(
                            (World.Rnd.NextFloat() - .5f) * motion,
                            (World.Rnd.NextFloat() - .5f) * motion,
                            (World.Rnd.NextFloat() - .5f) * motion);
                    }
                   // ClientMain.EffectRender.SpawnParticle(particle, pos + of, m, parameter);
                }
            }
        }

        #endregion

        #region Packets

        /// <summary>
        /// Отправить пакет на сервер
        /// </summary>
        public virtual void TrancivePacket(IPacket packet) { }

        /// <summary>
        /// Сетевой игрок зашёл или вышел из игры сервера, для чата
        /// </summary>
        public void PlayerEntryRemove(PacketS06PlayerEntryRemove packet)
        {
            if (packet.Login == "")
            {
                // Remove
                Players.Remove(packet.Index);
            }
            else
            {
                // Entry
                if (Players.ContainsKey(packet.Index))
                {
                    Players[packet.Index] = packet.Login;
                }
                else
                {
                    Players.Add(packet.Index, packet.Login);
                }
            }
        }

        #endregion

        public virtual void Dispose() => Host.DisposeGame();

        public override string ToString()
        {
            return string.Format("{0} ping: {1} ms Traffic: {3}{2} Tick {4}\r\n{5}\r\n{6} {7}",
                IsLoacl ? "Local" : "Net", // 0
                Player.Ping, IsGamePaused ? " Pause" : "", // 1-2
                _ToTraffic(), 0,//TickCounter, // 3-4
                Player, // 5
                Host.ToDebugString(), // 6
                World != null ? World.ToString() : "" // 7
                );
        }

        private string _ToTraffic()
        {
            long traffic = _packets.Traffic;
            if (traffic < 1048576)
            {
                return (traffic / 1024f).ToString("0.0") + " kb";
            }
            else
            {
                return (traffic / 1048576f).ToString("0.0") + " mb";
            }
        }

        #region Event

        /// <summary>
        /// Событие остановлена игра
        /// </summary>
        public event GameStopEventHandler Stoped;
        protected void _OnStoped(string notification, bool isWarning)
        {
            if (!_flagStoped)
            {
                Ce.IsDebugDrawChunks = Ce.IsDebugDraw = false;
                // флаг нужен, так-как можно попасть сюда много раз, из-за разрыва сети.
                _flagStoped = true;
                Log.Client(Srl.StoppedClient, notification == "" ? "" : " [" + notification + "]");
                Log.Save();
                Stoped?.Invoke(this, new GameStopEventArgs(notification, isWarning));
            }
        }

        /// <summary>
        /// Событие ошибки на сервере
        /// </summary>
        public event ThreadExceptionEventHandler Error;
        protected void _OnError(Exception ex)
        {
            Host.OnWorldStoping();
            Error?.Invoke(this, new ThreadExceptionEventArgs(ex));
        }

        /// <summary>
        /// Событие текст отладки сервера
        /// </summary>
        public event StringEventHandler ServerTextDebug;
        protected virtual void _OnServerTextDebug(string text) 
            => ServerTextDebug?.Invoke(this, new StringEventArgs(text));

        /// <summary>
        /// Событие tag отладки сервера
        /// </summary>
        public event StringEventHandler TagDebug;
        protected virtual void _OnTagDebug(StringEventArgs e)
            => TagDebug?.Invoke(this, e);

        #endregion
    }
}
