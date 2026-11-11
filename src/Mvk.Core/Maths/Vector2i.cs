using System;
using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;

namespace Mvk.Core.Maths
{
    /// <summary>
    /// Представляет двумерный вектор
    /// </summary>
    public struct Vector2i : IEquatable<Vector2i>
    {
        public int X;
        public int Y;

        public int this[int index]
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

        public Vector2i(int s) => X = Y = s;

        public Vector2i(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2i(Vector2i v)
        {
            X = v.X;
            Y = v.Y;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator +(Vector2i lhs, Vector2i rhs)
            => new Vector2i(lhs.X + rhs.X, lhs.Y + rhs.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator +(Vector2i lhs, int rhs)
            => new Vector2i(lhs.X + rhs, lhs.Y + rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator -(Vector2i lhs, Vector2i rhs)
            => new Vector2i(lhs.X - rhs.X, lhs.Y - rhs.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator -(Vector2i lhs, int rhs)
            => new Vector2i(lhs.X - rhs, lhs.Y - rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator *(Vector2i self, int s)
            => new Vector2i(self.X * s, self.Y * s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator *(int s, Vector2i self)
            => new Vector2i(self.X * s, self.Y * s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator *(Vector2i lhs, Vector2i rhs)
            => new Vector2i(rhs.X * lhs.X, rhs.Y * lhs.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator /(Vector2i lhs, int rhs)
            => new Vector2i(lhs.X / rhs, lhs.Y / rhs);

        public int[] ToArray() => new[] { X, Y };

        /// <summary>
        /// Покомпонентное сравнение без упаковки (boxing).
        /// Семантика оригинала сохранена: оператор == по каждой компоненте.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector2i other) => X == other.X && Y == other.Y;

        public override readonly bool Equals(object? obj) => obj is Vector2i other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2i v1, Vector2i v2) => v1.Equals(v2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector2i v1, Vector2i v2) => !v1.Equals(v2);

        public override readonly int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => X + "; " + Y;
    }
}
