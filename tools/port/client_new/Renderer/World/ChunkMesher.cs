using System;
using Mvk.Core.Util;
using Mvk.Engine.World.Block;

namespace Mvk.Engine.Renderer.World
{
    /// <summary>
    /// Построитель сеток чанков: буферы вершин и рендеры блоков.
    /// В оригинале это статические поля Gi (VertexDense, BlockRendFull и другие);
    /// здесь это объект: у каждого потока построения сеток свой (WorldRenderer, этап 7).
    /// </summary>
    public sealed class ChunkMesher : IDisposable
    {
        /// <summary>Буфер вершин сплошных блоков</summary>
        public readonly VertexBlockBuffer VertexDense = new VertexBlockBuffer(512000);
        /// <summary>Буфер вершин альфа блоков</summary>
        public readonly VertexBlockBuffer VertexAlpha = new VertexBlockBuffer(128000);
        /// <summary>Рендер сплошных блоков</summary>
        public readonly BlockRenderFull BlockRendFull;
        /// <summary>Рендер альфа блоков</summary>
        public readonly BlockRenderFull BlockAlphaRendFull;
        /// <summary>Рендер жидкостей</summary>
        public readonly BlockRenderLiquid BlockLiquidAlphaRendFull;
        /// <summary>Буфер альфа блоков чанка для сортировки по расстоянию (в оригинале статический в ChunkRender)</summary>
        public readonly ListFast<BlockBufferDistance> AlphaBuffer = new ListFast<BlockBufferDistance>();
        /// <summary>Вершины альфа блоков чанка подряд, AlphaBuffer хранит смещения в нём (этап 7: без ToArray на блок)</summary>
        public readonly VertexBlockBuffer AlphaScratch = new VertexBlockBuffer(128000);

        /// <summary>
        /// Альфа блок построен в VertexAlpha: перенести его вершины в общий буфер и запомнить для сортировки
        /// </summary>
        public void AddAlphaBlock(float distance)
        {
            AlphaBuffer.Add(new BlockBufferDistance()
            {
                StartFloat = AlphaScratch.BufferFloat.Count,
                CountFloat = VertexAlpha.BufferFloat.Count,
                StartByte = AlphaScratch.BufferByte.Count,
                CountByte = VertexAlpha.BufferByte.Count,
                Distance = distance
            });
            AlphaScratch.Append(VertexAlpha);
        }

        /// <summary>
        /// Начать альфа блоки чанка
        /// </summary>
        public void ClearAlpha()
        {
            AlphaBuffer.Clear();
            AlphaScratch.Clear();
        }

        public ChunkMesher()
        {
            BlockRendFull = new BlockRenderFull(VertexDense);
            BlockAlphaRendFull = new BlockRenderFull(VertexAlpha);
            BlockLiquidAlphaRendFull = new BlockRenderLiquid(VertexAlpha);
        }

        /// <summary>
        /// Рендер для блока (в оригинале поле BlockBase.BlockRender)
        /// </summary>
        public BlockRenderFull Of(BlockBase block)
        {
            switch (block.RenderType)
            {
                case EnumBlockRender.AlphaFull: return BlockAlphaRendFull;
                case EnumBlockRender.LiquidAlpha: return BlockLiquidAlphaRendFull;
                default: return BlockRendFull;
            }
        }

        public void Dispose()
        {
            VertexDense.Dispose();
            VertexAlpha.Dispose();
            AlphaScratch.Dispose();
        }
    }
}
