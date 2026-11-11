using System;
using System.Runtime.CompilerServices;

namespace Mvk.Core.Maths
{
    /// <summary>
    /// Представляет двумерный вектор с 64 разнрядным числом
    /// </summary>
    public struct Vector2l : IEquatable<Vector2l>
    {
        public long X;
        public long Y;

        public long this[int index]
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

        public Vector2l(long x, long y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Покомпонентное сравнение без упаковки (boxing).
        /// Семантика оригинала сохранена: оператор == по каждой компоненте.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector2l other) => X == other.X && Y == other.Y;

        public override readonly bool Equals(object? obj) => obj is Vector2l other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2l v1, Vector2l v2) => v1.Equals(v2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector2l v1, Vector2l v2) => !v1.Equals(v2);

        public override readonly int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => X + "; " + Y;
    }
}
