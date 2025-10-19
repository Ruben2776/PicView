using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Animations;
using PicView.Avalonia.Crop;
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
using Unit = R3.Unit;
using ABI.Windows.AI.MachineLearning.Preview;
using PicView.Core.History;

namespace PicView.Avalonia.ViewModels;

public class ImageCropperViewModel : IDisposable
{
    public ImageCropperViewModel(Bitmap bitmap)
    {
        CropImageCommand = new ReactiveCommand(CropImageAsync);
        SaveCropImageCommand = new ReactiveCommand(SaveCroppedImageAsync);
        CopyCropImageCommand = new ReactiveCommand(CopyCroppedImageAsync);
        CloseCropCommand = new ReactiveCommand(HandleCloseCrop);
        Bitmap.Value = bitmap;
    }

    public ReactiveCommand CropImageCommand { get; private set; }
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

        if (vm.PicViewer.ImageSource.CurrentValue is not Bitmap sourceBitmap)
            return;

        vm.MainWindow.IsLoadingIndicatorShown.Value = true;

        try
        {
            var currentFileInfo = vm.PicViewer.FileInfo.Value;

            // History: capture pre-change state
            using (var pre = sourceBitmap.ToMagickImage())
            {
                vm.History.AddStep(new HistoryEntry
                {
                    Kind = EditKind.Crop,
                    Description = $"Before Crop",
                    Snapshot = (MagickImage)pre.Clone()
                });
            }
            
            var croppedBitmap = await Task.Run(() =>
            {
                int x = Convert.ToInt32(SelectionX.CurrentValue / AspectRatio.CurrentValue);
                int y = Convert.ToInt32(SelectionY.CurrentValue / AspectRatio.CurrentValue);
                int width  = (int)PixelSelectionWidth.CurrentValue;
                int height = (int)PixelSelectionHeight.CurrentValue;
                

                using var magick = sourceBitmap.ToMagickImage();
                var cropRect = new MagickGeometry(x, y, (uint)width, (uint)height);
                magick.Crop(cropRect);
                magick.Page = new MagickGeometry(0, 0, (uint)magick.Width, (uint)magick.Height);

                return magick.ToAvaloniaBitmap();
            });

            if (croppedBitmap is Bitmap newBitmap)
            {
                // History: capture the new state
                using (var post = croppedBitmap.ToMagickImage())
                {
                    vm.History.AddStep(new HistoryEntry
                    {
                        Kind = EditKind.Crop,
                        Description = $"Crop {croppedBitmap.PixelSize.Width}×{croppedBitmap.PixelSize.Height}",
                        Snapshot = (MagickImage)post.Clone()
                    });
                }

                var imageModel = new ImageModel
                {
                    Image = croppedBitmap,
                    PixelWidth = croppedBitmap?.PixelSize.Width ?? 0,
                    PixelHeight = croppedBitmap?.PixelSize.Height ?? 0,
                    ImageType = ImageType.Bitmap

                };
                await UpdateImage.SetCroppedImageAsync(imageModel.Image, imageModel.ImageType, String.Concat(currentFileInfo?.Name ?? TranslationManager.Translation.ClipboardImage, "*"), vm);
                vm.PicViewer.HasChanges.Value = true;
            }

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