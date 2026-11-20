#nullable enable
using System;
using System.Runtime.CompilerServices;
using Mvk.Core.Util;
using Mvk.Engine.Util;
using Mvk.Engine.World.Block;
using Mvk.Core.Maths;

/// <summary>
/// GraphicalInterface
/// Константы и общие ресурсы GUI. Состояние вида (размер окна, Si, матрицы) в объекте Mvk.Engine.ViewState
/// </summary>
public sealed class Gi
{
    /// <summary>
    /// Цвет текста для чата
    /// </summary>
    public readonly static Vector3 ColorTextWhite = new Vector3(.9f);
    /// <summary>
    /// Цвет текста для GUI
    /// </summary>
    public readonly static Vector3 ColorTextBlack = new Vector3(0); //Vector3(.8f);
    /// <summary>
    /// Цвет текста для GUI где наведена мышка
    /// </summary>
    public readonly static Vector3 ColorTextEnter = new Vector3(0);//Vector3(.9f, .9f, .5f);
    /// <summary>
    /// Цвет неактивного текста для GUI
    /// </summary>
    public readonly static Vector3 ColorTextInactive = new Vector3(.588f, .51f, .459f);//Vector3(.5f);

    /// <summary>
    /// Перечень 16-ти цветов Красного как в майне
    /// </summary>
    public readonly static float[] ColorRed = new float[] { 0, 0, 0, 0, .67f, .67f, 1, .67f, .33f, .33f, .33f, .33f, 1, 1, 1, 1 };
    /// <summary>
    /// Перечень 16-ти цветов Зелёного как в майне
    /// </summary>
    public readonly static float[] ColorGreen = new float[] { 0, 0, .67f, .67f, 0, 0, .67f, .67f, .33f, .33f, 1, 1, .33f, .33f, 1, 1 };
    /// <summary>
    /// Перечень 16-ти цветов Синего как в майне
    /// </summary>
    public readonly static float[] ColorBlue = new float[] { 0, .67f, 0, .67f, 0, .67f, 0, .67f, .33f, 1, .33f, 1, .33f, 1, .33f, 1 };



    /// <summary>
    /// Выбранный объект блока для рендера (у каждого потока построения сеток свой)
    /// </summary>
    [ThreadStatic]
    public static BlockBase Block = null!;
    /// <summary>
    /// Объект процесса разруцшения блока
    /// </summary>
    public readonly static DestroyBlockProgress DestroyBlock = new DestroyBlockProgress();
    /// <summary>
    /// Второй объект процесса разруцшения блока, альфа
    /// </summary>
    public readonly static DestroyBlockProgress DestroyBlockSecond = new DestroyBlockProgress();


    /// <summary>
    /// Какой радиус для рендера псевдо чанков альфа блоков, при смещении больше 16 блоков
    /// </summary>
    public const int UpdateAlphaChunk = 4;
    /// <summary>
    /// Дистанция в чанках, для прорисовки сетки без MipMap, как правило это уникальные блоки
    /// </summary>
    public const int MaxDistanceNotMipMap = 12;

    /// <summary>
    /// Получить сетку разрушения блока, согласно выбранного блока
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vertex3d[] GetDestroyVertices(int progress, Vertex3d[] vertices)
        => Block.IsDestorySecond ? DestroyBlockSecond.GetVertices(progress, vertices)
            : DestroyBlock.GetVertices(progress, vertices);
}
