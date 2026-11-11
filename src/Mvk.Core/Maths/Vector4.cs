using System;
using System.Runtime.CompilerServices;

namespace Mvk.Core.Maths
{
    /// <summary>
    /// Представляет четырехмерный вектор
    /// </summary>
    public struct Vector4 : IEquatable<Vector4>
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public float this[int index]
        {
            get
            {
                if (index == 0) return X;
                else if (index == 1) return Y;
                else if (index == 2) return Z;
                else if (index == 3) return W;
                else throw new ArgumentOutOfRangeException(nameof(index));
            }
            set
            {
                if (index == 0) X = value;
                else if (index == 1) Y = value;
                else if (index == 2) Z = value;
                else if (index == 3) W = value;
                else throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public Vector4(float s)
        {
            X = Y = Z = W = s;
        }

        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4(Vector4 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
            W = v.W;
        }

        public Vector4(Vector3 xyz, float w)
        {
            X = xyz.X;
            Y = xyz.Y;
            Z = xyz.Z;
            W = w;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator +(Vector4 lhs, Vector4 rhs)
            => new Vector4(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z, lhs.W + rhs.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator +(Vector4 lhs, float rhs)
            => new Vector4(lhs.X + rhs, lhs.Y + rhs, lhs.Z + rhs, lhs.W + rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator -(Vector4 lhs, float rhs)
            => new Vector4(lhs.X - rhs, lhs.Y - rhs, lhs.Z - rhs, lhs.W - rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator -(Vector4 lhs, Vector4 rhs)
            => new Vector4(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z, lhs.W - rhs.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(Vector4 self, float s)
            => new Vector4(self.X * s, self.Y * s, self.Z * s, self.W * s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(float lhs, Vector4 rhs)
            => new Vector4(rhs.X * lhs, rhs.Y * lhs, rhs.Z * lhs, rhs.W * lhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(Vector4 lhs, Vector4 rhs)
            => new Vector4(rhs.X * lhs.X, rhs.Y * lhs.Y, rhs.Z * lhs.Z, rhs.W * lhs.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator /(Vector4 lhs, float rhs)
            => new Vector4(lhs.X / rhs, lhs.Y / rhs, lhs.Z / rhs, lhs.W / rhs);

        public float[] ToArray() => new[] { X, Y, Z, W };

        public float[] ToArray3() => new[] { X, Y, Z };

        /// <summary>
        /// Покомпонентное сравнение без упаковки (boxing).
        /// Семантика оригинала сохранена: оператор == по каждой компоненте.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector4 other) => X == other.X && Y == other.Y && Z == other.Z && W == other.W;

        public override readonly bool Equals(object? obj) => obj is Vector4 other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector4 v1, Vector4 v2) => v1.Equals(v2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector4 v1, Vector4 v2) => !v1.Equals(v2);

        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);

        public override string ToString()
            => "[" + X + ", " + Y + ", " + Z + ", " + W + "]";
    }
}
