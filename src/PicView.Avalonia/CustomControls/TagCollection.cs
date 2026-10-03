using System.Collections.ObjectModel;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> of strings that enforces an optional maximum tag count.
/// </summary>
public class TagCollection : ObservableCollection<string>
{
    private readonly int _maxTags;

    public TagCollection(TagBox owner)
    {
        Owner = owner;
    }

    public TagCollection(int maxTags)
    {
        _maxTags = maxTags;
    }

    public TagCollection(IEnumerable<string> collection, int maxTags = 0) : base(collection)
    {
        _maxTags = maxTags;
        if (_maxTags > 0 && Count > _maxTags)
        {
            throw new ArgumentException($"A maximum of {_maxTags} tags are allowed.", nameof(collection));
        }
    }

    public TagBox? Owner { get; internal set; }

    public int MaxTags => Owner?.MaxTags ?? _maxTags;

    protected override void InsertItem(int index, string item)
    {
        var max = MaxTags;
        if (max > 0 && Count >= max)
        {
            throw new InvalidOperationException($"A maximum of {max} tags are allowed.");
        }

        base.InsertItem(index, item);
    }
}
