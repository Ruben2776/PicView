using System.Windows.Media;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.Sizing;

namespace PicView.WPF.Views.UserControls.Misc;

public partial class DragDropOverlayPic
{
    public DragDropOverlayPic(ImageSource source)
    {
        InitializeComponent();

        UpdateSource(source);
    }

    private void UpdateSource(ImageSource source)
    {
        var padding = ConfigureWindows.GetMainWindow.TitleBar.ActualHeight +
                      ConfigureWindows.GetMainWindow.LowerBar.ActualHeight;
        var boxedWidth = ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth *
            WindowSizing.MonitorInfo.DpiScaling - padding;
        var boxedHeight = ConfigureWindows.GetMainWindow.MinHeight * WindowSizing.MonitorInfo.DpiScaling;
        var scaleWidth = boxedWidth / source?.Width ?? 600;
        var scaleHeight = boxedHeight / source?.Height ?? 340;
        var scale = Math.Min(scaleHeight, scaleWidth);
        ContentHolderSource.ImageSource = source;
        ContentHolder.Width = source?.Width * scale ?? 550;
        ContentHolder.Height = source?.Height * scale ?? 300;
    }
}