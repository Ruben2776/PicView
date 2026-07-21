using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.Win32.WindowImpl;
using PicView.Avalonia.WindowBehavior;
using WindowInitializer = PicView.Avalonia.Services.WindowInitializer;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Win32.Views;

public partial class WinMainWindow : MainWindow, IPlatformWindowService
{
    public WinMainWindow()
    {
        if (Application.Current!.DataContext is not CoreViewModel core)
        {
            return;
        }

        Debug.Assert(core.GlobalSettings != null);
        var mainWindowViewModel = new MainWindowViewModel(core.Translation, this, core.GlobalSettings, core.GallerySettings);
        DataContext = mainWindowViewModel;

        InitializeComponent();
        CaptionButtonCornerHandler.Attach(this, () => IsCloseButtonEffectivelyVisible(mainWindowViewModel));
        
        SharedBottomBar = BottomBar;
        SharedTitleBar = Titlebar;
        SharedMainView = MainView;
        UIHelper.Initialize(this);
        LoadedInitialization();
    }

    private bool IsCloseButtonEffectivelyVisible(MainWindowViewModel vm) =>
        (Titlebar.IsEffectivelyVisible && vm.TopTitlebarViewModel.IsBtnPanelVisible.CurrentValue) ||
        MainView.IsAlternativeCloseButtonEffectivelyVisible;

    private void LoadedInitialization()
    {
        Loaded += delegate
        {
            MainWindowInitializer ??= new WindowInitializer(new Win32WindowProvider());
            if (DataContext is not MainWindowViewModel windowViewModel)
            {
                return;
            }

            Debug.Assert(FrameProvider != null, nameof(FrameProvider) + " != null");
            Observable.EveryValueChanged(this, x => x.WindowState, FrameProvider)
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
            }, DebugHelper.LogError(nameof(WinMainWindow), nameof(WindowState)));

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
        WinMainWindow? targetWindow = null;

        foreach (var window in desktop.Windows)
        {
            if (window == this || window is not WinMainWindow macWindow)
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
            MainWindowViewModel? newVm;
            Dispatcher.UIThread.Invoke(() =>
            {
                // Create a new window with the detached tab
                var newWindow = new WinMainWindow
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
                if (newVm is null)
                {
                    return;
                }
                core.MainWindows.MainWindows.Add(newVm);
                core.MainWindows.ActiveWindow.Value = newVm;
                WindowFunctions.DetachedWindowStartup(core, desktop, newWindow);

                // Fix null DataContext
                if (tab.CurrentView.CurrentValue is Control control)
                {
                    control.DataContext = tab;
                }

                desktop.MainWindow = newWindow;
                
                TabNavigationInitializer.InitializeDetachedWindow(this, parentVm, newVm, tab);
            }, DispatcherPriority.Send);
        });
    }
    
    #region Window interface implementations
    
    public int CombinedTitleButtonsWidth
    {
        get => (int)(Settings.WindowProperties.Maximized && !Settings.WindowProperties.Fullscreen
            ? OffScreenMargin.Left + OffScreenMargin.Right + field : field);
        set;
    } = 185;
    
    public void ShowAboutWindow() =>
        MainWindowInitializer?.ShowAboutWindow();

    public async Task ShowImageInfoWindow() =>
        await MainWindowInitializer?.ShowImageInfoWindow(DataContext as MainWindowViewModel);

    public async Task ShowKeybindingsWindow() =>
        await MainWindowInitializer?.ShowKeybindingsWindow();

    public async ValueTask ShowSettingsWindow() =>
        await MainWindowInitializer.ShowSettingsWindow();

    public void ShowSingleImageResizeWindow() =>
        MainWindowInitializer?.ShowSingleImageResizeWindow();

    public async ValueTask ShowBatchResizeWindow() =>
        await MainWindowInitializer.ShowBatchResizeWindow();

    public void ShowFileAssociationsWindow() =>
        MainWindowInitializer?.ShowFileAssociationsWindow();

    public void ShowEffectsWindow() =>
        _ = MainWindowInitializer?.ShowEffectsWindow();

    public void ShowConvertWindow() =>
        MainWindowInitializer?.ShowConvertWindow();
    
    public async Task ShowPrintWindow(string path)
    {
        var vm = Dispatcher.UIThread.Invoke(() => DataContext as MainWindowViewModel);
        Debug.Assert(MainWindowInitializer != null, nameof(MainWindowInitializer) + " != null");
        await MainWindowInitializer.ShowPrintWindow(path, vm);
    }

    /// <inheritdoc />
    public async Task Maximize(bool saveSetting = true) =>
        await Win32Window.Maximize(this, (DataContext as MainWindowViewModel)!, saveSetting);
    
    /// <inheritdoc />
    public async Task MaximizeRestore(bool saveSetting = true) =>
        await Win32Window.ToggleMaximize(this, (DataContext as MainWindowViewModel)!, saveSetting);

    /// <inheritdoc />
    public async Task Fullscreen(bool saveSetting = true) =>
        await Win32Window.Fullscreen(this, (DataContext as MainWindowViewModel)!, saveSetting);
    
    /// <inheritdoc />
    public async Task ToggleFullscreen(bool saveSetting = true) =>
        await Win32Window.ToggleFullscreen(this, (DataContext as MainWindowViewModel)!, saveSetting);
    
    /// <inheritdoc />
    public async Task Restore() =>
        await Win32Window.Restore(this, (DataContext as MainWindowViewModel)!);
    
    public void Minimize() =>
        Win32Window.Minimize(this);
    
    #endregion
}