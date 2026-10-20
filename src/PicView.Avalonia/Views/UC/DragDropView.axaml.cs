using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Localization;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using MainWindowViewModel = PicView.Core.ViewModels.MainWindowViewModel;

namespace PicView.Avalonia.Views.UC;

public partial class DragDropView : UserControl
{
    private LinkChain? _linkChain;
    private DirectoryIcon? _directoryIcon;
    private ZipIcon? _zipIcon;

    private const int MaxSize = SizeDefaults.WindowMinSize - 30;
    
    public bool IsLinkChainVisible => _linkChain != null && ParentPanel.Children.Contains(_linkChain);
    public bool IsDirectoryIconVisible => _directoryIcon != null && ParentPanel.Children.Contains(_directoryIcon);
    public bool IsZipIconVisible => _zipIcon != null && ParentPanel.Children.Contains(_zipIcon);
    
    public DragDropView(MainWindow mainWindow)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        DataContext = core.MainWindows.ActiveWindow.CurrentValue;
        InitializeComponent();
        InitializeView(mainWindow);
    }

    private void InitializeView(MainWindow mainWindow)
    {
        TxtDragToView.Text = TranslationManager.Translation.DropToLoad;
        UpdateViewSize(mainWindow);
    }

    private void UpdateViewSize(MainWindow mainWindow)
    {
        Width = mainWindow.UIHelper.GetMainView.Bounds.Width;
        Height = mainWindow.UIHelper.GetMainView.Bounds.Height;
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

    public void UpdateThumbnail(Bitmap image, MainWindow mainWindow)
    {
        UpdateViewSize(mainWindow);
        if (DataContext is not MainWindowViewModel vm || image is null)
        {
            return;
        }

        var scale = CalculateScale(image.PixelSize.Width, image.PixelSize.Height, vm, mainWindow);
        UpdateContentHolder(image, scale);
    }

    public void UpdateSvgThumbnail(object svg, MainWindow mainWindow)
    {
        UpdateViewSize(mainWindow);
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var svgSource = SvgSource.Load(svg as string);
        var svgImage = new SvgImage { Source = svgSource };
        var scale = CalculateScale(svgImage?.Size.Width ?? MaxSize, svgImage?.Size.Height ?? MaxSize, vm, mainWindow);

        ContentHolder.Background = new ImageBrush { Opacity = 0.95 };
        ContentHolder.Child = new Image
        {
            Source = svgImage,
            Width = svgImage?.Size.Width * scale ?? MaxSize,
            Height = svgImage?.Size.Height * scale ?? MaxSize
        };
        ContentHolder.IsVisible = true;
    }

    private static double CalculateScale(double width, double height, MainWindowViewModel vm, MainWindow mainWindow)
    {
        var screen = mainWindow.WindowFitter.Screen;
        var padding = vm.BottombarHeight.CurrentValue + vm.TitlebarHeight.CurrentValue + 50;
        var boxedWidth = mainWindow.UIHelper.GetMainView.Bounds.Width * screen.Scaling - padding;
        var boxedHeight = mainWindow.UIHelper.GetMainView.Bounds.Height * screen.Scaling - padding;
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
