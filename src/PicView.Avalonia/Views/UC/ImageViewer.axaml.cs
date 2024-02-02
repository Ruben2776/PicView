using Avalonia.Controls;
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
    }

    private void InputElement_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel)
            return;

        if (e.Delta.Y < 0)
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                mainViewModel.NextCommand?.Execute(null);
                
            }
            else
            {
                mainViewModel.PreviousCommand?.Execute(null);
            }
        }
        else
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                mainViewModel.PreviousCommand?.Execute(null);
            }
            else
            {
                mainViewModel.NextCommand?.Execute(null);
            }
        }
    }
}