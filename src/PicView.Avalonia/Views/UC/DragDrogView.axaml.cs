using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;

namespace PicView.Avalonia.Views.UC;

public partial class DragDropView : UserControl
{
    private LinkChain? _linkChain;
    private DirectoryIcon? _directoryIcon;
    private ZipIcon? _zipIcon;
    public DragDropView()
    {
        InitializeComponent();
        TxtDragToView.Text = TranslationHelper.Translation.DropToLoad;
        Width = UIHelper.GetMainView.Bounds.Width;
        Height = UIHelper.GetMainView.Bounds.Height;
    }

    public void AddLinkChain()
    {
        _linkChain ??= new LinkChain {  };
        if (!ParentPanel.Children.Contains(_linkChain))
        {
            ParentPanel.Children.Add(_linkChain);
            ParentPanel.Children.Move(ParentPanel.Children.IndexOf(_linkChain), 0);
        }
        ContentHolder.Background = null;
        ContentHolder.IsVisible = false;
    }

    public void AddDirectoryIcon()
    {
        _directoryIcon ??= new DirectoryIcon();
        if (!ParentPanel.Children.Contains(_directoryIcon))
        {
            ParentPanel.Children.Add(_directoryIcon);
            ParentPanel.Children.Move(ParentPanel.Children.IndexOf(_directoryIcon), 0);
        }
        ContentHolder.Background = null;
        ContentHolder.IsVisible = false;
    }

    public void AddZipIcon()
    {
        _zipIcon ??= new ZipIcon();
        if (!ParentPanel.Children.Contains(_zipIcon))
        {
            ParentPanel.Children.Add(_zipIcon);
            ParentPanel.Children.Move(ParentPanel.Children.IndexOf(_zipIcon), 0);
        }
        ContentHolder.Background = null;
        ContentHolder.IsVisible = false;
    }

    public void UpdateThumbnail(Bitmap image)
    {
        TxtDragToView.Text = TranslationHelper.Translation.DropToLoad;
        Width = UIHelper.GetMainView.Bounds.Width;
        Height = UIHelper.GetMainView.Bounds.Height;
        
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        var screen = ScreenHelper.ScreenSize;
        var padding = vm.BottombarHeight + vm.TitlebarHeight + 200;
        var boxedWidth = UIHelper.GetMainView.Bounds.Width * screen.Scaling - padding;
        var boxedHeight = UIHelper.GetMainView.Bounds.Height * screen.Scaling - padding;
        var scaledWidth = boxedWidth / image?.PixelSize.Width ?? 600;
        var scaledHeight = boxedHeight / image?.PixelSize.Height ?? 340;
        var scale = Math.Min(scaledWidth, scaledHeight);
        ContentHolder.Width = image?.PixelSize.Width * scale ?? 550;
        ContentHolder.Height = image?.PixelSize.Height * scale ?? 340;
        ContentHolder.Background = new ImageBrush
        {
            Opacity = 0.95,
            Source = image
        };
        ContentHolder.IsVisible = true;
    }

    public void RemoveThumbnail()
    {
        ContentHolder.Background = null;
        ParentPanel.Children.Remove(_linkChain);
    }
}