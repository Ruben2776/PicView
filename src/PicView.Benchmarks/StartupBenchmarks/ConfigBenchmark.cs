using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using PicView.Core.Config;
using PicView.Core.Config.ConfigFileManagement;
using PicView.Core.DebugTools;

namespace PicView.Benchmarks.StartupBenchmarks;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class SettingsGenerationContext : JsonSerializerContext;

[MemoryDiagnoser] // track allocations
public class ConfigBenchmark
{
    private static AppSettings? Settings { get; set; }

    private static SettingsConfiguration? Configuration { get; set; }
    
    [Benchmark]
    public async ValueTask Initial()
    {
        await LoadSettingsAsync();
    }

    [Benchmark]
    public void Current()
    {
        LoadSettings();
    }

    [Benchmark]
    public void WithStreamReader()
    {
        LoadSettingsWithStreamReader();
    }

    [Benchmark]
    public void ReadAllText()
    {
        LoadSettingsAllText();
    }

    [Benchmark]
    public void ReadAllBytes()
    {
        LoadSettingsBytes();
    }

    [Benchmark]
    public async ValueTask ReadAllBytesAsync()
    {
        await LoadSettingsBytesAsync();
    }

    [Benchmark]
    public async ValueTask ReadAllLinesAsync()
    {
        await LoadSettingsLinesAsync();
    }

