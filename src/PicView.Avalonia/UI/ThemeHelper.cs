using Avalonia;
using Avalonia.Media;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.UI;
public static class ThemeHelper
{
    #region Background
    public static void ChangeBackground(MainViewModel vm)
    {
        if (vm.ImageSource is null)
        {
            return;
        }
        if (!ImageHelper.HasTransparentBackground(vm.ImageSource))
        {
            return;
        }
        SettingsHelper.Settings.UIProperties.BgColorChoice = (SettingsHelper.Settings.UIProperties.BgColorChoice + 1) % 5;
        vm.ImageBackground = BackgroundColorBrush;
    }
    
    public static void SetBackground(MainViewModel vm)
    {
        vm.ImageBackground = BackgroundColorBrush;
    }
    
    private static Brush BackgroundColorBrush => SettingsHelper.Settings.UIProperties.BgColorChoice switch
    {
        0 => new SolidColorBrush(Colors.Transparent),
        1 => new SolidColorBrush(Colors.White),
        2 => new SolidColorBrush(Color.FromRgb(15, 15, 15)),
        3 => CreateCheckerboardBrush(),
        4 => CreateCheckerboardBrush(Color.FromRgb(235, 235, 235), Color.FromRgb(40, 40, 40), 60),
        _ => new SolidColorBrush(Colors.Transparent),
    };
    
    private static DrawingBrush CreateCheckerboardBrush(Color primaryColor = default, Color secondaryColor = default, int size = 30)
    {
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

        drawingGroup.Children.Add(primaryGeometry);
        drawingGroup.Children.Add(secondaryGeometry);
        checkerboardBrush.Drawing = drawingGroup;

        return checkerboardBrush;
    }
    
    #endregion

    
}
