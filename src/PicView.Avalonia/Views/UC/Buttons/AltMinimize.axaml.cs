using Avalonia.Controls;
using Avalonia.Media;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.Views.UC.Buttons;
public partial class AltMinimize : UserControl
{
    public AltMinimize()
    {
        InitializeComponent();
        if (!Settings.Theme.Dark)
        {
            ApplyLightTheme();
        }
    }
    
    private void ApplyLightTheme()
    {
        PointerEntered += (_, _) =>
        {
            Icon.Fill = Brushes.White;
        };

        PointerExited += (_, _) =>
        {
            Icon.Fill = UIHelper.GetBrush("MainTextColor");
        };
    }
}
