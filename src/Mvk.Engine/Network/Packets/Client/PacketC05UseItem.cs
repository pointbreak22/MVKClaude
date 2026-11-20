#nullable enable
namespace Mvk.Engine.Network.Packets.Client
{
    /// <summary>
    /// Отправляем на сервер взаимодействие с выбранным предметом в руке без RayCast блока
    /// </summary>
    public struct PacketC05UseItem : IPacket
    {
        public byte Id => 0x05;

        /// <summary>
        /// Вспомогательное ли действие
        /// </summary>
        public bool IsSecond { get; private set; }
        /// <summary>
        /// Дополнительный цифровой параметр
        /// </summary>
        public int Number { get; private set; }

        /// <summary>
        /// Действие левой рукой (выживание, две руки)
        /// </summary>
        public bool Left { get; private set; }

        /// <summary>
        /// Задать руку действия
        /// </summary>
        public PacketC05UseItem Hand(bool left)
        {
            Left = left;
            return this;
        }

        /// <summary>
        /// Взаимодействие вспомогательного предмета без блока с параметром
        /// </summary>
        public PacketC05UseItem(int number)
        {
            Number = number;
            IsSecond = true;
        }

        public void ReadPacket(ReadPacket stream)
        {
            byte flags = stream.Byte();
            IsSecond = (flags & 1) != 0;
            Left = (flags & 2) != 0;
            if (IsSecond)
            {
                Number = stream.Int();
            }
        }

        public void WritePacket(WritePacket stream)
        {
            stream.Byte((byte)((IsSecond ? 1 : 0) | (Left ? 2 : 0)));
            if (IsSecond)
            {
                stream.Int(Number);
            }
        }
    }
}
