using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

// Файл общий для двух проектов и не содержит своего using на математику:
// в GoldenGen подключается старый WinGL.Util, в Mvk.Core.Tests новый Mvk.Core.Maths
// (через <Using Include="..." /> в csproj). Так обе стороны считают один и тот же код.
namespace GoldenGen
{
    /// <summary>
    /// Эталон математики: хэши результатов на большой выборке аргументов.
    /// Новый Mvk.Core.Maths обязан давать те же биты, что старый WinGL.Util (от этого зависит генерация).
    /// </summary>
    public static class MathGolden
    {
        /// <summary>
        /// Посчитать хэши всех проверяемых функций: имя → sha256
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Compute()
        {
            yield return _Pair("Sin", _HashFloat(i => Glm.Sin(_Arg(i))));
            yield return _Pair("Cos", _HashFloat(i => Glm.Cos(_Arg(i))));
            yield return _Pair("Tan", _HashFloat(i => Glm.Tan(_Arg(i))));
            yield return _Pair("WrapAngleToPi", _HashFloat(i => Glm.WrapAngleToPi(_Arg(i))));
            yield return _Pair("Floor", _HashInt(i => Mth.Floor(_Arg(i))));
            yield return _Pair("Ceiling", _HashInt(i => Mth.Ceiling(_Arg(i))));
            yield return _Pair("Round", _HashInt(i => Mth.Round(_Arg(i))));
            yield return _Pair("Sqrt", _HashFloat(i => Mth.Sqrt(Math.Abs(_Arg(i)))));
            yield return _Pair("Normalize", _HashFloat(i =>
            {
                Vector3 v = Glm.Normalize(new Vector3(_Arg(i), _Arg(i + 7), _Arg(i + 13) + .5f));
                return v.X + v.Y * 3 + v.Z * 7;
            }));
            yield return _Pair("Ray", _HashFloat(i =>
            {
                Vector3 v = Glm.Ray(_Arg(i), _Arg(i + 3) * .25f);
                return v.X + v.Y * 3 + v.Z * 7;
            }));
            yield return _Pair("Mat4Rotate", _HashFloat(i =>
            {
                Mat4 m = Mat4.Identity();
                m.RotateX(_Arg(i));
                m.RotateY(_Arg(i + 1));
                m.RotateZ(_Arg(i + 2));
                m.Translate(_Arg(i + 3), 1, 2);
                float[] a = m.ToArray();
                float s = 0;
                for (int k = 0; k < a.Length; k++) s += a[k] * (k + 1);
                return s;
            }, 20000));
        }

        /// <summary>
        /// Записать эталон в файл
        /// </summary>
        public static void Write(string output)
        {
            using StreamWriter file = new StreamWriter(output);
            file.NewLine = "\n";
            file.WriteLine("# Эталон математики WinGL.Util (original dfbd814): имя;sha256 результатов");
            foreach (KeyValuePair<string, string> pair in Compute())
            {
                file.WriteLine(pair.Key + ";" + pair.Value);
            }
        }

        private static KeyValuePair<string, string> _Pair(string key, string value)
            => new KeyValuePair<string, string>(key, value);

        /// <summary>
        /// Аргумент выборки: от -1000 до 1000 с мелкими сдвигами
        /// </summary>
        private static float _Arg(int i) => (i - 100000) * 0.01f + i % 7 * 0.001f;

        private static string _HashFloat(Func<int, float> f, int count = 200000)
        {
            byte[] bytes = new byte[count * 4];
            for (int i = 0; i < count; i++)
            {
                BitConverter.TryWriteBytes(new Span<byte>(bytes, i * 4, 4), f(i));
            }
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }

        private static string _HashInt(Func<int, int> f, int count = 200000)
        {
            byte[] bytes = new byte[count * 4];
            for (int i = 0; i < count; i++)
            {
                BitConverter.TryWriteBytes(new Span<byte>(bytes, i * 4, 4), f(i));
            }
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }
    }
}
