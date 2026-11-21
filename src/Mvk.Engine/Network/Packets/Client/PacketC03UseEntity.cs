#nullable enable
namespace Mvk.Engine.Network.Packets.Client
{
    /// <summary>
    /// Отправляем на сервер взаимодействие с сущностью
    /// </summary>
    public struct PacketC03UseEntity : IPacket
    {
        public byte Id => 0x03;

        /// <summary>
        /// Id сущности с которой взаимодействуем
        /// </summary>
        public int Index { get; private set; }
        public EnumAction Action { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }
        /// <summary>
        /// Высота удара, при атаке
        /// </summary>
        public float HitY { get; private set; }

        /// <summary>
        /// Действие левой рукой (выживание, две руки)
        /// </summary>
        public bool Left { get; private set; }

        /// <summary>
        /// Задать руку действия
        /// </summary>
        public PacketC03UseEntity Hand(bool left)
        {
            Left = left;
            return this;
        }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Колющий выпад (урон от скорости игрока)
        /// </summary>
        public bool Thrust { get; private set; }

        /// <summary>
        /// Пометить удар как колющий выпад
        /// </summary>
        public PacketC03UseEntity AsThrust()
        {
            Thrust = true;
            return this;
        }

        /// <summary>
        /// Взаимодействие сущности, Атаки!
        /// </summary>
        /// <param name="hitY">Высота удара, 0 до высоты size.Y</param>
        /// <param name="x">Импульс</param>
        /// <param name="y">Импульс</param>
        /// <param name="z">Импульс</param>
        public PacketC03UseEntity(int id, float hitY, float x, float y, float z)
        {
            Index = id;
            Action = EnumAction.Attack;
            HitY = hitY;
            X = x;
            Y = y;
            Z = z;
        }

        //public PacketC03UseEntity(int id, EnumAction action, float x, float y, float z)
        //{
        //    Index = id;
        //    Action = action;
        //    HitY = 0;
        //    X = x;
        //    Y = y;
        //    Z = z;
        //}

        /// <summary>
        /// Взаимодействие сущности, Импульс
        /// </summary>
        /// <param name="x">Импульс</param>
        /// <param name="y">Импульс</param>
        /// <param name="z">Импульс</param>
        public PacketC03UseEntity(int id, float x, float y, float z)
        {
            Index = id;
            Action = EnumAction.Impulse;
            HitY = 0;
            X = x;
            Y = y;
            Z = z;
        }
        public PacketC03UseEntity(int id, EnumAction action)
        {
            Index = id;
            Action = action;
            HitY = X = Y = Z = 0;
        }

        public void ReadPacket(ReadPacket stream)
        {
            Index = stream.Int();
            byte action = stream.Byte();
            Left = (action & 0x80) != 0;
            Thrust = (action & 0x40) != 0;
            Action = (EnumAction)(action & 0x3F);
            X = stream.Float();
            Y = stream.Float();
            Z = stream.Float();
            if (Action == EnumAction.Attack)
            {
                HitY = stream.Float();
            }
        }

        public void WritePacket(WritePacket stream)
        {
            stream.Int(Index);
            stream.Byte((byte)((byte)Action | (Left ? 0x80 : 0) | (Thrust ? 0x40 : 0)));
            stream.Float(X);
            stream.Float(Y);
            stream.Float(Z);
            if (Action == EnumAction.Attack)
            {
                stream.Float(HitY);
            }
        }

        /// <summary>
        /// Варианты действия
        /// </summary>
        public enum EnumAction
        {
            /// <summary>
            /// Импульс
            /// </summary>
            Impulse = 1,
            /// <summary>
            /// Пробудить
            /// </summary>
            Awaken = 2,
            /// <summary>
            /// Взаимодействие
            /// </summary>
            Interact = 3,
            /// <summary>
            /// Атака
            /// </summary>
            Attack = 4
        }
    }
}

