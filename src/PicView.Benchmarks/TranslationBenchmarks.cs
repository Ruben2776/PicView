using System.Globalization;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using PicView.Core.DebugTools;
using PicView.Core.Localization;
using ZLinq;

namespace PicView.Benchmarks;

[MemoryDiagnoser]
public class TranslationBenchmarks
{
    private string _languageFilePath = string.Empty;
    private static LanguageModel DummyTranslation { get; set; } = new();

    [GlobalSetup]
    public void Setup()
    {
        SetDefaults();
        _languageFilePath = ResolveLanguageFilePath();
    }

    [Benchmark]
    public LanguageModel? ReadAllBytes()
    {
        var bytes = File.ReadAllBytes(_languageFilePath);
        return JsonSerializer.Deserialize(bytes, LanguageSourceGenerationContext.Default.LanguageModel);
    }

    [Benchmark]
    public async ValueTask<LanguageModel?> ReadAllBytesAsync()
    {
        var bytes = await File.ReadAllBytesAsync(_languageFilePath).ConfigureAwait(false);
        return JsonSerializer.Deserialize(bytes, LanguageSourceGenerationContext.Default.LanguageModel);
    }

    [Benchmark]
    public LanguageModel? ReadAllText()
    {
        var text = File.ReadAllText(_languageFilePath);
        return JsonSerializer.Deserialize(text, LanguageSourceGenerationContext.Default.LanguageModel);
    }

    [Benchmark]
    public async ValueTask<LanguageModel?> ReadAllTextAsync()
    {
        var text = await File.ReadAllTextAsync(_languageFilePath).ConfigureAwait(false);
        return JsonSerializer.Deserialize(text, LanguageSourceGenerationContext.Default.LanguageModel);
    }

    [Benchmark]
    public LanguageModel? ReadAllLines()
    {
        var lines = File.ReadAllLines(_languageFilePath);
        var text = string.Concat(lines);
        return JsonSerializer.Deserialize(text, LanguageSourceGenerationContext.Default.LanguageModel);
    }

    [Benchmark]
    public async ValueTask<LanguageModel?> ReadAllLinesAsync()
    {
        var lines = await File.ReadAllLinesAsync(_languageFilePath).ConfigureAwait(false);
        var text = string.Concat(lines);
        return JsonSerializer.Deserialize(text, LanguageSourceGenerationContext.Default.LanguageModel);
    }

    [Benchmark]
    public LanguageModel? Stream()
    {
        using var stream = File.OpenRead(_languageFilePath);
        return JsonSerializer.Deserialize(stream, LanguageSourceGenerationContext.Default.LanguageModel);
    }

    [Benchmark]
    public async ValueTask<LanguageModel?> StreamAsync()
    {
        await using var stream = File.OpenRead(_languageFilePath);
        return await JsonSerializer.DeserializeAsync(stream, LanguageSourceGenerationContext.Default.LanguageModel).ConfigureAwait(false);
    }

