namespace PicView.Avalonia.Input;

/// <summary>
/// Represents a snapshot of all keybinding assignments at a point in time.
/// </summary>
public sealed class KeybindSnapshot : IEquatable<KeybindSnapshot>
{
    public Dictionary<string, List<Keybind>> Bindings { get; }

    public KeybindSnapshot(List<KeybindCategoryGroup> categories)
    {
        Bindings = new Dictionary<string, List<Keybind>>(StringComparer.Ordinal);
        foreach (var category in categories)
        {
            foreach (var (box, functionName) in category.Entries)
            {
                var keybinds = new List<Keybind>();
                if (box.Keybinds is not null)
                {
                    foreach (var kb in box.Keybinds)
                    {
                        keybinds.Add(kb);
                    }
                }
                Bindings[functionName] = keybinds;
            }
        }
    }

    #region IEquatable

    public bool Equals(KeybindSnapshot? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Bindings.Count != other.Bindings.Count)
        {
            return false;
        }

        foreach (var kvp in Bindings)
        {
            if (!other.Bindings.TryGetValue(kvp.Key, out var otherList))
            {
                return false;
            }

            if (kvp.Value.Count != otherList.Count)
            {
                return false;
            }

            for (var i = 0; i < kvp.Value.Count; i++)
            {
                if (kvp.Value[i] != otherList[i])
                {
                    return false;
                }
            }
        }

        return true;
    }

    public override bool Equals(object? obj) =>
        obj is KeybindSnapshot other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Bindings.Count);

        var entriesHash = 0;
        foreach (var (key, list) in Bindings)
        {
            var entryHash = new HashCode();
            entryHash.Add(key, StringComparer.Ordinal);
            entryHash.Add(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                entryHash.Add(list[i]);
            }

            entriesHash ^= entryHash.ToHashCode();
        }

        hash.Add(entriesHash);
        return hash.ToHashCode();
    }

    public static bool operator ==(KeybindSnapshot? left, KeybindSnapshot? right) =>
        ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    public static bool operator !=(KeybindSnapshot? left, KeybindSnapshot? right) =>
        !(left == right);

    #endregion
}
