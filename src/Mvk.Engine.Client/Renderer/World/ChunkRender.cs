using Mvk.Rendering;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mvk.Engine.Renderer.Mesh;
using Mvk.Core.Util;
using Mvk.Engine.Util;
using Mvk.Engine.World;
using Mvk.Engine.World.Block;
using Mvk.Engine.World.Chunk;
using Mvk.Core.Maths;

namespace Mvk.Engine.Renderer.World
{
    /// <summary>
    /// Объект рендера чанка, он же клиентский чанк
    /// </summary>
    public class ChunkRender : ChunkClient
    {
        /// <summary>
        /// Массив буфера блоков альфы
        /// </summary>
        private static readonly ListFast<BlockBufferDistance> _listAlphaBuffer = new ListFast<BlockBufferDistance>();


        /// <summary>
        /// Сетка чанка сплошных блоков
        /// </summary>
        private MeshBlocks _meshDense;
        /// <summary>
        /// Сетка чанка альфа блоков
        /// </summary>
        private MeshBlocks _meshAlpha;
        /// <summary>
        /// Список всех видимых альфа блоков
        /// Координаты в битах 0000 yyyy zzzz xxxx
        /// </summary>
        private readonly List<ushort>[] _listAlphaBlock;
        /// <summary>
        /// Количество альфа блоков в чанке
        /// </summary>
        private int _countAlpha;
        /// <summary>
        /// Секции сколько в какой альфа блоков для сортировки
        /// </summary>
        private readonly ushort[] _sectionsCountAlphaSort;


        /// <summary>
        /// Соседние чанки, заполняются перед рендером
        /// </summary>
        private readonly ChunkRender[] _chunks = new ChunkRender[8];

        /// <summary>
        /// Буфер сетки секций чанков сплошных блоков
        /// </summary>
        private readonly ChunkSectionBuffer[] _sectionsBuffer;
        /// <summary>
        /// Массив какие секции сплошных чанков надо рендерить, для потока где идёт рендер
        /// Двойной флаг нужен, для того, что рендер идёт в другом потоке! (первый флаг в _sectionsBuffer.IsModifiedRender)
        /// </summary>
        private readonly bool[] _isRenderingSection;

        /// <summary>
        /// Рендер мира: устройство для сеток и построитель сеток
        /// </summary>
        private readonly WorldRenderer _render;
        /// <summary>
        /// Построитель сеток (буферы вершин и рендеры блоков), в оригинале статические поля Gi
        /// </summary>
        private readonly ChunkMesher _mesher;

        public ChunkRender(WorldClient worldClient, WorldRenderer render, int chunkPosX, int chunkPosY) 
            : base(worldClient, chunkPosX, chunkPosY)
        {
            _render = render;
            _mesher = render.Mesher;
            // Тут создавать сетки нельзя из-за VSync, он может создать паузу в момент создания объекта

            _listAlphaBlock = new List<ushort>[NumberSections];
            _sectionsBuffer = new ChunkSectionBuffer[NumberSections];
            _isRenderingSection = new bool[NumberSections];
            _sectionsCountAlphaSort = new ushort[NumberSections];

            for (int index = 0; index < NumberSections; index++)
            {
                _sectionsBuffer[index] = new ChunkSectionBuffer();
                _listAlphaBlock[index] = new List<ushort>();
            }
        }

        /// <summary>
        /// Инициализация Сеток GL
        /// </summary>
        public void InitGL()
        {
            if (_meshDense == null)
            {
                _meshDense = new MeshBlocks(_render.Context)
                {
                    IsModifiedRender = true
                };
                _meshAlpha = new MeshBlocks(_render.Context);
            }
        }

        #region Modified

        /// <summary>
        /// Проверка, нужен ли рендер этого псевдо чанка
        /// </summary>
        public bool IsModifiedRender => _meshDense != null && _meshDense.IsModifiedRender;

        /// <summary>
        /// Проверка, нужен ли рендер этого псевдо чанка  альфа блоков
        /// </summary>
        public bool IsModifiedRenderAlpha => _meshAlpha != null && _meshAlpha.IsModifiedRender;

        /// <summary>
        /// Пометить что надо перерендерить сетку чанка
        /// </summary>
        public override void ModifiedToRender(int y)
        {
            if (_meshDense != null) _meshDense.IsModifiedRender = true;
            if (y >= 0 && y < NumberSections)
            {
                _sectionsBuffer[y].IsModifiedRender = true;
            }
        }