    public static async ValueTask<bool> LoadSettingsAsync()
    {
        try
        {
            // Load user config (User Profile or Program Path)
            Configuration ??= new SettingsConfiguration();
            var userPath = ConfigFileManager.ResolveDefaultConfigPath(Configuration);
            Configuration.CorrectPath = userPath;

            if (File.Exists(userPath))
            {
                await using var userStream = File.OpenRead(userPath);
                if (userStream.Length > 0)
                {
                    var settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                        userStream, SettingsGenerationContext.Default.AppSettings).ConfigureAwait(false);
                    Settings = EnsureSettings(settings);
                }
            }

            if (Settings is not null)
            {
                return true;
            }

            // Fallback to defaults if no user config found
            SetDefaults();
            return false;

        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(SettingsManager), nameof(LoadSettingsAsync), ex);
            SetDefaults();
            return false;
        }
    }

    public static bool LoadSettingsWithStreamReader()
    {
        try
        {
            // Load user config (User Profile or Program Path)
            Configuration ??= new SettingsConfiguration();
            var userPath = ConfigFileManager.ResolveDefaultConfigPath(Configuration);
            Configuration.CorrectPath = userPath;

            if (File.Exists(userPath))
            {
                using var streamReader = new StreamReader(userPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(
                    streamReader.ReadToEnd(), SettingsGenerationContext.Default.AppSettings);
                Settings = EnsureSettings(settings);
            }

            // Fallback to defaults if no user config found
            if (Settings is null)
            {
                Settings = GetDefaults();
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(SettingsManager), nameof(LoadSettingsAsync), ex);
            SetDefaults();
            return false;
        }
    }

    public static bool LoadSettingsAllText()
    {
        try
        {
            Configuration ??= new SettingsConfiguration();

            var userPath = ConfigFileManager.ResolveDefaultConfigPath(Configuration);
            Configuration.CorrectPath = userPath;

            if (File.Exists(userPath))
            {
                var json = File.ReadAllText(userPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(
                    json, SettingsGenerationContext.Default.AppSettings);
                Settings = EnsureSettings(settings);
            }

            if (Settings is null)
            {
                Settings = GetDefaults();
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(SettingsManager), nameof(LoadSettingsAsync), ex);
            SetDefaults();
            return false;
        }
    }

    public static bool LoadSettingsBytes()
    {
        try
        {
            Configuration ??= new SettingsConfiguration();

            var userPath = ConfigFileManager.ResolveDefaultConfigPath(Configuration);
            Configuration.CorrectPath = userPath;

            if (File.Exists(userPath))
            {
                var bytes = File.ReadAllBytes(userPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(
                    bytes, SettingsGenerationContext.Default.AppSettings);
                Settings = EnsureSettings(settings);
            }

            if (Settings is null)
            {
                Settings = GetDefaults();
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(SettingsManager), nameof(LoadSettingsAsync), ex);
            SetDefaults();
            return false;
        }
    }

    public static async ValueTask<bool> LoadSettingsBytesAsync()
    {
        try
        {
            Configuration ??= new SettingsConfiguration();

            var userPath = ConfigFileManager.ResolveDefaultConfigPath(Configuration);
            Configuration.CorrectPath = userPath;

            if (File.Exists(userPath))
            {
                var bytes = await File.ReadAllBytesAsync(userPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(
                    bytes, SettingsGenerationContext.Default.AppSettings);
                Settings = EnsureSettings(settings);
            }

            if (Settings is null)
            {
                Settings = GetDefaults();
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(SettingsManager), nameof(LoadSettingsAsync), ex);
            SetDefaults();
            return false;
        }
    }

    public static async ValueTask<bool> LoadSettingsLinesAsync()
    {
        try
        {
            Configuration ??= new SettingsConfiguration();

            var userPath = ConfigFileManager.ResolveDefaultConfigPath(Configuration);
            Configuration.CorrectPath = userPath;

            if (File.Exists(userPath))
            {
                var json = await File.ReadAllTextAsync(userPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(
                    json, SettingsGenerationContext.Default.AppSettings);
                Settings = EnsureSettings(settings);
            }

            if (Settings is null)
            {
                Settings = GetDefaults();
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(SettingsManager), nameof(LoadSettingsAsync), ex);
            SetDefaults();
            return false;
        }
    }
    
    public static bool LoadSettings()
    {
        try
        {
            Configuration ??= new SettingsConfiguration();
            var path = ConfigFileManager.ResolveDefaultConfigPath(Configuration);
            
            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                var settings = JsonSerializer.Deserialize<AppSettings>(
                    bytes, SettingsGenerationContext.Default.AppSettings);
                Settings = EnsureSettings(settings);
            }
            else
            {
                // Fallback to defaults if no user config found
                Settings = GetDefaults();
                return false;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(SettingsManager), nameof(LoadSettings), ex);
            SetDefaults();
            return false;
        }

        return true;
    }
}

/*

// * Summary *
                                                                                                                                                                                                                                                             
BenchmarkDotNet v0.16.0-preview.1, Windows 10 (10.0.19045.6466/22H2/2022Update)
AMD Ryzen 7 9800X3D 4.70GHz, 1 CPU, 16 logical and 8 physical cores                                                                                                                                                                                      
Memory: 61.65 GB Total, 36.62 GB Available                                                                                                                                                                                                               
.NET SDK 11.0.100-preview.6.26359.118                                                                                                                                                                                                                    
  [Host]     : .NET 11.0.0 (11.0.0-preview.6.26359.118, 11.0.26.36018), X64 RyuJIT x86-64-v4                                                                                                                                                             
  DefaultJob : .NET 11.0.0 (11.0.0-preview.6.26359.118, 11.0.26.36018), X64 RyuJIT x86-64-v4                                                                                                                                                             
                                                                                                                                                                                                                                                         

| Method            | Mean     | Error   | StdDev  | Allocated |
|------------------ |---------:|--------:|--------:|----------:|
| Initial           | 302.9 us | 2.67 us | 2.50 us |   3.48 KB |                                                                                                                                                                                         
| Current           | 264.0 us | 2.16 us | 2.02 us |   5.05 KB |
| WithStreamReader  | 271.7 us | 1.48 us | 1.32 us |  23.36 KB |
| ReadAllText       | 274.9 us | 3.68 us | 3.44 us |  23.36 KB |
| ReadAllBytes      | 262.1 us | 1.25 us | 1.05 us |   5.05 KB |
| ReadAllBytesAsync | 300.1 us | 1.53 us | 1.35 us |   5.75 KB |
| ReadAllLinesAsync | 369.1 us | 7.19 us | 6.73 us |  21.84 KB |


*/