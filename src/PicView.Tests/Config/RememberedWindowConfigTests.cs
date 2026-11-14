using PicView.Core.Config;

namespace PicView.Tests.Config;

public sealed class RememberedWindowConfigTests : IDisposable
{
    private static readonly string[] Configs =
        ["BatchResize", "Convert", "Effects", "ImageInfo", "Keybinding", "Print", "Resize", "Settings"];

    private static readonly string[] Scenarios =
        ["RoundTrip", "BomFile", "MissingFile", "CorruptFile", "EmptyFile", "NonFiniteSize"];

    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "PicViewTests", Guid.NewGuid().ToString("N"));

    public RememberedWindowConfigTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    public static TheoryData<string, string> Cases()
    {
        var data = new TheoryData<string, string>();
        foreach (var config in Configs)
        {
            foreach (var scenario in Scenarios)
            {
                data.Add(config, scenario);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task RememberedWindowConfig_Scenario_BehavesAsRemembered(string config, string scenario)
    {
        var check = config switch
        {
            "BatchResize" => Check(new BatchResizeWindowConfig(), () => new BatchResizeWindowConfig(), scenario, null,
                null),
            "Convert" => Check(new ConvertWindowConfig(), () => new ConvertWindowConfig(), scenario, null, null),
            "Effects" => Check(new EffectsWindowConfig(), () => new EffectsWindowConfig(), scenario, null, null),
            "ImageInfo" => Check(new ImageInfoWindowConfig(), () => new ImageInfoWindowConfig(), scenario, 850, 495),
            "Keybinding" => Check(new KeybindingWindowConfig(), () => new KeybindingWindowConfig(), scenario, null,
                null),
            "Print" => Check(new PrintWindowConfig(), () => new PrintWindowConfig(), scenario, 850, 495),
            "Resize" => Check(new ResizeWindowConfig(), () => new ResizeWindowConfig(), scenario, null, null),
            "Settings" => Check(new SettingsWindowConfig(), () => new SettingsWindowConfig(), scenario, null, null),
            _ => throw new ArgumentOutOfRangeException(nameof(config), config, null)
        };
        await check;
    }

    /// <summary>
    /// A remembered window whose properties are missing from the shared context would load defaults and fail to save
    /// without an error, and one missing from <see cref="Configs" /> would not be checked by the theory.
    /// </summary>
    [Fact]
    public void RememberedWindowConfigs_EveryOne_IsInTheContextAndTheTheory()
    {
        var configTypes = typeof(RememberedWindowConfig<>).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && type.BaseType is { IsGenericType: true } baseType &&
                           baseType.GetGenericTypeDefinition() == typeof(RememberedWindowConfig<>))
            .ToList();

        foreach (var type in configTypes)
        {
            var propertiesType = type.BaseType!.GetGenericArguments()[0];
            Assert.True(RememberedWindowGenerationContext.Default.GetTypeInfo(propertiesType) is not null,
                $"{propertiesType.Name} is not in {nameof(RememberedWindowGenerationContext)}.");
        }

        Assert.Equal(Configs.Select(name => name + "WindowConfig").Order(StringComparer.Ordinal),
            configTypes.Select(type => type.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task SettingsWindowConfig_SaveThenLoad_KeepsLastTab()
    {
        var config = Prepare(new SettingsWindowConfig());
        config.Load();
        config.WindowProperties!.LastTab = 3;
        await config.SaveAsync();

        var loaded = Reload(config, new SettingsWindowConfig());
        Assert.Equal(3, loaded.WindowProperties!.LastTab);
    }

    [Fact]
    public async Task PrintWindowConfig_SaveThenLoad_KeepsPrintSettings()
    {
        var config = Prepare(new PrintWindowConfig());
        config.Load();
        config.WindowProperties!.PrinterName = "Printer A";
        config.WindowProperties.Copies = 2;
        config.WindowProperties.MarginTop = 5;
        await config.SaveAsync();

        var loaded = Reload(config, new PrintWindowConfig());
        Assert.Equal("Printer A", loaded.WindowProperties!.PrinterName);
        Assert.Equal(2, loaded.WindowProperties.Copies);
        Assert.Equal(5, loaded.WindowProperties.MarginTop);
    }

    [Fact]
    public async Task BatchResizeWindowConfig_SaveThenLoad_KeepsChoices()
    {
        var config = Prepare(new BatchResizeWindowConfig());
        config.Load();
        config.WindowProperties!.Conversion = "Png";
        config.WindowProperties.Quality = 80;
        config.WindowProperties.KeepAspectRatio = false;
        config.WindowProperties.Thumbnails = [new() { Subfolder = "small", Size = "Width", Value = 200 }];
        await config.SaveAsync();

        var properties = Reload(config, new BatchResizeWindowConfig()).WindowProperties!;
        Assert.Equal("Png", properties.Conversion);
        Assert.Equal(80u, properties.Quality);
        Assert.False(properties.KeepAspectRatio);
        var thumbnail = Assert.Single(properties.Thumbnails!);
        Assert.Equal("small", thumbnail.Subfolder);
        Assert.Equal("Width", thumbnail.Size);
        Assert.Equal(200, thumbnail.Value);
    }

    /// <summary>
    /// A file as the previous version wrote it, with the window's own fields before the shared ones and an old index
    /// field, still loads every value.
    /// </summary>
    [Fact]
    public async Task BatchResizeWindowConfig_TodaysFile_LoadsEveryValue()
    {
        var config = Prepare(new BatchResizeWindowConfig());
        await File.WriteAllTextAsync(config.PathOverride!, """
            {
              "IsQualityEnabled": true,
              "Quality": 75,
              "Conversion": "Webp",
              "Compression": "Lossless",
              "Resize": "Percentage",
              "KeepAspectRatio": false,
              "Thumbnails": [ { "Subfolder": "tiny", "Size": "Height", "Value": 64 } ],
              "ConvertToIndex": 2,
              "CompressionIndex": 1,
              "ResizeIndex": 3,
              "GenerateThumbnailsIndex": 4,
              "Top": 30,
              "Left": 40,
              "Width": 700,
              "Height": 500,
              "Maximized": true,
            }
            """);

        config.Load();

        var properties = config.WindowProperties!;
        Assert.Equal(30, properties.Top);
        Assert.Equal(40, properties.Left);
        Assert.Equal(700, properties.Width);
        Assert.Equal(500, properties.Height);
        Assert.True(properties.Maximized);
        Assert.True(properties.IsQualityEnabled);
        Assert.Equal(75u, properties.Quality);
        Assert.Equal("Webp", properties.Conversion);
        Assert.Equal("Lossless", properties.Compression);
        Assert.Equal("Percentage", properties.Resize);
        Assert.False(properties.KeepAspectRatio);
        var thumbnail = Assert.Single(properties.Thumbnails!);
        Assert.Equal("tiny", thumbnail.Subfolder);
        Assert.Equal("Height", thumbnail.Size);
        Assert.Equal(64, thumbnail.Value);
        Assert.Equal(2, properties.ConvertToIndex);
        Assert.Equal(1, properties.CompressionIndex);
        Assert.Equal(3, properties.ResizeIndex);
        Assert.Equal(4, properties.GenerateThumbnailsIndex);
    }

    [Fact]
    public async Task BatchResizeWindowConfig_Save_DoesNotWriteLegacyIndexes()
    {
        var config = Prepare(new BatchResizeWindowConfig());
        config.Load();
        await config.SaveAsync();

        var json = await File.ReadAllTextAsync(config.PathOverride!);
        Assert.DoesNotContain("ConvertToIndex", json);
        Assert.DoesNotContain("CompressionIndex", json);
        Assert.DoesNotContain("ResizeIndex", json);
        Assert.DoesNotContain("GenerateThumbnailsIndex", json);
    }

    private TConfig Prepare<TConfig>(TConfig config) where TConfig : Core.Config.ConfigFileManagement.ConfigFile
    {
        config.PathOverride = Path.Combine(_tempDir, config.ConfigFileName);
        return config;
    }

    private static TConfig Reload<TConfig, T>(RememberedWindowConfig<T> saved, TConfig fresh)
        where TConfig : RememberedWindowConfig<T> where T : RememberedWindowProperties, new()
    {
        fresh.PathOverride = saved.PathOverride;
        fresh.Load();
        return fresh;
    }

    private async Task Check<T>(RememberedWindowConfig<T> config, Func<RememberedWindowConfig<T>> create,
        string scenario, double? defaultWidth, double? defaultHeight) where T : RememberedWindowProperties, new()
    {
        config.PathOverride = Path.Combine(_tempDir, config.ConfigFileName);

        switch (scenario)
        {
            case "RoundTrip":
            {
                config.Load();
                Assert.NotNull(config.WindowProperties);
                config.WindowProperties.Left = 10;
                config.WindowProperties.Top = 20;
                config.WindowProperties.Width = 640;
                config.WindowProperties.Height = 480;
                config.WindowProperties.Maximized = true;
                await config.SaveAsync();

                var loaded = create();
                loaded.PathOverride = config.PathOverride;
                loaded.Load();
                Assert.NotNull(loaded.WindowProperties);
                Assert.Equal(10, loaded.WindowProperties.Left);
                Assert.Equal(20, loaded.WindowProperties.Top);
                Assert.Equal(640, loaded.WindowProperties.Width);
                Assert.Equal(480, loaded.WindowProperties.Height);
                Assert.True(loaded.WindowProperties.Maximized);
                return;
            }
            case "BomFile":
            {
                // A file hand-edited by a tool that writes a byte-order mark still loads
                await File.WriteAllTextAsync(config.PathOverride, """{"Left": 40, "Top": 50}""",
                    new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                config.Load();
                Assert.NotNull(config.WindowProperties);
                Assert.Equal(40, config.WindowProperties.Left);
                Assert.Equal(50, config.WindowProperties.Top);
                return;
            }
            case "MissingFile":
                break;
            case "CorruptFile":
                await File.WriteAllTextAsync(config.PathOverride, "{ not json");
                break;
            case "EmptyFile":
                await File.WriteAllBytesAsync(config.PathOverride, []);
                break;
            case "NonFiniteSize":
                await File.WriteAllTextAsync(config.PathOverride, """{"Width": NaN, "Height": Infinity}""");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
        }

        config.Load();
        Assert.NotNull(config.WindowProperties);
        Assert.Null(config.WindowProperties.Left);
        Assert.Null(config.WindowProperties.Top);
        Assert.Equal(defaultWidth, config.WindowProperties.Width);
        Assert.Equal(defaultHeight, config.WindowProperties.Height);
        Assert.False(config.WindowProperties.Maximized);
    }
}
