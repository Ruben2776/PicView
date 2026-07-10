using Avalonia.Controls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.MacOS.PlatformUpdate;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.MacOS.Printing;
using PicView.Core.Update;
using PicView.Core.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.MacOS.WindowImpl;

public class MacWindowProvider : IWindowProvider
{
    public Window CreateAboutWindow() => new AboutWindow();

    public Window CreateBatchResizeWindow(BatchResizeWindowConfig config) => new BatchResizeWindow(config);
    
    public Window CreateFileAssociationsWindow() => null!;

    public Window CreateConvertWindow() => new ConvertWindow();

    public Window CreateEffectsWindow(EffectsWindowConfig config) => new EffectsWindow(config);

    public Window CreateImageInfoWindow(MainWindowViewModel vm) => new ImageInfoWindow(vm);

    public Window CreateKeybindingsWindow(KeybindingWindowConfig config) => new KeybindingsWindow(config);

    public Window CreateSettingsWindow(SettingsWindowConfig config) => new SettingsWindow(config);

    public Window CreateSingleImageResizeWindow(MainWindowViewModel vm) => new SingleImageResizeWindow(vm);

    public Window CreatePrintPreviewWindow(PrintWindowConfig config) => new PrintPreviewWindow(config);

    public async ValueTask InitializePrintAsync(MainWindowViewModel vm, string path, Window printPreviewWindow)
    {
        if (printPreviewWindow is PrintPreviewWindow win)
        {
            await MacPrintInitialization.InitializeAsync(vm, path);
        }
    }

    public async Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath)
    {
        await MacUpdateHelper.HandleMacOSUpdate(updateInfo, tempPath);
    }
}
