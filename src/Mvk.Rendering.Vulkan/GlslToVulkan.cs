using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Mvk.Rendering.Vulkan
{
    /// <summary>
    /// Uniform шейдера в блоке std140 (переводчик собирает свободные uniform GLSL 330 в один блок)
    /// </summary>
    internal readonly record struct UniformSlot(string Name, string Type, int ArraySize, int Offset, int Stride);

    /// <summary>
    /// Перевод шейдеров OpenGL (GLSL 330 core, как в ассетах оригинала) в GLSL 450 для Vulkan без правки ассетов:
    /// - свободные uniform вершинного и фрагментного шейдеров собираются в общий блок std140 (binding 0);
    /// - сэмплеры получают binding 1 + номер в ShaderDescription.Samplers (как текстурные блоки GL);
    /// - входы вершинного шейдера получают location по ShaderDescription.Attributes (как glBindAttribLocation);
    /// - выходы вершинного и входы фрагментного шейдера получают location по имени;
    /// - глубина переводится из -1..1 (GL) в 0..1 (Vulkan) в конце вершинного шейдера.
    /// </summary>
    internal static class GlslToVulkan
    {
        private static readonly Regex _uniform = new(@"^\s*uniform\s+(\w+)\s+(\w+)\s*(?:\[\s*(\d+)\s*\])?\s*;\s*$", RegexOptions.Multiline);
        private static readonly Regex _io = new(@"^(\s*)((?:flat|smooth|noperspective)\s+)?(in|out)\s+(\w+)\s+(\w+)\s*(\[\s*\d+\s*\])?\s*;", RegexOptions.Multiline);
        private static readonly Regex _version = new(@"^\s*#version[^\n]*", RegexOptions.Multiline);
        private static readonly Regex _main = new(@"\bvoid\s+main\s*\(");

        /// <summary>Результат перевода</summary>
        public sealed class Result
        {
            public string Vertex = "";
            public string Fragment = "";
            public readonly List<UniformSlot> Uniforms = new();
            /// <summary>Размер блока uniform в байтах (кратен 16, не меньше 16)</summary>
            public int BlockSize;
            /// <summary>Сколько сэмплеров (binding 1..N)</summary>
            public int SamplerCount;
        }

        public static Result Translate(ShaderDescription d)
        {
            var result = new Result();
            var samplers = new List<string>(d.Samplers);
            string vs = Strip(d.VertexSource);
            string fs = Strip(d.FragmentSource);

            // Uniform обоих шейдеров: имя -> (тип, размер массива), порядок первого появления
            var order = new List<(string Name, string Type, int Array)>();
            var seen = new HashSet<string>();
            void Collect(string src)
            {
                foreach (Match m in _uniform.Matches(src))
                {
                    string type = m.Groups[1].Value, name = m.Groups[2].Value;
                    if (type.StartsWith("sampler", StringComparison.Ordinal))
                    {
                        if (!samplers.Contains(name)) samplers.Add(name);
                        continue;
                    }
                    if (seen.Add(name))
                    {
                        order.Add((name, type, m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0));
                    }
                }
            }
            Collect(vs);
            Collect(fs);
            result.SamplerCount = samplers.Count;

            // Раскладка std140
            int offset = 0;
            var block = new StringBuilder();
            block.Append("layout(std140, set = 0, binding = 0) uniform MvkUniforms\n{\n");
            foreach (var (name, type, array) in order)
            {
                (int size, int align) = _Std140(type);
                int stride = size;
                if (array > 0)
                {
                    align = Math.Max(align, 16);
                    stride = (size + 15) & ~15;
                }
                offset = (offset + align - 1) / align * align;
                result.Uniforms.Add(new UniformSlot(name, type, array, offset, stride));
                offset += array > 0 ? stride * array : size;
                block.Append("    ").Append(type).Append(' ').Append(name);
                if (array > 0) block.Append('[').Append(array).Append(']');
                block.Append(";\n");
            }
            if (order.Count == 0) block.Append("    float mvk_unused;\n");
            block.Append("};\n");
            result.BlockSize = Math.Max(16, (offset + 15) & ~15);

            string Uniforms(string src)
            {
                src = _uniform.Replace(src, m =>
                {
                    string type = m.Groups[1].Value, name = m.Groups[2].Value;
                    if (!type.StartsWith("sampler", StringComparison.Ordinal)) return "";
                    return $"layout(set = 0, binding = {1 + samplers.IndexOf(name)}) uniform {type} {name};";
                });
                // Блок сразу после #version
                Match v = _version.Match(src);
                return "#version 450\n" + block + src.Substring(v.Index + v.Length);
            }

            // Вершинный шейдер: входы по Attributes, выходы по порядку
            var varyings = new Dictionary<string, int>();
            int nextVarying = 0;
            vs = _io.Replace(vs, m =>
            {
                string name = m.Groups[5].Value;
                string type = m.Groups[4].Value;
                if (m.Groups[3].Value == "in")
                {
                    int loc = -1;
                    for (int i = 0; i < d.Attributes.Count; i++) if (d.Attributes[i] == name) loc = i;
                    if (loc < 0) throw new InvalidOperationException($"Шейдер {d.Name}: вход {name} не задан в Attributes");
                    return $"{m.Groups[1].Value}layout(location = {loc}) in {type} {name}{m.Groups[6].Value};";
                }
                int location = nextVarying;
                varyings[name] = location;
                nextVarying += _Locations(type) * (m.Groups[6].Success ? _ArrayLength(m.Groups[6].Value) : 1);
                return $"{m.Groups[1].Value}layout(location = {location}) {m.Groups[2].Value}out {type} {name}{m.Groups[6].Value};";
            });
            vs = vs.Replace("gl_VertexID", "gl_VertexIndex");
            vs = _main.Replace(vs, "void mvk_main(", 1)
                + "\nvoid main()\n{\n    mvk_main();\n    gl_Position.z = (gl_Position.z + gl_Position.w) * 0.5;\n}\n";

            // Фрагментный шейдер: входы по именам выходов вершинного, выход цвета location 0
            int nextOut = 0;
            fs = _io.Replace(fs, m =>
            {
                string name = m.Groups[5].Value;
                string type = m.Groups[4].Value;
                if (m.Groups[3].Value == "in")
                {
                    if (!varyings.TryGetValue(name, out int loc))
                    {
                        // Вход без выхода в вершинном шейдере: отдельный location, значение не определено, как в GL
                        loc = nextVarying++;
                    }
                    return $"{m.Groups[1].Value}layout(location = {loc}) {m.Groups[2].Value}in {type} {name}{m.Groups[6].Value};";
                }
                return $"{m.Groups[1].Value}layout(location = {nextOut++}) out {type} {name}{m.Groups[6].Value};";
            });

            result.Vertex = Uniforms(vs);
            result.Fragment = Uniforms(fs);
            return result;
        }

        /// <summary>Комментарии мешают регулярным выражениям: убираем их, не меняя код</summary>
        private static string Strip(string src)
        {
            src = Regex.Replace(src, @"/\*.*?\*/", "", RegexOptions.Singleline);
            src = Regex.Replace(src, @"//[^\n]*", "");
            return src.Replace("\r\n", "\n");
        }

        private static int _ArrayLength(string brackets) => int.Parse(brackets.Trim('[', ']', ' '));

        /// <summary>Сколько location занимает тип входа или выхода</summary>
        private static int _Locations(string type) => type switch
        {
            "mat4" or "mat4x3" or "mat4x4" => 4,
            "mat3" or "mat3x3" => 3,
            "mat2" => 2,
            _ => 1
        };

        /// <summary>Размер и выравнивание типа в std140</summary>
        private static (int Size, int Align) _Std140(string type) => type switch
        {
            "float" or "int" or "uint" or "bool" => (4, 4),
            "vec2" or "ivec2" => (8, 8),
            "vec3" or "ivec3" => (12, 16),
            "vec4" or "ivec4" => (16, 16),
            "mat4" or "mat4x4" => (64, 16),
            // mat4x3: 4 столбца vec3, каждый выровнен на 16
            "mat4x3" => (64, 16),
            "mat3" => (48, 16),
            _ => throw new NotSupportedException("Тип uniform " + type)
        };
    }
}
