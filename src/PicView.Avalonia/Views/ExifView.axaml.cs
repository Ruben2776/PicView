using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.Converters;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Resizing;
using PicView.Avalonia.ViewModels;
using PicView.Core.ImageDecoding;
using PicView.Core.Navigation;
using ReactiveUI;

namespace PicView.Avalonia.Views;

public partial class ExifView : UserControl
{
    private IDisposable? _imageUpdateSubscription;
    
    public ExifView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            KeyDown += (_, e) =>
            {
                switch (e.Key)
                {
                    case Key.Down:
                    case Key.PageDown:
                        ScrollViewer.LineDown();
                        break;
                    case Key.Up:
                    case Key.PageUp:
                        ScrollViewer.LineUp();
                        break;
                    case Key.Home:
                        ScrollViewer.ScrollToHome();
                        break;
                    case Key.End:
                        ScrollViewer.ScrollToEnd();
                        break;
                }
            };
            
            PixelWidthTextBox.KeyDown += async (s, e) => await SaveImageOnEnter(s,e);
            PixelHeightTextBox.KeyDown += async (s, e) => await SaveImageOnEnter(s,e);

            PixelWidthTextBox.KeyUp += delegate { AdjustAspectRatio(PixelWidthTextBox); };
            PixelHeightTextBox.KeyUp += delegate { AdjustAspectRatio(PixelHeightTextBox); };
            
            if (DataContext is not MainViewModel vm)
            {
                return;
            }
            
            ExifHandling.UpdateExifValues(vm);
            
            _imageUpdateSubscription = vm.WhenAnyValue(x => x.FileInfo).Select(x => x is not null).Subscribe(_ =>
            {
                ExifHandling.UpdateExifValues(vm);
            });
            ResetButton.Click += (_, _) =>
            {
                PixelWidthTextBox.Text = vm.PixelWidth.ToString();
                PixelHeightTextBox.Text = vm.PixelHeight.ToString();
                AdjustAspectRatio(PixelWidthTextBox);
                FullPathTextBox.Text = vm.FileInfo?.FullName ?? "";
                DirectoryNameTextBox.Text = vm.FileInfo?.DirectoryName ?? "";
                FileNameTextBox.Text = vm.FileInfo?.Name ?? ""; 
            };
            
            SaveButton.Click += async (_, _) =>
            {
                var ext = GetExtension();
                var location = FullPathTextBox.Text; // TODO check if this is a valid path
                                                     // and sync with file name/directory text boxes
                await SendToImageSaver(vm.FileInfo?.FullName, location, PixelWidthTextBox.Text, PixelHeightTextBox.Text, ext).ConfigureAwait(false);
            };

            SaveAsButton.Click += async (_, _) =>
            {
                var fileInfoFullName = vm.FileInfo.FullName;
                var ext = DetermineFileExtension(vm, ref fileInfoFullName);
        
                var file = await FilePicker.PickFileForSavingAsync(vm.FileInfo?.FullName, ext);
                if (file is null)
                {
                    return;
                }
                await SendToImageSaver( vm.FileInfo?.FullName, file, PixelWidthTextBox.Text, PixelHeightTextBox.Text, ext).ConfigureAwait(false);
            };
        };
    }

    private static async Task SendToImageSaver(string? location, string destination, string? width, string? height, string ext)
    {
        if (!uint.TryParse(width, out var widthValue) || !uint.TryParse(height, out var heightValue))
        {
            return;
        }
        await SaveImageFileHelper.SaveImageAsync(null, location, destination, widthValue, heightValue, null, ext).ConfigureAwait(false);
    }

    private string GetExtension () => ConversionComboBox.SelectedIndex switch
    {
        1 => ".png",
        2 => ".jpg",
        3 => ".webp",
        4 => ".avif",
        5 => ".heic",
        6 => ".jxl",
        _ => ""
    };

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _imageUpdateSubscription.Dispose();
    }

    private void AdjustAspectRatio(TextBox sender)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        if (!int.TryParse(PixelWidthTextBox.Text, out var width) || !int.TryParse(PixelHeightTextBox.Text, out var height))
        {
            return;
        }

        if (width <=0 || height <= 0)
        {
            return;
        }
        var printSizes = AspectRatioHelper.GetPrintSizes(width, height, vm.DpiX, vm.DpiY);
        PrintSizeInchTextBox.Text = printSizes.PrintSizeInch;
        PrintSizeCmTextBox.Text = printSizes.PrintSizeCm;
        SizeMpTextBox.Text = printSizes.SizeMp;

        var gcd = ImageTitleFormatter.GCD(width, height);
        var aspectRatio = (double)vm.PixelWidth / vm.PixelHeight;
        AspectRatioTextBox.Text = AspectRatioHelper.GetFormattedAspectRatio(gcd, vm.PixelWidth, vm.PixelHeight);
        
        AspectRatioHelper.SetAspectRatioForTextBox(PixelWidthTextBox, PixelHeightTextBox, sender == PixelWidthTextBox,
            aspectRatio, DataContext as MainViewModel);
    }

    private static async Task DoResize(MainViewModel vm, bool isWidth, object width, object height)
    {
        if (isWidth)
        {
            if (!double.TryParse((string?)width, out var widthValue))
            {
                return;
            }
            if (widthValue > 0)
            {
                var success = await ConversionHelper.ResizeByWidth(vm.FileInfo, widthValue).ConfigureAwait(false);
                if (success)
                {
                    if (vm.ImageIterator is not null)
                    {
                        await vm.ImageIterator.QuickReload().ConfigureAwait(false);
                    }
                }
            }
        }
        else
        {
            if (!double.TryParse((string?)height, out var heightValue))
            {
                return;
            }
            if (heightValue > 0)
            {
                var success = await ConversionHelper.ResizeByHeight(vm.FileInfo, heightValue).ConfigureAwait(false);
                if (success)
                {
                    vm.ImageIterator?.RemoveCurrentItemFromPreLoader();
                    await NavigationHelper.Navigate(vm.ImageIterator.CurrentIndex, vm).ConfigureAwait(false);
                }
            }
        }
    }
    
    private async Task SaveImageOnEnter(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            await DoResize(vm, Equals(sender, PixelWidthTextBox), PixelWidthTextBox.Text, PixelHeightTextBox.Text).ConfigureAwait(false);
        }
    }
    
    private string DetermineFileExtension(MainViewModel vm, ref string destination)
    {
        var ext = vm.FileInfo.Extension;
        if (NoConversion.IsSelected)
        {
            return ext;
        }

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
        destination = Path.ChangeExtension(destination, ext);
        return ext;
    }
}