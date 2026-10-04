using Avalonia.Media.Imaging;
using PicView.Avalonia.CustomControls;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Crop;

public static class CropManager
{
    public static bool IsCropping(MainWindow mainWindow) => GetService(mainWindow)?.IsCropping ?? false;

    private static CropService? GetService(MainWindow mainWindow)
    {
        if (mainWindow.UIHelper.GetMainView.DataContext is not MainWindowViewModel vm)
        {
            return null;
        }

        var activeTab = vm.WindowTabs.ActiveTab.Value;
        if (activeTab is null)
        {
            return null;
        }

        if (activeTab.CropService is not CropService service)
        {
            activeTab.CropService = service = new CropService(activeTab, mainWindow);
        }

        return service;
    }

    public static bool SetIfCropEnabled(MainWindow mainWindow)
    {
        if (mainWindow.UIHelper.GetMainView.DataContext is not MainWindowViewModel vm)
        {
            return false;
        }
        if (IsCropping(mainWindow))
        {
            return false;
        }
        
        var tab = vm.WindowTabs.ActiveTab.Value;
        
        if (tab.Image.CurrentValue is not Bitmap || Settings.ImageScaling.ShowImageSideBySide)
        {
            tab.ShouldCropBeEnabled.Value = false;
            return false;
        }
        
        if (vm.IsEditableTitlebarOpen.CurrentValue)
        {
            return false;
        }
        
        if (tab.RotationAngle.CurrentValue is 0 && tab.ScaleX.CurrentValue is 1)
        {
            tab.ShouldCropBeEnabled.Value = true;
            return true;
        }
        
        tab.ShouldCropBeEnabled.Value = false;
        return false;
    }

    /// <summary>
    /// Starts the cropping functionality by setting up the ImageCropperViewModel 
    /// and adding the CropControl to the main view.
    /// </summary>
    /// <remarks>
    /// This method checks if cropping can be enabled and if the image source is valid.
    /// If conditions are met, it configures the crop control with the appropriate dimensions
    /// and updates the view model's title and tooltip to reflect the cropping state.
    /// </remarks>
    public static async Task StartCropControlAsync(MainWindowViewModel vm, MainWindow mainWindow)
    {
        var activeTab = vm.WindowTabs.ActiveTab.Value;
        if (activeTab is null)
        {
            return;
        }

        if (activeTab.CropService is not CropService service)
        {
            activeTab.CropService = service = new CropService(activeTab, mainWindow);
        }

        await service.StartCropControlAsync().ConfigureAwait(false);
    }

    public static void CloseCropControl(MainWindowViewModel vm)
    {
        var activeTab = vm.WindowTabs.ActiveTab.Value;

        (activeTab?.CropService as CropService)?.CloseCropControl();
    }
}