    [Benchmark]
    public LanguageModel TranslationManager_GetLanguageModelSync()
    {
        return TranslationManager.GetLanguageModel(CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public async ValueTask<bool> TranslationManager_LoadLanguageAsync()
    {
        return await LoadLanguage("en").ConfigureAwait(false);
    }

    [Benchmark]
    public async ValueTask TranslationManager_DetermineAndLoadLanguageAsync()
    {
        var isoLanguageCode = TranslationManager.DetermineCorrectLanguage();
        Settings.UIProperties.UserLanguage = isoLanguageCode;
        await LoadLanguage(isoLanguageCode).ConfigureAwait(false);
    }
    
    public static async ValueTask<bool> LoadLanguage(string isoLanguageCode)
    {
        var jsonLanguageFile = DetermineLanguageFilePath(isoLanguageCode);

        try
        {
            await LoadLanguageFromFileAsync(jsonLanguageFile).ConfigureAwait(false);
            return true;
        }
        catch (FileNotFoundException fnfEx)
        {
            DebugHelper.LogDebug(nameof(TranslationManager), nameof(LoadLanguage), fnfEx);
            return false;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(TranslationManager), nameof(LoadLanguage), ex);
            return false;
        }
    }
    
    private static async ValueTask LoadLanguageFromFileAsync(string filePath)
    {
        var jsonString = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var language = JsonSerializer.Deserialize(jsonString, typeof(LanguageModel), LanguageSourceGenerationContext.Default) as LanguageModel;
        if (language is not null)
        {
            DummyTranslation = language;
        }
    }
    
    private static string DetermineLanguageFilePath(string isoLanguageCode)
    {
        var matchingFile = TranslationManager.GetLanguages().Where(x =>
            x.Name.StartsWith(isoLanguageCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        return matchingFile?.FullName ?? Path.Combine(TranslationManager.GetLanguagesDirectory, "en.json");
    }

    private static string ResolveLanguageFilePath()
    {
        var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Languages", "en.json");
        if (File.Exists(localPath))
        {
            return localPath;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !dir.Name.Equals("src", StringComparison.OrdinalIgnoreCase))
        {
            dir = dir.Parent;
        }

        if (dir != null)
        {
            var srcPath = Path.Combine(dir.FullName, "PicView.Core", "Config", "Languages", "en.json");
            if (File.Exists(srcPath))
            {
                return srcPath;
            }
        }

        throw new FileNotFoundException("Could not locate 'en.json' language file.");
    }
}

/*

// * Summary *

BenchmarkDotNet v0.16.0-preview.1, Windows 10 (10.0.19045.6466/22H2/2022Update)
AMD Ryzen 7 9800X3D 4.70GHz, 1 CPU, 16 logical and 8 physical cores
Memory: 61.65 GB Total, 22.63 GB Available
.NET SDK 11.0.100-rc.1.26425.128
  [Host]     : .NET 11.0.0 (11.0.0-rc.1.26425.128, 11.0.26.42628), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 11.0.0 (11.0.0-rc.1.26425.128, 11.0.26.42628), X64 RyuJIT x86-64-v4


| Method                                           | Mean      | Error    | StdDev   | Median    | Gen0   | Gen1   | Allocated |
|------------------------------------------------- |----------:|---------:|---------:|----------:|-------:|-------:|----------:|
| ReadAllBytes                                     |  61.79 us | 1.120 us | 0.993 us |  61.63 us | 0.9155 |      - |   45.7 KB |
| ReadAllBytesAsync                                |  86.59 us | 1.729 us | 2.123 us |  85.48 us | 0.8545 |      - |   46.2 KB |
| ReadAllText                                      |  75.80 us | 1.508 us | 1.481 us |  75.57 us | 2.3193 | 0.3662 | 117.91 KB |
| ReadAllTextAsync                                 | 323.91 us | 2.532 us | 2.114 us | 323.73 us | 2.4414 |      - | 120.13 KB |
| ReadAllLines                                     |  81.58 us | 1.597 us | 2.797 us |  80.88 us | 2.4414 | 0.4883 | 124.13 KB |
| ReadAllLinesAsync                                | 334.68 us | 3.955 us | 3.699 us | 334.71 us | 2.4414 |      - | 127.14 KB |
| Stream                                           |  64.00 us | 1.216 us | 1.195 us |  63.85 us | 0.4883 |      - |  28.76 KB |
| StreamAsync                                      |  70.09 us | 1.370 us | 1.281 us |  69.93 us | 0.4883 |      - |  29.77 KB |
| TranslationManager_GetLanguageModelSync          |  94.39 us | 1.884 us | 4.728 us |  92.79 us | 1.2207 | 0.1221 |  62.49 KB |
| TranslationManager_LoadLanguageAsync             | 359.19 us | 5.470 us | 4.849 us | 358.86 us | 2.4414 | 0.4883 | 123.51 KB |
| TranslationManager_DetermineAndLoadLanguageAsync | 367.06 us | 4.124 us | 3.858 us | 365.71 us | 2.4414 | 0.4883 | 126.86 KB |

*/