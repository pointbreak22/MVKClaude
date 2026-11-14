using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mvk.Game.Tests
{
    /// <summary>
    /// [ПОВЕДЕНИЕ] known-issues п. 9 и 11: у всех ключей перевода в коде (L.S, L.T) есть русская и английская строка
    /// в assets/Mvk2Texts. В оригинале не было русских строк команд (погода, время, телепорт, режим игры) и части
    /// экранов, а английские файлы были пусты: в чат и на экран попадал ключ.
    /// </summary>
    public class TranslationTests
    {
        /// <summary>
        /// Ключи, которые не проверяются: "Slot" собирается с именем слота (SlotHat и т. п.), "InTable" только
        /// в закомментированном коде, ключ RegenChunk содержит ':' и не может быть записан в файл перевода
        /// </summary>
        private static readonly HashSet<string> _skip = new HashSet<string>
        {
            "Slot", "InTable", "RegenChunk[{0}:{1}] {2:0.00}ms"
        };

        [Theory]
        [InlineData("Ru")]
        [InlineData("En")]
        public void EveryKeyTranslated(string language)
        {
            string root = _Root();
            var used = new Dictionary<string, HashSet<string>> { ["S"] = new(), ["T"] = new() };
            foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
            {
                string[] parts = file.Split(Path.DirectorySeparatorChar);
                if (parts.Contains("bin") || parts.Contains("obj")) continue;
                foreach (Match m in Regex.Matches(File.ReadAllText(file), "\\bL\\.(S|T)\\(\"([^\"]+)\""))
                {
                    used[m.Groups[1].Value].Add(m.Groups[2].Value);
                }
            }
            string texts = Path.Combine(root, "assets", "Mvk2Texts");
            var missing = used["S"].Except(_Keys(Path.Combine(texts, language + "Server.txt")))
                .Concat(used["T"].Except(_Keys(Path.Combine(texts, language + ".txt"))))
                .Where(k => !_skip.Contains(k))
                .OrderBy(k => k)
                .ToList();
            Assert.True(missing.Count == 0, $"Нет строки {language}: " + string.Join(", ", missing));
        }

        /// <summary>
        /// Английские файлы содержат те же ключи, что русские
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("Server")]
        [InlineData("Items")]
        public void EnglishHasRussianKeys(string file)
        {
            string texts = Path.Combine(_Root(), "assets", "Mvk2Texts");
            var missing = _Keys(Path.Combine(texts, "Ru" + file + ".txt"))
                .Except(_Keys(Path.Combine(texts, "En" + file + ".txt"))).OrderBy(k => k).ToList();
            Assert.True(missing.Count == 0, "Нет английской строки: " + string.Join(", ", missing));
        }

        private static HashSet<string> _Keys(string path)
            => File.ReadAllLines(path)
                .Where(l => !l.TrimStart().StartsWith('#') && l.IndexOf(':') > 0)
                .Select(l => l.Substring(0, l.IndexOf(':')))
                .ToHashSet();

        private static string _Root()
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MvkClaude.slnx"))) dir = dir.Parent;
            Assert.NotNull(dir);
            return dir.FullName;
        }
    }
}
