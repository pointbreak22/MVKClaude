using Mvk.Engine.World;
using Mvk.Engine.World.Block;
using Mvk.Engine.World.Chunk;

namespace Mvk.Engine.Network.Packets.Server
{
    /// <summary>
    /// Отправляем клиенту один изменённый блок
    /// </summary>
    public struct PacketS23BlockChange : IPacket
    {
        public byte Id => 0x23;

        /// <summary>
        /// Процесс разрушения
        /// </summary>
        public byte Progress { get; private set; }
        private BlockPos _blockPos;
        private BlockState _blockState;

        public BlockPos GetBlockPos() => _blockPos;
        public BlockState GetBlockState() => _blockState;

        public PacketS23BlockChange(ChunkBase chunk, BlockPos blockPos)
        {
            _blockPos = blockPos;
            _blockState = chunk.GetBlockState(blockPos);
            Progress = chunk.GetBlockDestroy(blockPos);
            if ((Progress & 1 << 7) != 0)
            {
                // Был флаг удаления, чтоб клиентам отправить
                chunk.SetBlockDestroy(blockPos, 0);
            }
            else
            {
                Progress = (byte)(Progress & 15);
            }
        }

        public void ReadPacket(ReadPacket stream)
        {
            _blockPos.ReadStream(stream);
            _blockState.ReadStream(stream);
            Progress = stream.Byte();
        }

        public void WritePacket(WritePacket stream)
        {
            _blockPos.WriteStream(stream);
            _blockState.WriteStream(stream);
            stream.Byte(Progress);
        }
    }
}
