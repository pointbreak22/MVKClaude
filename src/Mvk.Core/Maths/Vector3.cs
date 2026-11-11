using System;
using System.Runtime.CompilerServices;

namespace Mvk.Core.Maths
{
    /// <summary>
    /// Представляет трехмерный вектор
    /// </summary>
    public struct Vector3 : IEquatable<Vector3>
    {
        public float X;
        public float Y;
        public float Z;

        public float this[int index]
        {
            get
            {
                if (index == 0) return X;
                else if (index == 1) return Y;
                else if (index == 2) return Z;
                else throw new ArgumentOutOfRangeException(nameof(index));
            }
            set
            {
                if (index == 0) X = value;
                else if (index == 1) Y = value;
                else if (index == 2) Z = value;
                else throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public Vector3(float s)
        {
            X = Y = Z = s;
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(Vector3 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        public Vector3(Vector3i v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        public Vector3(Vector4 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        public Vector3(Vector2 xy, float z)
        {
            X = xy.X;
            Y = xy.Y;
            Z = z;
        }

        /// <summary>
        /// Получить точку из матрици перемещения, вращения
        /// </summary>
        /// <param name="m4"></param>
        public Vector3(Mat4 m4)
        {
            X = m4[3][0];
            Y = m4[3][1];
            Z = m4[3][2];
        }

        /// <summary>
        /// Угол по вертикали в радианах
        /// </summary>
        public Vector3 RotatePitch(float angle)
        {
            float c = Glm.Cos(angle);
            float s = Glm.Sin(angle);
            float y2 = Y * c + Z * s;
            float z2 = Z * c - Y * s;
            return new Vector3(X, y2, z2);
        }

        /// <summary>
        /// Угол по горизонтали в радианах
        /// </summary>
        public Vector3 RotateYaw(float angle)
        {
            float c = Glm.Cos(angle);
            float s = Glm.Sin(angle);
            float x2 = X * c + Z * s;
            float z2 = Z * c - X * s;
            return new Vector3(x2, Y, z2);
        }

        /// <summary>
        /// Инверсия
        /// </summary>
        public Vector3 Inverse() => this * -1f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(Vector3 lhs, Vector3 rhs)
            => new Vector3(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(Vector3 lhs, float rhs)
            => new Vector3(lhs.X + rhs, lhs.Y + rhs, lhs.Z + rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(Vector3 lhs, Vector3 rhs)
            => new Vector3(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(Vector3 lhs, float rhs)
            => new Vector3(lhs.X - rhs, lhs.Y - rhs, lhs.Z - rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(Vector3 self, float s)
            => new Vector3(self.X * s, self.Y * s, self.Z * s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(float s, Vector3 self)
            => new Vector3(self.X * s, self.Y * s, self.Z * s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(Vector3 lhs, float rhs)
            => new Vector3(lhs.X / rhs, lhs.Y / rhs, lhs.Z / rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(Vector3 lhs, Vector3 rhs)
            => new Vector3(rhs.X * lhs.X, rhs.Y * lhs.Y, rhs.Z * lhs.Z);

        public float[] ToArray() => new[] { X, Y, Z };

        public Vector3 Normalize() => Glm.Normalize(this);

        /// <summary>
        /// Равно ли значение нулю
        /// </summary>
        public bool IsZero() => X == 0 && Y == 0 && Z == 0;

        /// <summary>
        /// Покомпонентное сравнение без упаковки (boxing).
        /// Семантика оригинала сохранена: оператор == по каждой компоненте.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector3 other) => X == other.X && Y == other.Y && Z == other.Z;

        public override readonly bool Equals(object? obj) => obj is Vector3 other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector3 v1, Vector3 v2) => v1.Equals(v2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector3 v1, Vector3 v2) => !v1.Equals(v2);

        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

        public override string ToString()
            => string.Format("{0:0.00}; {1:0.00}; {2:0.00}", X, Y, Z);
    }
}
