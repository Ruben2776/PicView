using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.MacOS.PlatformUpdate;
using PicView.Core.IPlatform;
using PicView.Core.Update;

namespace PicView.Avalonia.MacOS.Views;

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
        await MacUpdateHelper.HandleMacOSUpdate(updateInfo, tempPath);
    }
}