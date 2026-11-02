using PicView.Core.Config;
using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Tests.Config;

[Collection("Sequential")]
public class WindowConfigTests
{
    [Fact]
    public async Task ResizeWindowConfig_SaveThenLoad_RoundTripsThePosition()
    {
        var config = new ResizeWindowConfig();
        using var backup = new ConfigFileBackup(config);
        DeleteSavedFiles(config);
        await config.LoadAsync();
        Assert.NotNull(config.WindowProperties);
        Assert.Null(config.WindowProperties.Left);

        config.WindowProperties.Left = 10;
        config.WindowProperties.Top = 20;
        await config.SaveAsync();

        var loaded = new ResizeWindowConfig { CorrectPath = config.CorrectPath };
        await loaded.LoadAsync();

        Assert.NotNull(loaded.WindowProperties);
        Assert.Equal(10, loaded.WindowProperties.Left);
        Assert.Equal(20, loaded.WindowProperties.Top);
    }

    [Fact]
    public async Task ConvertWindowConfig_SaveThenLoad_RoundTripsThePosition()
    {
        var config = new ConvertWindowConfig();
        using var backup = new ConfigFileBackup(config);
        DeleteSavedFiles(config);
        await config.LoadAsync();
        Assert.NotNull(config.WindowProperties);
        Assert.Null(config.WindowProperties.Left);

        config.WindowProperties.Left = 10;
        config.WindowProperties.Top = 20;
        await config.SaveAsync();

        var loaded = new ConvertWindowConfig { CorrectPath = config.CorrectPath };
        await loaded.LoadAsync();

        Assert.NotNull(loaded.WindowProperties);
        Assert.Equal(10, loaded.WindowProperties.Left);
        Assert.Equal(20, loaded.WindowProperties.Top);
    }

    [Fact]
    public async Task ResizeWindowConfig_CorruptFile_LoadsDefaults()
    {
        var config = new ResizeWindowConfig();
        using var backup = new ConfigFileBackup(config);
        config.CorrectPath = config.RoamingConfigPath;
        Directory.CreateDirectory(Path.GetDirectoryName(config.CorrectPath)!);
        await File.WriteAllTextAsync(config.CorrectPath, "{ not json");

        await config.LoadAsync();

        Assert.NotNull(config.WindowProperties);
        Assert.Null(config.WindowProperties.Left);
        Assert.Null(config.WindowProperties.Top);
        Assert.Null(config.WindowProperties.Width);
        Assert.Null(config.WindowProperties.Height);
        Assert.False(config.WindowProperties.Maximized);
    }

    /// <summary>
    /// Deletes the user's own copies of the file, so the test starts from nothing remembered. The backup puts them
    /// back.
    /// </summary>
    private static void DeleteSavedFiles(ConfigFile config)
    {
        if (File.Exists(config.RoamingConfigPath))
        {
            File.Delete(config.RoamingConfigPath);
        }

        if (File.Exists(config.LocalConfigPath))
        {
            File.Delete(config.LocalConfigPath);
        }
    }
}
