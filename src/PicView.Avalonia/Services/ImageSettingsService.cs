using Avalonia;
using PicView.Avalonia.Views.UC;
using PicView.Core.ISettings;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Services;

public class ImageSettingsService : IImageSettingsService
{
    public void TriggerScalingModeUpdate(bool nearestNeighbor)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        if (core.MainWindows.ActiveWindow.CurrentValue is not { } activeWindow)
        {
            return;
        }

        if (activeWindow.WindowTabs.ActiveTab.CurrentValue.CurrentView.Value is NavigableView { Viewer: { } viewer })
        {
            viewer.TriggerScalingModeUpdate(nearestNeighbor);
        }
    }
}
