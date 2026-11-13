using System;

namespace Mvk.Rendering
{
    /// <summary>
    /// Буфер на GPU (вершины или индексы)
    /// </summary>
    public interface IGpuBuffer : IDisposable
    {
        BufferKind Kind { get; }
        /// <summary>Сколько байт загружено последним SetData</summary>
        int Size { get; }

        /// <summary>
        /// Заменить содержимое. Если данных больше, чем выделено, буфер перевыделяется
        /// (привязки к VAO и командам при этом остаются верными).
        /// </summary>
        void SetData<T>(ReadOnlySpan<T> data) where T : unmanaged;
    }

    /// <summary>
    /// Текстура на GPU (2D или массив 2D)
    /// </summary>
    public interface ITexture : IDisposable
    {
        TextureDescription Description { get; }

        /// <summary>
        /// Загрузить пиксели одного уровня мипмапа одного слоя.
        /// Размер уровня = max(1, Width >> level) x max(1, Height >> level).
        /// </summary>
        void SetData(ReadOnlySpan<byte> pixels, PixelFormat format, int level = 0, int layer = 0);

        /// <summary>
        /// Загрузить прямоугольник пикселей в слой (уровень 0), остальное содержимое слоя не меняется.
        /// Нужен для картинок меньше слоя (текстуры сущностей в массиве оригинала, TexSubImage3D).
        /// </summary>
        void SetSubData(ReadOnlySpan<byte> pixels, PixelFormat format, int x, int y, int width, int height, int layer = 0);

        /// <summary>
        /// Построить мипмапы средствами драйвера из уровня 0
        /// </summary>
        void GenerateMipmaps();
    }

    /// <summary>
    /// Цель рендера вне окна: кадровый буфер с текстурой глубины (карта теней), без цвета
    /// </summary>
    public interface IRenderTarget : IDisposable
    {
        /// <summary>Текстура глубины</summary>
        ITexture Depth { get; }
    }

    /// <summary>
    /// Ссылка на uniform-переменную шейдера. Получается один раз после создания шейдера.
    /// </summary>
    public readonly record struct UniformHandle(int Location)
    {
        /// <summary>Переменной нет в шейдере (вырезана компилятором). Запись в неё игнорируется.</summary>
        public bool IsValid => Location >= 0;
    }

    /// <summary>
    /// Скомпилированная программа шейдеров
    /// </summary>
    public interface IShader : IDisposable
    {
        string Name { get; }

        /// <summary>
        /// Найти uniform по имени. Если её нет, вернётся недействительная ссылка (как -1 в GL).
        /// </summary>
        UniformHandle GetUniform(string name);
    }

    /// <summary>
    /// Неизменяемый конвейер, создаётся заранее, при загрузке
    /// </summary>
    public interface IPipeline : IDisposable
    {
        PipelineDescription Description { get; }
    }
}
