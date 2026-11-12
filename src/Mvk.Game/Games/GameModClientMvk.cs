using Mvk.Game.Entity;
using Mvk.Game.Entity.List;
using Mvk.Game.Entity.Particle;
using Mvk.Game.World;
using Mvk.Game.World.Biome;
using Mvk.Game.World.BlockEntity;
using System;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Games;
using Mvk.Engine.Network.Packets.Server;
using Mvk.Engine.World;
using Mvk.Engine.Actions;

namespace Mvk.Game.Games
{
    /// <summary>
    /// Объект игрового мода для клиентской части, для Малювеки 2
    /// </summary>
    public class GameModClientMvk : GameModClient
    {

        /// <summary>
        /// Игрок мода, на клиенте
        /// </summary>
        public PlayerClientOwnerMvk Player { get; private set; }

        public GameModClientMvk()
        {
            Colors.CreateGrass(Biomes.ColorsGrass);
            Colors.CreateWater(Biomes.ColorsWater);
#if DEBUG
            Console.WriteLine("_construct GameModClientMvk");
#endif
        }

        /// <summary>
        /// Корректировка блоков, сущностей и прочего перед инициализации миров, 
        /// тут только сетевой!
        /// Для инициализация ID сущностей и подобного.
        /// </summary>
        public override void CorrectObjects(PacketS02LoadingGame packet)
        {
            base.CorrectObjects(packet);
            // Присвоение корректных ID
            EntitiesRegMvk.InitId();
            BlocksEntityRegMvk.InitId();
            EntitiesFXRegMvk.InitId();

        }

        /// <summary>
        /// Создать настройки мира по id
        /// </summary>
        public override WorldSettings CreateWorldSettings(byte id)
        {
#if DEBUG
            Console.WriteLine("_function GameModClientMvk.CreateWorldSettings " + id);
#endif
            // Определяем настройки, в зависимости от мира
            if (id == 2)
            {
                return new WorldSettingsNightmare();
            }

            return new WorldSettingsIsland();
        }

        /// <summary>
        /// Создать объект сетевого игрока
        /// </summary>
        public override PlayerClient CreatePlayerClient(PacketS0CSpawnPlayer packet)
            => new PlayerClientMvk(Game, packet.Index, packet.Uuid, packet.Login, packet.IdWorld);

        /// <summary>
        /// Создать объект игрока владельца
        /// </summary>
        public override PlayerClientOwner CreatePlayerClientOwner()
        {
            Player = new PlayerClientOwnerMvk(Game);
            return Player;
        }

        #region Key

        /// <summary>
        /// Клавиша нажата
        /// </summary>
        public override void OnKeyDown(Keys keys)
        {
            switch (keys)
            {
                case Keys.F5: Player.ViewCameraNext(); break;
                // DrawOrto
                case Keys.F7: Player.DebugOrtoNext(); break;
                    
            }
        }

        #endregion
    }
}
