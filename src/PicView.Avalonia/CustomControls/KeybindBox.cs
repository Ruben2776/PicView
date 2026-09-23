using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.Input;
using MouseButton = PicView.Avalonia.Input.MouseButton;

namespace PicView.Avalonia.CustomControls;

[TemplatePart(PartTagBox, typeof(TagBox))]
public class KeybindBox : TemplatedControl
{
    public const string PartTagBox = "PART_TagBox";

    private bool _isSyncing;
    private Key _lastKeyDown = Key.None;
    private KeyModifiers _lastModifiers = KeyModifiers.None;

    internal TagBox? TagBox { get; private set; }
    
    public static readonly StyledProperty<string?> ActionNameProperty =
        AvaloniaProperty.Register<KeybindBox, string?>(nameof(ActionName));

    /// <summary>
    /// Gets or sets the placeholder text.
    /// </summary>
    public string? ActionName
    {
        get => GetValue(ActionNameProperty);
        set => SetValue(ActionNameProperty, value);
    }

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
        Keybinds = [];
        SubscribeToTagBox(TagBox);
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
        if (TagBox is not null)
        {
            UnsubscribeFromTagBox(TagBox);
        }

        TagBox = e.NameScope.Find<TagBox>(PartTagBox);
        if (TagBox is null || Tags is null)
        {
            return;
        }

        TagBox.MaxTags = MaxTags;
        TagBox.Tags = Tags;
        SubscribeToTagBox(TagBox);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _lastKeyDown = Key.None;
        _lastModifiers = KeyModifiers.None;
        MainKeyboardShortcuts.IsEscKeyEnabled = true;
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
        if (!e.Handled)
        {
            HandlePointerPressed(e);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!e.Handled)
        {
            HandlePointerReleased(e);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled)
        {
            HandleKeyDown(e);
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (!e.Handled)
        {
            HandleKeyUp(e);
        }
    }

    private void SubscribeToTagBox(TagBox? tagBox)
    {
        if (tagBox is null)
        {
            return;
        }

        tagBox.AddHandler(KeyDownEvent, OnTagBoxKeyDown, RoutingStrategies.Bubble);
        tagBox.AddHandler(KeyUpEvent, OnTagBoxKeyUp, RoutingStrategies.Bubble);
        tagBox.AddHandler(PointerPressedEvent, OnTagBoxPointerPressed, RoutingStrategies.Bubble);
        tagBox.AddHandler(PointerReleasedEvent, OnTagBoxPointerReleased, RoutingStrategies.Bubble);
        tagBox.GotFocus += OnTagBoxGotFocus;
        tagBox.LostFocus += OnTagBoxLostFocus;
    }

    private void UnsubscribeFromTagBox(TagBox? tagBox)
    {
        if (tagBox is null)
        {
            return;
        }

        tagBox.RemoveHandler(KeyDownEvent, OnTagBoxKeyDown);
        tagBox.RemoveHandler(KeyUpEvent, OnTagBoxKeyUp);
        tagBox.RemoveHandler(PointerPressedEvent, OnTagBoxPointerPressed);
        tagBox.RemoveHandler(PointerReleasedEvent, OnTagBoxPointerReleased);
        tagBox.GotFocus -= OnTagBoxGotFocus;
        tagBox.LostFocus -= OnTagBoxLostFocus;
    }

    private void OnTagBoxGotFocus(object? sender, FocusChangedEventArgs e)
    {
        MainKeyboardShortcuts.IsEscKeyEnabled = false;
    }

