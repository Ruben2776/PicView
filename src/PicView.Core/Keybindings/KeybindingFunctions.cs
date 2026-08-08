using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Core.Keybindings;

public static class KeybindingFunctions
{
    public static KeyBindingsConfiguration? KeyBindingsConfiguration { get; private set; }
    public static string CurrentKeybindingsPath => KeyBindingsConfiguration?.CorrectPath ?? string.Empty;
    
    public static async Task SaveKeyBindingsFile(string json)
    {
        try
        {
            KeyBindingsConfiguration ??= new KeyBindingsConfiguration();
            if (string.IsNullOrEmpty(KeyBindingsConfiguration.CorrectPath))
            {
                KeyBindingsConfiguration.CorrectPath = ConfigFileManager.ResolveDefaultConfigPath(KeyBindingsConfiguration);
            }
            var writer = new StreamWriter(CurrentKeybindingsPath);
            await using (writer.ConfigureAwait(false))
            {
                await writer.WriteAsync(json).ConfigureAwait(false);
            }
        }
        catch (UnauthorizedAccessException)
        {
            var writer = new StreamWriter(KeyBindingsConfiguration.RoamingConfigPath);
            await using (writer.ConfigureAwait(false))
            {
                await writer.WriteAsync(json).ConfigureAwait(false);
            }
        }
    }

    public static async Task<string?> LoadKeyBindingsFile()
    {
        var path = ConfigFileManager.ResolveDefaultConfigPath(KeyBindingsConfiguration ??= new KeyBindingsConfiguration());
        if (!File.Exists(path))
        {
            return null;
        }

        var text = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        
        KeyBindingsConfiguration.CorrectPath = path;
        return text;
    }
}