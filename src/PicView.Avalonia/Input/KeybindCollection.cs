using System.Collections.ObjectModel;

namespace PicView.Avalonia.Input;

public sealed class KeybindCollection : ObservableCollection<Keybind>
{
    public const int MaxKeybinds = 2;

    public KeybindCollection()
    {
    }

    public KeybindCollection(IEnumerable<Keybind> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (collection is ICollection<Keybind> { Count: > MaxKeybinds })
        {
            throw new ArgumentException($"A maximum of {MaxKeybinds} keybinds are allowed.", nameof(collection));
        }

        foreach (var item in collection)
        {
            if (Count >= MaxKeybinds)
            {
                throw new ArgumentException($"A maximum of {MaxKeybinds} keybinds are allowed.", nameof(collection));
            }

            Add(item);
        }
    }

    public KeybindCollection(List<Keybind> list) : base(list)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (list.Count > MaxKeybinds)
        {
            throw new ArgumentException($"A maximum of {MaxKeybinds} keybinds are allowed.", nameof(list));
        }
    }

    protected override void InsertItem(int index, Keybind item)
    {
        if (Count >= MaxKeybinds)
        {
            throw new InvalidOperationException($"A maximum of {MaxKeybinds} keybinds are allowed.");
        }

        base.InsertItem(index, item);
    }
}
