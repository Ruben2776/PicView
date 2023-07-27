using System.Windows.Media;
using PicView.UILogic;
using PicView.UILogic.Sizing;

namespace PicView.Views.UserControls.Misc;

public partial class DragDropOverlayPic
{
    public DragDropOverlayPic(ImageSource source)
    {
        InitializeComponent();

        UpdateSource(source);
    }

    private void UpdateSource(ImageSource source)
    {
        if (source == null) { return; }

        var padding = ConfigureWindows.GetMainWindow.TitleBar.ActualHeight + ConfigureWindows.GetMainWindow.LowerBar.ActualHeight;
        var boxedWidth = ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth * WindowSizing.MonitorInfo.DpiScaling - padding;
        var boxedHeight = ConfigureWindows.GetMainWindow.MinHeight * WindowSizing.MonitorInfo.DpiScaling;
        var scaleWidth = boxedWidth / source.Width;
        var scaleHeight = boxedHeight / source.Height;
        var scale = Math.Min(scaleHeight, scaleWidth);
        ContentHolderSource.ImageSource = source;
        ContentHolder.Width = source.Width * scale;
        ContentHolder.Height = source.Height * scale;
    }
}