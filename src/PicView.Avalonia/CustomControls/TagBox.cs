using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Metadata;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// A tag-chip container that displays a collection of removable tag chips inside a bordered box.
/// Each tag chip shows a text label and an "×" button to remove it.
/// </summary>
[TemplatePart(PartTextPresenter, typeof(TextPresenter))]
[PseudoClasses(Empty, Max)]
public class TagBox : TemplatedControl
{
    public const string PartTextPresenter = "PART_TextPresenter";
    public const string Empty = ":empty";
    public const string Max = ":max";

    internal TextPresenter? Presenter { get; private set; }

    public static readonly StyledProperty<ObservableCollection<string>> TagsProperty =
        AvaloniaProperty.Register<TagBox, ObservableCollection<string>>(nameof(Tags));

    /// <summary>Gets or sets the collection of tag strings displayed as chips.</summary>
    public ObservableCollection<string> Tags
    {
        get => GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    public static readonly StyledProperty<int> MaxTagsProperty =
        AvaloniaProperty.Register<TagBox, int>(nameof(MaxTags), defaultValue: 0);

    /// <summary>
    /// Gets or sets the maximum number of tags allowed. 0 or less indicates no limit.
    /// </summary>
    public int MaxTags
    {
        get => GetValue(MaxTagsProperty);
        set => SetValue(MaxTagsProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether the maximum number of tags has been reached.
    /// </summary>
    public bool IsAtMax => MaxTags > 0 && Tags is not null && Tags.Count >= MaxTags;

    public static readonly StyledProperty<ICommand?> RemoveTagCommandProperty =
        AvaloniaProperty.Register<TagBox, ICommand?>(nameof(RemoveTagCommand));

    /// <summary>
    /// Optional external command. When set it is called instead of the built-in remove logic.
    /// The command parameter is the tag string to remove.
    /// </summary>
    public ICommand? RemoveTagCommand
    {
        get => GetValue(RemoveTagCommandProperty);
        set => SetValue(RemoveTagCommandProperty, value);
    }

    #region TextPresenter

    /// <summary>
    /// Defines the <see cref="CaretBrush"/> property
    /// </summary>
    public static readonly StyledProperty<IBrush?> CaretBrushProperty =
        AvaloniaProperty.Register<TagBox, IBrush?>(nameof(CaretBrush));

    /// <summary>
    /// Defines the <see cref="CaretBlinkInterval"/> property
    /// </summary>
    public static readonly StyledProperty<TimeSpan> CaretBlinkIntervalProperty =
        AvaloniaProperty.Register<TagBox, TimeSpan>(nameof(CaretBlinkInterval), defaultValue: TimeSpan.FromMilliseconds(500));

    /// <summary>
    /// Defines the <see cref="CaretIndex"/> property
    /// </summary>
    public static readonly StyledProperty<int> CaretIndexProperty =
        AvaloniaProperty.Register<TagBox, int>(nameof(CaretIndex));

    /// <summary>
    /// Defines see <see cref="TextPresenter.LineHeight"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LineHeightProperty =
        TextBlock.LineHeightProperty.AddOwner<TagBox>(new StyledPropertyMetadata<double>(defaultValue: 16));

    /// <summary>
    /// Gets or sets a brush that is used for the text caret
    /// </summary>
    public IBrush? CaretBrush
    {
        get => GetValue(CaretBrushProperty);
        set => SetValue(CaretBrushProperty, value);
    }

    /// <inheritdoc cref="TextPresenter.CaretBlinkInterval"/>
    public TimeSpan CaretBlinkInterval
    {
        get => GetValue(CaretBlinkIntervalProperty);
        set => SetValue(CaretBlinkIntervalProperty, value);
    }

    /// <summary>
    /// Gets or sets the caret index.
    /// </summary>
    public int CaretIndex
    {
        get => GetValue(CaretIndexProperty);
        set => SetValue(CaretIndexProperty, value);
    }

    /// <summary>
    /// Gets or sets the line height.
    /// </summary>
    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="PlaceholderText"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<TagBox, string?>(nameof(PlaceholderText));

    /// <summary>
    /// Gets or sets the placeholder text in the tag text box
    /// </summary>
    [Content]
    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    #endregion

    static TagBox()
    {
        TagsProperty.Changed.AddClassHandler<TagBox>((box, e) => box.OnTagsChanged(e));
        MaxTagsProperty.Changed.AddClassHandler<TagBox>((box, e) => box.OnMaxTagsChanged(e));
    }

    public TagBox()
    {
        Tags = new TagCollection(this);
        RemoveTagCommand = new TagRemoveCommand(this);
    }

    private void OnMaxTagsChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is int newMax && newMax > 0 && Tags is not null && Tags.Count > newMax)
        {
            throw new ArgumentException($"A maximum of {newMax} tags are allowed.", nameof(MaxTags));
        }

        UpdateMaxPseudoClass();
    }

    private void OnTagsChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is TagCollection oldTagCol && ReferenceEquals(oldTagCol.Owner, this))
        {
            oldTagCol.Owner = null;
        }

        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= OnTagsCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            if (e.NewValue is TagCollection newTagCol)
            {
                newTagCol.Owner = this;
            }

