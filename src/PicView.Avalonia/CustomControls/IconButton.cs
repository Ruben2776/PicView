using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// A custom button control that displays an icon, which can be either a <see cref="DrawingImage"/>
/// or a <see cref="StreamGeometry"/>. It also supports dynamic brush changes to reflect hover states.
/// </summary>
public class IconButton : Button
{
    /// <summary>
    /// Defines the <see cref="Icon"/> property.
    /// The icon is displayed as a <see cref="DrawingImage"/> with support for dynamic brush changes.
    /// </summary>
    public static readonly AvaloniaProperty<DrawingImage?> IconProperty =
        AvaloniaProperty.Register<IconButton, DrawingImage?>(nameof(Icon));

    /// <summary>
    /// Defines the <see cref="Data"/> property.
    /// The icon can also be displayed as a <see cref="StreamGeometry"/> for path-based rendering.
    /// </summary>
    public static readonly AvaloniaProperty<StreamGeometry> PathProperty =
        AvaloniaProperty.Register<CopyButton, StreamGeometry>(nameof(Data));

    /// <summary>
    /// Defines the <see cref="IconWidth"/> property.
    /// The width of the icon, whether it is a <see cref="DrawingImage"/> or <see cref="StreamGeometry"/>.
    /// </summary>
    public static readonly AvaloniaProperty<double> IconWidthProperty =
        AvaloniaProperty.Register<IconButton, double>(nameof(IconWidth));

    /// <summary>
    /// Defines the <see cref="IconHeight"/> property.
    /// The height of the icon, whether it is a <see cref="DrawingImage"/> or <see cref="StreamGeometry"/>.
    /// </summary>
    public static readonly AvaloniaProperty<double> IconHeightProperty =
        AvaloniaProperty.Register<IconButton, double>(nameof(IconHeight));

    /// <summary>
    /// Overrides the default style key to <see cref="RepeatButton"/>.
    /// </summary>
    protected override Type StyleKeyOverride => typeof(RepeatButton);

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
        Content = BuildIcon();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty)
        {
            Content = BuildIcon();
        }

        if (change.Property == IsPressedProperty && !change.GetNewValue<bool>())
        {
            StopTimer();
        }
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
                    var brush = UIHelper.GetBrush("MainTextColor");
                    foreach (var drawing in drawingGroup.Children)
                    {
                        if (drawing is GeometryDrawing { Pen: Pen pen })
                        {
                            pen.Brush = brush;
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