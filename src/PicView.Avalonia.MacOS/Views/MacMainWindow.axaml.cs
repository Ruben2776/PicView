using Avalonia;
using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.MacOS.WindowImpl;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.MacOS.Views;

public partial class MacMainWindow : MainWindow
{
    public MacMainWindow() : this(null)
    {
    }

    public MacMainWindow(CoreViewModel? core)
    {
        core ??= Application.Current?.DataContext as CoreViewModel;
        if (core is null)
        {
            return;
        }
        var mainWindowViewModel = new MainWindowViewModel(core, core.Translation, this, core.GlobalSettings, core.GallerySettings);
        DataContext = mainWindowViewModel;

        InitializeComponent();
        
        SharedBottomBar = BottomBar;
        SharedTitleBar = Titlebar;
        SharedMainView = MainView;
        UIHelper.Initialize(this);

        Loaded += delegate
        {
            if (DataContext is not MainWindowViewModel vm)
            {
                return;
            }
            
            // Hide macOS traffic lights buttons when interface is hidden
            Observable.EveryValueChanged(vm, x => x.IsTopToolbarShown.CurrentValue, FrameProvider).Subscribe(shown =>
            {
                if (Settings.WindowProperties.Fullscreen)
                {
                    WindowDecorations = WindowDecorations.Full;
                }
                else
                {
                    WindowDecorations = shown ? WindowDecorations.Full : WindowDecorations.None;
                }
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(MacMainWindow), nameof(vm.IsTopToolbarShown), result.Exception);
                }
#endif
            })
            .AddTo(Disposables);
        };
    }

    protected override IWindowProvider CreateWindowProvider() => new MacWindowProvider();

    protected override bool SkipsInitialWindowState => true;

    protected override async Task OnWindowStateChangedAsync(WindowState state)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        switch (state)
        {
            case WindowState.FullScreen:
                if (!Settings.WindowProperties.Fullscreen)
                {
                    await MacOSWindow.Fullscreen(this, vm);
                }
                break;
            case WindowState.Maximized:
                if (!Settings.WindowProperties.Maximized && !Settings.WindowProperties.Fullscreen)
                {
                    await MacOSWindow.Maximize(this, vm);
                }
                break;
            case WindowState.Normal:
                if (Settings.WindowProperties.Maximized || Settings.WindowProperties.Fullscreen)
                {
                    await MacOSWindow.Restore(this, vm);
                }
                break;
        }
    }

    public override int CombinedTitleButtonsWidth { get; set; } = 165;

    /// <inheritdoc />
    public override async Task Maximize(bool saveSetting = true) =>
        await MacOSWindow.Maximize(this, DataContext as MainWindowViewModel, saveSetting);
    
    /// <inheritdoc />
    public override async Task MaximizeRestore(bool saveSetting = true) =>
        await MacOSWindow.ToggleMaximize(this, DataContext as MainWindowViewModel, saveSetting);

    /// <inheritdoc />
    public override async Task Fullscreen(bool saveSetting = true) =>
        await MacOSWindow.Fullscreen(this, DataContext as MainWindowViewModel, saveSetting);
    
    /// <inheritdoc />
    public override async Task ToggleFullscreen(bool saveSetting = true) =>
        await MacOSWindow.ToggleFullscreen(this, DataContext as MainWindowViewModel, saveSetting);
    
    /// <inheritdoc />
    public override async Task Restore() =>
        await MacOSWindow.Restore(this, DataContext as MainWindowViewModel);
    
    public override void Minimize() =>
        MacOSWindow.Minimize(this);
}
