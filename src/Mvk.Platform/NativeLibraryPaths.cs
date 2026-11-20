using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Silk.NET.Core.Loader;

namespace Mvk.Platform
{
    /// <summary>
    /// Поиск нативных библиотек Silk.NET (GLFW, OpenAL, Shaderc) в папке runtimes/&lt;ОС&gt;-&lt;архитектура&gt;/native рядом
    /// с программой. Свой поиск Silk.NET определяет папку по графу RID, и на новых дистрибутивах (Ubuntu 26.04)
    /// не находит её: игра требовала системные библиотеки. Вызывать в начале Main, до первого обращения к Silk.NET.
    /// </summary>
    public static class NativeLibraryPaths
    {
        private static bool _registered;

        public static void Register()
        {
            if (_registered || PathResolver.Default is not DefaultPathResolver resolver) return;
            _registered = true;
            resolver.Resolvers.Insert(0, _Runtimes);
        }

        private static IEnumerable<string> _Runtimes(string name)
        {
            string? arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                Architecture.X86 => "x86",
                Architecture.Arm => "arm",
                _ => null
            };
            if (arch == null) yield break;
            string os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
            string path = Path.Combine(AppContext.BaseDirectory, "runtimes", os + "-" + arch, "native", Path.GetFileName(name));
            if (File.Exists(path)) yield return path;
        }
    }
}
