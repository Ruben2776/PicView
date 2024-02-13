using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace PicView.Avalonia.Views.UC.Menus;

public partial class StarOutlineButton : UserControl
{
    public StarOutlineButton()
    {
        InitializeComponent();
    }

    public void FillStar()
    {
        if (!this.TryFindResource("StarFilledDrawingImage", ThemeVariant.Default, out var resourceValue))
        {
            return;
        }
        var drawingImage = resourceValue as DrawingImage;
        Star.Source = drawingImage;
    }

    public void OutlineStar()
    {
        if (!this.TryFindResource("StarOutlineDrawingImage", ThemeVariant.Default, out var resourceValue))
        {
            return;
        }
        var drawingImage = resourceValue as DrawingImage;
        Star.Source = drawingImage;
    }
}