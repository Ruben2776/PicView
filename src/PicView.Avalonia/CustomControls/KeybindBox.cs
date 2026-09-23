using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using PicView.Avalonia.Input;
using MouseButton = PicView.Avalonia.Input.MouseButton;

namespace PicView.Avalonia.CustomControls;

[TemplatePart(PartTagBox, typeof(TagBox))]
public class KeybindBox : TemplatedControl
{
    public const string PartTagBox = "PART_TagBox";

    private bool _isSyncing;

    internal TagBox? TagBox { get; private set; }

    /// <summary>
    /// Defines the <see cref="PlaceholderText"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<KeybindBox, string?>(nameof(PlaceholderText));

    /// <summary>
    /// Gets or sets the placeholder text.
    /// </summary>
    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="MaxTags"/> property.
    /// </summary>
    public static readonly StyledProperty<int> MaxTagsProperty =
        TagBox.MaxTagsProperty.AddOwner<KeybindBox>(new StyledPropertyMetadata<int>(defaultValue: 2));

    /// <summary>
    /// Gets or sets the maximum number of keybind tags allowed.
    /// </summary>
    public int MaxTags
    {
        get => GetValue(MaxTagsProperty);
        set => SetValue(MaxTagsProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="Keybinds"/> property.
    /// </summary>
    public static readonly StyledProperty<ObservableCollection<Keybind>> KeybindsProperty =
        AvaloniaProperty.Register<KeybindBox, ObservableCollection<Keybind>>(nameof(Keybinds));

    /// <summary>
    /// Gets or sets the collection of keybinds.
    /// </summary>
    public ObservableCollection<Keybind> Keybinds
    {
        get => GetValue(KeybindsProperty);
        set => SetValue(KeybindsProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="Tags"/> property.
    /// </summary>
    public static readonly StyledProperty<ObservableCollection<string>> TagsProperty =
        AvaloniaProperty.Register<KeybindBox, ObservableCollection<string>>(nameof(Tags));

    /// <summary>
    /// Gets or sets the collection of tag strings displayed via the TagBox.
    /// </summary>
    public ObservableCollection<string> Tags
    {
        get => GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    static KeybindBox()
    {
        KeybindsProperty.Changed.AddClassHandler<KeybindBox>((box, e) => box.OnKeybindsChanged(e));
        TagsProperty.Changed.AddClassHandler<KeybindBox>((box, e) => box.OnTagsChanged(e));
        MaxTagsProperty.Changed.AddClassHandler<KeybindBox>((box, e) => box.OnMaxTagsChanged(e));
    }

    public KeybindBox()
    {
        TagBox = new TagBox { MaxTags = 2 };
        Tags = TagBox.Tags;
#if DEBUG
        // Temp test
        Keybinds = [with([new Keybind(Key.A), new Keybind(MouseButton.Left, KeyModifiers.Alt)])];
#else
        Keybinds = [];
#endif
    }

    private void OnMaxTagsChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (TagBox is not null && e.NewValue is int maxTags)
        {
            TagBox.MaxTags = maxTags;
        }
    }

    private void OnKeybindsChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= OnKeybindsCollectionChanged;
        }

        if (e.NewValue is ObservableCollection<Keybind> newCollection)
        {
            if (MaxTags > 0 && newCollection.Count > MaxTags)
            {
                throw new ArgumentException($"A maximum of {MaxTags} keybinds are allowed.", nameof(Keybinds));
            }

            newCollection.CollectionChanged += OnKeybindsCollectionChanged;
        }

        SyncKeybindsToTags();
    }

    private void OnTagsChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= OnTagsCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += OnTagsCollectionChanged;
        }

        if (TagBox is not null && Tags is not null && !ReferenceEquals(TagBox.Tags, Tags))
        {
            TagBox.Tags = Tags;
        }
    }

    private void OnKeybindsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;
        try
        {
            if (Tags is null || Keybinds is null)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems is not null)
                    {
                        var targetIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : Tags.Count;
                        foreach (Keybind item in e.NewItems)
                        {
                            try
                            {
                                Tags.Insert(targetIndex++, item.ToString());
                            }
                            catch
                            {
                                Keybinds.Remove(item);
                                throw;
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems is not null)
                    {
                        foreach (Keybind item in e.OldItems)
                        {
                            var tag = item.ToString();
                            Tags.Remove(tag);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.NewItems is not null && e.OldItems is not null)
                    {
                        for (var i = 0; i < e.NewItems.Count; i++)
                        {
                            var replaceIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex + i : -1;
                            var newKeybind = (Keybind)e.NewItems[i]!;
                            if (replaceIndex >= 0 && replaceIndex < Tags.Count)
                            {
                                Tags[replaceIndex] = newKeybind.ToString();
                            }
                            else
                            {
                                var oldTag = ((Keybind)e.OldItems[i]!).ToString();
                                var idx = Tags.IndexOf(oldTag);
                                if (idx >= 0)
                                {
                                    Tags[idx] = newKeybind.ToString();
                                }
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Tags.Clear();
                    for (var i = 0; i < Keybinds.Count; i++)
                    {
                        Tags.Add(Keybinds[i].ToString());
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0 &&
                        e.OldStartingIndex < Tags.Count && e.NewStartingIndex < Tags.Count)
                    {
                        Tags.Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    break;
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void OnTagsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;
        try
        {
            if (Keybinds is null || Tags is null)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems is not null)
                    {
                        foreach (string tag in e.OldItems)
                        {
                            for (var i = 0; i < Keybinds.Count; i++)
                            {
                                if (Keybinds[i].ToString() == tag)
                                {
                                    Keybinds.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Keybinds.Clear();
                    break;

                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems is not null)
                    {
                        foreach (string tag in e.NewItems)
                        {
                            if (TagBox is not null && TagBox.MaxTags > 0 && Keybinds.Count >= TagBox.MaxTags)
                            {
                                break;
                            }
                            var keybind = Keybind.Parse(tag);
                            Keybinds.Add(keybind);
                        }
                    }
                    break;
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void SyncKeybindsToTags()
    {
        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;
        try
        {
            if (Tags is null)
            {
                return;
            }

            Tags.Clear();
            if (Keybinds is not null)
            {
                for (var i = 0; i < Keybinds.Count; i++)
                {
                    Tags.Add(Keybinds[i].ToString());
                }
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        TagBox = e.NameScope.Find<TagBox>(PartTagBox);
        if (TagBox is null || Tags is null)
        {
            return;
        }

        TagBox.MaxTags = MaxTags;
        TagBox.Tags = Tags;
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        if (ReferenceEquals(e.Source, this) && TagBox is not null && !TagBox.IsAtMax)
        {
            TagBox.Focus();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (TagBox is not null && !TagBox.IsAtMax)
        {
            TagBox.Focus();
        }
    }
}