        /// <summary>
        /// Пометить что надо перерендерить сетку чанка альфа блоков
        /// </summary>
        public override bool ModifiedToRenderAlpha(int y)
        {
            if (_meshAlpha != null && y >= 0 && y < NumberSections && _sectionsCountAlphaSort[y] > 0)
            {
                _meshAlpha.IsModifiedRender = true;
                return true;
            }
            return false;
        }

        #endregion

        #region Render

        /// <summary>
        /// Прорисовка сплошных блоков чанка
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawDense(CommandList commands) => _meshDense.Draw(commands);

        /// <summary>
        /// Прорисовка альфа блоков чанка
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawAlpha(CommandList commands) => _meshAlpha.Draw(commands);

        public override void DisposeMesh()
        {
            for (int y = 0; y < NumberSections; y++)
            {
                _isRenderingSection[y] = false;
            }
            if (_meshDense != null)
            {
                _meshDense.Dispose();
            }
            if (_meshAlpha != null)
            {
                _meshAlpha.Dispose();
            }
        }

        /// <summary>
        /// Рендер чанка, сплошных и альфа блоков
        /// </summary>
        public void Render()
        {
            long timeBegin = _worldClient.Game.ElapsedTicks();

            Vector3i posPlayer = _worldClient.Game.Player.PositionAlphaBlock;
            _mesher.VertexDense.Clear();

            ChunkStorage chunkStorage;
            //int cbX = CurrentChunkX << 4;
            //int cbZ = CurrentChunkY << 4;
            int cbY, realY, index, yb, x, z, indexY, indexYZ;
            int data, id;

            for (cbY = 0; cbY < NumberSections; cbY++)
            {
                if (_sectionsBuffer[cbY].BufferFloat == null)
                {
                    _sectionsBuffer[cbY].BufferFloat = new float[0];
                    _sectionsBuffer[cbY].BufferByte = new byte[0];
                }
            }

            _mesher.BlockRendFull.InitChunk(this);
            _mesher.BlockAlphaRendFull.InitChunk(this);
            _mesher.BlockLiquidAlphaRendFull.InitChunk(this);

            for (cbY = 0; cbY < NumberSections; cbY++)
            {
                if (_isRenderingSection[cbY])
                {
                    //long timeBeginSection = _worldClient.Game.ElapsedTicks();

                    _sectionsCountAlphaSort[cbY] = 0;
                    _listAlphaBlock[cbY].Clear();
                    chunkStorage = StorageArrays[cbY];
                    if (chunkStorage.Data != null && !chunkStorage.IsEmptyData())
                    {
                        _mesher.BlockRendFull.InitStorage(cbY);
                        _mesher.BlockAlphaRendFull.InitStorage(cbY);
                        _mesher.BlockLiquidAlphaRendFull.InitStorage(cbY);
                        // Имекется хоть один блок
                        for (yb = 0; yb < 16; yb++)
                        {
                            realY = cbY << 4 | yb;
                            indexY = yb << 8;
                            for (z = 0; z < 16; z++)
                            {
                                indexYZ = indexY | z << 4;
                                for (x = 0; x < 16; x++)
                                {
                                    index = indexYZ | x;
                                    data = chunkStorage.Data[index];
                                    // Если блок воздуха, то пропускаем рендер сразу
                                    if (data == 0) continue;
                                    // Определяем id блока
                                    id = (ushort)(data & 0xFFF);
                                    // Определяем объект блока
                                    Gi.Block = Ce.Blocks.BlockObjects[id];
                                    if (Gi.Block.IsAir) continue; // Надо для блоков которые отсутствуют

                                    _mesher.Of(Gi.Block).PosChunkX = x;
                                    _mesher.Of(Gi.Block).PosChunkY = realY;
                                    _mesher.Of(Gi.Block).PosChunkZ = z;
                                    // Определяем met блока
                                    _mesher.Of(Gi.Block).Met = data >> 12;
                                    
                                    if (_mesher.Of(Gi.Block).CheckSide())
                                    {
                                        if (Gi.Block.Alpha)
                                        {
                                            // Имеется видимый альфа блок, заносим в буфер для отельного рендера альфы
                                            _listAlphaBlock[cbY].Add((ushort)(yb << 8 | z << 4 | x));
                                            if (Gi.Block.AlphaSort)
                                            {
                                                // Помечаем только те альфа блоки которые сортируются, образно Лава не сортируется
                                                _sectionsCountAlphaSort[cbY]++;
                                            }
                                        }
                                        else
                                        {
                                            // Определяем met блока
                                            _mesher.Of(Gi.Block).Light = chunkStorage.Light[index];
                                            if (chunkStorage.Destroy.ContainsKey(index))
                                            {
                                                _mesher.Of(Gi.Block).Destroy = chunkStorage.Destroy[index];
                                            }
                                            else if (_mesher.Of(Gi.Block).Destroy != 0)
                                            {
                                                _mesher.Of(Gi.Block).Destroy = 0;
                                            }
                                            _mesher.Of(Gi.Block).RenderSide();

                                            // Тут можно Gi.Block залить в метод чек _mesher.Of(Gi.Block).Met для понимания имеется ли жидкость
                                            if (Gi.Block.IsAddLiquid(data >> 12))
                                            {
                                                _listAlphaBlock[cbY].Add((ushort)(yb << 8 | z << 4 | x));
                                                if (Gi.Block.AlphaSort)
                                                {
                                                    // Помечаем только те альфа блоки которые сортируются, образно Лава не сортируется
                                                    _sectionsCountAlphaSort[cbY]++;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        _sectionsBuffer[cbY].BufferFloat = _mesher.VertexDense.BufferFloat.ToArray();
                        _sectionsBuffer[cbY].BufferByte = _mesher.VertexDense.BufferByte.ToArray();
                        _mesher.VertexDense.Clear();
                    }
                    else
                    {
                        // Нет блоков, все блоки воздуха очищаем буфера
                        if (_sectionsBuffer[cbY].BufferFloat.Length != 0)
                        {
                            _sectionsBuffer[cbY].BufferFloat = new float[0];
                            _sectionsBuffer[cbY].BufferByte = new byte[0];
                        }
                    }
                    _isRenderingSection[cbY] = false;

                    //float timeSection = (_worldClient.Game.ElapsedTicks() - timeBeginSection) / (float)Ticker.TimerFrequency;
                    //Console.WriteLine("Render: " + timeSection);
                }
            }
            // Пересчитываем количество альфа блоков
            _countAlpha = 0;
            // Клеим секции
            for (cbY = 0; cbY < NumberSections; cbY++)
            {
                _countAlpha += _listAlphaBlock[cbY].Count;
                if (_sectionsBuffer[cbY].BufferFloat.Length != 0)
                {
                    _mesher.VertexDense.BufferFloat.AddRange(_sectionsBuffer[cbY].BufferFloat);
                    _mesher.VertexDense.BufferByte.AddRange(_sectionsBuffer[cbY].BufferByte);
                }
            }
            _meshDense.SetBuffer(_mesher.VertexDense);

            // Для отладочной статистики
            float time = (_worldClient.Game.ElapsedTicks() - timeBegin) / (float)Ticker.TimerFrequency;
            Debug.RenderChunckTime8 = time;// (Debug.RenderChunckTime8 * 100f + time) / 101f;
            Debug.RenderChunckTime[(++Debug.dct) % 32] = time;

            // Если был рендер сплошных блоков, обязательно надо сделать рендер альфа блоков
            if (_countAlpha == 0)
            {
                // Надо принудительно перезаписать, так-как в прошлый раз могли быть альфа блоки
                _meshAlpha.SetBufferClear();
            }
            else
            {
                RenderAlpha();
            }
        }

        /// <summary>
        /// Рендер видимых альфа блоков
        /// </summary>
        public void RenderAlpha()
        {
            if (_countAlpha > 0)
            {
                long timeBegin = _worldClient.Game.ElapsedTicks();

                int cbX = CurrentChunkX << 4;
                int cbZ = CurrentChunkY << 4;

                int cbY, i, x, y, z, realY, index, count;
                int data, id;
                ChunkStorage chunkStorage;
                Vector3i posPlayer = _worldClient.Game.Player.PositionAlphaBlock;
                _mesher.VertexAlpha.Clear();
                _listAlphaBuffer.Clear();
                _mesher.BlockAlphaRendFull.InitChunk(this);
                _mesher.BlockLiquidAlphaRendFull.InitChunk(this);

                for (cbY = 0; cbY < NumberSections; cbY++)
                {
                    count = _listAlphaBlock[cbY].Count;
                    if (count > 0)
                    {
                        chunkStorage = StorageArrays[cbY];

                        if (chunkStorage.Data != null)
                        {
                            _mesher.BlockAlphaRendFull.InitStorage(cbY);
                            _mesher.BlockLiquidAlphaRendFull.InitStorage(cbY);
                            for (i = 0; i < count; i++)
                            {
                                index = _listAlphaBlock[cbY][i];
                                x = index & 0xF;
                                z = (index >> 4) & 0xF;
                                y = index >> 8;
                                realY = cbY << 4 | y;

                                index = index & 0xFFF;
                                data = chunkStorage.Data[index];
                                if (data != 0)
                                {
                                    id = (ushort)(data & 0xFFF);
                                    Gi.Block = Ce.Blocks.BlockObjects[id];
                                    if (!Gi.Block.IsAir && Gi.Block.Alpha)
                                    {
                                        _mesher.Of(Gi.Block).PosChunkX = x;
                                        _mesher.Of(Gi.Block).PosChunkY = realY;
                                        _mesher.Of(Gi.Block).PosChunkZ = z;
                                        // Определяем met блока
                                        _mesher.Of(Gi.Block).Met = data >> 12;

                                        if (_mesher.Of(Gi.Block).CheckSide())
                                        {
                                            _mesher.Of(Gi.Block).Light = chunkStorage.Light[index];
                                            if (chunkStorage.Destroy.ContainsKey(index))
                                            {
                                                _mesher.Of(Gi.Block).Destroy = chunkStorage.Destroy[index];
                                            }
                                            else if (_mesher.Of(Gi.Block).Destroy != 0)
                                            {
                                                _mesher.Of(Gi.Block).Destroy = 0;
                                            }
                                            _mesher.Of(Gi.Block).RenderSide();
                                            _listAlphaBuffer.Add(new BlockBufferDistance()
                                            {
                                                BufferFloat = _mesher.VertexAlpha.BufferFloat.ToArray(),
                                                BufferByte = _mesher.VertexAlpha.BufferByte.ToArray(),
                                                Distance = Glm.Distance(posPlayer,
                                                    new Vector3i(cbX | x, realY, cbZ | z))
                                            });
                                            _mesher.VertexAlpha.Clear();
                                        }
                                    }
                                    // Проверяем дополнительную жидкость
                                    if (Gi.Block.IsAddLiquid(data >> 12))
                                    {
                                        Gi.Block = Ce.Blocks.GetAddLiquid(data >> 12);
                                        if (!Gi.Block.IsAir && Gi.Block.Alpha)
                                        {
                                            _mesher.Of(Gi.Block).PosChunkX = x;
                                            _mesher.Of(Gi.Block).PosChunkY = realY;
                                            _mesher.Of(Gi.Block).PosChunkZ = z;
                                            // Определяем met блока
                                            _mesher.Of(Gi.Block).Met = data >> 12;

                                            if (_mesher.Of(Gi.Block).CheckSide())
                                            {
                                                _mesher.Of(Gi.Block).Light = chunkStorage.Light[index];
                                                _mesher.Of(Gi.Block).RenderSide();
                                                _listAlphaBuffer.Add(new BlockBufferDistance()
                                                {
                                                    BufferFloat = _mesher.VertexAlpha.BufferFloat.ToArray(),
                                                    BufferByte = _mesher.VertexAlpha.BufferByte.ToArray(),
                                                    Distance = Glm.Distance(posPlayer,
                                                        new Vector3i(cbX | x, realY, cbZ | z))
                                                });
                                                _mesher.VertexAlpha.Clear();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                _AlphaBlocksSort();

                float time = (_worldClient.Game.ElapsedTicks() - timeBegin) / (float)Ticker.TimerFrequency;
                Debug.RenderChunckTimeAlpha8 = (Debug.RenderChunckTimeAlpha8 * 7f + time) / 8f;
            }
            else
            {
                // Надо принудительно перезаписать, так-как в прошлый раз могли быть альфа блоки
                _meshAlpha.SetBufferClear();
            }
        }

        /// <summary>
        /// Отсортировать альфа блоки и подготовить буфер к бинду
        /// </summary>
        private void _AlphaBlocksSort()
        {
            int count = _listAlphaBuffer.Count;
            if (count > 0)
            {
                _listAlphaBuffer.Sort();
                for (int i = count - 1; i >= 0; i--)
                {
                    _mesher.VertexAlpha.AddBlockBufferDistance(_listAlphaBuffer[i]);
                }
            }
            _meshAlpha.SetBuffer(_mesher.VertexAlpha);
        }

        /// <summary>
        /// Старт рендеринга
        /// </summary>
        public void StartRendering()
        {
            for (int y = 0; y < NumberSections; y++)
            {
                _isRenderingSection[y] = _sectionsBuffer[y].IsModifiedRender;
                _sectionsBuffer[y].IsModifiedRender = false;
            }
            _meshDense.StatusRendering();
        }

        /// <summary>
        /// Старт рендеринга только альфа
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartRenderingAlpha() => _meshAlpha.StatusRendering();

        /// <summary>
        /// Занести буфер сплошных блоков чанка если это требуется
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindBufferDense() => _meshDense.BindBuffer();

        /// <summary>
        /// Занести буфер альфа блоков чанка если это требуется
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindBufferAlpha() => _meshAlpha.BindBuffer();

        #endregion

        #region BufferChunks

        /// <summary>
        /// Заполнить буфе боковых чанков
        /// </summary>
        public void UpBufferChunks()
        {
            for (int i = 0; i < 8; i++)
            {
                _chunks[i] = (ChunkRender)_worldClient.ChunkPrClient.GetChunkClient(
                    CurrentChunkX + Ce.AreaOne8X[i], CurrentChunkY + Ce.AreaOne8Y[i]
                );
            }
        }

        /// <summary>
        /// Очистить буфер соседних чанков
        /// </summary>
        public void ClearBufferChunks()
        {
            for (int i = 0; i < 8; i++) _chunks[i] = null;
        }

        /// <summary>
        /// Получить соседний чанк, где x и y -1..1
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ChunkRender Chunk(int x, int y) => _chunks[Ce.GetAreaOne8(x, y)];

        #endregion

        #region Status

        /// <summary>
        /// Статсус возможности для рендера сплошных блоков
        /// </summary>
        public bool IsMeshDenseWait => _meshDense.Status == MeshBlocks.StatusMesh.Wait 
            || _meshDense.Status == MeshBlocks.StatusMesh.Null;
        /// <summary>
        /// Статсус возможности для рендера альфа блоков
        /// </summary>
        public bool IsMeshAlphaWait => _meshAlpha.Status == MeshBlocks.StatusMesh.Wait 
            || _meshAlpha.Status == MeshBlocks.StatusMesh.Null;
        /// <summary>
        /// Статсус связывания сетки с OpenGL для рендера сплошных блоков
        /// </summary>
        public bool IsMeshDenseBinding => _meshDense.Status == MeshBlocks.StatusMesh.Binding;
        /// <summary>
        /// Статсус связывания сетки с OpenGL для рендера альфа блоков
        /// </summary>
        public bool IsMeshAlphaBinding => _meshAlpha.Status == MeshBlocks.StatusMesh.Binding;
        /// <summary>
        /// Статсус не пустой для рендера сплошных или уникальных блоков
        /// </summary>
        public bool NotNullMeshDense => _meshDense.Status != MeshBlocks.StatusMesh.Null;
        /// <summary>
        /// Статсус не пустой для рендера альфа блоков
        /// </summary>
        
        public bool NotNullMeshAlpha => _meshAlpha.Status != MeshBlocks.StatusMesh.Null;
        /// <summary>
        /// Изменить статус на отмена рендеринга альфа блоков
        /// </summary>
        public void NotRenderingAlpha() => _meshAlpha.IsModifiedRender = false;

        #endregion

        /// <summary>
        /// Получить цвет стороны от по координатам локальным блока 0..15
        /// </summary>
        /// <param name="typeColor">Цвет биома, 1 - трава, 2 - листа, 3 - вода</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetColorSideFromBiom(byte typeColor, int bx, int bz)
        {
            // Трава
            if (typeColor == 1) return _worldClient.Game.ModClient.Colors.Grass(Biome[bz << 4 | bx]);
            // Вода
            if (typeColor == 3) return _worldClient.Game.ModClient.Colors.Water(Biome[bz << 4 | bx]);

            return BlockRenderFull.ColorWhite;
        }

        public override string ToString()
            => CurrentChunkX + " : " + CurrentChunkY + " MD:" + _meshDense?.Status + " MA:" + _meshAlpha?.Status;
    }
}
