using System;
using Mvk.Core.Maths;
using Mvk.Rendering;

namespace Mvk.Platform.Tests
{
    public class CommandListTests
    {
        [Fact]
        public void RecordsInOrderAndReuses()
        {
            var list = new CommandList();
            list.SetViewport(0, 0, 640, 480);
            list.Clear(0.1f, 0.2f, 0.3f, 1f);
            list.Draw(6);
            list.DrawIndexed(0);
            Assert.Equal(3, list.Commands.Length);
            Assert.Equal(CommandType.Viewport, list.Commands[0].Type);
            Assert.Equal(640, list.Commands[0].C);
            Assert.Equal(CommandType.Clear, list.Commands[1].Type);
            Assert.Equal(0.2f, list.Data[list.Commands[1].A + 1]);
            Assert.Equal(CommandType.Draw, list.Commands[2].Type);

            list.Reset();
            Assert.Equal(0, list.Commands.Length);
            Assert.Equal(0, list.Data.Length);
        }

        [Fact]
        public void MatrixIsColumnMajor()
        {
            var list = new CommandList();
            Mat4 m = Mat4.Identity();
            m.Translate(5, 6, 7);
            list.SetUniform(new UniformHandle(3), m);
            RecordedCommand c = list.Commands[0];
            Assert.Equal(CommandType.UniformMatrix4, c.Type);
            Assert.Equal(3, c.A);
            ReadOnlySpan<float> d = list.Data.Slice(c.B, 16);
            Assert.Equal(m.ToArray(), d.ToArray());
            Assert.Equal(5f, d[12]);
            Assert.Equal(6f, d[13]);
            Assert.Equal(7f, d[14]);
        }

        [Fact]
        public void Matrix4x3ArrayCopiesCountMatrices()
        {
            var list = new CommandList();
            float[] bones = new float[12 * 24];
            for (int i = 0; i < bones.Length; i++) bones[i] = i;
            list.SetUniformMatrix4x3(new UniformHandle(7), bones, 24);
            bones[0] = -1; // список хранит копию
            RecordedCommand c = list.Commands[0];
            Assert.Equal(CommandType.UniformMatrix4x3, c.Type);
            Assert.Equal(7, c.A);
            Assert.Equal(24, c.C);
            ReadOnlySpan<float> d = list.Data.Slice(c.B, 12 * 24);
            Assert.Equal(0f, d[0]);
            Assert.Equal(287f, d[287]);
        }

        [Fact]
        public void InvalidUniformIsSkipped()
        {
            var list = new CommandList();
            list.SetUniform(new UniformHandle(-1), 1f);
            list.SetUniform(new UniformHandle(-1), 1);
            Assert.Equal(0, list.Commands.Length);
        }

        [Fact]
        public void GrowsBeyondInitialCapacity()
        {
            var list = new CommandList();
            for (int i = 0; i < 1000; i++) list.SetUniform(new UniformHandle(1), i, i, i, i);
            Assert.Equal(1000, list.Commands.Length);
            Assert.Equal(999f, list.Data[list.Commands[999].B]);
        }
    }
}
