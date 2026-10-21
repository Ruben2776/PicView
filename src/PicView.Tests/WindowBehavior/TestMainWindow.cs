using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Printing;
using PicView.Core.Config;
using PicView.Core.Update;
using PicView.Core.ViewModels;

namespace PicView.Tests.WindowBehavior;

/// <summary>
/// A main window for headless tests, whose platform only supplies <paramref name="adapter"/>, or everything
/// <paramref name="provider"/> creates when one is given.
/// </summary>
internal sealed class TestMainWindow(IWindowModeAdapter adapter, IWindowProvider? provider = null) : MainWindow
{
    protected override IWindowProvider CreateWindowProvider() => provider ?? new TestWindowProvider(adapter);

    private sealed class TestWindowProvider(IWindowModeAdapter adapter) : IWindowProvider
    {
        public MainWindow CreateMainWindow(TabViewModel? firstTab = null) => throw new NotSupportedException();
        public IWindowModeAdapter CreateWindowModeAdapter() => adapter;
        public Window CreateAboutWindow() => throw new NotSupportedException();
        public Window CreateBatchResizeWindow(BatchResizeWindowConfig config) => throw new NotSupportedException();
        public Window CreateFileAssociationsWindow() => throw new NotSupportedException();
        public Window CreateConvertWindow() => throw new NotSupportedException();
        public Window CreateEffectsWindow(EffectsWindowConfig config) => throw new NotSupportedException();
        public Window CreateImageInfoWindow(MainWindowViewModel vm) => throw new NotSupportedException();
        public Window CreateKeybindingsWindow(KeybindingWindowConfig config) => throw new NotSupportedException();
        public Window CreateSettingsWindow(SettingsWindowConfig config) => throw new NotSupportedException();
        public Window CreateSingleImageResizeWindow(MainWindowViewModel vm) => throw new NotSupportedException();
        public Window CreatePrintPreviewWindow(PrintWindowConfig config) => throw new NotSupportedException();

        public ISystemPrinters CreateSystemPrinters() => throw new NotSupportedException();

        public Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath) => throw new NotSupportedException();
    }
}
