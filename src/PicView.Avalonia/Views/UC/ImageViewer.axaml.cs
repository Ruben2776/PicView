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
    
    private bool _isControlDown;

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.Control)
        {
            _isControlDown = true;
        }
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.LeftCtrl or Key.RightCtrl)
        {
            _isControlDown = false;
        }
    }

    private async Task Main_OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel)
            return;
        
        if (e.Delta.Y < 0)
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                if (_isControlDown)
                {
                    //zoom
                }
                else
                {
                    await mainViewModel.SetImageNavigation(NavigateTo.Next).ConfigureAwait(false);
                }
            }
            else
            {
                if (_isControlDown)
                {
                    //zoom
                }
                else
                {
                    await mainViewModel.SetImageNavigation(NavigateTo.Previous).ConfigureAwait(false);
                }
            }
        }
        else
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                if (_isControlDown)
                {
                    //zoom
                }
                else
                {
                    await mainViewModel.SetImageNavigation(NavigateTo.Previous).ConfigureAwait(false);
                }
            }
            else
            {
                if (_isControlDown)
                {
                    //zoom
                }
                else
                {
                    await mainViewModel.SetImageNavigation(NavigateTo.Next).ConfigureAwait(false);
                }
            }
        }
    }
}