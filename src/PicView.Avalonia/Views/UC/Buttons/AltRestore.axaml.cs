using Avalonia.Controls;
using Avalonia.Media;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.Views.UC.Buttons;
public partial class AltRestore : UserControl
{
    public AltRestore()
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
            RestoreIcon.Fill = Brushes.White;
            FullScreenIcon.Fill = Brushes.White;
        };

        PointerExited += (_, _) =>
        {
            var mainBrush = UIHelper.GetBrush("MainTextColor");
            RestoreIcon.Fill = mainBrush;
            FullScreenIcon.Fill = mainBrush;
        };
    }
}
