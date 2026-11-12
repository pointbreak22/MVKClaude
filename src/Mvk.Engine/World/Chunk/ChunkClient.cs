using System.Runtime.CompilerServices;
using Mvk.Core.Util;
using Mvk.Engine.World.Block;

namespace Mvk.Engine.World.Chunk
{
    /// <summary>
    /// Клиентский чанк без сеток: логика, общая для любого отображения.
    /// В оригинале это была часть ChunkRender; сетки и рендер остались в ChunkRender (Engine.Client),
    /// который наследует этот класс.
    /// </summary>
    public class ChunkClient : ChunkBase
    {
        /// <summary>
        /// Дистанция до камеры, для рендера, пополняется в момент FrustumCulling
        /// </summary>
        public int Distance;

        /// <summary>
        /// Клиентский мир
        /// </summary>
        protected readonly WorldClient _worldClient;
        /// <summary>
        /// Список всех блоков которым нужен принудительные частички в любых местах
        /// Координаты в битах 0000 yyyy zzzz xxxx
        /// </summary>
        private readonly ListMessy<ushort>[] _listRequiredParticles;
        /// <summary>
        /// Количество блоков для принудительных частичек, во всём чанке
        /// </summary>
        private int _countRequiredParticles;

        public ChunkClient(WorldClient worldClient, int chunkPosX, int chunkPosY)
            : base(worldClient, worldClient.ChunkPr.Settings, chunkPosX, chunkPosY)
        {
            _worldClient = worldClient;
            _listRequiredParticles = new ListMessy<ushort>[NumberSections];
            for (int index = 0; index < NumberSections; index++)
            {
                _listRequiredParticles[index] = new ListMessy<ushort>();
            }
        }

        /// <summary>
        /// Пометить что надо перерендерить сетку секции y. Без отображения ничего не делает
        /// </summary>
        public virtual void ModifiedToRender(int y) { }

        /// <summary>
        /// Пометить что надо перерендерить сетку чанка альфа блоков.
        /// Без отображения сеток нет, вернёт false
        /// </summary>
        public virtual bool ModifiedToRenderAlpha(int y) => false;

        /// <summary>
        /// Освободить сетки чанка (при выгрузке). Без отображения ничего не делает
        /// </summary>
        public virtual void DisposeMesh() { }

        /// <summary>
        /// Обязательные блоки случайных частичек в тиках
        /// </summary>
        public void RequiredRandomDisplayTick()
        {
            if (_countRequiredParticles > 0)
            {
                int cbY, i, x, y, z, key, count;
                BlockPos blockPos = new BlockPos();
                BlockState blockState;

                for (cbY = 0; cbY < NumberSections; cbY++)
                {
                    count = _listRequiredParticles[cbY].Count;
                    for (i = 0; i < count; i++)
                    {
                        if (_worldClient.Rnd.Next(10) == 0)
                        {
                            key = _listRequiredParticles[cbY][i];
                            x = key & 15;
                            z = (key >> 4) & 15;
                            y = key >> 8;

                            blockPos.X = BlockX + x;
                            blockPos.Y = cbY << 4 | y;
                            blockPos.Z = BlockZ + z;
                            blockState = GetBlockState(x, blockPos.Y, z);
                            blockState.GetBlock().RequiredRandomDisplayTick(_worldClient, blockPos, blockState);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Изменить состояние обязательного случайного тика
        /// </summary>
        protected override void _SetRequiredRandomTick(int x, int y, int z, BlockBase blockOld, BlockBase blockNew)
        {
            if (blockOld.RequiredRandomTick != blockNew.RequiredRandomTick)
            {
                int sy = y >> 4;
                ushort key = (ushort)((y & 15) << 8 | z << 4 | x);
                if (blockOld.RequiredRandomTick)
                {
                    // Удалить
                    _listRequiredParticles[sy].Remove(key);
                    _countRequiredParticles--;
                }
                else
                {
                    // Добавить
                    _listRequiredParticles[sy].Add(key);
                    _countRequiredParticles++;
                }
            }
        }

        /// <summary>
        /// Обновить в секции блоки состояния обязательного случайного тика
        /// </summary>
        protected override void _UpSectionRequiredRandomTick(int sy)
        {
            _listRequiredParticles[sy].Clear();
            // Боль, надо пробежаться по всем 4096 блокам и внести в список
            ChunkStorage storage = StorageArrays[sy];
            if (storage.CountBlock > 0)
            {
                for (ushort i = 0; i < 4096; i++)
                {
                    if (Ce.Blocks.BlocksRequiredRandomTick[storage.Data[i] & 0xFFF])
                    {
                        _listRequiredParticles[sy].Add(i);
                    }
                }
            }
            _countRequiredParticles = 0;
            for (sy = 0; sy < NumberSections; sy++)
            {
                _countRequiredParticles += _listRequiredParticles[sy].Count;
            }
        }

        public override string ToString() => CurrentChunkX + " : " + CurrentChunkY;
    }
}