    private void OnTagBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        _lastKeyDown = Key.None;
        _lastModifiers = KeyModifiers.None;
        MainKeyboardShortcuts.IsEscKeyEnabled = true;
    }

    private void OnTagBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.Handled)
        {
            HandleKeyDown(e);
        }
    }

    private void OnTagBoxKeyUp(object? sender, KeyEventArgs e)
    {
        if (!e.Handled)
        {
            HandleKeyUp(e);
        }
    }

    private void OnTagBoxPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.Handled)
        {
            HandlePointerPressed(e);
        }
    }

    private void OnTagBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!e.Handled)
        {
            HandlePointerReleased(e);
        }
    }

    private void HandleKeyDown(KeyEventArgs e)
    {
        if (TagBox is null || (!TagBox.IsFocused && !TagBox.IsKeyboardFocusWithin) || TagBox.IsAtMax)
        {
            return;
        }

        if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            _lastKeyDown = Key.None;
            _lastModifiers = KeyModifiers.None;
            e.Handled = true;
            TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
            return;
        }

        if (IsModifierKey(e.Key))
        {
            _lastModifiers |= e.KeyModifiers;
            return;
        }

        _lastKeyDown = e.Key;
        _lastModifiers = e.KeyModifiers | _lastModifiers;
        e.Handled = true;
    }

    private void HandleKeyUp(KeyEventArgs e)
    {
        if (TagBox is null || (!TagBox.IsFocused && !TagBox.IsKeyboardFocusWithin) || TagBox.IsAtMax)
        {
            return;
        }

        if (IsModifierKey(e.Key))
        {
            return;
        }

        var keyToCommit = _lastKeyDown != Key.None ? _lastKeyDown : e.Key;
        if (keyToCommit == Key.None || IsModifierKey(keyToCommit))
        {
            return;
        }

        var modifiers = _lastModifiers | e.KeyModifiers;
        _lastKeyDown = Key.None;
        _lastModifiers = KeyModifiers.None;

        if (HasTooManyModifiers(modifiers))
        {
            e.Handled = true;
            return;
        }

        var keybind = new Keybind(keyToCommit, modifiers);
        TryAddKeybind(keybind);
        e.Handled = true;
    }

    private void HandlePointerPressed(PointerPressedEventArgs e)
    {
        if (TagBox is null || TagBox.IsAtMax)
        {
            return;
        }

        var isFocused = TagBox.IsFocused || TagBox.IsKeyboardFocusWithin;

        if (TryGetBindableMouseButton(e, this, out var mouseButton))
        {
            if (isFocused)
            {
                var modifiers = e.KeyModifiers | _lastModifiers;
                if (!HasTooManyModifiers(modifiers))
                {
                    var keybind = new Keybind(mouseButton, modifiers);
                    TryAddKeybind(keybind);
                }
                e.Handled = true;
            }
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (!isFocused)
            {
                TagBox.Focus();
            }
        }
    }

    private void HandlePointerReleased(PointerReleasedEventArgs e)
    {
        if (TagBox is null || TagBox.IsAtMax)
        {
            return;
        }

        var isFocused = TagBox.IsFocused || TagBox.IsKeyboardFocusWithin;
        if (isFocused && TryGetBindableMouseButton(e, this, out var mouseButton))
        {
            var modifiers = e.KeyModifiers | _lastModifiers;
            if (!HasTooManyModifiers(modifiers))
            {
                var keybind = new Keybind(mouseButton, modifiers);
                TryAddKeybind(keybind);
            }
            e.Handled = true;
        }
    }

    private void TryAddKeybind(Keybind keybind)
    {
        if (Keybinds is null)
        {
            Keybinds = [];
        }

        if (TagBox is not null && TagBox.IsAtMax)
        {
            return;
        }

        if (MaxTags > 0 && Keybinds.Count >= MaxTags)
        {
            return;
        }

        if (Keybinds.Contains(keybind))
        {
            return;
        }

        Keybinds.Add(keybind);
    }

    private static bool TryGetBindableMouseButton(PointerEventArgs e, Visual? visual, out MouseButton mouseButton)
    {
        mouseButton = MouseButton.None;
        PointerPointProperties props;
        try
        {
            props = e.GetCurrentPoint(visual).Properties;
        }
        catch
        {
            props = e.GetCurrentPoint(null).Properties;
        }

        if (props.PointerUpdateKind == PointerUpdateKind.MiddleButtonPressed ||
            props.PointerUpdateKind == PointerUpdateKind.MiddleButtonReleased ||
            props.IsMiddleButtonPressed)
        {
            mouseButton = MouseButton.Middle;
            return true;
        }

        if (props.PointerUpdateKind == PointerUpdateKind.XButton1Pressed ||
            props.PointerUpdateKind == PointerUpdateKind.XButton1Released ||
            props.IsXButton1Pressed)
        {
            mouseButton = MouseButton.XButton1;
            return true;
        }

        if (props.PointerUpdateKind == PointerUpdateKind.XButton2Pressed ||
            props.PointerUpdateKind == PointerUpdateKind.XButton2Released ||
            props.IsXButton2Pressed)
        {
            mouseButton = MouseButton.XButton2;
            return true;
        }

        return false;
    }

    private static bool IsModifierKey(Key key) => key switch
    {
        Key.LeftCtrl or Key.RightCtrl or
        Key.LeftShift or Key.RightShift or
        Key.LeftAlt or Key.RightAlt or
        Key.LWin or Key.RWin => true,
        _ => false
    };

    private static bool HasTooManyModifiers(KeyModifiers modifiers)
    {
        var count = 0;
        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            count++;
        }
        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            count++;
        }
        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            count++;
        }
        if (modifiers.HasFlag(KeyModifiers.Meta))
        {
            count++;
        }
        return count > 2;
    }
}