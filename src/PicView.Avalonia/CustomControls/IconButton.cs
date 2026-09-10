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
    // --- Properties ---

    /// <summary>
    /// Defines the <see cref="Icon"/> property.
    /// The icon is displayed as a <see cref="DrawingImage"/> with support for dynamic brush changes.
    /// </summary>
    public static readonly StyledProperty<DrawingImage?> IconProperty =
        AvaloniaProperty.Register<IconButton, DrawingImage?>(nameof(Icon));

    /// <summary>
    /// Defines the <see cref="Data"/> property.
    /// The icon can also be displayed as a <see cref="StreamGeometry"/> for path-based rendering.
    /// </summary>
    public static readonly StyledProperty<StreamGeometry?> PathProperty =
        AvaloniaProperty.Register<IconButton, StreamGeometry?>(nameof(Data));

    /// <summary>
    /// Defines the <see cref="IconWidth"/> property.
    /// The width of the icon, whether it is a <see cref="DrawingImage"/> or <see cref="StreamGeometry"/>.
    /// </summary>
    public static readonly StyledProperty<double> IconWidthProperty =
        AvaloniaProperty.Register<IconButton, double>(nameof(IconWidth));

    /// <summary>
    /// Defines the <see cref="IconHeight"/> property.
    /// The height of the icon, whether it is a <see cref="DrawingImage"/> or <see cref="StreamGeometry"/>.
    /// </summary>
    public static readonly StyledProperty<double> IconHeightProperty =
        AvaloniaProperty.Register<IconButton, double>(nameof(IconHeight));

    /// <summary>
    /// Defines the <see cref="IconMargin"/> property.
    /// </summary>
    public static readonly StyledProperty<Thickness> IconMarginProperty =
        AvaloniaProperty.Register<IconButton, Thickness>(nameof(IconMargin));

    // --- Internal Controls & State ---

    private readonly Image _iconImage;
    private readonly PathIcon _pathIcon;

    internal DrawingImage? LocalIconCopy { get; private set; }

    /// <summary>
    /// Overrides the default style key to <see cref="RepeatButton"/>.
    /// </summary>
    protected override Type StyleKeyOverride => typeof(RepeatButton);

    public IconButton()
    {
        // 1. Initialize internal controls once
        _iconImage = new Image { IsVisible = false };
        _pathIcon = new PathIcon { IsVisible = false };

        var container = new Panel();
        container.Children.Add(_iconImage);
        container.Children.Add(_pathIcon);

        Content = container;

        // 2. Attach pointer events once
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
    }

    // --- Property Accessors ---

    /// <summary>
    /// Gets or sets the <see cref="DrawingImage"/> displayed as the icon of the button.
    /// </summary>
    [Content]
    public DrawingImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="StreamGeometry"/> used as the icon's path data.
    /// </summary>
    public StreamGeometry? Data
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    /// <summary>
    /// Gets or sets the width of the icon.
    /// </summary>
    public double IconWidth
    {
        get => GetValue(IconWidthProperty);
        set => SetValue(IconWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the height of the icon.
    /// </summary>
    public double IconHeight
    {
        get => GetValue(IconHeightProperty);
        set => SetValue(IconHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the margin of the icon.
    /// </summary>
    public Thickness IconMargin
    {
        get => GetValue(IconMarginProperty);
        set => SetValue(IconMarginProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty || change.Property == PathProperty)
        {
            UpdateIconVisibility();
            ApplyInitialIconColors();
        }
        else if (change.Property == ForegroundProperty)
        {
            _pathIcon.Foreground = Foreground;
            ApplyInitialIconColors();
        }
        else if (change.Property == IconWidthProperty)
        {
            _iconImage.Width = IconWidth;
            _pathIcon.Width = IconWidth;
        }
        else if (change.Property == IconHeightProperty)
        {
            _iconImage.Height = IconHeight;
            _pathIcon.Height = IconHeight;
        }
        else if (change.Property == IconMarginProperty)
        {
            _iconImage.Margin = IconMargin;
            _pathIcon.Margin = IconMargin;
        }
        else if (change.Property == IsPressedProperty && !change.GetNewValue<bool>())
        {
            StopTimer();
        }
    }

    // --- Icon Clone & Visibility Logic ---

    private void UpdateIconVisibility()
    {
        // Generate a private copy of the icon so we don't mutate global StaticResources
        LocalIconCopy = CreateLocalIconCopy(Icon);

        _iconImage.Source = LocalIconCopy;
        _pathIcon.Data = Data;

        var hasDrawing = LocalIconCopy != null;
        var hasPath = Data != null;

        _iconImage.IsVisible = hasDrawing;
        _pathIcon.IsVisible = !hasDrawing && hasPath;
    }

    private static DrawingImage? CreateLocalIconCopy(DrawingImage? sourceIcon)
    {
        if (sourceIcon?.Drawing is not DrawingGroup sourceGroup)
        {
            return sourceIcon;
        }

        var clonedGroup = new DrawingGroup();

        foreach (var child in sourceGroup.Children)
        {
            if (child is GeometryDrawing geomDrawing)
            {
                var clonedGeomDrawing = new GeometryDrawing
                {
                    Geometry = geomDrawing.Geometry,
                    Brush = geomDrawing.Brush
                };

                // Clone the Pen so modifying it later doesn't affect the shared instance
                if (geomDrawing.Pen is Pen sourcePen)
                {
                    clonedGeomDrawing.Pen = new Pen
                    {
                        Brush = sourcePen.Brush,
                        Thickness = sourcePen.Thickness,
                        DashStyle = sourcePen.DashStyle,
                        LineCap = sourcePen.LineCap,
                        LineJoin = sourcePen.LineJoin,
                        MiterLimit = sourcePen.MiterLimit
                    };
                }

                clonedGroup.Children.Add(clonedGeomDrawing);
            }
            else
            {
                clonedGroup.Children.Add(child);
            }
        }

        return new DrawingImage { Drawing = clonedGroup };
    }

    // --- Hover State Logic ---

    private void ApplyInitialIconColors()
    {
        // Make sure we operate on our private clone, not the public Icon property
        if (LocalIconCopy?.Drawing is not DrawingGroup drawingGroup) return;

        var isGlass = Settings.Theme?.GlassTheme == true;
        var brush = isGlass
            ? UIHelper.GetBrush("SecondaryTextColor")
            : Foreground ?? UIHelper.GetBrush("MainTextColor");

        foreach (var drawing in drawingGroup.Children)
        {
            if (drawing is GeometryDrawing { Pen: Pen pen })
            {
                pen.Brush = brush;
            }
        }
    }

    internal void TriggerPointerEntered() => OnPointerEntered(this, null);
    internal void TriggerPointerExited() => OnPointerExited(this, null);

    private void OnPointerEntered(object? sender, PointerEventArgs? e)
    {
        if (Classes.Contains("HoverBarHover") || Classes.Contains("MenuItemHover"))
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            var secondaryBrush = UIHelper.GetBrush("SecondaryTextColor");

            _pathIcon.Foreground = secondaryBrush;

            if (LocalIconCopy?.Drawing is not DrawingGroup drawingGroup)
            {
                return;
            }

            foreach (var drawing in drawingGroup.Children)
            {
                if (drawing is GeometryDrawing { Pen: Pen pen })
                {
                    pen.Brush = secondaryBrush;
                }
            }
        });
    }

    private void OnPointerExited(object? sender, PointerEventArgs? e)
    {
        if (Classes.Contains("HoverBarHover") || Classes.Contains("MenuItemHover"))
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            _pathIcon.Foreground = Foreground;

            if (LocalIconCopy?.Drawing is not DrawingGroup drawingGroup)
            {
                return;
            }

            var brush = Foreground ?? UIHelper.GetBrush("MainTextColor");

            foreach (var drawing in drawingGroup.Children)
            {
                if (drawing is GeometryDrawing { Pen: Pen pen })
                {
                    pen.Brush = brush;
                }
            }
        });
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