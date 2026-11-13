using System;
using Mvk.Core.Maths;

namespace Mvk.Core.Tests
{
    /// <summary>
    /// Mat4 значимый тип (improvements R19): копии независимы, операции не выделяют память.
    /// Побитное совпадение формул с оригиналом проверяет MathParityTests (Golden/math.csv).
    /// </summary>
    public class Mat4ValueTests
    {
        [Fact]
        public void CopyIsIndependent()
        {
            Mat4 a = Mat4.Identity();
            Mat4 b = a;
            b.Translate(1, 2, 3);
            Assert.Equal(Mat4.Identity(), a);
            Assert.Equal(3f, b[3, 2]);

            Mat4 c = Mat4.Identity();
            b.Copy(ref c);
            Assert.Equal(b, c);
            c.RotateX(1f);
            Assert.NotEqual(b, c);
        }

        [Fact]
        public void ArrayElementsChangeInPlace()
        {
            Mat4[] bones = { Mat4.Identity(), Mat4.Identity() };
            bones[1].Translate(5, 0, 0);
            bones[0] = bones[1];
            bones[1].RotateY(1f);
            Assert.Equal(5f, bones[0][3, 0]);
            Assert.NotEqual(bones[0], bones[1]);
        }

        [Fact]
        public void OperationsDoNotAllocate()
        {
            Mat4 m = Mat4.Identity();
            Mat4 r = new Mat4(1, 2, 3);
            float[] buffer = new float[24];
            // Прогрев JIT
            _Work(ref m, r, buffer);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) _Work(ref m, r, buffer);
            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        private static void _Work(ref Mat4 m, Mat4 r, float[] buffer)
        {
            m.Clear();
            m.RotateX(.1f);
            m.RotateQuat(0, .6f, 0, .8f);
            m.Multiply(r);
            m = m * r * 2f;
            m.ConvArray4x3(buffer, 12);
        }
    }
}
