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

        public PacketC0BBlocking(bool blocking) => Blocking = blocking;

        public void ReadPacket(ReadPacket stream) => Blocking = stream.Bool();
        public void WritePacket(WritePacket stream) => stream.Bool(Blocking);
    }
}
