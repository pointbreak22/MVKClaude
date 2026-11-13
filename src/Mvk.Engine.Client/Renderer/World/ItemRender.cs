using System;
using System.Runtime.CompilerServices;
using Mvk.Engine.Entity.Render;
using Mvk.Engine.Item;
using Mvk.Engine.Renderer.Mesh;
using Mvk.Engine.Renderer.Shaders;

namespace Mvk.Engine.Renderer.World
{
    /// <summary>
    /// Объект рисует предмет в Gui
    /// </summary>
    public class ItemRender : IDisposable
    {
        /// <summary>
        /// Шейдор
        /// </summary>
        private readonly ShadersEntity _shader;
        /// <summary>
        /// Сетка сущности
        /// </summary>
        private readonly MeshGuiEntity _mesh;
        /// <summary>
        /// Объём
        /// </summary>
        public readonly bool Volume;

        public ItemRender(int id, RenderMain gl, ShadersEntity shader, ItemRenderBuffer renderBuffer)
        {
            _shader = shader;
            VertexEntityBuffer buffer = renderBuffer.GetBufferGui();
            if (buffer == null)
            {
                throw new Exception(ClientStrings.GetString(ClientStrings.BufferItemIsMissing, Ce.Items.ItemAlias[id]));
            }
            Volume = renderBuffer.Valume;
            _mesh = new MeshGuiEntity(gl);
            _mesh.Reload(buffer);
        }

        /// <summary>
        /// Прорисовать сетку
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MeshDraw(float x, float y)
        {
            _shader.UniformPosGui(x, y);
            _shader.UniformVolumeGui(Volume);
            _mesh.Draw();
        }

        /// <summary>
        /// Выгрузить сетку с OpenGL
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _mesh.Dispose();
    }
}
