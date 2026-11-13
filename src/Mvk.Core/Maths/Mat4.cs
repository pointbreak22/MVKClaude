using System;
using System.Runtime.CompilerServices;

namespace Mvk.Core.Maths
{
    /// <summary>
    /// Матрица 4x4 по столбцам (как в GLSL и glUniformMatrix4fv без транспонирования).
    /// </summary>
    /// <remarks>
    /// Настоящий значимый тип: четыре столбца лежат в самой структуре (improvements R19).
    /// В оригинале (WinGL.Util.Mat4) столбцы хранились в массиве в куче: каждое создание матрицы
    /// выделяло память, копии структуры делили один массив, а умножение шло через общий статический кеш.
    /// Формулы и порядок операций прежние, результат побитно тот же (Golden/math.csv).
    /// Копия теперь независима: чтобы записать матрицу в другую, используйте присваивание.
    /// </remarks>
    public struct Mat4 : IEquatable<Mat4>
    {
        private Vector4 _c0, _c1, _c2, _c3;

        #region Construction

        /// <summary>
        /// Копия матрицы
        /// </summary>
        public Mat4(Mat4 mat)
        {
            _c0 = mat._c0;
            _c1 = mat._c1;
            _c2 = mat._c2;
            _c3 = mat._c3;
        }

        /// <summary>
        /// Диагональная матрица со значением scale
        /// </summary>
        public Mat4(float scale)
        {
            _c0 = new Vector4(scale, 0, 0, 0);
            _c1 = new Vector4(0, scale, 0, 0);
            _c2 = new Vector4(0, 0, scale, 0);
            _c3 = new Vector4(0, 0, 0, scale);
        }

        /// <summary>
        /// Диагональная матрица
        /// </summary>
        public Mat4(float m00, float m11, float m22, float m33)
        {
            _c0 = new Vector4(m00, 0, 0, 0);
            _c1 = new Vector4(0, m11, 0, 0);
            _c2 = new Vector4(0, 0, m22, 0);
            _c3 = new Vector4(0, 0, 0, m33);
        }

        /// <summary>
        /// Матрица переноса
        /// </summary>
        public Mat4(float x, float y, float z)
        {
            _c0 = new Vector4(1, 0, 0, 0);
            _c1 = new Vector4(0, 1, 0, 0);
            _c2 = new Vector4(0, 0, 1, 0);
            _c3 = new Vector4(x, y, z, 1);
        }

        /// <summary>
        /// Матрица из четырёх столбцов массива
        /// </summary>
        public Mat4(Vector4[] cols)
        {
            _c0 = cols[0];
            _c1 = cols[1];
            _c2 = cols[2];
            _c3 = cols[3];
        }

        /// <summary>
        /// Матрица из четырёх столбцов
        /// </summary>
        public Mat4(Vector4 a, Vector4 b, Vector4 c, Vector4 d)
        {
            _c0 = a;
            _c1 = b;
            _c2 = c;
            _c3 = d;
        }

        /// <summary>
        /// Единичная матрица
        /// </summary>
        public static Mat4 Identity() => new Mat4(1f);

        /// <summary>
        /// Привести текущую матрицу к Identity
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => this = new Mat4(1f);

        /// <summary>
        /// Копировать текущую матрицу в матрицу m (в оригинале m передавалась по значению
        /// и менялась через общий массив; теперь явно по ссылке)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Copy(ref Mat4 m) => m = this;

        #endregion

        #region Index Access

