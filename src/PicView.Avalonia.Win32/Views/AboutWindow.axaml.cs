using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Avalonia.Win32.PlatformUpdate;
using PicView.Core.IPlatform;
using PicView.Core.Update;

namespace PicView.Avalonia.Win32.Views;

public partial class AboutWindow : GenericWindow, IPlatformSpecificUpdate
{
    public AboutWindow()
    {
        InitializeComponent();

        GenericWindowHelper.AboutWindowInitialize(this);
    }

    public async Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath)
    {
        await WinUpdateHelper.HandleWindowsUpdate(updateInfo, tempPath);
    }
}