            if (e.NewValue is ObservableCollection<string> newCol && MaxTags > 0 && newCol.Count > MaxTags)
            {
                throw new ArgumentException($"A maximum of {MaxTags} tags are allowed.", nameof(Tags));
            }

            newCollection.CollectionChanged += OnTagsCollectionChanged;
        }

        UpdateEmptyPseudoClass();
        UpdateMaxPseudoClass();
    }

    private void OnTagsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateEmptyPseudoClass();
        UpdateMaxPseudoClass();

        if (MaxTags > 0 && Tags is not null && Tags.Count > MaxTags)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            {
                try
                {
                    foreach (string item in e.NewItems)
                    {
                        Tags.Remove(item);
                    }
                }
                catch (InvalidOperationException)
                {
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        foreach (string item in e.NewItems)
                        {
                            Tags.Remove(item);
                        }
                    });
                }
            }

            throw new InvalidOperationException($"A maximum of {MaxTags} tags are allowed.");
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (IsFocused && !IsAtMax)
        {
            Presenter?.ShowCaret();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Presenter?.HideCaret();
        if (Tags is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged -= OnTagsCollectionChanged;
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Presenter = e.NameScope.Find<TextPresenter>(PartTextPresenter);
        UpdateEmptyPseudoClass();
        UpdateMaxPseudoClass();
        if (IsFocused && !IsAtMax)
        {
            Presenter?.ShowCaret();
        }
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        if (!IsAtMax)
        {
            Presenter?.ShowCaret();
        }
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        Presenter?.HideCaret();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (IsAtMax)
            {
                e.Handled = true;
                return;
            }

            Focus();
            if (Presenter is not null)
            {
                var point = e.GetPosition(Presenter);
                Presenter.MoveCaretToPoint(point);
                SetCurrentValue(CaretIndexProperty, Presenter.CaretIndex);
                Presenter.ShowCaret();
            }
            e.Handled = true;
        }
    }

    internal void RemoveTag(string tag)
    {
        if (RemoveTagCommand is not null and not TagRemoveCommand && RemoveTagCommand.CanExecute(tag))
        {
            RemoveTagCommand.Execute(tag);
        }
        else
        {
            Tags?.Remove(tag);
        }
        UpdateEmptyPseudoClass();
        UpdateMaxPseudoClass();
    }

    private void UpdateEmptyPseudoClass()
    {
        PseudoClasses.Set(Empty, Tags is null || Tags.Count is 0);
    }

    private void UpdateMaxPseudoClass()
    {
        var isAtMax = IsAtMax;
        PseudoClasses.Set(Max, isAtMax);
        if (isAtMax)
        {
            Presenter?.HideCaret();
        }
        else if (IsFocused)
        {
            Presenter?.ShowCaret();
        }
    }

    private sealed class TagRemoveCommand(TagBox owner) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => parameter is string;

        public void Execute(object? parameter)
        {
            if (parameter is string tag)
            {
                owner.Tags?.Remove(tag);
                owner.UpdateEmptyPseudoClass();
                owner.UpdateMaxPseudoClass();
            }
        }
    }
}