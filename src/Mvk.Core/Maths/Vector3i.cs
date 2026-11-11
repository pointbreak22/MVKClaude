using System;
using System.Runtime.CompilerServices;

namespace Mvk.Core.Maths
{
    /// <summary>
    /// Представляет трехмерный вектор
    /// </summary>
    public struct Vector3i : IEquatable<Vector3i>
    {
        public int X;
        public int Y;
        public int Z;

        public int this[int index]
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

        public Vector3i(int s)
        {
            X = Y = Z = s;
        }

        public Vector3i(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3i(float x, float y, float z)
        {
            X = Mth.Floor(x);
            Y = Mth.Floor(y);
            Z = Mth.Floor(z);
        }

        public Vector3i(Vector3 v)
        {
            X = Mth.Floor(v.X);
            Y = Mth.Floor(v.Y);
            Z = Mth.Floor(v.Z);
        }

        public Vector3i(Vector3i v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        public Vector3i(Vector2i xy, int z)
        {
            X = xy.X;
            Y = xy.Y;
            Z = z;
        }

        /// <summary>
        /// Инверсия
        /// </summary>
        public Vector3i Inverse() => this * -1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator +(Vector3i lhs, Vector3i rhs)
            => new Vector3i(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator +(Vector3i lhs, int rhs)
            => new Vector3i(lhs.X + rhs, lhs.Y + rhs, lhs.Z + rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator -(Vector3i lhs, Vector3i rhs)
            => new Vector3i(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator -(Vector3i lhs, int rhs)
            => new Vector3i(lhs.X - rhs, lhs.Y - rhs, lhs.Z - rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator *(Vector3i self, int s)
            => new Vector3i(self.X * s, self.Y * s, self.Z * s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator *(int s, Vector3i self)
            => new Vector3i(self.X * s, self.Y * s, self.Z * s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator /(Vector3i lhs, int rhs)
            => new Vector3i(lhs.X / rhs, lhs.Y / rhs, lhs.Z / rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator *(Vector3i lhs, Vector3i rhs)
            => new Vector3i(rhs.X * lhs.X, rhs.Y * lhs.Y, rhs.Z * lhs.Z);

        public int[] ToArray() => new[] { X, Y, Z };

        /// <summary>
        /// Равно ли значение нулю
        /// </summary>
        public bool IsZero() => X == 0 && Y == 0 && Z == 0;

        /// <summary>
        /// Покомпонентное сравнение без упаковки (boxing).
        /// Семантика оригинала сохранена: оператор == по каждой компоненте.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector3i other) => X == other.X && Y == other.Y && Z == other.Z;

        public override readonly bool Equals(object? obj) => obj is Vector3i other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector3i v1, Vector3i v2) => v1.Equals(v2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector3i v1, Vector3i v2) => !v1.Equals(v2);

        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

        public override string ToString() => X + "; " + Y + "; " + Z;
    }
}
