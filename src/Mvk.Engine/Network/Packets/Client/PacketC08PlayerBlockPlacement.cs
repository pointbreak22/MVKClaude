#nullable enable
using Mvk.Core.Util;
using Mvk.Engine.Util;
using Mvk.Engine.World.Block;
using Mvk.Core.Maths;

namespace Mvk.Engine.Network.Packets.Client
{
    /// <summary>
    /// Отправляем на сервер установку или взаимодействия с блоком
    /// </summary>
    public struct PacketC08PlayerBlockPlacement : IPacket
    {
        public byte Id => 0x08;

        /// <summary>
        /// С какой стороны устанавливаем блок
        /// </summary>
        public Pole Side { get; private set; }
        /// <summary>
        /// Точка куда устанавливаем блок (параметр с RayCast)
        /// значение в пределах 0..1, образно фиксируем пиксел клика на стороне
        /// </summary>
        public Vector3 Facing { get; private set; }
        /// <summary>
        /// Может на этот блок поставить другой, к примеру трава
        /// </summary>
        public bool Replaceable { get; private set; }
        /// <summary>
        /// Поставить блок
        /// </summary>
        public bool Put { get; private set; }

        /// <summary>
        /// Действие левой рукой (выживание, две руки)
        /// </summary>
        public bool Left { get; private set; }

        /// <summary>
        /// Задать руку действия
        /// </summary>
        public PacketC08PlayerBlockPlacement Hand(bool left)
        {
            Left = left;
            return this;
        }

        private BlockPos _blockPos;
        /// <summary>
        /// Позиция блока где устанавливаем блок
        /// </summary>
        public BlockPos GetBlockPos() => _blockPos;

        /// <summary>
        /// Установить блок
        /// </summary>
        public PacketC08PlayerBlockPlacement(BlockPos blockPos, Pole side, Vector3 facing,
            bool replaceable, bool put)
        {
            _blockPos = blockPos;
            Side = side;
            Facing = facing;
            Replaceable = replaceable;
            Put = put;
        }

        public void ReadPacket(ReadPacket stream)
        {
            byte flags = stream.Byte();
            Put = (flags & 1) != 0;
            Left = (flags & 2) != 0;
            Replaceable = stream.Bool();
            _blockPos.ReadStream(stream);
            Side = (Pole)stream.Byte();
            Facing = new Vector3(stream.Byte() / 16f, stream.Byte() / 16f, stream.Byte() / 16f);
        }

        public void WritePacket(WritePacket stream)
        {
            stream.Byte((byte)((Put ? 1 : 0) | (Left ? 2 : 0)));
            stream.Bool(Replaceable);
            _blockPos.WriteStream(stream);
            stream.Byte((byte)Side);
            stream.Byte((byte)(Facing.X * 16f));
            stream.Byte((byte)(Facing.Y * 16f));
            stream.Byte((byte)(Facing.Z * 16f));
        }
    }
}
