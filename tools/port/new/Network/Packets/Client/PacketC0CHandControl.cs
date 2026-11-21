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
        /// Слот кармана (для LeftFromSlot); для Drop: 1 — весь стак
        /// </summary>
        public byte Slot { get; private set; }
        /// <summary>
        /// Левая ли рука (для Drop, ThrowBegin, ThrowEnd)
        /// </summary>
        public bool Left { get; private set; }
        /// <summary>
        /// Id сущности-получателя (для Give)
        /// </summary>
        public int Target { get; private set; }

        public PacketC0CHandControl(EnumAction action, byte slot = 0, bool left = false)
        {
            Action = action;
            Slot = slot;
            Left = left;
            Target = 0;
        }

        /// <summary>
        /// Отдать один предмет руки игроку target
        /// </summary>
        public static PacketC0CHandControl Give(int target, bool left)
        {
            PacketC0CHandControl packet = new PacketC0CHandControl(EnumAction.Give, 0, left);
            packet.Target = target;
            return packet;
        }

        public void ReadPacket(ReadPacket stream)
        {
            Action = (EnumAction)stream.Byte();
            Slot = stream.Byte();
            Left = stream.Bool();
            if (Action == EnumAction.Give) Target = stream.Int();
        }

        public void WritePacket(WritePacket stream)
        {
            stream.Byte((byte)Action);
            stream.Byte(Slot);
            stream.Bool(Left);
            if (Action == EnumAction.Give) stream.Int(Target);
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
            Ignite = 5,
            /// <summary>Отдать один предмет руки игроку, на которого смотрит (Target)</summary>
            Give = 6
        }
    }
}
