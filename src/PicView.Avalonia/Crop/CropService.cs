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
using PicView.Avalonia.Views.UC;
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
    
    private object? _backUpView;
    private bool _couldNavigateBackwards;
    private bool _couldNavigateForwards;

    public async Task StartCropControlAsync()
    {
        if (!CropManager.SetIfCropEnabled(mainWindow) || mainWindow.DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        _backUpView = tabViewModel.CurrentView.Value;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Cropping hides the gallery, so fit before reading the size the crop area takes
            tabViewModel.Crop = new CropViewModel();
            mainWindow.WindowFitter.Refit();
            tabViewModel.Crop.AspectRatio = tabViewModel.InitialZoom.CurrentValue;

            var cropControl = new CropControl
            {
                DataContext = tabViewModel,
                Width = vm.ImageWidth.CurrentValue,
                Height = vm.ImageHeight.CurrentValue,
                Margin = new Thickness(0)
            };
            tabViewModel.CurrentView.Value = cropControl;
        });

        IsCropping = true;
        
        tabViewModel.Title.Value = TranslationManager.Translation.CropMessage!;
        tabViewModel.TitleTooltip.Value = TranslationManager.Translation.CropMessage!;

        _couldNavigateBackwards = tabViewModel.CanNavigateBackwards.CurrentValue;
        _couldNavigateForwards = tabViewModel.CanNavigateForwards.CurrentValue;
        tabViewModel.CanNavigateBackwards.Value = false;
        tabViewModel.CanNavigateForwards.Value = false;
        
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

        var x = Convert.ToInt32(tabViewModel.Crop.SelectionX.CurrentValue / tabViewModel.Crop.AspectRatio);
        var y = Convert.ToInt32(tabViewModel.Crop.SelectionY.CurrentValue / tabViewModel.Crop.AspectRatio);
        var rect = new PixelRect(x, y, (int)tabViewModel.Crop.PixelSelectionWidth.CurrentValue, (int)tabViewModel.Crop.PixelSelectionHeight.CurrentValue);

        var croppedBitmap = new CroppedBitmap(sourceBitmap, rect);
        return BitmapHelper.ConvertCroppedBitmapToBitmap(croppedBitmap);
    }

    public void CloseCropControl()
    {
        if (_backUpView is not null)
        {
            tabViewModel.CurrentView.Value = _backUpView;
            _backUpView = null;
        }

        IsCropping = false;
        tabViewModel.UpdateTabTitle();
        
        tabViewModel.Crop = null;
        
        tabViewModel.CanNavigateBackwards.Value = _couldNavigateBackwards;
        tabViewModel.CanNavigateForwards.Value = _couldNavigateForwards;

        // Leaving crop shows the gallery again, if the tab has one
        mainWindow.WindowFitter.Refit();
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
        vm.IsLoadingIndicatorShown.Value = true;
        await SaveImage(fileName, fileInfo, bitmap).ConfigureAwait(false);
        if (string.Equals(tabViewModel.FileInfo.Value.FullName, fileName, StringComparison.Ordinal))
        {
            var newModel = await GetImageModel.GetImageModelAsync(fileInfo).ConfigureAwait(false);
            tabViewModel.ImageIterator.Cache.Put(tabViewModel.Id, fileInfo, newModel);
            tabViewModel.Display(newModel, tabViewModel.SecondaryModel);
        }
        
        await Dispatcher.UIThread.InvokeAsync(CloseCropControl);
        vm.IsLoadingIndicatorShown.Value = false;
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
        var crop = tabViewModel.Crop!;
        var x = Convert.ToInt32(crop.SelectionX.CurrentValue / crop.AspectRatio);
        var y = Convert.ToInt32(crop.SelectionY.CurrentValue / crop.AspectRatio);
        var width = (int)crop.PixelSelectionWidth.CurrentValue;
        var height = (int)crop.PixelSelectionHeight.CurrentValue;
        var croppedBitmap = new CroppedBitmap(tabViewModel.Image.CurrentValue as Bitmap, new PixelRect(x, y, width, height));
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
        var x = Convert.ToInt32(crop.SelectionX.CurrentValue / crop.AspectRatio);
        var y = Convert.ToInt32(crop.SelectionY.CurrentValue / crop.AspectRatio);
        var width = crop.PixelSelectionWidth.CurrentValue;
        var height = crop.PixelSelectionHeight.CurrentValue;
        var geometry = new MagickGeometry(x, y, width, height);

        image.Crop(geometry);
        await image.WriteAsync(saveFilePath).ConfigureAwait(false);
        return image.ToWriteableBitmap();
    }
}