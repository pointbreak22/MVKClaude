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
            // Матрицы: все 16 чисел результата (перевод Mat4 в значимый тип, improvements R19)
            yield return _Pair("Mat4Multiply", _HashMat(i =>
            {
                Mat4 m = _Mat(i);
                m.Multiply(_Mat(i + 5));
                return m;
            }));
            yield return _Pair("Mat4MultiplySelf", _HashMat(i =>
            {
                Mat4 m = _Mat(i);
                m.Multiply(m);
                return m;
            }));
            yield return _Pair("Mat4Operator", _HashMat(i => _Mat(i) * _Mat(i + 3) * _Arg(i + 9)));
            yield return _Pair("Mat4RotateQuat", _HashMat(i =>
            {
                Mat4 m = _Mat(i);
                Vector3 v = Glm.Normalize(new Vector3(_Arg(i), _Arg(i + 7), _Arg(i + 13) + .5f));
                m.RotateQuat(v.X * .6f, v.Y * .6f, v.Z * .6f, .8f);
                return m;
            }));
            yield return _Pair("Mat4RotateAxis", _HashMat(i =>
            {
                Mat4 m = _Mat(i);
                Vector3 v = Glm.Normalize(new Vector3(_Arg(i + 2), _Arg(i + 5), _Arg(i + 11) + .5f));
                m.Rotate(_Arg(i + 1), v.X, v.Y, v.Z);
                m.RotateXYZ(_Arg(i + 3), _Arg(i + 4), _Arg(i + 6));
                m.RotateZYX(_Arg(i + 8), _Arg(i + 10), _Arg(i + 12));
                m.Scale(1.5f, _Arg(i) * .01f, 2f);
                return m;
            }));
            yield return _Pair("Mat4Clear", _HashMat(i =>
            {
                Mat4 m = _Mat(i);
                m.Clear();
                m.Translate(_Arg(i), _Arg(i + 1), _Arg(i + 2));
                return m;
            }));
            yield return _Pair("Mat4Inverse", _HashMat(i => Glm.Inverse(_Mat(i))));
            yield return _Pair("Mat4LookAt", _HashMat(i =>
            {
                Vector3 eye = new Vector3(_Arg(i), _Arg(i + 1) * .5f, _Arg(i + 2));
                Vector3 front = Glm.Ray(_Arg(i + 3), _Arg(i + 4) * .25f);
                Mat4 m = Glm.PerspectiveFov(1.2f + i % 5 * .1f, 1280, 720 + i % 300, 0.01f, 1450);
                m.Multiply(Glm.LookAt(eye, eye + front, new Vector3(0, 1, 0)));
                return m;
            }));
            yield return _Pair("Mat4Ortho", _HashMat(i => Glm.Ortho(-_Arg(i), _Arg(i) + 1, -_Arg(i + 1), _Arg(i + 1) + 1, -500, 500)));
            yield return _Pair("Mat4Vector", _HashFloat(i =>
            {
                Vector4 v = _Mat(i) * new Vector4(_Arg(i), _Arg(i + 1), _Arg(i + 2), 1);
                return v.X + v.Y * 3 + v.Z * 7 + v.W * 11;
            }, 20000));
            yield return _Pair("Mat4Array4x3", _HashFloat(i =>
            {
                float[] a = new float[24];
                _Mat(i).ConvArray4x3(a, 12);
                float s = 0;
                for (int k = 0; k < a.Length; k++) s += a[k] * (k + 1);
                return s;
            }, 20000));
        }

        /// <summary>
        /// Матрица выборки: поворот, перенос и масштаб от аргумента
        /// </summary>
        private static Mat4 _Mat(int i)
        {
            Mat4 m = Mat4.Identity();
            m.RotateX(_Arg(i));
            m.RotateY(_Arg(i + 1) * .3f);
            m.RotateZ(_Arg(i + 2) * .7f);
            m.Translate(_Arg(i + 3), _Arg(i + 4) * .5f, 2);
            m.Scale(1f + i % 3, 1f, 1f + i % 5 * .5f);
            return m;
        }

        private static string _HashMat(Func<int, Mat4> f, int count = 20000)
        {
            byte[] bytes = new byte[count * 64];
            float[] a = new float[16];
            for (int i = 0; i < count; i++)
            {
                f(i).ConvArray(a);
                for (int k = 0; k < 16; k++)
                {
                    BitConverter.TryWriteBytes(new Span<byte>(bytes, i * 64 + k * 4, 4), a[k]);
                }
            }
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
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
