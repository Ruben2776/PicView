using System.Runtime.InteropServices;

namespace PicView.Avalonia.Input;

/// <summary>
/// The owner of a <see cref="Keybind"/> in a <see cref="Keymap"/>.
/// <see cref="Value"/> is the canonical action name.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct KeymapResolution(string Value)
{
    public static KeymapResolution None => default;

    public static KeymapResolution Action(string name) => new(name);
}
