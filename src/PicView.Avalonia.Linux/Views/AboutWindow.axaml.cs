using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Linux.PlatformUpdate;
using PicView.Core.IPlatform;
using PicView.Core.Update;

namespace PicView.Avalonia.Linux.Views;

public partial class AboutWindow : GenericWindow, IPlatformSpecificUpdate
{
    public AboutWindow()
    {
        InitializeComponent();
        if (!Settings.Theme.Dark || Settings.Theme.GlassTheme)
        {
            XAboutView.Background = Brushes.Transparent;
        }
    }
    
    public async Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath)
    {
        await LinuxUpdateHelper.HandleLinuxUpdate(updateInfo, tempPath);
    }
}