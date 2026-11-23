using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using PicView.Core.ColorHandling;
using PicView.Core.DebugTools;
using PicView.Avalonia.SettingsManagement;
using PicView.Core.ViewModels;
using R3;

// ReSharper disable PossibleLossOfFraction

namespace PicView.Avalonia.ColorManagement;

/// <summary>
/// Manages the background appearance for the image viewer.
/// It provides methods to change and set the background color or pattern.
/// </summary>
public static class BackgroundManager
{
    /// <summary>
    /// Default checkerboard size in pixels
    /// </summary>
    private const int DefaultCheckerboardSize = 30;
    
    /// <summary>
    /// Alternative checkerboard size in pixels
    /// </summary>
    private const int AlternativeCheckerboardSize = 60;

    /// <summary>
    /// Gets the appropriate brush for the current background setting
    /// </summary>
    public static Brush GetBackgroundBrush(BackgroundType backgroundType) => backgroundType switch
    {
        BackgroundType.Transparent => DetermineAndGetTransparentBg(),
        BackgroundType.NoiseTexture => GetNoiseTextureBrush(),
        BackgroundType.Checkerboard => CreateCheckerboardBrush(),
        BackgroundType.CheckerboardAlternative => CreateCheckerboardBrushAlt(),
        BackgroundType.White => new SolidColorBrush(Colors.White),
        BackgroundType.LightGray => new SolidColorBrush(Color.FromRgb(200, 200, 200)),
        BackgroundType.MediumGray => new SolidColorBrush(Color.FromRgb(155, 155, 155)),
        BackgroundType.SemiTransparentDarkGray => new SolidColorBrush(Color.FromArgb(200, 100, 100, 100)),
        BackgroundType.SemiTransparentDarkerGray => new SolidColorBrush(Color.FromArgb(200, 50, 50, 50)),
        BackgroundType.NearBlack => new SolidColorBrush(Color.FromRgb(5, 5, 5)),
        BackgroundType.DarkGray => new SolidColorBrush(Color.FromRgb(48, 48, 48)),
        BackgroundType.MainBackgroundColor => new SolidColorBrush(Color.FromRgb(43, 43, 43)),
        _ => DetermineAndGetTransparentBg()
    };

    /// <summary>
    /// The Transparent background: clear over the blur, or a tint in the theme's color when the blur is off.
    /// </summary>
    public static Brush DetermineAndGetTransparentBg()
    {
        if (ThemeManager.IsAcrylicEnabled)
        {
            return new SolidColorBrush(Colors.Transparent);
        }
        
        return Settings.Theme.Dark 
            ? new SolidColorBrush(Color.FromArgb(0xAD, 0x16, 0x16, 0x16)) 
            : new SolidColorBrush(Color.FromArgb(0xD6, 0xFF, 0xFF, 0xFF));
    }

    /// <summary>
    /// A tool window's background when the blur is off, in the theme's colour.
    /// </summary>
    public static Brush GetToolWindowBg() => Settings.Theme.Dark
        ? new SolidColorBrush(Color.FromArgb(254, 37, 37, 37))
        : new SolidColorBrush(Color.FromArgb(0xD6, 0xFF, 0xFF, 0xFF));

    /// <summary>
    /// Moves the window's background choice on to the next one in the cycle, which the B key does. Its follower paints
    /// the new brushes; nothing is written to the settings record, which the window stores when it closes.
    /// </summary>
    public static void ChangeBackground(WindowSettings settings) =>
        ToggleHelper.ChangeWindowSetting(settings, static s => s.BackgroundChoice,
            static choice => (choice + 1) % ((int)BackgroundType.MaxValue + 1));

    /// <summary>
    /// Sets the Active Main Window's background choice, as the Settings window's palette does; does nothing when no
    /// Main Window is active.
    /// </summary>
    public static void SetBackground(MainWindowOverviewViewModel? mainWindows, int choice)
    {
        if (mainWindows?.ActiveWindow.Value?.WindowSettings is not { } settings)
        {
            return;
        }

        ToggleHelper.ChangeWindowSetting(settings, static s => s.BackgroundChoice, _ => choice);
    }

    /// <summary>
    /// Retrieves the noise texture brush from the application resources.
    /// </summary>
    /// <returns>A brush containing the noise texture or a transparent brush if unavailable.</returns>
    private static Brush GetNoiseTextureBrush()
    {
        if (Application.Current.TryGetResource("NoisyTexture", ThemeVariant.Dark, out var texture) && 
            texture is ImageBrush imageBrush)
        {
            return imageBrush;
        }
        
        return new SolidColorBrush(Colors.Transparent);
    }

