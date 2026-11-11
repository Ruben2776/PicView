using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Animations;
using PicView.Avalonia.Clipboard;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Crop;

public class CropService(TabViewModel tabViewModel, MainWindow mainWindow) : ICropService
{
    public bool IsCropping { get; private set; }

    public async Task StartCropControlAsync()
    {
        if (!CropManager.SetIfCropEnabled(mainWindow) || mainWindow.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            tabViewModel.Crop = new CropViewModel(tabViewModel.Zoom, tabViewModel.Model.PixelWidth,
                tabViewModel.Model.PixelHeight);
        });

        IsCropping = true;

        vm.TopTitlebarViewModel.CloseDropDownMenu();

        if (tabViewModel.Crop is not null)
        {
            tabViewModel.Crop.CloseCropCommand.Subscribe(_ =>
            {
                CloseCropControl();
            },DebugHelper.LogError(nameof(CropService), nameof(CloseCropControl)));
            
            tabViewModel.Crop.CopyCropImageCommand.SubscribeAwait(async (_, _) =>
            {
                await CopyCroppedImageAsync().ConfigureAwait(false);
            },DebugHelper.LogError(nameof(CropService), nameof(CloseCropControl)));
            
            tabViewModel.Crop.CropImageCommand.SubscribeAwait(async (_, _) =>
            {
                await SaveCropAsAsync().ConfigureAwait(false);
            },DebugHelper.LogError(nameof(CropService), nameof(CloseCropControl)));
        }
    }

    public async ValueTask CopyCroppedImageAsync()
    {
        if (GetCroppedImage() is Bitmap bitmap)
        {
            await mainWindow.ClipboardCopyService.CopyImageAsync(bitmap).ConfigureAwait(false);
        }
    }

    public object? GetCroppedImage()
    {
        if (tabViewModel.Image.CurrentValue is not Bitmap sourceBitmap || tabViewModel.Crop is null)
        {
            return null;
        }

        var croppedBitmap = new CroppedBitmap(sourceBitmap, SelectionOf(tabViewModel.Crop));
        return BitmapHelper.ConvertCroppedBitmapToBitmap(croppedBitmap);
    }

    /// <summary>The crop's selection, which it holds in image pixels.</summary>
    private static PixelRect SelectionOf(CropViewModel crop) =>
        new(crop.PixelX.Value, crop.PixelY.Value, (int)crop.PixelSelectionWidth.Value,
            (int)crop.PixelSelectionHeight.Value);

    public void CloseCropControl()
    {
        IsCropping = false;
        
        tabViewModel.Crop = null;
    }

    public async Task SaveCropAsAsync()
    {
        if (tabViewModel.Crop is null)
        {
            return;
        }
        
        var (fileName, fileInfo, bitmap) = PrepareCropData();
        
        var saveFileDialogResult = await FilePicker.PickFileForSavingAsync(fileName).ConfigureAwait(false);
        if (saveFileDialogResult is null)
        {
            return;
        }
        await SaveCroppedImage(saveFileDialogResult, fileInfo, bitmap).ConfigureAwait(false);
    }

    private async Task SaveCroppedImage(string fileName, FileInfo fileInfo, Bitmap bitmap)
    {
        var vm = await Dispatcher.UIThread.InvokeAsync(() => mainWindow.DataContext as MainWindowViewModel);
        if (vm is null)
        {
            return;
        }
        using var busy = tabViewModel.ClaimBusy();
        await SaveImage(fileName, fileInfo, bitmap).ConfigureAwait(false);
        if (string.Equals(tabViewModel.FileInfo.Value.FullName, fileName, StringComparison.Ordinal))
        {
            var newModel = await GetImageModel.GetImageModelAsync(fileInfo).ConfigureAwait(false);
            tabViewModel.ImageIterator.Cache.Put(tabViewModel.Id, fileInfo, newModel);
            tabViewModel.Display(newModel, tabViewModel.SecondaryModel);
        }
        
        await Dispatcher.UIThread.InvokeAsync(CloseCropControl);
    }

    private (string fileName, FileInfo? fileInfo, Bitmap? bitmap) PrepareCropData()
    {
        if (tabViewModel.FileInfo is null || !tabViewModel.FileInfo.CurrentValue.Exists)
        {
            return CreateNewCroppedImage();
        }

        return (tabViewModel.FileInfo.Value.FullName, tabViewModel.FileInfo.Value, null);
    }

    private (string fileName, FileInfo fileInfo, Bitmap bitmap) CreateNewCroppedImage()
    {
        var fileName = $"{TranslationManager.Translation.Crop} {new Random().Next(9999)}.png";
        var croppedBitmap = new CroppedBitmap(tabViewModel.Image.CurrentValue as Bitmap, SelectionOf(tabViewModel.Crop!));
        var bitmap = BitmapHelper.ConvertCroppedBitmapToBitmap(croppedBitmap);
        return (fileName, new FileInfo(fileName), bitmap);
    }

    private async ValueTask<Bitmap?> SaveImage(string saveFilePath, FileInfo? fileInfo, Bitmap? bitmap)
    {
        if (bitmap is not null)
        {
            bitmap.Save(saveFilePath, PngBitmapEncoderOptions.Default);
            return bitmap;
        }

        if (fileInfo is not null && tabViewModel.Crop is not null)
        {
            return await SaveWithMagickImage(tabViewModel.Crop, saveFilePath, fileInfo).ConfigureAwait(false);
        }

        return null;
    }

    private static async ValueTask<Bitmap> SaveWithMagickImage(CropViewModel crop, string saveFilePath, FileInfo fileInfo)
    {
        using var image = new MagickImage(fileInfo.FullName);
        var geometry = new MagickGeometry(crop.PixelX.Value, crop.PixelY.Value, crop.PixelSelectionWidth.Value,
            crop.PixelSelectionHeight.Value);

        image.Crop(geometry);
        await image.WriteAsync(saveFilePath).ConfigureAwait(false);
        return image.ToWriteableBitmap();
    }
}