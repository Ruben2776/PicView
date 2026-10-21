using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Printing;
using PicView.Core.Config;
using PicView.Core.Update;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Interfaces;

public interface IWindowProvider
{
    /// <summary>
    /// Creates a Main Window, which opens with <paramref name="firstTab"/> when a tab moved out of another one, or
    /// with a new tab otherwise.
    /// </summary>
    MainWindow CreateMainWindow(TabViewModel? firstTab = null);

    /// <summary>
    /// Creates what this platform does natively when a main window changes Window Mode.
    /// </summary>
    IWindowModeAdapter CreateWindowModeAdapter();
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

    /// <summary>
    /// Creates the adapter to this platform's printers, which Print Jobs are sent to.
    /// </summary>
    ISystemPrinters CreateSystemPrinters();

    Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath);
}
