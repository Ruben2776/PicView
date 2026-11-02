using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Printing;
using PicView.Avalonia.MacOS.PlatformUpdate;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.MacOS.Printing;
using PicView.Core.Update;
using PicView.Core.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.MacOS.WindowImpl;

public class MacWindowProvider : IWindowProvider
{
    public MainWindow CreateMainWindow(TabViewModel? firstTab = null) => new MacMainWindow(null, firstTab);

    public IWindowModeAdapter CreateWindowModeAdapter() => new MacWindowModeAdapter();

    public Window CreateAboutWindow() => new AboutWindow();

    public Window CreateBatchResizeWindow() => new BatchResizeWindow();
    
    public Window CreateFileAssociationsWindow() => null!;

    public Window CreateConvertWindow() => new ConvertWindow();

    public Window CreateEffectsWindow() => new EffectsWindow();

    public Window CreateImageInfoWindow(MainWindowViewModel vm) => new ImageInfoWindow(vm);

    public Window CreateKeybindingsWindow() => new KeybindingsWindow();

    public Window CreateSettingsWindow(SettingsWindowConfig config) => new SettingsWindow(config);

    public Window CreateSingleImageResizeWindow(MainWindowViewModel vm) => new SingleImageResizeWindow(vm);

    public Window CreatePrintPreviewWindow() => new PrintPreviewWindow();

    public ISystemPrinters CreateSystemPrinters() => new MacPrinters();

    public async Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath)
    {
        await MacUpdateHelper.HandleMacOSUpdate(updateInfo, tempPath);
    }
}
