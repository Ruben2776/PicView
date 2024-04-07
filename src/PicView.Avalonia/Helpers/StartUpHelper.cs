using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;

namespace PicView.Avalonia.Helpers;

public static class StartUpHelper
{
    public static async Task Start(MainViewModel vm)
    {
        vm.ImageViewer = new ImageViewer();
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            await vm.LoadPicFromString(args[1]).ConfigureAwait(false);
        }
        else if (SettingsHelper.Settings.StartUp.OpenLastFile)
        {
            await vm.LoadPicFromString(SettingsHelper.Settings.StartUp.LastFile).ConfigureAwait(false);
        }
        else
        {
            vm.CurrentView = new StartUpMenu();
        }

        vm.IsLoading = false;
    }
}