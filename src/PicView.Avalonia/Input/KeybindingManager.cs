using PicView.Core.IPlatform;

namespace PicView.Avalonia.Input;

public static class KeybindingManager
{
    private static KeyBindingsService? _keyBindingsService;

    public static Dictionary<Keybind, string>? CustomShortcuts
    {
        get
        {
            _keyBindingsService ??= new KeyBindingsService();
            return _keyBindingsService.CustomShortcuts;
        }
    }

    public static void LoadKeybindings(IPlatformSpecificService platformSpecificService)
    {
        _keyBindingsService ??= new KeyBindingsService();
        _keyBindingsService.LoadKeybindings(platformSpecificService);
    }

    public static async ValueTask UpdateKeyBindingsFile()
    {
        _keyBindingsService ??= new KeyBindingsService();
        await _keyBindingsService.UpdateKeyBindingsFile().ConfigureAwait(false);
    }

    public static void PopulateCustomShortcuts(Dictionary<string, string> keyValues)
    {
        _keyBindingsService ??= new KeyBindingsService();
        _keyBindingsService.PopulateCustomShortcuts(keyValues);
    }

    public static void SetDefaultKeybindings(IPlatformSpecificService platformSpecificService)
    {
        _keyBindingsService ??= new KeyBindingsService();
        _keyBindingsService.SetDefaultKeybindings(platformSpecificService);
    }

    public static string? GetActionName(Keybind keybind)
    {
        _keyBindingsService ??= new KeyBindingsService();
        return _keyBindingsService.GetActionName(keybind);
    }

    /// <summary>
    /// Builds and returns a dictionary of default keybindings for the current platform.
    /// </summary>
    public static Dictionary<Keybind, string>? GetDefaultShortcuts(IPlatformSpecificService platformSpecificService)
    {
        _keyBindingsService ??= new KeyBindingsService();
        return _keyBindingsService.GetDefaultShortcuts(platformSpecificService);
    }
}
