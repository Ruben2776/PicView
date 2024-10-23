using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Calculations;
using PicView.Core.Localization;

namespace PicView.Avalonia.Views.UC;

public partial class DragDropView : UserControl
{
    private LinkChain? _linkChain;
    private DirectoryIcon? _directoryIcon;
    private ZipIcon? _zipIcon;

    private const int MaxSize = SizeDefaults.WindowMinSize - 30;
    
    public DragDropView()
    {
        InitializeComponent();
        InitializeView();
    }

    private void InitializeView()
    {
        TxtDragToView.Text = TranslationHelper.Translation.DropToLoad;
        UpdateViewSize();
    }

    private void UpdateViewSize()
    {
        Width = UIHelper.GetMainView.Bounds.Width;
        Height = UIHelper.GetMainView.Bounds.Height;
    }

    private void AddIconToPanel(Control icon)
    {
        if (!ParentPanel.Children.Contains(icon))
        {
            ParentPanel.Children.Add(icon);
            ParentPanel.Children.Move(ParentPanel.Children.IndexOf(icon), 0);
        }
        ClearContentHolder();
    }

    private void ClearContentHolder()
    {
        ContentHolder.Background = null;
        ContentHolder.IsVisible = false;
        ContentHolder.Child = null;
    }

    public void AddLinkChain()
    {
        _linkChain ??= new LinkChain();
        AddIconToPanel(_linkChain);
    }

    public void AddDirectoryIcon()
    {
        _directoryIcon ??= new DirectoryIcon();
        AddIconToPanel(_directoryIcon);
    }

    public void AddZipIcon()
    {
        _zipIcon ??= new ZipIcon();
        AddIconToPanel(_zipIcon);
    }

    public void UpdateThumbnail(Bitmap image)
    {
        UpdateViewSize();
        if (DataContext is not MainViewModel vm || image is null) 
            return;

        var scale = CalculateScale(image.PixelSize.Width, image.PixelSize.Height, vm);
        UpdateContentHolder(image, scale);
    }

    public void UpdateSvgThumbnail(object svg)
    {
        UpdateViewSize();
        if (DataContext is not MainViewModel vm) return;

        var svgSource = SvgSource.Load(svg as string);
        var svgImage = new SvgImage { Source = svgSource };
        var scale = CalculateScale(svgImage?.Size.Width ?? MaxSize, svgImage?.Size.Height ?? MaxSize, vm);

        ContentHolder.Background = new ImageBrush { Opacity = 0.95 };
        ContentHolder.Child = new Image
        {
            Source = svgImage,
            Width = svgImage?.Size.Width * scale ?? MaxSize,
            Height = svgImage?.Size.Height * scale ?? MaxSize
        };
        ContentHolder.IsVisible = true;
    }

    private double CalculateScale(double width, double height, MainViewModel vm)
    {
        var screen = ScreenHelper.ScreenSize;
        var padding = vm.BottombarHeight + vm.TitlebarHeight + 50;
        var boxedWidth = UIHelper.GetMainView.Bounds.Width * screen.Scaling - padding;
        var boxedHeight = UIHelper.GetMainView.Bounds.Height * screen.Scaling - padding;
        var scaledWidth = boxedWidth / width;
        var scaledHeight = boxedHeight / height;
        return Math.Min(scaledWidth, scaledHeight);
    }

    private void UpdateContentHolder(Bitmap image, double scale)
    {
        ContentHolder.Width = image?.PixelSize.Width * scale ?? MaxSize;
        ContentHolder.Height = image?.PixelSize.Height * scale ?? MaxSize;
        ContentHolder.Background = new ImageBrush
        {
            Opacity = 0.95,
            Source = image
        };
        ContentHolder.Child = null;
        ContentHolder.IsVisible = true;
    }

    public void RemoveThumbnail()
    {
        ContentHolder.Background = null;
        
        if (_linkChain != null)
        {
            ParentPanel.Children.Remove(_linkChain);
        }

        if (_directoryIcon != null)
        {
            ParentPanel.Children.Remove(_directoryIcon);
        }

        if (_zipIcon != null)
        {
            ParentPanel.Children.Remove(_zipIcon);
        }
    }
}
