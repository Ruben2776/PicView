using System.Runtime.InteropServices;
using Avalonia.Input;

namespace PicView.Avalonia.Input;

[StructLayout(LayoutKind.Auto)]
public readonly struct Keybind : IEquatable<Keybind>
{
    public Key Key { get; }
    public MouseButton MouseButton { get; }
    public KeyModifiers Modifiers { get; }

    /// <summary>
    /// Initializes a new Keybind with a standard keyboard key.
    /// </summary>
    public Keybind(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        ValidateModifiers(modifiers);
        Key = key;
        MouseButton = MouseButton.None;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Initializes a new Keybind with a mouse button.
    /// </summary>
    public Keybind(MouseButton mouseButton, KeyModifiers modifiers = KeyModifiers.None)
    {
        ValidateModifiers(modifiers);
        Key = Key.None;
        MouseButton = mouseButton;
        Modifiers = modifiers;
    }

    private static void ValidateModifiers(KeyModifiers modifiers)
    {
        var count = 0;
        if ((modifiers & KeyModifiers.Control) != 0)
        {
            count++;
        }

        if ((modifiers & KeyModifiers.Shift) != 0)
        {
            count++;
        }

        if ((modifiers & KeyModifiers.Alt) != 0)
        {
            count++;
        }

        if ((modifiers & KeyModifiers.Meta) != 0)
        {
            count++;
        }

        if (count > 2)
        {
            throw new ArgumentException("A maximum of two modifier keys are allowed.");
        }
    }

    /// <summary>
    /// Parses a string into a Keybind struct without allocating intermediate strings.
    /// </summary>
    public static Keybind Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new ArgumentException("Keybind string cannot be null or empty.");
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

            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                mods |= KeyModifiers.Control;
            }
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                mods |= KeyModifiers.Shift;
            }
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("Option", StringComparison.OrdinalIgnoreCase))
            {
                mods |= KeyModifiers.Alt; // Parses both Alt and Mac's Option mapped to Alt[cite: 1]
            }
            else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("Meta", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("Cmd", StringComparison.OrdinalIgnoreCase))
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
        var index = 0;

        if ((Modifiers & KeyModifiers.Control) != 0)
        {
            AddPart("Ctrl", ref p0, ref p1, ref p2, ref index);
        }

        if ((Modifiers & KeyModifiers.Shift) != 0)
        {
            AddPart("Shift", ref p0, ref p1, ref p2, ref index);
        }

        if ((Modifiers & KeyModifiers.Alt) != 0)
        {
            // Translates Alt to Option for macOS displays and configurations[cite: 1]
            AddPart(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Option" : "Alt", ref p0, ref p1, ref p2, ref index);
        }

        if ((Modifiers & KeyModifiers.Meta) != 0)
        {
            AddPart(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Cmd" : "Win", ref p0, ref p1, ref p2, ref index);
        }

        if (Key != Key.None)
        {
            AddPart(Key.ToString(), ref p0, ref p1, ref p2, ref index);
        }
        else if (MouseButton != MouseButton.None)
        {
            AddPart(MouseButton.ToString(), ref p0, ref p1, ref p2, ref index);
        }

        return index switch
        {
            1 => p0 ?? string.Empty,
            2 => string.Create(p0!.Length + 1 + p1!.Length, (p0, p1), static (destination, state) =>
            {
                var (first, second) = state;
                first.AsSpan().CopyTo(destination);
                destination[first.Length] = '+';
                second.AsSpan().CopyTo(destination[(first.Length + 1)..]);
            }),
            3 => string.Create(p0!.Length + 1 + p1!.Length + 1 + p2!.Length, (p0, p1, p2), static (destination, state) =>
            {
                var (first, second, third) = state;
                first.AsSpan().CopyTo(destination);
                var offset = first.Length;
                destination[offset++] = '+';
                second.AsSpan().CopyTo(destination[offset..]);
                offset += second.Length;
                destination[offset++] = '+';
                third.AsSpan().CopyTo(destination[offset..]);
            }),
            _ => string.Empty
        };

        static void AddPart(string part, ref string? p0, ref string? p1, ref string? p2, ref int index)
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