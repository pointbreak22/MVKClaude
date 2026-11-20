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
    /// не находит её: игра требовала системные библиотеки. На Linux и macOS библиотеки папки загружаются заранее
    /// (GLFW грузится по имени). Вызывать в начале Main, до первого обращения к Silk.NET.
    /// </summary>
    public static class NativeLibraryPaths
    {
        private static bool _registered;

        public static void Register()
        {
            if (_registered) return;
            _registered = true;
            if (PathResolver.Default is DefaultPathResolver resolver) resolver.Resolvers.Insert(0, _Runtimes);
            if (OperatingSystem.IsWindows()) return;
            // GLFW Silk.NET грузит по имени (dlopen("libglfw.so.3")) мимо поиска путей. Библиотека, уже загруженная
            // по полному пути, находится по своему SONAME, поэтому заранее грузим всё из runtimes/<rid>/native
            string? dir = _Directory();
            if (dir == null || !Directory.Exists(dir)) return;
            foreach (string file in Directory.GetFiles(dir))
            {
                NativeLibrary.TryLoad(file, out _);
            }
        }

        private static string? _Directory()
        {
            string? arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                Architecture.X86 => "x86",
                Architecture.Arm => "arm",
                _ => null
            };
            if (arch == null) return null;
            string os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
            return Path.Combine(AppContext.BaseDirectory, "runtimes", os + "-" + arch, "native");
        }

        private static IEnumerable<string> _Runtimes(string name)
        {
            string? dir = _Directory();
            if (dir == null) yield break;
            string path = Path.Combine(dir, Path.GetFileName(name));
            if (File.Exists(path)) yield return path;
        }
    }
}
