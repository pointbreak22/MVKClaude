#nullable enable
namespace Mvk.Engine.Network.Packets.Client
{
    /// <summary>
    /// Параметры игрока
    /// </summary>
    public struct PacketC15PlayerSetting : IPacket
    {
        public byte Id => 0x15;

        /// <summary>
        /// Обзор чанков
        /// </summary>
        public byte OverviewChunk { get; private set; }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Игрок со скином пользователя (UserSkin)
        /// </summary>
        public bool UserSkin { get; private set; }

        public PacketC15PlayerSetting(byte overviewChunk, bool userSkin)
        {
            OverviewChunk = overviewChunk;
            UserSkin = userSkin;
        }

        public void ReadPacket(ReadPacket stream)
        {
            OverviewChunk = stream.Byte();
            UserSkin = stream.Bool();
        }

        public void WritePacket(WritePacket stream)
        {
            stream.Byte(OverviewChunk);
            stream.Bool(UserSkin);
        }
    }
}
