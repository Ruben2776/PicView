using BenchmarkDotNet.Attributes;
using ZLinq;
using ZLinq.Linq;
using ZLinq.Traversables;

namespace PicView.Benchmarks.StartupBenchmarks;

/// <summary>
/// Benchmarks determining language file paths.
/// For LanguageModel file loading and deserialization benchmarks (bytes vs lines/text, sync vs async), see <see cref="TranslationBenchmarks"/>.
/// </summary>
[MemoryDiagnoser] // track allocations
public class LanguageBenchmark
{
    [Benchmark]
    public void DetermineLanguageFilePath()
    {
        DetermineLanguageFilePath("en");
    }

    [Benchmark]
    public void DetermineLanguageFilePathSpan()
    {
        DetermineLanguageFilePathSpan("en");
    }

    [Benchmark]
    public void DetermineLanguageFilePathForeach()
    {
        DetermineLanguageFilePathForeach("en");
    }

    [Benchmark]
    public void DetermineLanguageFilePathSpanForeach()
    {
        DetermineLanguageFilePathSpanForeach("en");
    }

    private string DetermineLanguageFilePathForeach(string isoLanguageCode)
    {
        var languagesDirectory = GetLanguagesDirectory;

        foreach (var file in GetLanguages(languagesDirectory))
        {
            if (file.Name.StartsWith(isoLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                return file.FullName; // ✅ early exit as soon as we find a match
            }
        }

        return Path.Combine(languagesDirectory, "en.json"); // fallback
    }

    private string DetermineLanguageFilePathSpanForeach(string isoLanguageCode)
    {
        var languagesDirectory = GetLanguagesDirectory;

        foreach (var file in GetLanguages(languagesDirectory))
        {
            if (FileNameMatches(file.Name, isoLanguageCode))
            {
                return file.FullName; // ✅ early exit
            }
        }

        return Path.Combine(languagesDirectory, "en.json");
    }

    private string DetermineLanguageFilePath(string isoLanguageCode)
    {
        var languagesDirectory = GetLanguagesDirectory;
        var matchingFile = GetLanguages(languagesDirectory).Where(x => x.Name.StartsWith(isoLanguageCode))
            .FirstOrDefault();
        return matchingFile?.FullName ?? Path.Combine(languagesDirectory, "en.json");
    }

    private string DetermineLanguageFilePathSpan(string isoLanguageCode)
    {
        var languagesDirectory = GetLanguagesDirectory;

        if (string.IsNullOrEmpty(isoLanguageCode))
        {
            return Path.Combine(languagesDirectory, "en.json");
        }

        var matchingFile = GetLanguages(languagesDirectory)
            .FirstOrDefault(file => FileNameMatches(file.Name, isoLanguageCode));

        return matchingFile?.FullName ?? Path.Combine(languagesDirectory, "en.json");
    }

    private bool FileNameMatches(string fileName, string isoLanguageCode)
    {
        var fileSpan = fileName.AsSpan();
        var isoSpan = isoLanguageCode.AsSpan();

        if (fileSpan.Length < isoSpan.Length)
        {
            return false;
        }

        return fileSpan[..isoSpan.Length]
            .Equals(isoSpan, StringComparison.OrdinalIgnoreCase);
    }

    private ValueEnumerable<Where<Children<FileSystemInfoTraverser, FileSystemInfo>, FileSystemInfo>, FileSystemInfo>
        GetLanguages(string languagesDirectory)
    {
        return new DirectoryInfo(languagesDirectory)
            .ChildrenAndSelf()
            .Where(x => x.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase));
    }

    private string GetLanguagesDirectory
    {
        get
        {
            // Find the 'src' directory starting from the app base directory
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !dir.Name.Equals("src", StringComparison.OrdinalIgnoreCase))
            {
                dir = dir.Parent;
            }

            if (dir == null)
            {
                throw new DirectoryNotFoundException(
                    "Could not locate 'src' directory starting from the application base directory.");
            }

            // Build the path from 'src'
            var languagesPath = Path.Combine(dir.FullName, "PicView.Core", "Config", "Languages");
            var fullPath = Path.GetFullPath(languagesPath);

            if (!Directory.Exists(fullPath))
            {
                throw new DirectoryNotFoundException($"Languages directory not found at '{fullPath}'.");
            }

            return fullPath;
        }
    }
}

/*

* Summary *

BenchmarkDotNet v0.15.2, Windows 10 (10.0.19045.6216/22H2/2022Update)
AMD Ryzen 7 9800X3D 4.70GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100-preview.7.25380.108
  [Host]     : .NET 10.0.0 (10.0.25.38108), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 10.0.0 (10.0.25.38108), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method                               | Mean     | Error    | StdDev   | Gen0   | Allocated |
|------------------------------------- |---------:|---------:|---------:|-------:|----------:|
| DetermineLanguageFilePath            | 12.12 us | 0.075 us | 0.067 us | 0.0763 |   4.36 KB |
| DetermineLanguageFilePathSpan        | 11.97 us | 0.034 us | 0.032 us | 0.0763 |    4.3 KB |
| DetermineLanguageFilePathForeach     | 12.10 us | 0.057 us | 0.047 us | 0.0763 |   4.27 KB |
| DetermineLanguageFilePathSpanForeach | 12.01 us | 0.063 us | 0.059 us | 0.0763 |   4.27 KB |

*/