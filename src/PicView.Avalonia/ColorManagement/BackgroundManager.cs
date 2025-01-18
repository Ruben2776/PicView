using Avalonia;
using Avalonia.Media;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.ColorManagement;

/// <summary>
/// Manages the background appearance for the image viewer.
/// It provides methods to change and set the background color or pattern.
/// </summary>
public static class BackgroundManager
{
    /// <summary>
    /// Returns a brush corresponding to the current background color choice.
    /// Choices include solid colors, transparency, noise texture, and checkerboard patterns.
    /// </summary>
    private static Brush BackgroundColorBrush => SettingsHelper.Settings.UIProperties.BgColorChoice switch
    {
        0 => new SolidColorBrush(Colors.Transparent),
        1 => GetNoiseTextureBrush(),
        2 => CreateCheckerboardBrush(),
        3 => CreateCheckerboardBrushAlt(),
        4 => new SolidColorBrush(Colors.White),
        5 => new SolidColorBrush(Color.FromRgb(200, 200, 200)),
        6 => new SolidColorBrush(Color.FromRgb(155, 155, 155)),
        7 => new SolidColorBrush(Color.FromArgb(200, 100, 100, 100)),
        8 => new SolidColorBrush(Color.FromArgb(200, 50, 50, 50)),
        9 => new SolidColorBrush(Color.FromRgb(5, 5, 5)),
        _ => new SolidColorBrush(Colors.Transparent)
    };

    /// <summary>
    /// Changes the background based on the current background color choice stored in the settings.
    /// Cycles through predefined background styles and updates the view model's ImageBackground property.
    /// </summary>
    /// <param name="vm">The main view model where the background is updated.</param>
    public static void ChangeBackground(MainViewModel vm)
    {
        if (vm.CurrentView != vm.ImageViewer)
        {
            return;
        }

        // Cycle through background choices (0 to 9)
        SettingsHelper.Settings.UIProperties.BgColorChoice =
            (SettingsHelper.Settings.UIProperties.BgColorChoice + 1) % 10;

        // Update the background in the view model
        vm.ImageBackground = BackgroundColorBrush;
        vm.BackgroundChoice = SettingsHelper.Settings.UIProperties.BgColorChoice;
    }

    /// <summary>
    /// Sets the background of the view model based on the current background choice.
    /// </summary>
    /// <param name="vm">The main view model where the background is set.</param>
    public static void SetBackground(MainViewModel vm)
    {
        vm.ImageBackground = BackgroundColorBrush;
    }
    
    /// <summary>
    /// Sets the background of the view model based on the current background choice.
    /// </summary>
    /// <param name="vm">The main view model where the background is set.</param>
    /// <param name="choice">The background choice to set.</param>
    public static void SetBackground(MainViewModel vm, int choice)
    {
        SettingsHelper.Settings.UIProperties.BgColorChoice = choice;
        vm.ImageBackground = BackgroundColorBrush;
    }

    /// <summary>
    /// Retrieves the noise texture brush from the application resources.
    /// If the texture is not available, returns a transparent brush.
    /// </summary>
    /// <returns>A brush containing the noise texture or a transparent brush if unavailable.</returns>
    private static Brush GetNoiseTextureBrush()
    {
        if (!Application.Current.TryGetResource("NoisyTexture", Application.Current.RequestedThemeVariant,
                out var texture))
        {
            return new SolidColorBrush(Colors.Transparent);
        }

        if (texture is ImageBrush imageBrush)
        {
            return imageBrush;
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public static DrawingBrush CreateCheckerboardBrushAlt()
    {
        return CreateCheckerboardBrush(Color.FromRgb(235, 235, 235), Color.FromRgb(40, 40, 40), 60);
    }
    
    public static DrawingBrush CreateCheckerboardBrushAlt(int size)
    {
        return CreateCheckerboardBrush(Color.FromRgb(235, 235, 235), Color.FromRgb(40, 40, 40), size);
    }

    /// <summary>
    /// Creates a checkerboard brush with two alternating colors.
    /// </summary>
    /// <param name="primaryColor">The primary color for the checkerboard squares. Defaults to white.</param>
    /// <param name="secondaryColor">The secondary color for the checkerboard squares. Defaults to a dark gray.</param>
    /// <param name="size">The size of the checkerboard squares in pixels. Defaults to 30.</param>
    /// <returns>A drawing brush representing the checkerboard pattern.</returns>
    public static DrawingBrush CreateCheckerboardBrush(Color primaryColor = default, Color secondaryColor = default,
        int size = 30)
    {
        // Default colors if not provided
        if (primaryColor == default)
        {
            primaryColor = Colors.White;
        }

        if (secondaryColor == default)
        {
            secondaryColor = Color.Parse("#F81F1F1F");
        }

        var checkerboardBrush = new DrawingBrush
        {
            DestinationRect = new RelativeRect(0, 0, size, size, RelativeUnit.Absolute),
            TileMode = TileMode.Tile,
            Stretch = Stretch.None
        };

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
        checkerboardBrush.Drawing = drawingGroup;

        return checkerboardBrush;
    }
}