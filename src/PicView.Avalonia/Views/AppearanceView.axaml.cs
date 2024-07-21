using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Views;

public partial class AppearanceView : UserControl
{
    public AppearanceView()
    {
        InitializeComponent();
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                TaskBarToggleButton.IsVisible = false;
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TaskBarToggleButton.IsVisible = false;
                });
            }
        }
        Loaded += AppearanceView_Loaded;
    }

    private void AppearanceView_Loaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            GalleryStretchMode.DetermineStretchMode(DataContext as MainViewModel);

        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(AppearanceView)} Add language caught exception: \n {exception}");
#endif
        }
    }

}