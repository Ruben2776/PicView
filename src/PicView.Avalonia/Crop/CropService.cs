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
using PicView.Avalonia.ImageTransformations;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.ImageTransformations;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;
using R3;
using Orientation = PicView.Core.ImageTransformations.Orientation;

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
                tabViewModel.Model.PixelHeight, tabViewModel.Orientation);
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

        return RenderSelection(sourceBitmap, tabViewModel.Crop);
    }

    /// <summary>
    /// The crop's selection of <paramref name="source"/> as shown, with the crop's turn and flip in its pixels.
    /// </summary>
    internal static Bitmap RenderSelection(Bitmap source, CropViewModel crop)
    {
        var area = crop.SourceSelection;
        var cut = BitmapHelper.ConvertCroppedBitmapToBitmap(
            new CroppedBitmap(source, new PixelRect(area.X, area.Y, (int)area.Width, (int)area.Height)));
        if (crop.Orientation == Orientation.Upright)
        {
            return cut;
        }

        // The turned copy is not disposed by callers, since pasting in PicView can show that same bitmap in a tab
        var turned = OrientedBitmap.Render(cut, crop.Orientation);
        cut.Dispose();
        return turned;
    }

    /// <summary>
    /// A crop saved over the tab's own file leaves the file upright, so keeping the tab's turn would turn it twice.
    /// </summary>
    internal static bool ShouldResetOrientation(string savedPath, FileInfo? tabFile) =>
        tabFile is not null && string.Equals(tabFile.FullName, savedPath, StringComparison.Ordinal);

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
        if (ShouldResetOrientation(fileName, tabViewModel.FileInfo.Value))
        {
            var newModel = await GetImageModel.GetImageModelAsync(fileInfo).ConfigureAwait(false);
            tabViewModel.ImageIterator.Cache.Put(tabViewModel.Id, fileInfo, newModel);
            await Dispatcher.UIThread.InvokeAsync(tabViewModel.Orientation.Reset);
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
        var bitmap = RenderSelection((Bitmap)tabViewModel.Image.CurrentValue!, tabViewModel.Crop!);
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
        CropRender.CutAndOrient(image, crop.SourceSelection, crop.Orientation);
        await image.WriteAsync(saveFilePath).ConfigureAwait(false);
        return image.ToWriteableBitmap();
    }
}