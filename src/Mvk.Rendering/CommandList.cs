using System;
using Mvk.Core.Maths;

namespace Mvk.Rendering
{
    /// <summary>
    /// Вид записанной команды
    /// </summary>
    public enum CommandType : byte
    {
        Viewport,
        Clear,
        SetPipeline,
        SetVertexBuffer,
        SetIndexBuffer,
        SetTexture,
        UniformInt,
        UniformFloat,
        UniformMatrix4,
        Draw,
        DrawIndexed
    }

    /// <summary>
    /// Одна записанная команда. Смысл полей A..D зависит от Type, см. методы CommandList.
    /// </summary>
    public readonly struct RecordedCommand
    {
        public readonly CommandType Type;
        public readonly int A, B, C, D;
        public readonly object? Ref;

        public RecordedCommand(CommandType type, int a, int b, int c, int d, object? r)
        {
            Type = type; A = a; B = b; C = c; D = d; Ref = r;
        }
    }

    /// <summary>
    /// Список команд отрисовки. Записывается в любом потоке, выполняется бэкендом в IGraphicsDevice.Submit.
    /// Не зависит от API: GL проигрывает его вызовами, Vulkan переложит в VkCommandBuffer.
    /// Память переиспользуется между кадрами (Reset), в установившемся режиме кадр не выделяет память.
    /// </summary>
    public sealed class CommandList
    {
        private RecordedCommand[] _commands = new RecordedCommand[256];
        private float[] _data = new float[1024];
        private int _count;
        private int _dataCount;

        /// <summary>Записанные команды</summary>
        public ReadOnlySpan<RecordedCommand> Commands => _commands.AsSpan(0, _count);
        /// <summary>Числа для Clear и uniform-команд (смещение лежит в поле команды)</summary>
        public ReadOnlySpan<float> Data => _data.AsSpan(0, _dataCount);

        /// <summary>Начать запись заново, сохранив выделенную память</summary>
        public void Reset()
        {
            // Ссылки на ресурсы обнуляем, чтобы список не держал удалённые объекты
            Array.Clear(_commands, 0, _count);
            _count = 0;
            _dataCount = 0;
        }

        /// <summary>Область вывода в пикселях, начало слева снизу</summary>
        public void SetViewport(int x, int y, int width, int height)
            => Add(CommandType.Viewport, x, y, width, height, null);

        /// <summary>Очистить цвет и глубину (глубина в 1)</summary>
        public void Clear(float r, float g, float b, float a, bool color = true, bool depth = true)
        {
            int off = AddData(4);
            _data[off] = r; _data[off + 1] = g; _data[off + 2] = b; _data[off + 3] = a;
            Add(CommandType.Clear, off, color ? 1 : 0, depth ? 1 : 0, 0, null);
        }

        /// <summary>Выбрать конвейер. Uniform-команды дальше относятся к его шейдеру.</summary>
        public void SetPipeline(IPipeline pipeline)
            => Add(CommandType.SetPipeline, 0, 0, 0, 0, pipeline);

        /// <summary>Вершинный буфер в слот раскладки конвейера</summary>
        public void SetVertexBuffer(int slot, IGpuBuffer buffer)
            => Add(CommandType.SetVertexBuffer, slot, 0, 0, 0, buffer);

        /// <summary>Индексный буфер</summary>
        public void SetIndexBuffer(IGpuBuffer buffer, IndexFormat format)
            => Add(CommandType.SetIndexBuffer, (int)format, 0, 0, 0, buffer);

        /// <summary>Текстура в слот. Номер слота = индекс в ShaderDescription.Samplers.</summary>
        public void SetTexture(int slot, ITexture texture)
            => Add(CommandType.SetTexture, slot, 0, 0, 0, texture);

        /// <summary>int / sampler / bool</summary>
        public void SetUniform(UniformHandle u, int value)
        {
            if (u.IsValid) Add(CommandType.UniformInt, u.Location, value, 0, 0, null);
        }

        public void SetUniform(UniformHandle u, float x)
        {
            if (!u.IsValid) return;
            int off = AddData(1);
            _data[off] = x;
            Add(CommandType.UniformFloat, u.Location, off, 1, 0, null);
        }

        public void SetUniform(UniformHandle u, float x, float y)
        {
            if (!u.IsValid) return;
            int off = AddData(2);
            _data[off] = x; _data[off + 1] = y;
            Add(CommandType.UniformFloat, u.Location, off, 2, 0, null);
        }

        public void SetUniform(UniformHandle u, float x, float y, float z)
        {
            if (!u.IsValid) return;
            int off = AddData(3);
            _data[off] = x; _data[off + 1] = y; _data[off + 2] = z;
            Add(CommandType.UniformFloat, u.Location, off, 3, 0, null);
        }

        public void SetUniform(UniformHandle u, float x, float y, float z, float w)
        {
            if (!u.IsValid) return;
            int off = AddData(4);
            _data[off] = x; _data[off + 1] = y; _data[off + 2] = z; _data[off + 3] = w;
            Add(CommandType.UniformFloat, u.Location, off, 4, 0, null);
        }

        public void SetUniform(UniformHandle u, Vector3 v) => SetUniform(u, v.X, v.Y, v.Z);

        /// <summary>Матрица 4x4 по столбцам (как glUniformMatrix4fv без транспонирования)</summary>
        public void SetUniform(UniformHandle u, in Mat4 m)
        {
            if (!u.IsValid) return;
            int off = AddData(16);
            for (int c = 0; c < 4; c++)
            {
                Vector4 col = m[c];
                int o = off + c * 4;
                _data[o] = col.X; _data[o + 1] = col.Y; _data[o + 2] = col.Z; _data[o + 3] = col.W;
            }
            Add(CommandType.UniformMatrix4, u.Location, off, 1, 0, null);
        }

        /// <summary>Матрица 4x4 из массива 16 чисел по столбцам (Gi.MatrixView)</summary>
        public void SetUniformMatrix4(UniformHandle u, ReadOnlySpan<float> m16)
        {
            if (!u.IsValid) return;
            int off = AddData(16);
            m16.Slice(0, 16).CopyTo(_data.AsSpan(off, 16));
            Add(CommandType.UniformMatrix4, u.Location, off, 1, 0, null);
        }

        /// <summary>Рисовать без индексов</summary>
        public void Draw(int vertexCount, int firstVertex = 0)
        {
            if (vertexCount > 0) Add(CommandType.Draw, vertexCount, firstVertex, 0, 0, null);
        }

        /// <summary>Рисовать по индексам</summary>
        public void DrawIndexed(int indexCount, int firstIndex = 0)
        {
            if (indexCount > 0) Add(CommandType.DrawIndexed, indexCount, firstIndex, 0, 0, null);
        }

        private void Add(CommandType type, int a, int b, int c, int d, object? r)
        {
            if (_count == _commands.Length) Array.Resize(ref _commands, _count * 2);
            _commands[_count++] = new RecordedCommand(type, a, b, c, d, r);
        }

        private int AddData(int n)
        {
            if (_dataCount + n > _data.Length) Array.Resize(ref _data, Math.Max(_data.Length * 2, _dataCount + n));
            int off = _dataCount;
            _dataCount += n;
            return off;
        }
    }
}
