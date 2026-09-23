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
    /// Defines the <see cref="Keybinds"/> property.
    /// </summary>
    public static readonly StyledProperty<ObservableCollection<Keybind>> KeybindsProperty =
        AvaloniaProperty.Register<KeybindBox, ObservableCollection<Keybind>>(nameof(Keybinds));

    /// <summary>
    /// Gets or sets the collection of keybinds. A maximum of two keybinds are allowed.
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
    }

    public KeybindBox()
    {
        Tags = [];
        #if DEBUG
        // Temp test
        Keybinds = new KeybindCollection([new Keybind(Key.A), new Keybind(MouseButton.Left, KeyModifiers.Alt)]);
        #else
        Keybinds = new KeybindCollection();
        #endif
    }

    private void OnKeybindsChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= OnKeybindsCollectionChanged;
        }

        if (e.NewValue is ObservableCollection<Keybind> newCollection)
        {
            if (newCollection.Count > KeybindCollection.MaxKeybinds)
            {
                throw new ArgumentException($"A maximum of {KeybindCollection.MaxKeybinds} keybinds are allowed.", nameof(Keybinds));
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

        if (TagBox is not null && Tags is not null)
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

        if (Keybinds is not null && Keybinds.Count > KeybindCollection.MaxKeybinds)
        {
            _isSyncing = true;
            try
            {
                if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
                {
                    foreach (Keybind item in e.NewItems)
                    {
                        Keybinds.Remove(item);
                    }
                }
            }
            finally
            {
                _isSyncing = false;
            }

            throw new InvalidOperationException($"A maximum of {KeybindCollection.MaxKeybinds} keybinds are allowed.");
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
                            Tags.Insert(targetIndex++, item.ToString());
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
                            if (Keybinds.Count >= KeybindCollection.MaxKeybinds)
                            {
                                throw new InvalidOperationException($"A maximum of {KeybindCollection.MaxKeybinds} keybinds are allowed.");
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
        if (TagBox is not null && Tags is not null)
        {
            TagBox.Tags = Tags;
        }
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        if (ReferenceEquals(e.Source, this))
        {
            TagBox?.Focus();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        TagBox?.Focus();
    }
}