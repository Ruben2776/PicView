using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Navigation;

namespace PicView.Avalonia.Views.UC;

public partial class ImageViewer : UserControl
{
    public ImageViewer()
    {
        InitializeComponent();
        PointerWheelChanged += async (_, e) => await Main_OnPointerWheelChanged(e);
    }

    private async Task Main_OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel)
            return;
        
        if (e.Delta.Y < 0)
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                await mainViewModel.SetImageNavigation(NavigateTo.Next).ConfigureAwait(false);
            }
            else
            {
                await mainViewModel.SetImageNavigation(NavigateTo.Previous).ConfigureAwait(false);
            }
        }
        else
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                await mainViewModel.SetImageNavigation(NavigateTo.Previous).ConfigureAwait(false);
            }
            else
            {
                await mainViewModel.SetImageNavigation(NavigateTo.Next).ConfigureAwait(false);
            }
        }
    }
}