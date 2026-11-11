using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Mvk.Engine.Tests
{
    /// <summary>
    /// Проверка правил зависимостей из CLAUDE.md:
    /// ядро, движок, контент и сервер не должны зависеть от графики, платформы и клиента.
    /// </summary>
    public class ArchitectureTests
    {
        /// <summary>
        /// Префиксы того, от чего серверная часть зависеть не может
        /// </summary>
        private static readonly string[] _forbidden =
        {
            "Mvk.Rendering", "Mvk.Platform", "Mvk.Engine.Client", "Mvk.Game.Client", "Silk.NET"
        };

        public static IEnumerable<object[]> HeadlessProjects() => new[]
        {
            new object[] { "src/Mvk.Core/Mvk.Core.csproj" },
            new object[] { "src/Mvk.Engine/Mvk.Engine.csproj" },
            new object[] { "src/Mvk.Game/Mvk.Game.csproj" },
            new object[] { "src/Hosts/Mvk.Server/Mvk.Server.csproj" }
        };

        [Theory]
        [MemberData(nameof(HeadlessProjects))]
        public void HeadlessProjectHasNoGraphicsReferences(string projectPath)
        {
            XDocument project = XDocument.Load(Path.Combine(_FindSolutionRoot(), projectPath));
            List<string> references = project.Descendants()
                .Where(e => e.Name.LocalName is "ProjectReference" or "PackageReference")
                .Select(e => Path.GetFileNameWithoutExtension((string?)e.Attribute("Include") ?? ""))
                .ToList();

            string[] violations = references
                .Where(r => _forbidden.Any(f => r.StartsWith(f, StringComparison.Ordinal)))
                .ToArray();

            Assert.True(violations.Length == 0,
                $"{projectPath} ссылается на запрещённые проекты: {string.Join(", ", violations)}");
        }

        [Fact]
        public void EngineAssemblyHasNoGraphicsReferences()
        {
            Assembly engine = Assembly.Load(new AssemblyName("Mvk.Engine"));
            string[] violations = engine.GetReferencedAssemblies()
                .Select(a => a.Name ?? "")
                .Where(n => _forbidden.Any(f => n.StartsWith(f, StringComparison.Ordinal)))
                .ToArray();

            Assert.Empty(violations);
        }

        /// <summary>
        /// Найти корень решения (папку с MvkClaude.slnx)
        /// </summary>
        private static string _FindSolutionRoot()
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MvkClaude.slnx")))
            {
                dir = dir.Parent;
            }
            return dir?.FullName ?? throw new DirectoryNotFoundException("Не найден MvkClaude.slnx");
        }
    }
}
