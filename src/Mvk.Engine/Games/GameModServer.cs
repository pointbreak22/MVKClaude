#nullable enable
using Mvk.Engine.Command;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Particle;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;
using Mvk.Engine.Network;
using Mvk.Engine.World.Block;
using Mvk.Engine.World.BlockEntity;

namespace Mvk.Engine.Games
{
    /// <summary>
    /// Объект игрового мода для серверной части, этот объект наследуется другими проектами
    /// </summary>
    public class GameModServer
    {
        /// <summary>
        /// Игровой сервер
        /// </summary>
        public GameServer Server { get; private set; } = null!;

        public GameModServer() { }

        /// <summary>
        /// Инициализация при создании сервера, в начале.
        /// </summary>
        public virtual void Init(GameServer server) => Server = server;

        /// <summary>
        /// Вторая инициализация когда готовы предметы и блоки и сущности
        /// </summary>
        public virtual void InitTwo() { }

        /// <summary>
        /// Инициализация прямо перед циклом сервера
        /// </summary>
        public virtual void InitBeforeLoop() { }

        /// <summary>
        /// Корректировка блоков, сущностей и прочего перед инициализации миров, 
        /// тут для сервера и/или сингла!
        /// Для инициализация ID сущностей и подобного.
        /// </summary>
        public virtual void CorrectObjects()
        {
            Server.Registries = Registries.Correct(Server.Settings.TableEntitiesFX, Server.Settings.TableBlocks,
                Server.Settings.TableItems, Server.Settings.TableEntities, Server.Settings.TableBlocksEntity);
        }

        /// <summary>
        /// Создать объект сетевого игрока
        /// </summary>
        public virtual PlayerServer CreatePlayerServer(string login, string token, SocketSide socketSide)
            => new PlayerServer(login, token, socketSide, Server);

        /// <summary>
        /// Инициализация, регистрация комманд
        /// </summary>
        public virtual void InitCommand(ManagerCommand managerCommand) { }
    }
}
