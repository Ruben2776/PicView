using PicView.Core.DebugTools;
using PicView.Core.FileHandling;

namespace PicView.Core.Config.ConfigFileManagement;

/// <summary>
/// Knows the two possible config folders and which one is in use.
/// </summary>
public static class ConfigFolders
{
    public const string SettingsFileName = "UserSettings.json";

    private static readonly string DefaultLocalDirectory =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

    private static readonly string DefaultRoamingDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ruben2776", "PicView", "Config");

    private static readonly Lock ResolveLock = new();
    private static volatile string? _current;

    /// <summary>
    /// Serializes config folder switches against config writes. Not reentrant: a holder must never call
    /// anything that waits on it again.
    /// </summary>
    internal static readonly SemaphoreSlim Gate = new(1, 1);

    public static IReadOnlyList<string> KnownFileNames { get; } =
    [
        SettingsFileName,
        "keybindings.json",
        "FileHistory.json",
        "Favorites.json",
        "SettingsWindow.json",
        "ImageInfoWindow.json",
        "KeybindingWindowConfig.json",
        "EffectsWindow.json",
        "ResizeWindow.json",
        "BatchResizeWindow.json",
        "ConvertWindow.json",
        "PrintWindow.json"
    ];

    /// <summary>The folder next to the executable.</summary>
    public static string LocalDirectory { get; private set; } = DefaultLocalDirectory;

    /// <summary>The roaming application data folder.</summary>
    public static string RoamingDirectory { get; private set; } = DefaultRoamingDirectory;

    /// <summary>
    /// The resolved config folder. Resolves it on first read.
    /// </summary>
    public static string Current
    {
        get
        {
            var current = _current;
            if (current is not null)
            {
                return current;
            }

            lock (ResolveLock)
            {
                return _current ?? ResolveAtStartup();
            }
        }
    }

    /// <summary>The folder that is not <see cref="Current" />.</summary>
    public static string OtherFolder => IsSame(Current, LocalDirectory) ? RoamingDirectory : LocalDirectory;

    /// <summary>
    /// Picks the folder to use at startup and stores it in <see cref="Current" />. Never creates a settings file.
    /// </summary>
    public static string ResolveAtStartup()
    {
        string resolved;
        if (OperatingSystem.IsMacOS())
        {
            resolved = RoamingDirectory;
        }
        else
        {
            var localSettings = Path.Combine(LocalDirectory, SettingsFileName);
            var roamingSettings = Path.Combine(RoamingDirectory, SettingsFileName);
            var hasLocal = HasContent(localSettings);
            var hasRoaming = HasContent(roamingSettings);

            if (hasLocal && hasRoaming)
            {
                resolved = File.GetLastWriteTimeUtc(roamingSettings) > File.GetLastWriteTimeUtc(localSettings)
                    ? RoamingDirectory
                    : LocalDirectory;
            }
            else if (hasLocal)
            {
                resolved = LocalDirectory;
            }
            else if (hasRoaming)
            {
                resolved = RoamingDirectory;
            }
            else
            {
                resolved = FileHelper.IsDirectoryWritable(LocalDirectory) ? LocalDirectory : RoamingDirectory;
            }
        }

        _current = resolved;
        return resolved;
    }

    /// <summary>
    /// The folder a mode points to. Does not check whether Portable is writable, that is the caller's job.
    /// </summary>
    public static string FolderFor(ConfigLocation mode)
    {
        if (OperatingSystem.IsMacOS())
        {
            return RoamingDirectory;
        }

        return mode switch
        {
            ConfigLocation.Portable => LocalDirectory,
            ConfigLocation.Roaming => RoamingDirectory,
            _ => FileHelper.IsDirectoryWritable(LocalDirectory) ? LocalDirectory : RoamingDirectory
        };
    }

    /// <summary>
    /// Moves every known config file from <paramref name="from" /> to <paramref name="to" />, overwriting the target.
    /// An empty source file is deleted instead, so it never replaces a real one. The caller must hold
    /// <see cref="Gate" />.
    /// </summary>
    /// <returns>False if any file failed; the others are still moved.</returns>
    internal static Task<bool> MoveAllAsync(string from, string to, bool includeSettings)
    {
        var ok = true;
        try
        {
            Directory.CreateDirectory(to);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ConfigFolders), nameof(MoveAllAsync), ex);
            return Task.FromResult(false);
        }

        foreach (var name in KnownFileNames)
        {
            if (!includeSettings && string.Equals(name, SettingsFileName, StringComparison.Ordinal))
            {
                continue;
            }

            var source = Path.Combine(from, name);
            try
            {
                var info = new FileInfo(source);
                if (!info.Exists)
                {
                    continue;
                }

                if (info.Length == 0)
                {
                    info.Delete();
                    continue;
                }

                File.Move(source, Path.Combine(to, name), overwrite: true);
            }
            catch (Exception ex)
            {
                DebugHelper.LogDebug(nameof(ConfigFolders), nameof(MoveAllAsync), ex);
                ok = false;
            }
        }

        return Task.FromResult(ok);
    }

    internal static void SetCurrent(string directory) => _current = directory;

    internal static void UseRoots(string localDirectory, string roamingDirectory)
    {
        LocalDirectory = localDirectory;
        RoamingDirectory = roamingDirectory;
        _current = null;
    }

    internal static void ResetRoots() => UseRoots(DefaultLocalDirectory, DefaultRoamingDirectory);

    private static bool HasContent(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists && info.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    internal static bool IsSame(string a, string b) =>
        string.Equals(Path.TrimEndingDirectorySeparator(a), Path.TrimEndingDirectorySeparator(b),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