    /// <summary>
    /// Creates a checkerboard brush with alternative colors and size.
    /// </summary>
    /// <param name="size">The size of the checkerboard squares in pixels.</param>
    /// <returns>A drawing brush representing the alternative checkerboard pattern.</returns>
    public static DrawingBrush CreateCheckerboardBrushAlt(int size = AlternativeCheckerboardSize)
    {
        var secondaryColor = ThemeManager.IsAcrylicEnabled
            ? Color.FromRgb(40, 40, 40)
            : Colors.Black;

        return CreateCheckerboardBrush(Color.FromRgb(235, 235, 235), secondaryColor, size);
    }

    public static DrawingBrush CreateCheckerboardBrush()
    {
        var primaryColor = Colors.White;
        var secondaryColor = ThemeManager.IsAcrylicEnabled
            ? Color.Parse("#F81F1F1F")
            : Colors.Black;

        return CreateCheckerboardBrush(primaryColor, secondaryColor);
    }

    /// <summary>
    /// Creates a checkerboard brush with two alternating colors.
    /// </summary>
    /// <param name="primaryColor">The primary color for the checkerboard squares. Defaults to white.</param>
    /// <param name="secondaryColor">The secondary color for the checkerboard squares. Defaults to a dark gray.</param>
    /// <param name="size">The size of the checkerboard squares in pixels. Defaults to 30.</param>
    /// <returns>A drawing brush representing the checkerboard pattern.</returns>
    public static DrawingBrush CreateCheckerboardBrush(Color primaryColor, Color secondaryColor = default,
        int size = DefaultCheckerboardSize)
    {
        // Default colors if not provided
        primaryColor = primaryColor == default ? Colors.White : primaryColor;
        secondaryColor = secondaryColor == default ? Color.Parse("#F81F1F1F") : secondaryColor;

        var drawingGroup = new DrawingGroup();

        // Primary color rectangles
        var primaryGeometry = new GeometryDrawing
        {
            Brush = new SolidColorBrush(primaryColor),
            Geometry = new GeometryGroup
            {
                Children =
                {
                    // The whole tile; the secondary squares are drawn over two of its quarters. Nothing may
                    // reach past the tile, or Stretch.None centres the larger drawing and crops them away
                    new RectangleGeometry(new Rect(0, 0, size, size))
                }
            }
        };

        // Secondary color rectangles
        var secondaryGeometry = new GeometryDrawing
        {
            Brush = new SolidColorBrush(secondaryColor),
            Geometry = new GeometryGroup
            {
                Children =
                {
                    new RectangleGeometry(new Rect(size / 2, 0, size / 2, size / 2)),
                    new RectangleGeometry(new Rect(0, size / 2, size / 2, size / 2))
                }
            }
        };

        // Add geometries to the drawing group
        drawingGroup.Children.Add(primaryGeometry);
        drawingGroup.Children.Add(secondaryGeometry);

        return new DrawingBrush
        {
            DestinationRect = new RelativeRect(0, 0, size, size, RelativeUnit.Absolute),
            TileMode = TileMode.Tile,
            Stretch = Stretch.None,
            Drawing = drawingGroup
        };
    }
    
    /// <summary>
    /// Paints a Main Window's two brushes from its own background choice and constrain setting, now when it is made,
    /// so it shows with them, and again on each change. The brushes are made on the UI thread whichever thread
    /// changed the setting. Ends when the window's settings are disposed as it closes, and paints nothing after that.
    /// </summary>
    /// <remarks>Call on the UI thread, before the window shows.</remarks>
    public static IDisposable Follow(WindowSettings settings)
    {
        return settings.BackgroundChoice.CombineLatest(settings.IsConstrainingBackgroundColor, static (_, _) => Unit.Default)
            .Subscribe(_ =>
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    Paint(settings);
                }
                else
                {
                    Dispatcher.UIThread.Post(() => Paint(settings));
                }
            }, DebugHelper.LogError(nameof(BackgroundManager), nameof(Follow)));
    }

    /// <summary>
    /// Paints a Main Window's two brushes again, for a change they depend on that its settings do not hold, such as
    /// the blur being turned on or off.
    /// </summary>
    /// <remarks>Call on the UI thread.</remarks>
    public static void Repaint(WindowSettings settings) => Paint(settings);

    /// <summary>
    /// Makes the two brushes from what the settings say now, on the UI thread.
    /// </summary>
    private static void Paint(WindowSettings settings)
    {
        // A change posted just before the window closed
        if (settings.IsDisposed)
        {
            return;
        }

        var brush = GetBackgroundBrush((BackgroundType)settings.BackgroundChoice.Value);
        var transparent = new SolidColorBrush(Colors.Transparent);
        if (settings.IsConstrainingBackgroundColor.Value)
        {
            settings.ImageBackground.Value = transparent;
            settings.ConstrainedImageBackground.Value = brush;
        }
        else
        {
            settings.ImageBackground.Value = brush;
            settings.ConstrainedImageBackground.Value = transparent;
        }
    }
}
