using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Printing;
using PicView.Avalonia.Linux.PlatformUpdate;
using PicView.Avalonia.Linux.Views;
using PicView.Avalonia.Linux.Printing;
using PicView.Core.Update;
using PicView.Core.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Linux.WindowImpl;

public class LinuxWindowProvider : IWindowProvider
{
    public MainWindow CreateMainWindow(TabViewModel? firstTab = null) => new LinuxMainWindow(firstTab);

    public IWindowModeAdapter CreateWindowModeAdapter() => new LinuxWindowModeAdapter();

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

    public ISystemPrinters CreateSystemPrinters() => new LinuxPrinters();

    public async Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath)
    {
        await LinuxUpdateHelper.HandleLinuxUpdate(updateInfo, tempPath);
    }
}
