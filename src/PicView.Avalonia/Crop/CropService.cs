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
using PicView.Core.IPlatform;
using PicView.Core.Localization;
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
        var isDockedGalleryShown = Settings.Gallery.IsGalleryDocked;
        // Hide gallery when entering crop mode
        if (isDockedGalleryShown)
        {
            // Reset setting before resizing
            Settings.Gallery.IsGalleryDocked = false;
            WindowResizing.SetSize(mainWindow, WindowResizeReason.Application);
        }
        
        var size = new Size(vm.ImageWidth.CurrentValue, vm.ImageHeight.CurrentValue);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            tabViewModel.Crop = new CropViewModel
            {
                AspectRatio = tabViewModel.InitialZoom.CurrentValue,
            };
            
            var cropControl = new CropControl
            {
                DataContext = tabViewModel,
                Width = size.Width,
                Height = size.Height,
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
        
        if (isDockedGalleryShown)
        {
            Settings.Gallery.IsGalleryDocked = true;
        }

        if (tabViewModel.Crop != null)
        {
            tabViewModel.Crop.CloseCropCommand.Subscribe(_ =>
            {
                CloseCropControl();
            },DebugHelper.LogError(nameof(CropService), nameof(CloseCropControl)));
            
            tabViewModel.Crop.CopyCropImageCommand.SubscribeAwait(async (_, _) =>
            {
                await CopyCroppedImageAsync();
            },DebugHelper.LogError(nameof(CropService), nameof(CloseCropControl)));
            
            tabViewModel.Crop.CropImageCommand.SubscribeAwait(async (_, _) =>
            {
                await PackAndSaveImage();
            },DebugHelper.LogError(nameof(CropService), nameof(CloseCropControl)));
        }
    }

    private async ValueTask CopyCroppedImageAsync()
    {
        if (GetCroppedImage() is Bitmap bitmap)
        {
            await Task.WhenAll(ClipboardImageOperations.CopyImageToClipboard(bitmap),
                AnimationsHelper.CopyAnimation(mainWindow));
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
        if (Settings.Gallery.IsGalleryDocked)
        {
            WindowResizing.SetSize(mainWindow, WindowResizeReason.Application);
        }
        
        tabViewModel.CurrentView.Value = _backUpView;
        IsCropping = false;
        tabViewModel.UpdateTabTitle();
        
        tabViewModel.Crop = null;
        
        tabViewModel.CanNavigateBackwards.Value = _couldNavigateBackwards;
        tabViewModel.CanNavigateForwards.Value = _couldNavigateForwards;
    }

    private async ValueTask PackAndSaveImage()
    {
        if (tabViewModel.Crop is null)
        {
            return;
        }
        
        var (fileName, fileInfo, bitmap) = PrepareCropData();
        
        var saveFileDialog = await FilePicker.PickFileForSavingAsync(fileName);
        if (saveFileDialog is null)
        {
            return;
        }
        
        await SaveImage(saveFileDialog, fileInfo, bitmap);
        
        CloseCropControl();

        if (tabViewModel.FileInfo.Value.FullName == saveFileDialog)
        {
            await tabViewModel.ImageIterator.ReloadAsync();
        }
    }

    private (string fileName, FileInfo? fileInfo, Bitmap? bitmap) PrepareCropData()
        => tabViewModel.FileInfo?.CurrentValue?.Exists ?? false
            ? CreateNewCroppedImage()
            : (tabViewModel.FileInfo.Value.FullName, tabViewModel.FileInfo.Value, null);

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

    private async ValueTask SaveImage(string saveFilePath, FileInfo? fileInfo, Bitmap? bitmap)
    {
        if (bitmap != null)
        {
            bitmap.Save(saveFilePath, PngBitmapEncoderOptions.Default);
            return;
        }

        if (fileInfo != null && tabViewModel.Crop != null)
        {
            await SaveWithMagickImage(tabViewModel.Crop, saveFilePath, fileInfo);
        }
    }

    private static async ValueTask SaveWithMagickImage(CropViewModel crop, string saveFilePath, FileInfo fileInfo)
    {
        using var image = new MagickImage(fileInfo.FullName);
        var x = Convert.ToInt32(crop.SelectionX.CurrentValue / crop.AspectRatio);
        var y = Convert.ToInt32(crop.SelectionY.CurrentValue / crop.AspectRatio);
        var width = crop.PixelSelectionWidth.CurrentValue;
        var height = crop.PixelSelectionHeight.CurrentValue;
        var geometry = new MagickGeometry(x, y, width, height);

        image.Crop(geometry);
        await image.WriteAsync(saveFilePath);
    }
}