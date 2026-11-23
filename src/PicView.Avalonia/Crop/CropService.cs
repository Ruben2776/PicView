using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.ImageTransformations;
using PicView.Core.DebugTools;
using PicView.Core.ImageTransformations;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Saving;
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

    public void CloseCropControl()
    {
        IsCropping = false;

        // Taken off the tab first, so the tab's view and everything that reads the tab's crop is done with it, then
        // disposed, so it stops following the tab's Orientation and its commands end the service's subscriptions
        var crop = tabViewModel.Crop;
        tabViewModel.Crop = null;
        crop?.Dispose();
    }

    public async Task SaveCropAsAsync()
    {
        if (tabViewModel.Crop is null)
        {
            return;
        }

        // A Single Image, such as a pasted one, has no file, so its crop is saved as a new PNG
        if (tabViewModel.FileInfo.CurrentValue is not { Exists: true } file)
        {
            await SaveSingleImageCropAsync().ConfigureAwait(false);
            return;
        }

        var picked = await FilePicker.PickFileForSavingAsync(file.FullName).ConfigureAwait(false);
        if (picked is null)
        {
            return;
        }

        var vm = await Dispatcher.UIThread.InvokeAsync(() => mainWindow.DataContext as MainWindowViewModel);
        if (vm is null)
        {
            return;
        }

        await SaveCropToAsync(vm.Core.Writer, picked).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the crop's selection of the tab's file to <paramref name="path"/> through the save module, over the file
    /// itself when that is the path, then closes the crop. The crop stays open when nothing was written.
    /// </summary>
    internal async Task<bool> SaveCropToAsync(ShownFileWriter writer, string path)
    {
        if (tabViewModel.Crop is not { } crop)
        {
            return false;
        }

        var job = new ShownFileWrite(WriteSource.Unedited, new WriteEdits(Cut: crop.SourceSelection),
            new WriteDestination.Copy(path));
        var result = await writer.WriteAsync(tabViewModel, job).ConfigureAwait(false);
        if (result.Outcome is not WriteOutcome.Written)
        {
            return false;
        }

        await Dispatcher.UIThread.InvokeAsync(CloseCropControl);
        return true;
    }

    private async Task SaveSingleImageCropAsync()
    {
        var fileName = $"{TranslationManager.Translation.Crop} {new Random().Next(9999)}.png";
        var picked = await FilePicker.PickFileForSavingAsync(fileName).ConfigureAwait(false);
        if (picked is null)
        {
            return;
        }

        using var busy = tabViewModel.ClaimBusy();
        var bitmap = await Dispatcher.UIThread.InvokeAsync(() =>
            RenderSelection((Bitmap)tabViewModel.Image.CurrentValue!, tabViewModel.Crop!));
        bitmap.Save(picked, PngBitmapEncoderOptions.Default);
        await Dispatcher.UIThread.InvokeAsync(CloseCropControl);
    }
}
