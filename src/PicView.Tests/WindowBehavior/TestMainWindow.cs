using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Core.Config;
using PicView.Core.Update;
using PicView.Core.ViewModels;

namespace PicView.Tests.WindowBehavior;

/// <summary>
/// A main window for headless tests, whose platform only supplies <paramref name="adapter"/>.
/// </summary>
internal sealed class TestMainWindow(IWindowModeAdapter adapter) : MainWindow
{
    protected override IWindowProvider CreateWindowProvider() => new TestWindowProvider(adapter);

    private sealed class TestWindowProvider(IWindowModeAdapter adapter) : IWindowProvider
    {
        public MainWindow CreateMainWindow() => throw new NotSupportedException();
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

        public ValueTask InitializePrintAsync(MainWindowViewModel vm, string path, Window printPreviewWindow) =>
            throw new NotSupportedException();

        public Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath) => throw new NotSupportedException();
    }
}
