using Mvk.Game.Command;
using Mvk.Game.Entity;
using Mvk.Game.Entity.List;
using Mvk.Game.Entity.Particle;
using Mvk.Game.World.BlockEntity;
using System;
using Mvk.Engine.Command;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Games;
using Mvk.Engine.Network;

namespace Mvk.Game.Games
{
    /// <summary>
    /// Объект игрового мода для серверной части, для Малювеки 2
    /// </summary>
    public class GameModServerMvk : GameModServer
    {
        public GameModServerMvk() : base()
        {
#if DEBUG
            Console.WriteLine("_construct GameModServerMvk");
#endif
        }

        /// <summary>
        /// Создать объект сетевого игрока
        /// </summary>
        public override PlayerServer CreatePlayerServer(string login, string token, SocketSide socketSide)
            => new PlayerServerMvk(login, token, socketSide, Server);

        /// <summary>
        /// Корректировка блоков, сущностей и прочего перед инициализации миров, 
        /// тут для сервера и/или сингла!
        /// Для инициализация ID сущностей и подобного.
        /// </summary>
        public override void CorrectObjects()
        {
            base.CorrectObjects();
            // Присвоение корректных ID
            EntitiesRegMvk.InitId();
            BlocksEntityRegMvk.InitId();
            EntitiesFXRegMvk.InitId();
        }

        /// <summary>
        /// Инициализация, регистрация комманд
        /// </summary>
        public override void InitCommand(ManagerCommand managerCommand)
            => ManagerCommandMvk.Init(Server, managerCommand);
    }
}
