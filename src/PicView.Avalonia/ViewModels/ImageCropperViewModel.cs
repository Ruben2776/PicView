using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Animations;
using PicView.Avalonia.Crop;
using PicView.Avalonia.Extensions;
using PicView.Avalonia.Extensions;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Core.Localization;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using R3;
using R3.Avalonia;
using R3.Avalonia;
using Unit = R3.Unit;
using ABI.Windows.AI.MachineLearning.Preview;
using PicView.Avalonia.History;

namespace PicView.Avalonia.ViewModels;

public class ImageCropperViewModel : IDisposable
{
    public ImageCropperViewModel(Bitmap bitmap)
    {
        CropImageCommand = new ReactiveCommand(CropImageAsync);
        SaveCropImageCommand = new ReactiveCommand(SaveCroppedImageAsync);
        CropImageCommand = new ReactiveCommand(CropImageAsync);
        SaveCropImageCommand = new ReactiveCommand(SaveCroppedImageAsync);
        CopyCropImageCommand = new ReactiveCommand(CopyCroppedImageAsync);
        CloseCropCommand = new ReactiveCommand(HandleCloseCrop);
        Bitmap.Value = bitmap;
    }

    public ReactiveCommand CropImageCommand { get; private set; }
    public ReactiveCommand SaveCropImageCommand { get; private set; }
    public ReactiveCommand SaveCropImageCommand { get; private set; }
    public ReactiveCommand CopyCropImageCommand { get; private set; }
    public ReactiveCommand CloseCropCommand { get; private set; }

    public BindableReactiveProperty<Bitmap> Bitmap { get; } = new();

    public BindableReactiveProperty<int> SelectionX { get; } = new();

    public BindableReactiveProperty<int> SelectionY { get; } = new();

    public BindableReactiveProperty<double> SelectionWidth { get; } = new();

    public BindableReactiveProperty<uint> PixelSelectionWidth { get; } = new();

    public BindableReactiveProperty<double> SelectionHeight { get; } = new();

    public BindableReactiveProperty<uint> PixelSelectionHeight { get; } = new();

    public BindableReactiveProperty<double> ImageWidth { get; } = new();
    public BindableReactiveProperty<double> ImageHeight { get; } = new();

    public BindableReactiveProperty<double> AspectRatio { get; } = new();

    public void Dispose()
    {
        Disposable.Dispose();
    }

    public void SetSelectionWidth(uint value)
    {
        SelectionWidth.Value = value;
        PixelSelectionWidth.Value = Convert.ToUInt32(value / AspectRatio.Value);
    }

    public void SetSelectionHeight(uint value)
    {
        SelectionHeight.Value = value;
        PixelSelectionHeight.Value = Convert.ToUInt32(value / AspectRatio.Value);
    }

    private static void HandleCloseCrop(Unit unit)
    {
        if (UIHelper.GetMainView.DataContext is MainViewModel vm)
        {
            CropFunctions.CloseCropControl(vm);
        }
    }

    public async ValueTask CropImageAsync(Unit unit, CancellationToken cancellationToken)
    {
        if (UIHelper.GetMainView.DataContext is not MainViewModel vm)
            return;

        if (vm.PicViewer.ImageSource.Value is not Bitmap bmp)
            return;

        vm.MainWindow.IsLoadingIndicatorShown.Value = true;

        try
        {
            var currentFileInfo = vm.PicViewer.FileInfo.Value;

            // Capture crop coordinates
            int x = (int)(SelectionX.CurrentValue / AspectRatio.CurrentValue);
            int y = (int)(SelectionY.CurrentValue / AspectRatio.CurrentValue);
            int width = (int)PixelSelectionWidth.CurrentValue;
            int height = (int)PixelSelectionHeight.CurrentValue;

            // Perform crop + convert to Bitmap off the UI thread
            var (croppedMagick, croppedBitmap) = await Task.Run(() =>
            {
                // Clone to avoid mutating original
                using var magick = bmp.ToMagickImage();

                // Crop operation
                magick.Crop(new MagickGeometry(x, y, (uint)width, (uint)height));
                magick.Page = new MagickGeometry(0, 0, (uint)magick.Width, (uint)magick.Height);

                // Convert to Avalonia Bitmap
                var bitmap = magick.ToAvaloniaBitmap();

                // Return both for reuse
                return ((MagickImage)magick.Clone(), bitmap);
            }, cancellationToken).ConfigureAwait(false);

            // Add to history (Bitmap version)
            await vm.HistoryManager
                .AddSnapshot(EditKind.Crop, $"Crop {width}×{height}", croppedBitmap)
                .ConfigureAwait(false);

            // Apply new image state on UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Dispose any previous bitmap
                if (vm.PicViewer.ImageSource.Value is Bitmap oldBmp)
                    oldBmp.Dispose();

                vm.PicViewer.CachedImage.Value = croppedBitmap;
                vm.PicViewer.ImageSource.Value = croppedBitmap;
                vm.PicViewer.HasChanges.Value = true;
            });

