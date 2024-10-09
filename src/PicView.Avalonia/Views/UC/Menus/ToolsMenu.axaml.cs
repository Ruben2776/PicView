using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Core.Config;

namespace PicView.Avalonia.Views.UC.Menus;

public partial class ToolsMenu : AnimatedMenu
{
    public ToolsMenu()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (SettingsHelper.Settings.Theme.GlassTheme)
            {
            }
            else if (!SettingsHelper.Settings.Theme.Dark)
            {
                TopBorder.Background = Brushes.White;
            }
        };
    }
}