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

    [Benchmark]
    public void ReadAllBytesEnsured()
    {
        LoadSettingsEnsuredBytes();
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
                    Settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                        userStream, SettingsGenerationContext.Default.AppSettings).ConfigureAwait(false);
                }
            }

            // Fallback to defaults if no user config found
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
                Settings = JsonSerializer.Deserialize<AppSettings>(
                    streamReader.ReadToEnd(), SettingsGenerationContext.Default.AppSettings);
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

    private static async ValueTask<AppSettings?> LoadConfigAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        if (stream.Length == 0)
        {
            return null;
        }

        return await JsonSerializer.DeserializeAsync<AppSettings>(
            stream, SettingsGenerationContext.Default.AppSettings).ConfigureAwait(false);
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
                Settings = JsonSerializer.Deserialize<AppSettings>(
                    json, SettingsGenerationContext.Default.AppSettings);
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
                Settings = JsonSerializer.Deserialize<AppSettings>(
                    bytes, SettingsGenerationContext.Default.AppSettings);
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
                Settings = JsonSerializer.Deserialize<AppSettings>(
                    bytes, SettingsGenerationContext.Default.AppSettings);
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
                Settings = JsonSerializer.Deserialize<AppSettings>(
                    json, SettingsGenerationContext.Default.AppSettings);
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

    public static bool LoadSettingsEnsuredBytes()
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
                                                                                                                                                                                                                                                             
BenchmarkDotNet v0.15.5, Windows 10 (10.0.19045.6456/22H2/2022Update)
AMD Ryzen 7 9800X3D 4.70GHz, 1 CPU, 16 logical and 8 physical cores                                                                                                                                                                                          
.NET SDK 10.0.100-rc.2.25502.107                                                                                                                                                                                                                             
  [Host]     : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v4                                                                                                                                                                      
  DefaultJob : .NET 10.0.0 (10.0.0-rc.2.25502.107, 10.0.25.50307), X64 RyuJIT x86-64-v4                                                                                                                                                                      
                                                                                                                                                                                                                                                             

| Method              | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-------------------- |----------:|---------:|---------:|-------:|----------:|
| Initial             | 115.99 us | 0.336 us | 0.298 us |      - |   3.68 KB |                                                                                                                                                                               
| WithStreamReader    |  43.29 us | 0.105 us | 0.098 us | 0.4272 |  22.92 KB |
| ReadAllText         |  43.44 us | 0.112 us | 0.099 us | 0.4272 |  22.92 KB |
| ReadAllBytes        |  39.50 us | 0.210 us | 0.196 us | 0.0610 |   4.85 KB |
| ReadAllBytesAsync   | 130.44 us | 2.556 us | 2.391 us |      - |   5.52 KB |
| ReadAllLinesAsync   | 170.78 us | 2.186 us | 2.044 us | 0.4883 |  21.14 KB |
| ReadAllBytesEnsured |  39.44 us | 0.052 us | 0.046 us | 0.0610 |   4.85 KB |

*/