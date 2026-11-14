using System;
using System.IO;
using Mvk.Core.Util;
using Mvk.Engine.Util;
using Mvk.Core.Maths;

namespace Mvk.Engine.World.Block
{
    /// <summary>
    /// Процесс разрушения блока
    /// </summary>
    public class DestroyBlockProgress
    {
        /// <summary>
        /// Вершины всех шагов разрушения
        /// </summary>
        private Vertex3d[][] _vertices;

        /// <summary>
        /// Регистрация вставляется в разделе регистрации блоков, для вставки в тикстурный атлас
        /// </summary>
        public void Registration(int count, string pathName)
        {
            _vertices = new Vertex3d[count][];
            QuadSide quads;
            for (int i = 0; i < count; i++)
            {
                quads = new QuadSide();
                quads.SetTexture(BlocksReg.BlockItemAtlas.AddSprite(pathName + i).Index, 
                    0, 0, 32, 32);
                _vertices[i] = quads.Vertex;
            }
        }

        /// <summary>
        /// Смена позиций всего квада, для разрушенния нужен
        /// </summary>
        public Vertex3d[] GetVertices(int progress, Vertex3d[] vertices)
        {
            Vertex3d[] step = _vertices[progress - 1];
            // Свой массив у каждого потока построения сеток: вершины шага (текстура) с позициями грани
            Vertex3d[] ds = _threadVertices ??= new Vertex3d[4];
            for (int i = 0; i < 4; i++)
            {
                ds[i] = step[i];
                ds[i].SetPosition(vertices[i]);
            }
            return ds;
        }

        /// <summary>
        /// Вершины разрушения потока построения сеток (результат GetVertices до следующего вызова в том же потоке)
        /// </summary>
        [ThreadStatic]
        private static Vertex3d[] _threadVertices;
    }
}
