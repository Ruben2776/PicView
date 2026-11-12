using PicView.Core.Config;
using PicView.Core.Config.ConfigFileManagement;
using PicView.Core.Favorites;
using PicView.Core.Keybindings;

namespace PicView.Tests.Config;

[Collection("Sequential")]
public class ConfigFileManagerTests : IDisposable
{
    private readonly string _root;
    private readonly string _local;
    private readonly string _roaming;

    public ConfigFileManagerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"PicView_ConfigFileManager_{Guid.NewGuid():N}");
        _local = Path.Combine(_root, "local");
        _roaming = Path.Combine(_root, "roaming");
        Directory.CreateDirectory(_local);
        Directory.CreateDirectory(_roaming);

        ConfigFolders.UseRoots(_local, _roaming);
        KeybindingFunctions.ResetConfiguration();
        FavoritesManager.Initialize();
        SetDefaults();
        ConfigFolders.SetCurrent(_local);
    }

    public void Dispose()
    {
        ConfigFolders.ResetRoots();
        KeybindingFunctions.ResetConfiguration();
        try
        {
            Directory.Delete(_root, true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private static Task<bool> SaveResizeAsync() => ConfigFileManager.SaveAsync(new ResizeWindowConfig(),
        new ResizeWindowConfig.ResizeWindowProperties(), typeof(ResizeWindowConfig.ResizeWindowProperties),
        ResizeWindowGenerationContext.Default);

    [Fact]
    public async Task SaveAsync_WritesIntoCurrentFolder()
    {
        Assert.True(await SaveResizeAsync());

        Assert.True(File.Exists(Path.Combine(_local, "ResizeWindow.json")));
    }

    [Fact]
    public async Task SaveAsync_CurrentFolderMissing_CreatesIt()
    {
        var missing = Path.Combine(_root, "new");
        ConfigFolders.SetCurrent(missing);

        Assert.True(await SaveResizeAsync());

        Assert.True(File.Exists(Path.Combine(missing, "ResizeWindow.json")));
    }

    [Fact]
    public async Task SaveAsync_RoamingModeAndTargetUnwritable_ReturnsFalseAndKeepsFolder()
    {
        Settings!.StartUp.ConfigLocation = ConfigLocation.Roaming;
        Directory.CreateDirectory(Path.Combine(_local, "ResizeWindow.json"));

        Assert.False(await SaveResizeAsync());

        Assert.Equal(_local, ConfigFolders.Current);
    }

    [Fact]
    public void LoadSettings_NoFileAnywhere_CreatesNoFile()
    {
        Assert.False(LoadSettings());

        Assert.Empty(Directory.GetFiles(_local));
        Assert.Empty(Directory.GetFiles(_roaming));
    }

    [Fact]
    public async Task KeybindingsSave_UsesCurrentFolder()
    {
        await KeybindingFunctions.SaveKeyBindingsFile("{}");

        Assert.True(File.Exists(Path.Combine(_local, "keybindings.json")));
    }

    [Fact]
    public void FilePath_AfterSetCurrent_FollowsNewFolder()
    {
        ConfigFolders.SetCurrent(_roaming);

        Assert.Equal(Path.Combine(_roaming, "UserSettings.json"), new SettingsConfiguration().FilePath);
    }
}
