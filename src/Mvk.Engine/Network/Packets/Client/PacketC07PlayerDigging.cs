#nullable enable
using Mvk.Engine.World.Block;

namespace Mvk.Engine.Network.Packets.Client
{
    /// <summary>
    /// Пакет Игрок копает / ломает
    /// Swing - взмах
    /// </summary>
    public struct PacketC07PlayerDigging : IPacket
    {
        public byte Id => 0x07;

        private BlockPos _blockPos;

        public EnumDigging Digging { get; private set; }
        /// <summary>
        /// Процесс разрушения блока
        /// </summary>
        public byte Process { get; private set; }

        /// <summary>
        /// Действие левой рукой (выживание, две руки)
        /// </summary>
        public bool Left { get; private set; }

        /// <summary>
        /// Задать руку действия
        /// </summary>
        public PacketC07PlayerDigging Hand(bool left)
        {
            Left = left;
            return this;
        }

        public BlockPos GetBlockPos() => _blockPos;

        public PacketC07PlayerDigging(BlockPos blockPos, byte process)
        {
            _blockPos = blockPos;
            Digging =  EnumDigging.ProcessDestroy;
            Process = process;
        }

        /// <summary>
        /// Удар по блоку (две руки, добычу считает сервер)
        /// </summary>
        public static PacketC07PlayerDigging Hit(BlockPos blockPos)
        {
            PacketC07PlayerDigging packet = new PacketC07PlayerDigging(blockPos);
            packet.Digging = EnumDigging.Hit;
            return packet;
        }

        public PacketC07PlayerDigging(BlockPos blockPos)
        {
            _blockPos = blockPos;
            Digging = EnumDigging.Destroy;
            Process = 0;
        }

        public void ReadPacket(ReadPacket stream)
        {
            _blockPos.ReadStream(stream);
            byte digging = stream.Byte();
            Left = (digging & 0x80) != 0;
            Digging = (EnumDigging)(digging & 0x7F);
            if (Digging == EnumDigging.ProcessDestroy)
            {
                Process = stream.Byte();
            }
        }

        public void WritePacket(WritePacket stream)
        {
            _blockPos.WriteStream(stream);
            stream.Byte((byte)((byte)Digging | (Left ? 0x80 : 0)));
            if (Digging == EnumDigging.ProcessDestroy)
            {
                stream.Byte(Process);
            }
        }

        /// <summary>
        /// Варианты действия разрушения
        /// </summary>
        public enum EnumDigging
        {
            /// <summary>
            /// Просто удар, никуда не попадая, взмах
            /// </summary>
            None = 0,
            /// <summary>
            /// Разрушен блок
            /// </summary>
            Destroy = 1,
            /// <summary>
            /// Процесс разрушения блока
            /// </summary>
            ProcessDestroy = 2,
            /// <summary>
            /// Удар по блоку: прогресс, разрушение и износ считает сервер
            /// </summary>
            Hit = 3
        }
    }
}
