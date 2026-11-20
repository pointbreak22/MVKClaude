using System;

namespace Mvk.Core.Nbt
{
    /// <summary>
    /// Тэг массива интов для NBT
    /// </summary>
    public class TagIntArray : NBTBase
    {
        private int[] intArray = new int[0];

        public TagIntArray() { }
        public TagIntArray(int[] intArray) => this.intArray = intArray;

        public override void Write(NBTStream output)
        {
            output.WriteInt(intArray.Length);
            for (int i = 0; i < intArray.Length; i++)
            {
                output.WriteInt(intArray[i]);
            }
        }
        public override void Read(NBTStream input)
        {
            intArray = new int[input.ReadInt()];
            for (int i = 0; i < intArray.Length; i++)
            {
                intArray[i] = input.ReadInt();
            }
        }

        public override NBTBase Copy()
        {
            int[] ar = new int[intArray.Length];
            Array.Copy(intArray, 0, ar, 0, intArray.Length);
            return new TagIntArray(ar);
        }
        public override byte GetId() => 11;

        public override string ToString()
        {
            string str = "[";
            for (int i = 0; i < intArray.Length; i++)
                str += intArray[i] + ",";
            str += "]";
            return str;
        }

        /// <summary>
        /// Равенство по содержимому (в оригинале по ссылке на массив: копия тега не была равна исходному)
        /// </summary>
        public override bool Equals(object? value)
            => base.Equals(value) && value is TagIntArray tag && intArray.AsSpan().SequenceEqual(tag.intArray);

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(GetId());
            hash.AddBytes(System.Runtime.InteropServices.MemoryMarshal.AsBytes(intArray.AsSpan()));
            return hash.ToHashCode();
        }

        public int[] GetIntArray() => intArray;
    }
}
