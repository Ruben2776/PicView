using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// A custom button control that displays text followed by an icon, which can be either a <see cref="DrawingImage"/>
/// or a <see cref="StreamGeometry"/>. It also supports dynamic brush changes to reflect hover states.
/// </summary>
public class TextIconButton : Button
{
    // --- Properties ---

    public static readonly StyledProperty<DrawingImage?> IconProperty =
        AvaloniaProperty.Register<TextIconButton, DrawingImage?>(nameof(Icon));

    public static readonly StyledProperty<StreamGeometry?> PathProperty =
        AvaloniaProperty.Register<TextIconButton, StreamGeometry?>(nameof(Data));

    public static readonly StyledProperty<double> IconWidthProperty =
        AvaloniaProperty.Register<TextIconButton, double>(nameof(IconWidth));

    public static readonly StyledProperty<double> IconHeightProperty =
        AvaloniaProperty.Register<TextIconButton, double>(nameof(IconHeight));

    public static readonly StyledProperty<double> TextMaxWidthProperty =
        AvaloniaProperty.Register<TextIconButton, double>(nameof(TextMaxWidth), double.PositiveInfinity);

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<TextIconButton, string?>(nameof(Text));

    public static readonly StyledProperty<Thickness> IconMarginProperty =
        AvaloniaProperty.Register<TextIconButton, Thickness>(nameof(IconMargin));

    public static readonly StyledProperty<Thickness> TextMarginProperty =
        AvaloniaProperty.Register<TextIconButton, Thickness>(nameof(TextMargin));

    // --- Internal Controls & State ---

    private readonly TextBlock _textBlock;
    private readonly Image _iconImage;
    private readonly PathIcon _pathIcon;
    private DrawingImage? _localIconCopy; // Holds our unique, private copy of the icon

    internal DrawingImage? LocalIconCopy => _localIconCopy;

    protected override Type StyleKeyOverride => typeof(Button);

    public TextIconButton()
    {
        // 1. Initialize internal controls once
        _textBlock = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        _textBlock.Classes.Add("txt");

        _iconImage = new Image { IsVisible = false };
        _pathIcon = new PathIcon { IsVisible = false };

        var container = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        container.Children.Add(_iconImage);
        container.Children.Add(_pathIcon);
        container.Children.Add(_textBlock);

        Content = container;

        // 2. Attach pointer events once
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
    }

    // --- Property Accessors ---

    public DrawingImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public StreamGeometry? Data
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public double IconWidth
    {
        get => GetValue(IconWidthProperty);
        set => SetValue(IconWidthProperty, value);
    }

    public double IconHeight
    {
        get => GetValue(IconHeightProperty);
        set => SetValue(IconHeightProperty, value);
    }

    public double TextMaxWidth
    {
        get => GetValue(TextMaxWidthProperty);
        set => SetValue(TextMaxWidthProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Thickness IconMargin
    {
        get => GetValue(IconMarginProperty);
        set => SetValue(IconMarginProperty, value);
    }

    public Thickness TextMargin
    {
        get => GetValue(TextMarginProperty);
        set => SetValue(TextMarginProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            _textBlock.Text = Text;
        }
        else if (change.Property == TextMarginProperty)
        {
            _textBlock.Margin = TextMargin;
        }
        else if (change.Property == TextMaxWidthProperty)
        {
            _textBlock.MaxWidth = TextMaxWidth;
        }
        else if (change.Property == ForegroundProperty)
        {
            _textBlock.Foreground = Foreground;
        }
        else if (change.Property == FontSizeProperty)
        {
            _textBlock.FontSize = FontSize;
        }
        else if (change.Property == IconProperty || change.Property == PathProperty)
        {
            UpdateIconVisibility();
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
        _localIconCopy = CreateLocalIconCopy(Icon);
        
        _iconImage.Source = _localIconCopy;
        _pathIcon.Data = Data;

        var hasDrawing = _localIconCopy != null;
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
        if (_localIconCopy?.Drawing is not DrawingGroup drawingGroup) return;

        var isGlass = Settings.Theme?.GlassTheme == true;
        var brush = isGlass 
            ? UIHelper.GetBrush("SecondaryTextColor") 
            : UIHelper.GetBrush("MainTextColor");

        foreach (var drawing in drawingGroup.Children)
        {
            if (drawing is GeometryDrawing { Pen: Pen pen })
            {
                pen.Brush = brush;
            }
        }
    }

    private void OnPointerEntered(object? sender, PointerEventArgs? e)
    {
        if (Classes.Contains("MenuItemHover"))
        {
            return;
        }
        Dispatcher.UIThread.Post(() =>
        {
            var secondaryBrush = UIHelper.GetBrush("SecondaryTextColor");

            _textBlock.Foreground = secondaryBrush;
            _pathIcon.Foreground = secondaryBrush;

            if (_localIconCopy?.Drawing is not DrawingGroup drawingGroup)
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
        if (Classes.Contains("MenuItemHover"))
        {
            return;
        }
        Dispatcher.UIThread.Post(() =>
        {
            _textBlock.Foreground = UIHelper.GetBrush("MainTextColor");
            _pathIcon.Foreground = Foreground;

            if (Settings.Theme.GlassTheme)
            {
                return;
            }

            if (_localIconCopy?.Drawing is not DrawingGroup drawingGroup)
            {
                return;
            }

            foreach (var drawing in drawingGroup.Children)
            {
                if (drawing is GeometryDrawing { Pen: Pen pen })
                {
                    pen.Brush = Foreground;
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