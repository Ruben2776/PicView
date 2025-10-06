using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// A custom button control that displays text followed by an icon, which can be either a <see cref="DrawingImage"/>
/// or a <see cref="StreamGeometry"/>. It also supports dynamic brush changes to reflect hover states.
/// </summary>
public class TextIconButton : Button
{
    /// <summary>
    /// Defines the <see cref="Icon"/> property.
    /// The icon is displayed as a <see cref="DrawingImage"/> with support for dynamic brush changes.
    /// </summary>
    public static readonly AvaloniaProperty<DrawingImage?> IconProperty =
        AvaloniaProperty.Register<TextIconButton, DrawingImage?>(nameof(Icon));

    /// <summary>
    /// Defines the <see cref="Data"/> property.
    /// The icon can also be displayed as a <see cref="StreamGeometry"/> for path-based rendering.
    /// </summary>
    public static readonly AvaloniaProperty<StreamGeometry> PathProperty =
        AvaloniaProperty.Register<TextIconButton, StreamGeometry>(nameof(Data));

    /// <summary>
    /// Defines the <see cref="IconWidth"/> property.
    /// The width of the icon, whether it is a <see cref="DrawingImage"/> or <see cref="StreamGeometry"/>.
    /// </summary>
    public static readonly AvaloniaProperty<double> IconWidthProperty =
        AvaloniaProperty.Register<TextIconButton, double>(nameof(IconWidth));

    /// <summary>
    /// Defines the <see cref="IconHeight"/> property.
    /// The height of the icon, whether it is a <see cref="DrawingImage"/> or <see cref="StreamGeometry"/>.
    /// </summary>
    public static readonly AvaloniaProperty<double> IconHeightProperty =
        AvaloniaProperty.Register<TextIconButton, double>(nameof(IconHeight));

    /// <summary>
    /// Defines the maximum width for the text content of the button.
    /// This property restricts the width that the text within the button can occupy, potentially affecting text wrapping or truncation.
    /// </summary>
    public static readonly AvaloniaProperty<double> TextMaxWidthProperty =
        AvaloniaProperty.Register<TextIconButton, double>(nameof(TextMaxWidth), double.PositiveInfinity);

    /// <summary>
    /// Defines the <see cref="Text"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<TextBlock, string?>(nameof(Text));

    public static readonly StyledProperty<Thickness> IconMarginProperty =
        AvaloniaProperty.Register<Layoutable, Thickness>(nameof(IconMargin));

    /// <summary>
    /// Defines the <see cref="TextMargin"/> property.
    /// Specifies the margin around the text content of the button.
    /// </summary>
    public static readonly StyledProperty<Thickness> TextMarginProperty =
        AvaloniaProperty.Register<Layoutable, Thickness>(nameof(TextMargin));

    private TextBlock? _textBlock;

    /// <summary>
    /// Overrides the default style key to <see cref="Button"/>.
    /// </summary>
    protected override Type StyleKeyOverride => typeof(Button);

