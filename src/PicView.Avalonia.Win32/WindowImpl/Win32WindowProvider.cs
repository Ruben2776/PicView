using Avalonia.Controls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Win32.PlatformUpdate;
using PicView.Avalonia.Win32.Views;
using PicView.Avalonia.Win32.Printing;
using PicView.Core.Update;
using PicView.Core.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Win32.WindowImpl;

public class Win32WindowProvider : IWindowProvider
{
    public Window CreateAboutWindow() => new AboutWindow();

    public Window CreateBatchResizeWindow(BatchResizeWindowConfig config) => new BatchResizeWindow(config);
    
    public Window CreateFileAssociationsWindow() => new FileAssociationWindow();

    public Window CreateConvertWindow() => new ConvertWindow();

    public Window CreateEffectsWindow(EffectsWindowConfig config) => new EffectsWindow(config);

    public Window CreateImageInfoWindow(MainWindowViewModel vm) => new ImageInfoWindow(vm);

    public Window CreateKeybindingsWindow(KeybindingWindowConfig config) => new KeybindingsWindow(config);

    public Window CreateSettingsWindow(SettingsWindowConfig config) => new SettingsWindow(config);

    public Window CreateSingleImageResizeWindow(MainWindowViewModel vm) => new SingleImageResizeWindow(vm);

    public Window CreatePrintPreviewWindow(PrintWindowConfig config) => new PrintPreviewWindow(config);

    public async ValueTask InitializePrintAsync(MainWindowViewModel vm, string path, Window printPreviewWindow) =>
        await Win32PrintInitialization.InitializeAsync(vm, path);

    public async Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath)
    {
        await WinUpdateHelper.HandleWindowsUpdate(updateInfo, tempPath);
    }
}