        /// <summary>
        /// Столбец матрицы
        /// </summary>
        public Vector4 this[int column]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                switch (column)
                {
                    case 0: return _c0;
                    case 1: return _c1;
                    case 2: return _c2;
                    case 3: return _c3;
                    default: throw new ArgumentOutOfRangeException(nameof(column));
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                switch (column)
                {
                    case 0: _c0 = value; break;
                    case 1: _c1 = value; break;
                    case 2: _c2 = value; break;
                    case 3: _c3 = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(column));
                }
            }
        }

        /// <summary>
        /// Элемент матрицы: столбец и строка
        /// </summary>
        public float this[int column, int row]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => this[column][row];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                switch (column)
                {
                    case 0: _c0[row] = value; break;
                    case 1: _c1[row] = value; break;
                    case 2: _c2[row] = value; break;
                    case 3: _c3[row] = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(column));
                }
            }
        }

        #endregion

        #region Transform

        /// <summary>
        /// Задать масштаб текущей матрицы
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Scale(float x, float y, float z)
        {
            _c0.X = x;
            _c1.Y = y;
            _c2.Z = z;
        }

        /// <summary>
        /// Вращение матрицы по кватерниону
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateQuat(float x, float y, float z, float w)
        {
            float wx, wy, wz, xx, yy, yz, xy, xz, zz, x2, y2, z2;

            x2 = x + x;
            y2 = y + y;
            z2 = z + z;
            xx = x * x2;
            xy = x * y2;
            xz = x * z2;
            yy = y * y2;
            yz = y * z2;
            zz = z * z2;
            wx = w * x2;
            wy = w * y2;
            wz = w * z2;

            Mat4 rotate = new Mat4(1f);
            rotate._c0.X = 1f - (yy + zz);
            rotate._c0.Y = xy + wz;
            rotate._c0.Z = xz - wy;
            rotate._c1.X = xy - wz;
            rotate._c1.Y = 1f - (xx + zz);
            rotate._c1.Z = yz + wx;
            rotate._c2.X = xz + wy;
            rotate._c2.Y = yz - wx;
            rotate._c2.Z = 1f - (xx + yy);
            this = this * rotate;
        }

        /// <summary>
        /// Вращение матрицы по кватерниону
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateQuat(Vector4 quaternion)
            => RotateQuat(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

        /// <summary>
        /// Вращение на угол вокруг оси (x, y, z)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rotate(float angle, float x, float y, float z)
        {
            float c = Glm.Cos(angle);
            float cm = 1 - c;
            float sz = Glm.Sin(angle);

            float tx = cm * x;
            float ty = cm * y;
            float tz = cm * z;

            float sx = sz * x;
            float sy = sz * y;
            sz *= z;

            Vector4 v0 = _c0 * (c + tx * x) + _c1 * (tx * y + sz) + _c2 * (tx * z - sy);
            Vector4 v1 = _c0 * (ty * x - sz) + _c1 * (c + ty * y) + _c2 * (ty * z + sx);
            _c2 = _c0 * (tz * x + sy) + _c1 * (tz * y - sx) + _c2 * (c + tz * z);
            _c0 = v0;
            _c1 = v1;
        }

        /// <summary>
        /// Вращение по X
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateX(float pitch)
        {
            float c = Glm.Cos(pitch);
            float s = Glm.Sin(pitch);

            Vector4 v = _c1 * c + _c2 * s;
            _c2 = _c1 * -s + _c2 * c;
            _c1 = v;
        }

        /// <summary>
        /// Вращение по Y
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateY(float yaw)
        {
            float c = Glm.Cos(yaw);
            float s = Glm.Sin(yaw);

            Vector4 v = _c0 * c + _c2 * -s;
            _c2 = _c0 * s + _c2 * c;
            _c0 = v;
        }

        /// <summary>
        /// Вращение по Z
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateZ(float roll)
        {
            float c = Glm.Cos(roll);
            float s = Glm.Sin(roll);

            Vector4 v = _c0 * c + _c1 * s;
            _c1 = _c0 * -s + _c1 * c;
            _c0 = v;
        }

        /// <summary>
        /// Вращение в порядке X, Y, Z
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateXYZ(float yaw, float pitch, float roll)
        {
            if (pitch != 0) RotateX(pitch);
            if (yaw != 0) RotateY(yaw);
            if (roll != 0) RotateZ(roll);
        }

        /// <summary>
        /// Вращение в порядке Z, Y, X
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateZYX(float yaw, float pitch, float roll)
        {
            if (roll != 0) RotateZ(roll);
            if (yaw != 0) RotateY(yaw);
            if (pitch != 0) RotateX(pitch);
        }

        /// <summary>
        /// Перенос
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Translate(float x, float y, float z)
            => _c3 += _c0 * x + _c1 * y + _c2 * z;

        #endregion

        #region Conversion

        /// <summary>
        /// Матрица в новый массив 16 чисел по столбцам
        /// </summary>
        public readonly float[] ToArray()
        {
            float[] array = new float[16];
            ConvArray(array);
            return array;
        }

        /// <summary>
        /// Матрица 4x3 (без нижней строки) в новый массив 12 чисел по столбцам
        /// </summary>
        public readonly float[] ToArray4x3()
        {
            float[] array = new float[12];
            ConvArray4x3(array, 0);
            return array;
        }

        /// <summary>
        /// Записать матрицу 4x3 в массив с позиции offset
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ConvArray4x3(float[] array, int offset)
        {
            array[offset] = _c0.X;
            array[offset + 1] = _c0.Y;
            array[offset + 2] = _c0.Z;
            array[offset + 3] = _c1.X;
            array[offset + 4] = _c1.Y;
            array[offset + 5] = _c1.Z;
            array[offset + 6] = _c2.X;
            array[offset + 7] = _c2.Y;
            array[offset + 8] = _c2.Z;
            array[offset + 9] = _c3.X;
            array[offset + 10] = _c3.Y;
            array[offset + 11] = _c3.Z;
        }

        /// <summary>
        /// Записать матрицу в массив 16 чисел по столбцам
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ConvArray(float[] array)
        {
            array[0] = _c0.X;
            array[1] = _c0.Y;
            array[2] = _c0.Z;
            array[3] = _c0.W;
            array[4] = _c1.X;
            array[5] = _c1.Y;
            array[6] = _c1.Z;
            array[7] = _c1.W;
            array[8] = _c2.X;
            array[9] = _c2.Y;
            array[10] = _c2.Z;
            array[11] = _c2.W;
            array[12] = _c3.X;
            array[13] = _c3.Y;
            array[14] = _c3.Z;
            array[15] = _c3.W;
        }

        #endregion

        #region Multiplication

        /// <summary>
        /// Умножить текущую матрицу на rhs справа (this = this * rhs)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(in Mat4 rhs)
        {
            Vector4 c0 = rhs._c0.X * _c0 + rhs._c0.Y * _c1 + rhs._c0.Z * _c2 + rhs._c0.W * _c3;
            Vector4 c1 = rhs._c1.X * _c0 + rhs._c1.Y * _c1 + rhs._c1.Z * _c2 + rhs._c1.W * _c3;
            Vector4 c2 = rhs._c2.X * _c0 + rhs._c2.Y * _c1 + rhs._c2.Z * _c2 + rhs._c2.W * _c3;
            Vector4 c3 = rhs._c3.X * _c0 + rhs._c3.Y * _c1 + rhs._c3.Z * _c2 + rhs._c3.W * _c3;
            _c0 = c0;
            _c1 = c1;
            _c2 = c2;
            _c3 = c3;
        }

        /// <summary>
        /// Умножение матрицы на вектор
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(Mat4 lhs, Vector4 rhs)
        {
            return new Vector4(
                lhs._c0.X * rhs.X + lhs._c1.X * rhs.Y + lhs._c2.X * rhs.Z + lhs._c3.X * rhs.W,
                lhs._c0.Y * rhs.X + lhs._c1.Y * rhs.Y + lhs._c2.Y * rhs.Z + lhs._c3.Y * rhs.W,
                lhs._c0.Z * rhs.X + lhs._c1.Z * rhs.Y + lhs._c2.Z * rhs.Z + lhs._c3.Z * rhs.W,
                lhs._c0.W * rhs.X + lhs._c1.W * rhs.Y + lhs._c2.W * rhs.Z + lhs._c3.W * rhs.W
            );
        }

        /// <summary>
        /// Произведение матриц lhs * rhs
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 operator *(Mat4 lhs, Mat4 rhs)
        {
            return new Mat4(
                rhs._c0.X * lhs._c0 + rhs._c0.Y * lhs._c1 + rhs._c0.Z * lhs._c2 + rhs._c0.W * lhs._c3,
                rhs._c1.X * lhs._c0 + rhs._c1.Y * lhs._c1 + rhs._c1.Z * lhs._c2 + rhs._c1.W * lhs._c3,
                rhs._c2.X * lhs._c0 + rhs._c2.Y * lhs._c1 + rhs._c2.Z * lhs._c2 + rhs._c2.W * lhs._c3,
                rhs._c3.X * lhs._c0 + rhs._c3.Y * lhs._c1 + rhs._c3.Z * lhs._c2 + rhs._c3.W * lhs._c3
            );
        }

        /// <summary>
        /// Умножение матрицы на число
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 operator *(Mat4 lhs, float s)
            => new Mat4(lhs._c0 * s, lhs._c1 * s, lhs._c2 * s, lhs._c3 * s);

        #endregion

        #region ToString support

        public override readonly string ToString()
        {
            return string.Format(
                "[{0}, {1}, {2}, {3}; {4}, {5}, {6}, {7}; {8}, {9}, {10}, {11}; {12}, {13}, {14}, {15}]",
                this[0, 0], this[1, 0], this[2, 0], this[3, 0],
                this[0, 1], this[1, 1], this[2, 1], this[3, 1],
                this[0, 2], this[1, 2], this[2, 2], this[3, 2],
                this[0, 3], this[1, 3], this[2, 3], this[3, 3]
            );
        }

        #endregion

        #region Comparision

        public readonly bool Equals(Mat4 mat)
            => mat._c0 == _c0 && mat._c1 == _c1 && mat._c2 == _c2 && mat._c3 == _c3;

        public override readonly bool Equals(object? obj) => obj is Mat4 mat && Equals(mat);

        public static bool operator ==(Mat4 m1, Mat4 m2) => m1.Equals(m2);

        public static bool operator !=(Mat4 m1, Mat4 m2) => !m1.Equals(m2);

        public override readonly int GetHashCode()
            => _c0.GetHashCode() ^ _c1.GetHashCode() ^ _c2.GetHashCode() ^ _c3.GetHashCode();

        #endregion
    }
}
