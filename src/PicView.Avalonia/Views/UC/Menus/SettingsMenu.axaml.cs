using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Core.Config;

namespace PicView.Avalonia.Views.UC.Menus;

public partial class SettingsMenu : AnimatedMenu
{
    public SettingsMenu()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (SettingsHelper.Settings.Theme.GlassTheme)
            {
                SettingsButton.Classes.Remove("noBorderHover");
                SettingsButton.Classes.Add("hover");
                
                AboutWindowButton.Classes.Remove("noBorderHover");
                AboutWindowButton.Classes.Add("hover");
            }
            else if (!SettingsHelper.Settings.Theme.Dark)
            {
                TopBorder.Background = Brushes.White;
            }
        };

    }
}