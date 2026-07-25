using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Linux.WindowImpl;
using WindowInitializer = PicView.Avalonia.Services.WindowInitializer;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.ViewModels;
using R3;
using R3.Avalonia;

namespace PicView.Avalonia.Linux.Views;

public partial class LinuxMainWindow : MainWindow, IPlatformWindowService
{
    private readonly AvaloniaRenderingFrameProvider? _frameProvider;
    private static WindowInitializer? _windowInitializer;

    public LinuxMainWindow()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        var mainWindowViewModel = new MainWindowViewModel(core.Translation, this, core.GlobalSettings, core.GallerySettings);
        DataContext = mainWindowViewModel;
        
        // initialize RenderingFrameProvider
        _frameProvider = new AvaloniaRenderingFrameProvider(GetTopLevel(this)!);

        InitializeComponent();
        
        SharedBottomBar = BottomBar;
        SharedTitleBar = Titlebar;
        SharedMainView = MainView;
        UIHelper.Initialize(this);
        LoadedInitialization();
    }

    private void LoadedInitialization()
    {
        Loaded += delegate
        {
            _windowInitializer ??= new WindowInitializer(new LinuxWindowProvider());
            if (Application.Current.DataContext is not CoreViewModel || DataContext is not MainWindowViewModel windowViewModel)
            {
                return;
            }

            Observable.EveryValueChanged(this, x => x.WindowState, _frameProvider)
                .SubscribeAwait(async (state, _) =>
            {
                switch (state)
                {
                    case WindowState.FullScreen:
                        if (!Settings.WindowProperties.Fullscreen)
                        {
                             await Fullscreen();
                        }
                        break;
                    case WindowState.Maximized:
                        if (!Settings.WindowProperties.Maximized)
                        {
                            await Maximize();
                        }
                        break;
                    case WindowState.Normal:
                        if (Settings.WindowProperties.Fullscreen || Settings.WindowProperties.Maximized)
                        {
                            await Restore();
                        }
                        break;
                }
            }, DebugHelper.LogError(nameof(LinuxMainWindow), nameof(WindowState)));

            // Close tabMenu when clicking outside of it
            PointerPressed += (_, _) =>
            {
                if (windowViewModel.IsEditableTitlebarOpen.Value && !Titlebar.IsPointerOver)
                {
                    Titlebar.EditableTitlebar.CloseTitlebar();
                }

                if (!UIHelper.GetDropDownMenu?.IsPointerOver ?? false)
                {
                    windowViewModel.TopTitlebarViewModel.CloseDropDownMenu();
                }
            };
            UIHelper.GetMainTabControl?.TabDetached += MainTabControlOnTabDetached;
        };
    }

    private void MainTabControlOnTabDetached(object? sender, TabDetachEventArgs e)
    {
        if (e.DetachedItem is not TabViewModel tab)
        {
            return;
        }

        if (DataContext is not MainWindowViewModel parentVm)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        // 1. Try to find a target window under the mouse
        LinuxMainWindow? targetWindow = null;

        foreach (var window in desktop.Windows)
        {
            if (window == this || window is not LinuxMainWindow macWindow)
            {
                continue;
            }

            var clientPoint = macWindow.PointToClient(e.ScreenPosition);
            if (!new Rect(0, 0, macWindow.ClientSize.Width, macWindow.ClientSize.Height).Contains(clientPoint))
            {
                continue;
            }

            targetWindow = macWindow;
            break;
        }

        // 2. If dropped on an existing window, attach the tab there
        if (targetWindow != null)
        {
            if (targetWindow.DataContext is not MainWindowViewModel targetVm)
            {
                return;
            }

            // Need to properly remove it from the previous location
            parentVm.WindowTabs.RemoveTab(tab);

            // Add to new window (if not already added by drag preview)
            if (!targetVm.WindowTabs.Tabs.Value.Contains(tab))
            {
                targetVm.WindowTabs.Tabs.Value.Add(tab);
            }

            targetVm.WindowTabs.SelectTab(tab);

            // Update context
            tab.ParentWindowContext = targetVm;

            // Refresh bindings
            if (tab.CurrentView.CurrentValue is Control control)
            {
                control.DataContext = tab;
            }

            return;
        }

        // 3. Fallback: Create a new window (Detaching behavior)
        Task.Run(() =>
        {
            MainWindowViewModel? newVm = null;
            Dispatcher.UIThread.Invoke(() =>
            {
                // Create a new window with the detached tab
                var newWindow = new LinuxMainWindow
                {
                    Position = new PixelPoint(e.ScreenPosition.X - 100, e.ScreenPosition.Y - 50),
                    Width = Width,
                    Height = Height
                };
                if (Application.Current.DataContext is not CoreViewModel core)
                {
                    return;
                }
                newVm = newWindow.DataContext as MainWindowViewModel;
                core.MainWindows.MainWindows.Add(newVm);
                core.MainWindows.ActiveWindow.Value = newVm;
                WindowFunctions.DetachedWindowStartup(core, desktop, newWindow);

                // Fix null DataContext
                if (tab.CurrentView.CurrentValue is Control control)
                {
                    control.DataContext = tab;
                }

                desktop.MainWindow = newWindow;
            }, DispatcherPriority.Send);

            TabNavigationInitializer.InitializeDetachedWindow(this, parentVm, newVm, tab);
        });
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        await WindowFunctions.WindowClosingBehavior(this);
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _frameProvider?.Dispose();
        Disposables.Dispose();
        base.OnClosed(e);
    }
    
    #region Window interface implementations
    
    public int CombinedTitleButtonsWidth
    {
        get => (int)(Settings.WindowProperties.Maximized && !Settings.WindowProperties.Fullscreen
            ? OffScreenMargin.Left + OffScreenMargin.Right + field : field);
        set;
    } = 185;
    
    public void ShowAboutWindow() =>
        _windowInitializer?.ShowAboutWindow();

    public async Task ShowImageInfoWindow() =>
        await _windowInitializer?.ShowImageInfoWindow(DataContext as MainWindowViewModel);

    public async Task ShowKeybindingsWindow() =>
        await _windowInitializer?.ShowKeybindingsWindow();

    public async ValueTask ShowSettingsWindow() =>
        await _windowInitializer.ShowSettingsWindow();

    public void ShowSingleImageResizeWindow() =>
        _windowInitializer?.ShowSingleImageResizeWindow();

    public async ValueTask ShowBatchResizeWindow() =>
        await _windowInitializer.ShowBatchResizeWindow();

    public void ShowFileAssociationsWindow() =>
        _windowInitializer?.ShowFileAssociationsWindow();

    public void ShowEffectsWindow() =>
        _ = _windowInitializer?.ShowEffectsWindow();

    public void ShowConvertWindow() =>
        _windowInitializer?.ShowConvertWindow();

    public async Task ShowPrintWindow(string path)
    {
        var vm = Dispatcher.UIThread.Invoke(() => DataContext as MainWindowViewModel);
        await _windowInitializer.ShowPrintWindow(path, vm);
    }

    /// <inheritdoc />
    public async Task Maximize(bool saveSetting = true) =>
        await LinuxWindow.Maximize(this, null, saveSetting);
    
    /// <inheritdoc />
    public async Task MaximizeRestore(bool saveSetting = true) =>
        await LinuxWindow.ToggleMaximize(this, null, saveSetting);

    /// <inheritdoc />
    public async Task Fullscreen(bool saveSetting = true) =>
        await LinuxWindow.Fullscreen(this, null, saveSetting);
    
    /// <inheritdoc />
    public async Task ToggleFullscreen(bool saveSetting = true) =>
        await LinuxWindow.ToggleFullscreen(this, null, saveSetting);
    
    /// <inheritdoc />
    public async Task Restore() =>
        await LinuxWindow.Restore(this, null);
    
    public void Minimize() =>
        LinuxWindow.Minimize(this);

    #endregion
}