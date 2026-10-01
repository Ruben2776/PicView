using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Core.ColorHandling;
using PicView.Core.ViewModels;

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
        BackgroundType.Transparent => new SolidColorBrush(Colors.Transparent),
        BackgroundType.NoiseTexture => GetNoiseTextureBrush(),
        BackgroundType.Checkerboard => CreateCheckerboardBrush(),
        BackgroundType.CheckerboardAlternative => CreateCheckerboardBrush(
            Color.FromRgb(235, 235, 235), 
            Color.FromRgb(40, 40, 40), 
            AlternativeCheckerboardSize),
        BackgroundType.White => new SolidColorBrush(Colors.White),
        BackgroundType.LightGray => new SolidColorBrush(Color.FromRgb(200, 200, 200)),
        BackgroundType.MediumGray => new SolidColorBrush(Color.FromRgb(155, 155, 155)),
        BackgroundType.SemiTransparentDarkGray => new SolidColorBrush(Color.FromArgb(200, 100, 100, 100)),
        BackgroundType.SemiTransparentDarkerGray => new SolidColorBrush(Color.FromArgb(200, 50, 50, 50)),
        BackgroundType.NearBlack => new SolidColorBrush(Color.FromRgb(5, 5, 5)),
        BackgroundType.DarkGray => new SolidColorBrush(Color.FromRgb(48, 48, 48)),
        BackgroundType.MainBackgroundColor => new SolidColorBrush(Color.FromRgb(43, 43, 43)),
        _ => new SolidColorBrush(Colors.Transparent)
    };

    /// <summary>
    /// Changes the background to the next option in the cycle and updates the view model.
    /// </summary>
    /// <param name="vm">The main view model where the background is updated.</param>
    public static void ChangeBackground(MainWindowViewModel vm)
    {
        // Cycle to the next background choice
        var nextChoice = (Settings.UIProperties.BgColorChoice + 1) % ((int)BackgroundType.MaxValue + 1);
        SetBackground(vm, nextChoice);
    }
    
    
    /// <inheritdoc cref="ChangeBackground(MainWindowViewModel)" />
    public static async Task ChangeBackgroundAsync(MainWindowViewModel vm)
    {
        await Dispatcher.UIThread.InvokeAsync(() => ChangeBackground(vm));
        await SaveSettingsAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the background of the view model to a specific background choice.
    /// </summary>
    /// <param name="vm">The main view model where the background is set.</param>
    /// <param name="choice">The background choice to set.</param>
    public static void SetBackground(MainWindowViewModel vm, int choice)
    {
        Settings.UIProperties.BgColorChoice = choice;
        if (Settings.UIProperties.IsConstrainBackgroundColorEnabled)
        {
            vm.GlobalSettings.ImageBackground.Value = new SolidColorBrush(Colors.Transparent);
            vm.GlobalSettings.ConstrainedImageBackground.Value = GetBackgroundBrush((BackgroundType)choice);
        }
        else
        {
            vm.GlobalSettings.ImageBackground.Value = GetBackgroundBrush((BackgroundType)choice);
            vm.GlobalSettings.ConstrainedImageBackground.Value = new SolidColorBrush(Colors.Transparent);
        }
        
        vm.GlobalSettings.BackgroundChoice.Value = choice;
    }

    /// <summary>
    /// Retrieves the noise texture brush from the application resources.
    /// </summary>
    /// <returns>A brush containing the noise texture or a transparent brush if unavailable.</returns>
    private static Brush GetNoiseTextureBrush()
    {
        if (Application.Current.TryGetResource("NoisyTexture", Application.Current.RequestedThemeVariant, out var texture) && 
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
        return CreateCheckerboardBrush(
            Color.FromRgb(235, 235, 235),
            Color.FromRgb(40, 40, 40), 
            size);
    }

    /// <summary>
    /// Creates a checkerboard brush with two alternating colors.
    /// </summary>
    /// <param name="primaryColor">The primary color for the checkerboard squares. Defaults to white.</param>
    /// <param name="secondaryColor">The secondary color for the checkerboard squares. Defaults to a dark gray.</param>
    /// <param name="size">The size of the checkerboard squares in pixels. Defaults to 30.</param>
    /// <returns>A drawing brush representing the checkerboard pattern.</returns>
    public static DrawingBrush CreateCheckerboardBrush(Color primaryColor = default, Color secondaryColor = default,
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
                    new RectangleGeometry(new Rect(0, 0, size, size)),
                    new RectangleGeometry(new Rect(size, size, size, size))
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
    
    public static void SetBackground(int backgroundIndex)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        var globalSettings = core.GlobalSettings;

        Settings.UIProperties.BgColorChoice = backgroundIndex;
                 
        var brush = GetBackgroundBrush((BackgroundType)backgroundIndex);
                 
        if (Settings.UIProperties.IsConstrainBackgroundColorEnabled)
        {
            globalSettings.ImageBackground.Value = new SolidColorBrush(Colors.Transparent);
            globalSettings.ConstrainedImageBackground.Value = brush;
        }
        else
        {
            globalSettings.ImageBackground.Value = brush;
            globalSettings.ConstrainedImageBackground.Value = new SolidColorBrush(Colors.Transparent);
        }
                 
        globalSettings.BackgroundChoice.Value = backgroundIndex;
    }
}