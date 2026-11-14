using PicView.Core.Config;

namespace PicView.Tests.Config;

public sealed class RememberedWindowConfigTests : IDisposable
{
    private static readonly string[] Configs = ["Resize"];

    private static readonly string[] Scenarios = ["RoundTrip", "MissingFile", "CorruptFile", "EmptyFile", "NonFiniteSize"];

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
            "Resize" => Check(new ResizeWindowConfig(), () => new ResizeWindowConfig(), scenario, null, null),
            _ => throw new ArgumentOutOfRangeException(nameof(config), config, null)
        };
        await check;
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
