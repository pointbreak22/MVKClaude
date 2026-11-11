using System;
using System.Runtime.CompilerServices;

namespace Mvk.Core.Maths
{
    /// <summary>
    /// Представляет двумерный вектор
    /// </summary>
    public struct Vector2 : IEquatable<Vector2>
    {
        public float X;
        public float Y;

        public float this[int index]
        {
            get
            {
                if (index == 0) return X;
                else if (index == 1) return Y;
                else throw new ArgumentOutOfRangeException(nameof(index));
            }
            set
            {
                if (index == 0) X = value;
                else if (index == 1) Y = value;
                else throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public Vector2(float s)
        {
            X = Y = s;
        }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Vector2(Vector2 v)
        {
            X = v.X;
            Y = v.Y;
        }

        public Vector2(Vector3 v)
        {
            X = v.X;
            Y = v.Y;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(Vector2 lhs, Vector2 rhs)
            => new Vector2(lhs.X + rhs.X, lhs.Y + rhs.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(Vector2 lhs, float rhs)
            => new Vector2(lhs.X + rhs, lhs.Y + rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 lhs, Vector2 rhs)
            => new Vector2(lhs.X - rhs.X, lhs.Y - rhs.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 lhs, float rhs)
            => new Vector2(lhs.X - rhs, lhs.Y - rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 self, float s)
            => new Vector2(self.X * s, self.Y * s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(float s, Vector2 self)
            => new Vector2(self.X * s, self.Y * s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 lhs, Vector2 rhs)
            => new Vector2(rhs.X * lhs.X, rhs.Y * lhs.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(Vector2 lhs, float rhs)
            => new Vector2(lhs.X / rhs, lhs.Y / rhs);

        public float[] ToArray() => new[] { X, Y };

        /// <summary>
        /// Покомпонентное сравнение без упаковки (boxing).
        /// Семантика оригинала сохранена: оператор == по каждой компоненте.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector2 other) => X == other.X && Y == other.Y;

        public override readonly bool Equals(object? obj) => obj is Vector2 other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2 v1, Vector2 v2) => v1.Equals(v2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector2 v1, Vector2 v2) => !v1.Equals(v2);

        public override readonly int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => string.Format("{0:0.00}; {1:0.00}", X, Y);
    }
}
