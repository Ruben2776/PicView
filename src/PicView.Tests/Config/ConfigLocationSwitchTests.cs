using System.Globalization;
using System.Text.Json;
using PicView.Core.Config;
using PicView.Core.Config.ConfigFileManagement;
using PicView.Core.Favorites;
using PicView.Core.Keybindings;

namespace PicView.Tests.Config;

[Collection("Sequential")]
public class ConfigLocationSwitchTests : IDisposable
{
    private readonly string _root;
    private readonly string _local;
    private readonly string _roaming;
    private readonly string _unwritable;

    public ConfigLocationSwitchTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"PicView_ConfigLocationSwitch_{Guid.NewGuid():N}");
        _local = Path.Combine(_root, "local");
        _roaming = Path.Combine(_root, "roaming");
        Directory.CreateDirectory(_local);
        Directory.CreateDirectory(_roaming);

        var blocker = Path.Combine(_root, "blocker");
        File.WriteAllText(blocker, "x");
        _unwritable = Path.Combine(blocker, "Config");

        ConfigFolders.UseRoots(_local, _roaming);
        KeybindingFunctions.ResetConfiguration();
        FavoritesManager.Initialize();
        SetDefaults();

        Seed(_local);
        Directory.CreateDirectory(Path.Combine(_local, "Languages"));
        File.WriteAllText(Path.Combine(_local, "Languages", "en.json"), "{\"lang\":\"en\"}");
        ConfigFolders.SetCurrent(_local);
        LoadSettings();
    }

    public void Dispose()
    {
        ConfigFolders.ResetRoots();
        KeybindingFunctions.ResetConfiguration();
        SetDefaults();
        try
        {
            Directory.Delete(_root, true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private static string ContentFor(string name) => $"{{\"n\":\"{name}\"}}";

    private static void Seed(string dir)
    {
        Directory.CreateDirectory(dir);
        foreach (var name in ConfigFolders.KnownFileNames)
        {
            var content = string.Equals(name, ConfigFolders.SettingsFileName, StringComparison.Ordinal)
                ? JsonSerializer.Serialize(GetDefaults(), SettingsGenerationContext.Default.AppSettings)
                : ContentFor(name);
            File.WriteAllText(Path.Combine(dir, name), content);
        }
    }

    private static IEnumerable<string> OtherKnownFiles()
    {
        foreach (var name in ConfigFolders.KnownFileNames)
        {
            if (!string.Equals(name, ConfigFolders.SettingsFileName, StringComparison.Ordinal))
            {
                yield return name;
            }
        }
    }

    private static ConfigLocation ReadMode(string dir)
    {
        var bytes = File.ReadAllBytes(Path.Combine(dir, ConfigFolders.SettingsFileName));
        var settings = JsonSerializer.Deserialize(bytes, SettingsGenerationContext.Default.AppSettings);
        return settings!.StartUp!.ConfigLocation;
    }

    [Fact]
    public async Task SetConfigLocationAsync_Roaming_MovesEveryKnownFile()
    {
        if (OperatingSystem.IsMacOS()) { return; }

        var result = await SetConfigLocationAsync(ConfigLocation.Roaming);

        Assert.Equal(ConfigLocationChange.Changed, result);
        foreach (var name in OtherKnownFiles())
        {
            Assert.Equal(ContentFor(name), File.ReadAllText(Path.Combine(_roaming, name)));
            Assert.False(File.Exists(Path.Combine(_local, name)), name);
        }

        Assert.Equal(ConfigLocation.Roaming, ReadMode(_roaming));
        Assert.False(File.Exists(Path.Combine(_local, ConfigFolders.SettingsFileName)));
        Assert.Equal("{\"lang\":\"en\"}", File.ReadAllText(Path.Combine(_local, "Languages", "en.json")));
        Assert.Equal(_roaming, ConfigFolders.Current);
    }

    [Fact]
    public async Task SetConfigLocationAsync_SameFolder_StillWritesMode()
    {
        if (OperatingSystem.IsMacOS()) { return; }

        var result = await SetConfigLocationAsync(ConfigLocation.Portable);

        Assert.Equal(ConfigLocationChange.Changed, result);
        Assert.Empty(Directory.GetFiles(_roaming));
        foreach (var name in OtherKnownFiles())
        {
            Assert.Equal(ContentFor(name), File.ReadAllText(Path.Combine(_local, name)));
        }

        Assert.Equal(ConfigLocation.Portable, ReadMode(_local));
        Assert.Equal(_local, ConfigFolders.Current);
    }

    [Fact]
    public async Task SetConfigLocationAsync_PortableNotWritable_RefusesAndChangesNothing()
    {
        if (OperatingSystem.IsMacOS()) { return; }
        ConfigFolders.UseRoots(_unwritable, _roaming);
        Seed(_roaming);
        ConfigFolders.SetCurrent(_roaming);
        Settings!.StartUp!.ConfigLocation = ConfigLocation.Roaming;

        var result = await SetConfigLocationAsync(ConfigLocation.Portable);

        Assert.Equal(ConfigLocationChange.PortableNotWritable, result);
        Assert.Equal(ConfigLocation.Roaming, Settings.StartUp.ConfigLocation);
        Assert.Equal(_roaming, ConfigFolders.Current);
        foreach (var name in OtherKnownFiles())
        {
            Assert.Equal(ContentFor(name), File.ReadAllText(Path.Combine(_roaming, name)));
        }

        Assert.Equal(ConfigLocation.Auto, ReadMode(_roaming));
    }

    [Fact]
    public async Task SetConfigLocationAsync_SettingsWriteFails_RollsBack()
    {
        if (OperatingSystem.IsMacOS()) { return; }
        Directory.CreateDirectory(Path.Combine(_roaming, ConfigFolders.SettingsFileName));

        var result = await SetConfigLocationAsync(ConfigLocation.Roaming);

        Assert.Equal(ConfigLocationChange.SaveFailed, result);
        Assert.Equal(ConfigLocation.Auto, Settings!.StartUp!.ConfigLocation);
        Assert.Equal(_local, ConfigFolders.Current);
        foreach (var name in ConfigFolders.KnownFileNames)
        {
            Assert.True(File.Exists(Path.Combine(_local, name)), name);
        }
    }

    [Fact]
    public async Task SetConfigLocationAsync_EmptySourceFile_DoesNotOverwriteTarget()
    {
        if (OperatingSystem.IsMacOS()) { return; }
        File.WriteAllText(Path.Combine(_local, "Favorites.json"), "");
        File.WriteAllText(Path.Combine(_roaming, "Favorites.json"), "[1]");

        await SetConfigLocationAsync(ConfigLocation.Roaming);

        Assert.Equal("[1]", File.ReadAllText(Path.Combine(_roaming, "Favorites.json")));
    }

    [Fact]
    public async Task SetConfigLocationAsync_MoveFails_ReturnsMoveIncompleteAndUsesNewFolder()
    {
        if (OperatingSystem.IsMacOS()) { return; }

        ConfigLocationChange result;
        using (new FileStream(Path.Combine(_local, "FileHistory.json"), FileMode.Open, FileAccess.Read,
                   FileShare.None))
        {
            result = await SetConfigLocationAsync(ConfigLocation.Roaming);
        }

        Assert.Equal(ConfigLocationChange.MoveIncomplete, result);
        Assert.Equal(_roaming, ConfigFolders.Current);
        Assert.True(File.Exists(Path.Combine(_roaming, ConfigFolders.SettingsFileName)));
    }

    [Fact]
    public async Task SetConfigLocationAsync_SaveQueuedBehindSwitch_LandsInNewFolder()
    {
        if (OperatingSystem.IsMacOS()) { return; }
        FavoritesManager.Initialize();
        var favoriteFolder = Path.Combine(_root, "favorite");
        Directory.CreateDirectory(favoriteFolder);
        await FavoritesManager.AddAsync(favoriteFolder);

        await ConfigFolders.Gate.WaitAsync(TestContext.Current.CancellationToken);
        var sw = SetConfigLocationAsync(ConfigLocation.Roaming);
        var fav = FavoritesManager.SaveToFileAsync();
        Assert.False(sw.IsCompleted || fav.IsCompleted);
        ConfigFolders.Gate.Release();
        await Task.WhenAll(sw, fav);

        Assert.Contains("favorite", File.ReadAllText(Path.Combine(_roaming, "Favorites.json")));
        Assert.False(File.Exists(Path.Combine(_local, "Favorites.json")));
    }

    [Fact]
    public async Task SaveSettingsAsync_ModeDisagreesWithFolder_MovesToMatch()
    {
        if (OperatingSystem.IsMacOS()) { return; }
        var settings = GetDefaults();
        settings.StartUp!.ConfigLocation = ConfigLocation.Roaming;
        File.WriteAllText(Path.Combine(_local, ConfigFolders.SettingsFileName),
            JsonSerializer.Serialize(settings, SettingsGenerationContext.Default.AppSettings));
        LoadSettings();
        Assert.Equal(_local, ConfigFolders.Current);

        Assert.True(await SaveSettingsAsync());

        foreach (var name in ConfigFolders.KnownFileNames)
        {
            Assert.True(File.Exists(Path.Combine(_roaming, name)), name);
            Assert.False(File.Exists(Path.Combine(_local, name)), name);
        }

        Assert.Equal(_roaming, ConfigFolders.Current);
    }

    [Fact]
    public async Task SaveAsync_AutoAndTargetUnwritable_FallsBackAndMovesEverything()
    {
        if (OperatingSystem.IsMacOS()) { return; }
        Assert.Equal(ConfigLocation.Auto, Settings!.StartUp!.ConfigLocation);
        ConfigFolders.UseRoots(_unwritable, _roaming);
        ConfigFolders.SetCurrent(_local);
        var resizePath = Path.Combine(_local, "ResizeWindow.json");
        File.Delete(resizePath);
        Directory.CreateDirectory(resizePath);

        var saved = await ConfigFileManager.SaveAsync(new ResizeWindowConfig(),
            new ResizeWindowConfig.ResizeWindowProperties(), typeof(ResizeWindowConfig.ResizeWindowProperties),
            ResizeWindowGenerationContext.Default);

        Assert.True(saved);
        Assert.Equal(_roaming, ConfigFolders.Current);
        Assert.True(File.Exists(Path.Combine(_roaming, "ResizeWindow.json")));
        Assert.True(File.Exists(Path.Combine(_roaming, "keybindings.json")));
        Assert.True(File.Exists(Path.Combine(_roaming, ConfigFolders.SettingsFileName)));
        Assert.False(File.Exists(Path.Combine(_local, "keybindings.json")));
    }

    [Fact]
    public async Task ResetDefaultsAsync_RoamingMode_KeepsModeAndLanguageAfterReload()
    {
        if (OperatingSystem.IsMacOS()) { return; }
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
            var defaultLooping = GetDefaults().UIProperties!.Looping;

            Assert.Equal(ConfigLocationChange.Changed, await SetConfigLocationAsync(ConfigLocation.Roaming));
            Settings!.UIProperties!.Looping = !defaultLooping;
            Settings.UIProperties.UserLanguage = "en";
            Assert.True(await SaveSettingsAsync());

            await ResetDefaultsAsync();
            LoadSettings();

            Assert.Equal(ConfigLocation.Roaming, Settings!.StartUp!.ConfigLocation);
            Assert.Equal(defaultLooping, Settings.UIProperties!.Looping);
            Assert.StartsWith("de", Settings.UIProperties.UserLanguage);
            Assert.Equal(_roaming, ConfigFolders.Current);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
        }
    }

    [Fact]
    public async Task ResetDefaultsAsync_AutoMode_DeletesSettingsFile()
    {
        if (OperatingSystem.IsMacOS()) { return; }

        await ResetDefaultsAsync();

        Assert.False(File.Exists(Path.Combine(_local, ConfigFolders.SettingsFileName)));
    }

    [Fact]
    public async Task ResetDefaultsAsync_AutoModeWithStaleOtherCopy_WritesDefaultsInsteadOfDeleting()
    {
        if (OperatingSystem.IsMacOS()) { return; }
        var stale = Path.Combine(_roaming, ConfigFolders.SettingsFileName);
        File.WriteAllText(stale, JsonSerializer.Serialize(GetDefaults(), SettingsGenerationContext.Default.AppSettings));
        File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddDays(-1));
        File.SetLastWriteTimeUtc(Path.Combine(_local, ConfigFolders.SettingsFileName), DateTime.UtcNow.AddHours(-1));

        await ResetDefaultsAsync();

        Assert.True(File.Exists(Path.Combine(_local, ConfigFolders.SettingsFileName)));
        Assert.Equal(_local, ConfigFolders.ResolveAtStartup());
    }
}
