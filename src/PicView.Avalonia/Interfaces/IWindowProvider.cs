using Avalonia.Controls;
using PicView.Core.Config;
using PicView.Core.Update;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Interfaces;

public interface IWindowProvider
{
    Window CreateAboutWindow();
    Window CreateBatchResizeWindow(BatchResizeWindowConfig config);
    Window CreateFileAssociationsWindow();
    Window CreateConvertWindow();
    Window CreateEffectsWindow(EffectsWindowConfig config);
    Window CreateImageInfoWindow(MainWindowViewModel vm);
    Window CreateKeybindingsWindow(KeybindingWindowConfig config);
    Window CreateSettingsWindow(SettingsWindowConfig config);
    Window CreateSingleImageResizeWindow(MainWindowViewModel vm);
    Window CreatePrintPreviewWindow(PrintWindowConfig config);

    ValueTask InitializePrintAsync(MainWindowViewModel vm, string path, Window printPreviewWindow);

    Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath);
}
