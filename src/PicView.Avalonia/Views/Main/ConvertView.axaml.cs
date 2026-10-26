using Avalonia.Controls;
using Avalonia.Input;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.Main;

public partial class ConvertView : UserControl
{
    public ConvertView()
    {
        InitializeComponent();
        
        Loaded += delegate
        {
            if (DataContext is not MainWindowViewModel vm)
            {
                return;
            }
        
            SaveButton.Click += (_, _) =>
            {
                var source = vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue.FullName;
                using var magick = new MagickImage(source);
                var destination = GetDestinationWithChangedExtension(source);
                magick.Write(destination);
            };
        
            SaveAsButton.Click += async (_, _) =>
            {
                var source = vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue.FullName;
                var suggestedFileName = GetDestinationWithChangedExtension(source);
                var file =  await FilePicker.PickFileForSavingAsync(suggestedFileName).ConfigureAwait(false);
                if (file is null)
                {
                    return;
                }
                using var magick = new MagickImage(source);
                magick.Write(file);
            };
        
            CancelButton.Click += (_, _) => SafeClose();
        };
    }
    
    private void SafeClose()
    {
        Dispatcher.Invoke(() =>
        {
            if (TopLevel.GetTopLevel(this) is not Window window)
            {
                return;
            }
            window.Close();
        });
    }

    private string GetDestinationWithChangedExtension(string source)
    {
        if (NoConversion.IsSelected)
        {
            return source;
        }
        string ext;
        
        if (PngItem.IsSelected)
        {
            ext = ".png";
        }
        else if (JpgItem.IsSelected)
        {
            ext = ".jpg";
        }
        else if (WebpItem.IsSelected)
        {
            ext = ".webp";
        }
        else if (AvifItem.IsSelected)
        {
            ext = ".avif";
        }
        else if (HeicItem.IsSelected)
        {
            ext = ".heic";
        }
        else if (JxlItem.IsSelected)
        {
            ext = ".jxl";
        }
        else
        {
            return source;
        }
        
        source = Path.ChangeExtension(source, ext);
        return source;
    }

    private void BottomBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not GenericWindow window)
        {
            return;
        }
        window.MoveWindow(sender,e);
    }
}