using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Core.Keybindings;

public static class KeybindingFunctions
{
    public static KeyBindingsConfiguration? KeyBindingsConfiguration { get; private set; }
    public static string CurrentKeybindingsPath => KeyBindingsConfiguration?.FilePath ?? string.Empty;

    /// <summary>
    /// Reads and writes keybindings at an explicit file path (local-substitutable seam for testing).
    /// </summary>
    internal static void UseConfigPath(string configPath) =>
        KeyBindingsConfiguration = new KeyBindingsConfiguration { PathOverride = configPath };

    /// <summary>
    /// Forgets the keybindings configuration, including any path override (test seam).
    /// </summary>
    internal static void ResetConfiguration() => KeyBindingsConfiguration = null;

    public static async Task SaveKeyBindingsFile(string json)
    {
        KeyBindingsConfiguration ??= new KeyBindingsConfiguration();
        await ConfigFileManager.WriteTextAsync(KeyBindingsConfiguration, json).ConfigureAwait(false);
    }

    public static byte[]? SetKeyBindingsFileAndReturnBytes()
    {
        KeyBindingsConfiguration ??= new KeyBindingsConfiguration();
        var info = new FileInfo(KeyBindingsConfiguration.FilePath);
        if (!info.Exists || info.Length == 0)
        {
            return null;
        }

        return File.ReadAllBytes(info.FullName);
    }
}