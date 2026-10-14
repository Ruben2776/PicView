using System.Runtime.InteropServices;
using Avalonia.Input;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.Input;

[StructLayout(LayoutKind.Auto)]
public readonly struct Keybind : IEquatable<Keybind>
{
    public const string CtrlModifier = "Ctrl";
    public const string ControlModifier = "Control";
    public const string ShiftModifier = "Shift";
    public const string AltModifier = "Alt";
    public const string OptionModifier = "Option";
    public const string WinModifier = "Win";
    public const string MetaModifier = "Meta";
    public const string CmdModifier = "Cmd";

    public const string MacCtrlModifier = "⌃ Ctrl";
    public const string MacShiftModifier = "⇧ Shift";
    public const string MacOptionModifier = "⌥ Option";
    public const string MacCmdModifier = "⌘ Cmd";

    public Key Key { get; }
    public MouseButton MouseButton { get; }
    public KeyModifiers Modifiers { get; }

    /// <summary>
    /// Initializes a new Keybind with a standard keyboard key.
    /// </summary>
    public Keybind(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        Key = key;
        MouseButton = MouseButton.None;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Initializes a new Keybind with a mouse button.
    /// </summary>
    public Keybind(MouseButton mouseButton, KeyModifiers modifiers = KeyModifiers.None)
    {
        Key = Key.None;
        MouseButton = mouseButton;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Creates a wheel Keybind named after the physical wheel direction: a positive <paramref name="deltaY"/> is up.
    /// </summary>
    public static Keybind FromWheel(double deltaY, KeyModifiers modifiers) =>
        new(deltaY > 0 ? MouseButton.WheelUp : MouseButton.WheelDown, modifiers);

    /// <summary>
    /// Parses a string into a Keybind struct without allocating intermediate strings.
    /// </summary>
    public static Keybind? Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            DebugHelper.LogDebug(nameof(Keybind), nameof(Parse), "Keybind string cannot be null or empty.");
            return null;
        }

        var mods = KeyModifiers.None;
        var key = Key.None;
        var mouseButton = MouseButton.None;

        var span = s.AsSpan();
        while (span.Length > 0)
        {
            var plusIndex = span.IndexOf('+');
            var part = plusIndex == -1 ? span : span.Slice(0, plusIndex);
            part = part.Trim();

            if (part.Equals(CtrlModifier, StringComparison.OrdinalIgnoreCase) ||
                part.Equals(ControlModifier, StringComparison.OrdinalIgnoreCase))
            {
                mods |= KeyModifiers.Control;
            }
            else if (part.Equals(ShiftModifier, StringComparison.OrdinalIgnoreCase))
            {
                mods |= KeyModifiers.Shift;
            }
            else if (part.Equals(AltModifier, StringComparison.OrdinalIgnoreCase) ||
                     part.Equals(OptionModifier, StringComparison.OrdinalIgnoreCase))
            {
                mods |= KeyModifiers.Alt; // Parses both Alt and Mac's Option mapped to Alt
            }
            else if (part.Equals(WinModifier, StringComparison.OrdinalIgnoreCase) ||
                     part.Equals(MetaModifier, StringComparison.OrdinalIgnoreCase) ||
                     part.Equals(CmdModifier, StringComparison.OrdinalIgnoreCase))
            {
                mods |= KeyModifiers.Meta;
            }
            else if (Enum.TryParse<MouseButton>(part, true, out var mb) && mb != MouseButton.None)
            {
                mouseButton = mb;
            }
            else if (Enum.TryParse<Key>(part, true, out var k))
            {
                key = k;
            }

            if (plusIndex == -1)
            {
                break;
            }

            span = span.Slice(plusIndex + 1);
        }

        return key != Key.None ? new Keybind(key, mods) : new Keybind(mouseButton, mods);
    }

    /// <summary>
    /// Formats the keybind with platform-specific modifier icons for UI display.
    /// </summary>
    public string GetFormattedWithIcon() => Format(withIcon: true, OperatingSystem.IsMacOS());

    /// <inheritdoc cref="GetFormattedWithIcon()" />
    public string GetFormattedWithIcon(bool isMac) => Format(withIcon: true, isMac);

    /// <summary>
    /// Formats the keybind with standard modifier names suitable for JSON serialization and parsing.
    /// </summary>
    public string GetFormattedForJSON() => Format(withIcon: false, OperatingSystem.IsMacOS());

    /// <inheritdoc cref="GetFormattedForJSON()" />
    public string GetFormattedForJSON(bool isMac) => Format(withIcon: false, isMac);

    /// <summary>
    /// Formats the keybind for display.
    /// </summary>
    public override string ToString() => GetFormattedWithIcon();

    private string Format(bool withIcon, bool isMac)
    {
        // Up to four modifiers plus a key or mouse button
        var parts = new string[5];
        var count = 0;

        if (Modifiers.HasFlag(KeyModifiers.Control))
        {
            parts[count++] = withIcon && isMac ? MacCtrlModifier : CtrlModifier;
        }

        if (Modifiers.HasFlag(KeyModifiers.Shift))
        {
            parts[count++] = withIcon && isMac ? MacShiftModifier : ShiftModifier;
        }

        if (Modifiers.HasFlag(KeyModifiers.Alt))
        {
            parts[count++] = isMac
                ? withIcon ? MacOptionModifier : OptionModifier
                : AltModifier;
        }

        if (Modifiers.HasFlag(KeyModifiers.Meta))
        {
            parts[count++] = isMac
                ? withIcon ? MacCmdModifier : CmdModifier
                : WinModifier;
        }

        if (Key != Key.None)
        {
            parts[count++] = Key.ToString();
        }
        else if (MouseButton != MouseButton.None)
        {
            parts[count++] = MouseButton.ToString();
        }

        return string.Join(" + ", parts, 0, count);
    }

    #region IEquatable
    
    public bool Equals(Keybind other) =>
        Key == other.Key && MouseButton == other.MouseButton && Modifiers == other.Modifiers;

    public override bool Equals(object? obj) =>
        obj is Keybind other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Key, MouseButton, Modifiers);

    public static bool operator ==(Keybind left, Keybind right) =>
        left.Equals(right);

    public static bool operator !=(Keybind left, Keybind right) =>
        !left.Equals(right);
    
    #endregion
}
