#nullable enable
namespace Mvk.Engine.Network.Packets.Client
{
    /// <summary>
    /// Игрок начал или закончил блок щитом (вторая кнопка зажата, щит во второй руке). Блок, снижение урона и износ
    /// щита считает сервер; пакет только сообщает намерение.
    /// </summary>
    public struct PacketC0BBlocking : IPacket
    {
        public byte Id => 0x0B;

        public bool Blocking { get; private set; }
        /// <summary>
        /// [ПОВЕДЕНИЕ] Блок поднят нажатием кнопки щита (не возврат после действия другой руки): начало окна парирования
        /// </summary>
        public bool Parry { get; private set; }

        public PacketC0BBlocking(bool blocking, bool parry = false)
        {
            Blocking = blocking;
            Parry = parry;
        }

        public void ReadPacket(ReadPacket stream)
        {
            byte flags = stream.Byte();
            Blocking = (flags & 1) != 0;
            Parry = (flags & 2) != 0;
        }

        public void WritePacket(WritePacket stream) => stream.Byte((byte)((Blocking ? 1 : 0) | (Parry ? 2 : 0)));
    }
}
