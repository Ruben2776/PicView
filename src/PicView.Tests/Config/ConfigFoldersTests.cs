using System.Text.Json;
using PicView.Core.Config;
using PicView.Core.Config.ConfigFileManagement;
using PicView.Core.FileHandling;

namespace PicView.Tests.Config;

[Collection("Sequential")]
public class ConfigFoldersTests : IDisposable
{
    private readonly string _root;
    private readonly string _local;
    private readonly string _roaming;
    private readonly string _unwritable;

    public ConfigFoldersTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"PicView_ConfigFolders_{Guid.NewGuid():N}");
        _local = Path.Combine(_root, "local");
        _roaming = Path.Combine(_root, "roaming");
        Directory.CreateDirectory(_local);
        Directory.CreateDirectory(_roaming);

        var blocker = Path.Combine(_root, "blocker");
        File.WriteAllText(blocker, "x");
        _unwritable = Path.Combine(blocker, "Config");

        ConfigFolders.UseRoots(_local, _roaming);
        SetDefaults();
    }

    public void Dispose()
    {
        ConfigFolders.ResetRoots();
        try
        {
            Directory.Delete(_root, true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private static void Seed(string dir, string name, string content, DateTime writeUtc)
    {
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);
        File.WriteAllText(path, content);
        File.SetLastWriteTimeUtc(path, writeUtc);
    }

    [Fact]
    public void ResolveAtStartup_OnlyLocalHasSettings_ReturnsLocal()
    {
        if (OperatingSystem.IsMacOS()) { return; }
        Seed(_local, ConfigFolders.SettingsFileName, "{}", DateTime.UtcNow);

        Assert.Equal(_local, ConfigFolders.ResolveAtStartup());
        Assert.Equal(_local, ConfigFolders.Current);
    }

    [Fact]
    public void ResolveAtStartup_OnlyRoamingHasSettings_ReturnsRoaming()
    {
        Seed(_roaming, ConfigFolders.SettingsFileName, "{}", DateTime.UtcNow);

        Assert.Equal(_roaming, ConfigFolders.ResolveAtStartup());
    }

    [Fact]
    public void ResolveAtStartup_BothHaveSettings_NewerWins()
    {
        if (OperatingSystem.IsMacOS()) { return; }
        Seed(_local, ConfigFolders.SettingsFileName, "{}", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Seed(_roaming, ConfigFolders.SettingsFileName, "{}", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.Equal(_roaming, ConfigFolders.ResolveAtStartup());

        Seed(_local, ConfigFolders.SettingsFileName, "{}", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.Equal(_local, ConfigFolders.ResolveAtStartup());
    }

    [Fact]
    public void ResolveAtStartup_NewerFileIsEmpty_OlderNonEmptyWins()
    {
        if (OperatingSystem.IsMacOS()) { return; }
        Seed(_local, ConfigFolders.SettingsFileName, "{}", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Seed(_roaming, ConfigFolders.SettingsFileName, "", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(_local, ConfigFolders.ResolveAtStartup());
    }

    [Fact]
    public void ResolveAtStartup_NeitherHasSettings_LocalWritable_ReturnsLocal()
    {
        if (OperatingSystem.IsMacOS()) { return; }

        Assert.Equal(_local, ConfigFolders.ResolveAtStartup());
    }

    [Fact]
    public void ResolveAtStartup_NeitherHasSettings_LocalNotWritable_ReturnsRoaming()
    {
        ConfigFolders.UseRoots(_unwritable, _roaming);

        Assert.Equal(_roaming, ConfigFolders.ResolveAtStartup());
    }

    [Fact]
    public void ResolveAtStartup_DoesNotCreateAnyFile()
    {
        ConfigFolders.ResolveAtStartup();

        Assert.Empty(Directory.EnumerateFiles(_local, "*", SearchOption.AllDirectories));
        Assert.Empty(Directory.EnumerateFiles(_roaming, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void IsDirectoryWritable_WritableFolder_LeavesNoFileBehind()
    {
        var dir = Path.Combine(_root, "probe");

        Assert.True(FileHelper.IsDirectoryWritable(dir));
        Assert.Empty(Directory.EnumerateFileSystemEntries(dir));
    }

    [Fact]
    public void IsDirectoryWritable_PathUnderAFile_ReturnsFalse()
    {
        Assert.False(FileHelper.IsDirectoryWritable(_unwritable));
    }

    [Fact]
    public void FolderFor_Portable_ReturnsLocalEvenIfNotWritable()
    {
        if (OperatingSystem.IsMacOS()) { return; }
        ConfigFolders.UseRoots(_unwritable, _roaming);

        Assert.Equal(_unwritable, ConfigFolders.FolderFor(ConfigLocation.Portable));
    }

    [Fact]
    public void AppSettings_WithoutConfigLocation_DeserializesAsAuto()
    {
        var loaded = JsonSerializer.Deserialize("{}", SettingsGenerationContext.Default.AppSettings);

        var settings = EnsureSettings(loaded!);

        Assert.Equal(ConfigLocation.Auto, settings.StartUp!.ConfigLocation);
    }
}