            // Update image model (other systems)
            await UpdateImage.SetCroppedImageAsync(
                croppedBitmap,
                ImageType.Bitmap,
                $"{currentFileInfo?.Name ?? TranslationManager.Translation.ClipboardImage}*",
                vm);

            CropFunctions.CloseCropControl(vm);
        }
        finally
        {
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        }
    }


    public async ValueTask CropImageAsync() => await CropImageAsync(Unit.Default, CancellationToken.None);

    private async ValueTask SaveCroppedImageAsync(Unit unit, CancellationToken cancellationToken)
    {
        if (UIHelper.GetMainView.DataContext is not MainViewModel vm)
        {
            return;
        }

        var (fileName, fileInfo, bitmap) = PrepareCropData(vm);

        var saveFileDialog = await FilePicker.PickFileForSavingAsync(fileName);
        if (saveFileDialog == null)
        {
            return;
        }

        await SaveImage(saveFileDialog, fileInfo, bitmap);

        CropFunctions.CloseCropControl(vm);

        if (vm.PicViewer.FileInfo.CurrentValue.FullName == saveFileDialog)
        {
            await ErrorHandling.ReloadAsync(vm);
        }
    }

    public async ValueTask SaveCroppedImageAsync() => await SaveCroppedImageAsync(Unit.Default, CancellationToken.None);

    private async ValueTask CopyCroppedImageAsync(Unit unit, CancellationToken cancellationToken)
    {
        if (UIHelper.GetMainView.DataContext is not MainViewModel vm ||
            vm.PicViewer.ImageSource.CurrentValue is not Bitmap sourceBitmap)
        {
            return;
        }

        var x = Convert.ToInt32(SelectionX.CurrentValue / AspectRatio.CurrentValue);
        var y = Convert.ToInt32(SelectionY.CurrentValue / AspectRatio.CurrentValue);
        var rect = new PixelRect(x, y, (int)PixelSelectionWidth.CurrentValue, (int)PixelSelectionHeight.CurrentValue);

        var croppedBitmap = new CroppedBitmap(sourceBitmap, rect);
        var bitmap = BitmapHelper.ConvertCroppedBitmapToBitmap(croppedBitmap);

        if (bitmap is not null)
        {
            await Task.WhenAll(vm.PlatformService.CopyImageToClipboard(bitmap), AnimationsHelper.CopyAnimation());
        }
    }

    public async ValueTask CopyCroppedImageAsync() => await CopyCroppedImageAsync(Unit.Default, CancellationToken.None);

    private (string fileName, FileInfo fileInfo, Bitmap? bitmap) PrepareCropData(MainViewModel vm)
        => NavigationManager.IsCollectionEmpty
            ? CreateNewCroppedImage()
            : (vm.PicViewer.FileInfo.Value.FullName, vm.PicViewer.FileInfo.Value, null);

    private (string fileName, FileInfo fileInfo, Bitmap bitmap) CreateNewCroppedImage()
    {
        var fileName = $"{TranslationManager.Translation.Crop} {new Random().Next(9999)}.png";
        var x = Convert.ToInt32(SelectionX.CurrentValue / AspectRatio.CurrentValue);
        var y = Convert.ToInt32(SelectionY.CurrentValue / AspectRatio.CurrentValue);
        var width = (int)PixelSelectionWidth.CurrentValue;
        var height = (int)PixelSelectionHeight.CurrentValue;
        var croppedBitmap = new CroppedBitmap(Bitmap.Value, new PixelRect(x, y, width, height));
        var bitmap = BitmapHelper.ConvertCroppedBitmapToBitmap(croppedBitmap);
        return (fileName, new FileInfo(fileName), bitmap);
    }

    private async ValueTask SaveImage(string saveFilePath, FileInfo fileInfo, Bitmap? bitmap)
    {
        if (bitmap != null)
        {
            bitmap.Save(saveFilePath);
            return;
        }

        await SaveWithMagickImage(saveFilePath, fileInfo);
    }

    private async ValueTask SaveWithMagickImage(string saveFilePath, FileInfo fileInfo)
    {
        using var image = new MagickImage(fileInfo.FullName);
        var x = Convert.ToInt32(SelectionX.CurrentValue / AspectRatio.CurrentValue);
        var y = Convert.ToInt32(SelectionY.CurrentValue / AspectRatio.CurrentValue);
        var width = PixelSelectionWidth.CurrentValue;
        var height = PixelSelectionHeight.CurrentValue;
        var geometry = new MagickGeometry(x, y, width, height);

        image.Crop(geometry);
        await image.WriteAsync(saveFilePath);
    }
}