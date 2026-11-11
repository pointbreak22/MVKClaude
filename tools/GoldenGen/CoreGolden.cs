using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
#if GOLDEN_ORIGINAL
using Vge.Json;
using Vge.NBT;
using Vge.Util;
#else
using Mvk.Core.Json;
using Mvk.Core.Nbt;
using Mvk.Core.Util;
#endif

// Файл общий для GoldenGen (старый Vge, константа GOLDEN_ORIGINAL) и Mvk.Core.Tests (новый Mvk.Core).
namespace GoldenGen
{
    /// <summary>
    /// Эталон утилит Core: случайные числа (от них зависит генерация), формат NBT (сохранения
    /// и сеть) и разбор Json (модели и настройки). Новый код обязан давать те же байты.
    /// </summary>
    public static class CoreGolden
    {
        /// <summary>
        /// Пример Json в стиле файлов игры: комментарии, вложенные объекты, массивы, числа
        /// </summary>
        public const string JsonSample =
            "# комментарий\n" +
            "{\n" +
            "  \"Name\": \"Камень\", \"Hardness\": 1.5, \"Solid\": true, \"Count\": -42,\n" +
            "  # ещё комментарий\n" +
            "  \"Textures\": [ \"stone\", \"stone_top\", 3, 0.25 ],\n" +
            "  \"Elements\": [ { \"From\": [0, 0, 0], \"To\": [16, 16, 16], \"Faces\": { \"Up\": { \"Uv\": [0,0,16,16] } } },\n" +
            "                { \"From\": [4, 0, 4], \"To\": [12, 8, 12] } ],\n" +
            "  \"Empty\": { }, \"EmptyArray\": [ ],\n" +
            "  \"Text\": \"a b\tc\"\n" +
            "}\n";

        public static IEnumerable<KeyValuePair<string, string>> Compute()
        {
            foreach (long seed in new long[] { 0, 1, 2, 20241004, -987654321, long.MaxValue, long.MinValue })
            {
                yield return _Pair("Rand" + seed, _Hash(w => _Rand(w, seed)));
            }
            yield return _Pair("NbtWrite", _Hash(w => w.Write(_NbtBytes(_NbtSample()))));
            yield return _Pair("NbtRoundTrip", _Hash(w =>
            {
                NBTStream input = new NBTStream();
                byte[] bytes = _NbtBytes(_NbtSample());
                input.Write(bytes, 0, bytes.Length);
                input.Position = 0;
                TagCompound read = new TagCompound();
                read.Read(input);
                w.Write(_NbtBytes(read));
                w.Write(_NbtBytes((TagCompound)read.Copy()));
                w.Write(read.GetInt("int"));
                w.Write(read.GetString("str"));
                w.Write(read.GetFloat("float"));
                w.Write(read.GetTagList("list", 5).TagCount());
            }));
            yield return _Pair("Json", _Hash(w =>
            {
                JsonRead json = new JsonRead("sample", JsonSample);
                _Dump(w, json.Compound);
                w.Write(json.Compound.GetString("name"));
                w.Write(json.Compound.GetFloat("Hardness"));
                w.Write(json.Compound.GetInt("Count"));
                w.Write(json.Compound.GetBool("Solid"));
                foreach (float f in json.Compound.GetArray("Textures").ToArrayFloat()) w.Write(f);
            }));
        }

        public static void Write(string output)
        {
            using StreamWriter file = new StreamWriter(output);
            file.NewLine = "\n";
            file.WriteLine("# Эталон утилит Vge (original dfbd814): Rand, NBT, Json; имя;sha256");
            foreach (KeyValuePair<string, string> pair in Compute())
            {
                file.WriteLine(pair.Key + ";" + pair.Value);
            }
        }

        private static void _Rand(BinaryWriter w, long seed)
        {
            Rand rand = new Rand(seed);
            for (int i = 0; i < 2000; i++)
            {
                w.Write(rand.Next());
                w.Write(rand.Next(1 + i % 50));
                w.Write(rand.Next(1 << (i % 31)));
                w.Write(rand.Next(1000003));
                w.Write(rand.NextLong());
                w.Write(rand.NextBool());
                w.Write(rand.NextFloat());
                w.Write(rand.NextDouble());
                w.Write(rand.NextGaussian());
            }
            byte[] bytes = new byte[37];
            rand.NextBytes(bytes);
            w.Write(bytes);
            rand.SetSeed(seed ^ 12345);
            w.Write(rand.Next(16));
        }

        private static TagCompound _NbtSample()
        {
            TagCompound nbt = new TagCompound();
            nbt.SetByte("byte", 200);
            nbt.SetShort("short", -1234);
            nbt.SetInt("int", 123456789);
            nbt.SetLong("long", -1234567890123456789L);
            nbt.SetFloat("float", -3.14159f);
            nbt.SetString("str", "Малювеки 2");
            nbt.SetBool("bool", true);
            nbt.SetByteArray("bytes", new byte[] { 0, 1, 2, 255, 128 });
            nbt.SetIntArray("ints", new int[] { int.MinValue, -1, 0, 1, int.MaxValue });
            TagList list = new TagList(1.5f, -2.25f, 1e10f);
            nbt.SetTag("list", list);
            TagCompound inner = new TagCompound();
            inner.SetString("empty", "");
            inner.SetInt("x", -7);
            TagList compounds = new TagList();
            compounds.AppendTag(inner);
            compounds.AppendTag(new TagCompound());
            nbt.SetTag("inner", inner.Copy());
            nbt.SetTag("compounds", compounds);
            return nbt;
        }

        private static byte[] _NbtBytes(TagCompound nbt)
        {
            NBTStream stream = new NBTStream();
            nbt.Write(stream);
            return stream.ToArray();
        }

        private static void _Dump(BinaryWriter w, IJson json)
        {
            w.Write(json.GetIdType());
            if (json is JsonValue value)
            {
                w.Write(value.Value ?? "<null>");
            }
            else if (json is JsonArray array)
            {
                w.Write(array.Items == null ? -1 : array.Items.Length);
                if (array.Items != null)
                {
                    foreach (IJsonArray item in array.Items) _Dump(w, (IJson)item);
                }
            }
            else if (json is JsonCompound compound)
            {
                w.Write(compound.Items == null ? -1 : compound.Items.Length);
                if (compound.Items != null)
                {
                    foreach (JsonKeyValue kv in compound.Items)
                    {
                        w.Write(kv.Key);
                        _Dump(w, kv.Value);
                    }
                }
            }
        }

        private static KeyValuePair<string, string> _Pair(string key, string value)
            => new KeyValuePair<string, string>(key, value);

        private static string _Hash(Action<BinaryWriter> write)
        {
            using MemoryStream stream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                write(writer);
            }
            return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
        }
    }
}
