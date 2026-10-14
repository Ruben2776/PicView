namespace PicView.Avalonia.Input;

public enum KeymapOwner
{
    None = 0,
    Action,
    Favorite
}

/// <summary>
/// The owner of a <see cref="Keybind"/> in a <see cref="Keymap"/>.
/// <see cref="Value"/> is the canonical action name or the favorite folder path.
/// </summary>
public readonly record struct KeymapResolution(KeymapOwner Owner, string Value)
{
    public static KeymapResolution None => default;

    public static KeymapResolution Action(string name) => new(KeymapOwner.Action, name);

    public static KeymapResolution Favorite(string path) => new(KeymapOwner.Favorite, path);
}
