using Avalonia.Controls;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.Views;

public partial class SingleImageResizeView : UserControl
{
    public SingleImageResizeView()
    {
        InitializeComponent();
        Loaded += delegate
        {
            if (DataContext is not MainViewModel vm)
                return;
            
            SaveButton.Click += async (_, _) => await SaveImage(vm).ConfigureAwait(false);
            SaveAsButton.Click += async (_, _) => await SaveImageAs(vm).ConfigureAwait(false);
        };
    }

    private async Task SaveImageAs(MainViewModel vm)
    {
        throw new NotImplementedException();
    }

    private async Task SaveImage(MainViewModel vm)
    {
        await DoSaveImage(vm, vm.FileInfo.FullName).ConfigureAwait(false);
    }
    
    private async Task DoSaveImage(MainViewModel vm, string destination)
    {
        if (!uint.TryParse(PixelWidthTextBox.Text, out var width) || !uint.TryParse(PixelHeightTextBox.Text, out var height))
        {
            return;
        }

        if (width == vm.PixelWidth)
        {
            if (height == vm.PixelHeight)
            {
                
            }
            else
            {
                width = 0;
            }
        }

        if (height == vm.PixelHeight)
        {
            if (width == vm.PixelWidth)
            {
                
            }
            else
            {
                height = 0;
            }
        }

        var file = vm.FileInfo.FullName;
        uint? quality = null;
        if (QualitySlider.IsEnabled)
        {
            quality = (uint)QualitySlider.Value;
        }
        var ext = vm.FileInfo.Extension;
        if (!NoConversion.IsSelected)
        {
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
        }
        var success = await SaveImageFileHelper.SaveImageAsync(null, file, destination, width, height, quality, ext, vm.RotationAngle).ConfigureAwait(false);
        if (!success)
        {
            // TODO: show error
            return;
        }
        if (destination == file)
        {
            if (!NavigationHelper.CanNavigate(vm))
            {
                return;
            }

            if (vm.ImageIterator is not null)
            {
                await vm.ImageIterator.QuickReload().ConfigureAwait(false);
            }
        }
        else
        {
            if (Path.GetDirectoryName(file) == Path.GetDirectoryName(destination))
            {
                await NavigationHelper.LoadPicFromFile(destination, vm).ConfigureAwait(false);
            }
        }
    }
}
