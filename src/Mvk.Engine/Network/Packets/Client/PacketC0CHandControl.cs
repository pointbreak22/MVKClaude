#nullable enable
namespace Mvk.Engine.Network.Packets.Client
{
    /// <summary>
    /// Управление руками в выживании: поменять предметы рук местами (F), взять в левую руку предмет из кармана
    /// (левый Shift + колесо), выбросить предмет из правой или левой руки (Q, Shift + Q), замах и бросок предмета (удержание). Перекладывание,
    /// выброс и бросок делает сервер, клиент получает слоты обычными пакетами.
    /// </summary>
    public struct PacketC0CHandControl : IPacket
    {
        public byte Id => 0x0C;

        public EnumAction Action { get; private set; }
        /// <summary>
        /// Слот кармана (для LeftFromSlot)
        /// </summary>
        public byte Slot { get; private set; }
        /// <summary>
        /// Левая ли рука (для Drop, ThrowBegin, ThrowEnd)
        /// </summary>
        public bool Left { get; private set; }

        public PacketC0CHandControl(EnumAction action, byte slot = 0, bool left = false)
        {
            Action = action;
            Slot = slot;
            Left = left;
        }

        public void ReadPacket(ReadPacket stream)
        {
            Action = (EnumAction)stream.Byte();
            Slot = stream.Byte();
            Left = stream.Bool();
        }

        public void WritePacket(WritePacket stream)
        {
            stream.Byte((byte)Action);
            stream.Byte(Slot);
            stream.Bool(Left);
        }

        public enum EnumAction : byte
        {
            /// <summary>Поменять предметы правой и левой руки</summary>
            Swap = 0,
            /// <summary>Поменять предмет левой руки с предметом слота кармана</summary>
            LeftFromSlot = 1,
            /// <summary>Выбросить один предмет из руки</summary>
            Drop = 2,
            /// <summary>Начало замаха броска (сервер запоминает такт)</summary>
            ThrowBegin = 3,
            /// <summary>Бросок: сила по времени замаха (сервер)</summary>
            ThrowEnd = 4,
            /// <summary>Поджечь предмет руки Left предметом другой руки (динамит факелом)</summary>
            Ignite = 5
        }
    }
}
