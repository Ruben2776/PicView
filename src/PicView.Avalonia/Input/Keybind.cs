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
    public const string CommandModifier = "Command";

    public const string ControlSymbol = "⌃";
    public const string OptionSymbol = "⌥";
    public const string ShiftSymbol = "⇧";
    public const string CmdSymbol = "⌘";

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

            if (part.StartsWith('⌃'))
            {
                mods |= KeyModifiers.Control;
                part = part[1..].Trim();
            }
            if (part.StartsWith('⇧'))
            {
                mods |= KeyModifiers.Shift;
                part = part[1..].Trim();
            }
            if (part.StartsWith('⌥'))
            {
                mods |= KeyModifiers.Alt;
                part = part[1..].Trim();
            }
            if (part.StartsWith('⌘'))
            {
                mods |= KeyModifiers.Meta;
                part = part[1..].Trim();
            }

            if (part.IsEmpty)
            {
                if (plusIndex == -1)
                {
                    break;
                }

                span = span.Slice(plusIndex + 1);
                continue;
            }

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
                     part.Equals(CmdModifier, StringComparison.OrdinalIgnoreCase) ||
                     part.Equals(CommandModifier, StringComparison.OrdinalIgnoreCase))
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
    /// Formats the keybind, automatically accounting for OS-specific modifier names.
    /// </summary>
    public override string ToString()
    {
        string? p0 = null;
        string? p1 = null;
        string? p2 = null;
        string? p3 = null;
        var index = 0;

        var isMac = OperatingSystem.IsMacOS();

        if (Modifiers.HasFlag(KeyModifiers.Control))
        {
            AddPart(isMac ? MacCtrlModifier : CtrlModifier, ref p0, ref p1, ref p2, ref p3, ref index);
        }

        if (Modifiers.HasFlag(KeyModifiers.Shift))
        {
            AddPart(isMac ? MacShiftModifier : ShiftModifier, ref p0, ref p1, ref p2, ref p3, ref index);
        }

        if (Modifiers.HasFlag(KeyModifiers.Alt))
        {
            // Translates Alt to Option for macOS displays and configurations
            AddPart(isMac ? MacOptionModifier : AltModifier, ref p0, ref p1, ref p2, ref p3, ref index);
        }

        if (Modifiers.HasFlag(KeyModifiers.Meta))
        {
            AddPart(isMac ? MacCmdModifier : WinModifier, ref p0, ref p1, ref p2, ref p3, ref index);
        }

        if (Key != Key.None)
        {
            AddPart(Key.ToString(), ref p0, ref p1, ref p2, ref p3, ref index);
        }
        else if (MouseButton != MouseButton.None)
        {
            AddPart(MouseButton.ToString(), ref p0, ref p1, ref p2, ref p3, ref index);
        }

        return index switch
        {
            1 => p0 ?? string.Empty,
            2 => string.Create(p0!.Length + 3 + p1!.Length, (p0, p1), static (destination, state) =>
            {
                var (first, second) = state;
                first.AsSpan().CopyTo(destination);
                destination[first.Length] = ' ';
                destination[first.Length + 1] = '+';
                destination[first.Length + 2] = ' ';
                second.AsSpan().CopyTo(destination[(first.Length + 3)..]);
            }),
            3 => string.Create(p0!.Length + 3 + p1!.Length + 3 + p2!.Length, (p0, p1, p2), static (destination, state) =>
            {
                var (first, second, third) = state;
                first.AsSpan().CopyTo(destination);
                var offset = first.Length;
                destination[offset++] = ' ';
                destination[offset++] = '+';
                destination[offset++] = ' ';
                second.AsSpan().CopyTo(destination[offset..]);
                offset += second.Length;
                destination[offset++] = ' ';
                destination[offset++] = '+';
                destination[offset++] = ' ';
                third.AsSpan().CopyTo(destination[offset..]);
            }),
            4 => string.Create(p0!.Length + 3 + p1!.Length + 3 + p2!.Length + 3 + p3!.Length, (p0, p1, p2, p3), static (destination, state) =>
            {
                var (first, second, third, fourth) = state;
                first.AsSpan().CopyTo(destination);
                var offset = first.Length;
                destination[offset++] = ' ';
                destination[offset++] = '+';
                destination[offset++] = ' ';
                second.AsSpan().CopyTo(destination[offset..]);
                offset += second.Length;
                destination[offset++] = ' ';
                destination[offset++] = '+';
                destination[offset++] = ' ';
                third.AsSpan().CopyTo(destination[offset..]);
                offset += third.Length;
                destination[offset++] = ' ';
                destination[offset++] = '+';
                destination[offset++] = ' ';
                fourth.AsSpan().CopyTo(destination[offset..]);
            }),
            _ => string.Empty
        };

        static void AddPart(string part, ref string? p0, ref string? p1, ref string? p2, ref string? p3, ref int index)
        {
            switch (index)
            {
                case 0:
                    p0 = part;
                    break;
                case 1:
                    p1 = part;
                    break;
                case 2:
                    p2 = part;
                    break;
                case 3:
                    p3 = part;
                    break;
            }
            index++;
        }
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
