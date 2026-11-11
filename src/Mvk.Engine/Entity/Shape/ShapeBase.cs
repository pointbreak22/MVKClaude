using System.Runtime.CompilerServices;
using Mvk.Engine.Entity.Model;
using Mvk.Core.Util;
using Mvk.Engine.Util;

namespace Mvk.Engine.Entity.Shape
{
    public abstract class ShapeBase
    {
        /// <summary>
        /// Индекс в глобальном массиве
        /// </summary>
        public readonly ushort Index;
        /// <summary>
        /// Текстуры для моба
        /// </summary>
        public BufferedImage[] Textures { get; protected set; }
        /// <summary>
        /// Индекс глубины текстуры для моба
        /// </summary>
        public int[] DepthTextures { get; protected set; }
        /// <summary>
        /// Минимальная текстура
        /// </summary>
        public bool TextureSmall { get; protected set; } = true;

        /// <summary>
        /// Объект отвечает за определяение модели сущности
        /// </summary>
        protected ModelEntityDefinition _definition;

        public ShapeBase(ushort index) => Index = index;

        /// <summary>
        /// Очистить объект отвечает за определяение модели сущности
        /// </summary>
        public void ClearDefinition() => _definition = null;

        /// <summary>
        /// Пометить модель в максимальную группу текстур
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void TextureGroupBig() => TextureSmall = false;

        /// <summary>
        /// Задать глубину в текстуру
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SetDepthTextures(int index, int depth) 
            => DepthTextures[index] = depth;

        /// <summary>
        /// Корректировка размера ширины текстуры, в буффере UV
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SizeAdjustmentTextureWidth(float coef) { }

        /// <summary>
        /// Корректировка размера высоты текстуры, в буффере UV
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SizeAdjustmentTextureHeight(float coef) { }

        /// <summary>
        /// Найти индекс текстуры по названию
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual int FindIndexTexture(string name) => -1;

        public override string ToString() => Index + " " + (TextureSmall ? "Small" : "Big");
    }
}