    /// <summary>
    /// Gets or sets the <see cref="DrawingImage"/> displayed as the icon of the button.
    /// </summary>
    [Content]
    public DrawingImage? Icon
    {
        get => (DrawingImage?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the margin around the icon.
    /// </summary>
    public Thickness IconMargin
    {
        get { return GetValue(IconMarginProperty); }
        set { SetValue(IconMarginProperty, value); }
    }

    /// <summary>
    /// Defines the <see cref="TextMaxWidth"/> property.
    /// Specifies the maximum width of the text content within the button.
    /// </summary>
    public double TextMaxWidth
    {
        get => (double)(GetValue(TextMaxWidthProperty) ?? 0);
        set => SetValue(TextMaxWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the margin around the text.
    /// </summary>
    public Thickness TextMargin
    {
        get { return GetValue(TextMarginProperty); }
        set { SetValue(TextMarginProperty, value); }
    }

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="StreamGeometry"/> used as the icon's path data.
    /// </summary>
    public StreamGeometry? Data
    {
        get => (StreamGeometry)GetValue(PathProperty)!;
        set => SetValue(PathProperty, value);
    }

    /// <summary>
    /// Gets or sets the width of the icon.
    /// </summary>
    public double IconWidth
    {
        get => (double)GetValue(IconWidthProperty)!;
        set => SetValue(IconWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the height of the icon.
    /// </summary>
    public double IconHeight
    {
        get => (double)GetValue(IconHeightProperty)!;
        set => SetValue(IconHeightProperty, value);
    }

    /// <summary>
    /// Called when the control is added to a visual tree. Initializes the content of the button with the icon.
    /// </summary>
    /// <param name="e">The event data associated with attaching the visual tree.</param>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Content = BuildControl();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty || change.Property == TextMaxWidthProperty)
        {
            Content = BuildControl();
        }

        if (change.Property == IsPressedProperty && !change.GetNewValue<bool>())
        {
            StopTimer();
        }

        if (change.Property == TextProperty)
        {
            _textBlock?.Text = change.NewValue.ToString();
        }
    }

    private StackPanel BuildControl()
    {
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };
        var icon = BuildIcon();
        icon.Margin = IconMargin;
        var textBlock = BuildText();
        stackPanel.Children.Add(icon);
        stackPanel.Children.Add(textBlock);
        _textBlock = textBlock;
        return stackPanel;
    }

    private TextBlock BuildText()
    {
        var textBlock = new TextBlock
        {
            Margin = TextMargin,
            Text = Text,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = FontSize,
            Foreground = Foreground,
            MaxWidth = TextMaxWidth
        };
        PointerEntered += (_, _) =>
        {
            textBlock.Foreground = UIHelper.GetBrush("SecondaryTextColor");
        };
        PointerExited += (_, _) =>
        {
            textBlock.Foreground = UIHelper.GetBrush("MainTextColor");
        };
        textBlock.Classes.Add("txt");
        return textBlock;
    }

    /// <summary>
    /// Builds the icon for the button, either from a <see cref="DrawingImage"/> or a <see cref="StreamGeometry"/>.
    /// It also sets up dynamic brush updates on mouse hover.
    /// </summary>
    /// <returns>A <see cref="Control"/> representing the icon, or <c>null</c> if no icon is set.</returns>
    private Control? BuildIcon()
    {
        if (Icon is { Drawing: DrawingGroup drawingGroup })
        {
            // Set the initial pen brush to match the Foreground color
            foreach (var drawing in drawingGroup.Children)
            {
                if (drawing is not GeometryDrawing { Pen: Pen pen })
                {
                    continue;
                }

                if (Settings.Theme.GlassTheme)
                {
                    pen.Brush = UIHelper.GetBrush("SecondaryTextColor");
                }
                else
                {
                    pen.Brush = UIHelper.GetBrush("MainTextColor");
                }
            }

            var image = new Image
            {
                Source = Icon,
                Width = IconWidth,
                Height = IconHeight
            };

            // Change brush to secondary accent color on pointer enter
            PointerEntered += delegate
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var brush = UIHelper.GetBrush("SecondaryTextColor");
                    foreach (var drawing in drawingGroup.Children)
                    {
                        if (drawing is GeometryDrawing { Pen: Pen pen })
                        {
                            pen.Brush = brush;
                        }
                    }
                });
            };

            // Revert brush to main text color on pointer exit
            PointerExited += delegate
            {
                if (Settings.Theme.GlassTheme)
                {
                    return;
                }

                Dispatcher.UIThread.Invoke(() =>
                {
                    foreach (var drawing in drawingGroup.Children)
                    {
                        if (drawing is GeometryDrawing { Pen: Pen pen })
                        {
                            pen.Brush = Foreground;
                        }
                    }
                });
            };

            return image;
        }

        // If no DrawingImage, use PathIcon
        // Make sure button has the hover class and the Foreground property is set
        if (Data is null)
        {
            return null;
        }

        var pathIcon = new PathIcon
        {
            Data = Data,
            Width = IconWidth,
            Height = IconHeight
        };

        PointerEntered += delegate
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                pathIcon.Foreground = UIHelper.GetBrush("SecondaryTextColor");
            });
        };

        PointerExited += (_, _) => { pathIcon.Foreground = Foreground; };

        return pathIcon;
    }

    #region Repeat

    /// <summary>
    /// Defines the <see cref="Interval"/> property.
    /// </summary>
    public static readonly StyledProperty<int> IntervalProperty =
        AvaloniaProperty.Register<RepeatButton, int>(nameof(Interval), 100);

    /// <summary>
    /// Defines the <see cref="Delay"/> property.
    /// </summary>
    public static readonly StyledProperty<int> DelayProperty =
        AvaloniaProperty.Register<RepeatButton, int>(nameof(Delay), 300);

    private DispatcherTimer? _repeatTimer;

    /// <summary>
    /// Gets or sets the amount of time, in milliseconds, of repeating clicks.
    /// </summary>
    public int Interval
    {
        get => GetValue(IntervalProperty);
        set => SetValue(IntervalProperty, value);
    }

    /// <summary>
    /// Gets or sets the amount of time, in milliseconds, to wait before repeating begins.
    /// </summary>
    public int Delay
    {
        get => GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    public static readonly StyledProperty<bool> IsRepeatEnabledProperty =
        AvaloniaProperty.Register<RepeatButton, bool>(nameof(IsRepeatEnabled), true);

    public bool IsRepeatEnabled
    {
        get => GetValue(IsRepeatEnabledProperty);
        set => SetValue(IsRepeatEnabledProperty, value);
    }

    private void StartTimer()
    {
        if (!IsRepeatEnabled)
        {
            return;
        }

        if (_repeatTimer == null)
        {
            _repeatTimer = new DispatcherTimer();
            _repeatTimer.Tick += RepeatTimerOnTick;
        }

        if (_repeatTimer.IsEnabled)
        {
            return;
        }

        _repeatTimer.Interval = TimeSpan.FromMilliseconds(Delay);
        _repeatTimer.Start();
    }

    private void RepeatTimerOnTick(object? sender, EventArgs e)
    {
        if (!IsRepeatEnabled)
        {
            return;
        }

        var interval = TimeSpan.FromMilliseconds(Interval);
        if (_repeatTimer!.Interval != interval)
        {
            _repeatTimer.Interval = interval;
        }

        OnClick();
    }

    private void StopTimer()
    {
        _repeatTimer?.Stop();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Space)
        {
            StartTimer();
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        StopTimer();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            StartTimer();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            StopTimer();
        }
    }

    #endregion
}