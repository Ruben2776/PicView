using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.Animations;
using PicView.Avalonia.Clipboard;
using PicView.Avalonia.Crop;
using PicView.Avalonia.DragAndDrop;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Services;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.Gallery;
using PicView.Avalonia.Views.Main;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.DebugTools;
using PicView.Core.FileHistory;
using PicView.Core.IPlatform;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;
using R3.Avalonia;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// The main window shared by every platform. It owns its Window Mode, tab detaching and opening the secondary
/// windows; a platform subclass supplies its <see cref="IWindowProvider"/>.
/// </summary>
public abstract class MainWindow : Window, IMainWindow, IPlatformWindowService
{
    [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")] 
    public WindowInitializer? MainWindowInitializer;
    
    public CompositeDisposable Disposables { get; set; } = new();
    /// Flag to prevent window state changes while resizing
    public bool IsChangingWindowState { get; set; }
    public BottomBar? SharedBottomBar { get; set; }
    public MainTitleBar? SharedTitleBar { get; set; }
    public MainView? SharedMainView { get; set; }
    public AvaloniaRenderingFrameProvider FrameProvider { get; set; }
    public UIControlHelper UIHelper { get; }
    public ClipboardCopyService? ClipboardCopyService { get; private set; }
    public ClipboardPasteService? ClipboardPasteService { get; private set; }

    /// <summary>
    /// Changes this window's Window Mode.
    /// </summary>
    public WindowModeSwitcher WindowModeSwitcher =>
        field ??= new WindowModeSwitcher(this, CreateWindowProvider().CreateWindowModeAdapter());

    /// <summary>
    /// Fits this window to what its active tab shows.
    /// </summary>
    public WindowFitter WindowFitter => field ??= new WindowFitter(this);

    /// <summary>
    /// This window's Window Mode. Read on the UI thread.
    /// </summary>
    public WindowModeState WindowModeState =>
        (DataContext as MainWindowViewModel)?.WindowModeState.CurrentValue ?? default;

    protected MainWindow()
    {
        UIHelper = new UIControlHelper();
        FrameProvider = new AvaloniaRenderingFrameProvider(GetTopLevel(this));
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Resized += WindowSizeChanged;
        
        // Keep window position when resizing
        Debug.Assert(FrameProvider != null, nameof(FrameProvider) + " != null");
        ClientSizeProperty.Changed.ToObservable()
            .Skip(1)
            .SubscribeOn(FrameProvider)
            .Subscribe(HandleWindowResize, DebugHelper.LogError(nameof(MainWindow), nameof(HandleWindowResize)))
            .AddTo(Disposables);
        
        Activated += OnActivated;
        
        ScalingChanged += OnScalingChanged;
        
        PointerExited += (_, _) => { DragAndDropManager.RemoveDragDropView(this); };

        Deactivated += OnDeactivated;

        MainWindowInitializer ??= new WindowInitializer(CreateWindowProvider());
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        // Changes the user makes with the operating system's own window controls
        var windowStates = Observable.EveryValueChanged(this, x => x.WindowState, FrameProvider);
        if (WindowModeSwitcher.SkipsInitialNativeState)
        {
            windowStates = windowStates.Skip(1);
        }
        windowStates
            .SubscribeAwait(async (state, _) => await WindowModeSwitcher.OnNativeStateAsync(state).ConfigureAwait(true),
                DebugHelper.LogError(nameof(MainWindow), nameof(WindowState)))
            .AddTo(Disposables);

        // Close the editable title bar and the dropdown menu when clicking outside of them
        PointerPressed += (_, _) =>
        {
            if (vm.IsEditableTitlebarOpen.Value && SharedTitleBar is { IsPointerOver: false })
            {
                SharedTitleBar.SharedEditableTitlebar?.CloseTitlebar();
            }

            if (!UIHelper.GetDropDownMenu?.IsPointerOver ?? false)
            {
                vm.TopTitlebarViewModel.CloseDropDownMenu();
            }
        };
        UIHelper.GetMainTabControl?.TabDetached += OnTabDetached;
    }

    #region Platform adapter

    /// <summary>
    /// Creates the provider for this platform's windows.
    /// </summary>
    protected abstract IWindowProvider CreateWindowProvider();

    /// <inheritdoc />
    public virtual int CombinedTitleButtonsWidth
    {
        get => (int)((DataContext as MainWindowViewModel)?.WindowModeState.CurrentValue.Mode is WindowMode.Maximized
            ? OffScreenMargin.Left + OffScreenMargin.Right + field : field);
        set;
    } = 185;

    /// <inheritdoc />
    public void Minimize() => WindowState = WindowState.Minimized;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.WindowModeSwitcher = WindowModeSwitcher;
        }
    }

    #endregion

    #region Secondary windows

    public void ShowAboutWindow() =>
        MainWindowInitializer?.ShowAboutWindow();

    public void ShowImageInfoWindow() =>
        MainWindowInitializer?.ShowImageInfoWindow(DataContext as MainWindowViewModel);

    public void ShowKeybindingsWindow() =>
        MainWindowInitializer?.ShowKeybindingsWindow();

    public async ValueTask ShowSettingsWindow()
    {
        if (MainWindowInitializer is null)
        {
            return;
        }
        await MainWindowInitializer.ShowSettingsWindow().ConfigureAwait(true);
    }

    public void ShowSingleImageResizeWindow() =>
        MainWindowInitializer?.ShowSingleImageResizeWindow();

    public async ValueTask ShowBatchResizeWindow()
    {
        if (MainWindowInitializer is null)
        {
            return;
        }
        await MainWindowInitializer.ShowBatchResizeWindow().ConfigureAwait(true);
    }

    public void ShowFileAssociationsWindow() =>
        MainWindowInitializer?.ShowFileAssociationsWindow();

    public void ShowEffectsWindow() =>
        _ = MainWindowInitializer?.ShowEffectsWindow();

    public void ShowConvertWindow() =>
        MainWindowInitializer?.ShowConvertWindow();

    public async Task ShowPrintWindow(string path)
    {
        if (MainWindowInitializer is null)
        {
            return;
        }
        var vm = await Dispatcher.UIThread.InvokeAsync(() => DataContext as MainWindowViewModel);
        await MainWindowInitializer.ShowPrintWindow(path, vm).ConfigureAwait(true);
    }

    #endregion

    #region Tab detaching

    private void OnTabDetached(object? sender, TabDetachEventArgs e)
    {
        if (e.DetachedItem is not TabViewModel tab || DataContext is not MainWindowViewModel parentVm)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        // Dropped on another main window: move the tab there
        var targetWindow = desktop.Windows
            .OfType<MainWindow>()
            .FirstOrDefault(window => window != this && window.ContainsScreenPoint(e.ScreenPosition));
        if (targetWindow?.DataContext is MainWindowViewModel targetVm)
        {
            MoveTab(tab, parentVm, targetVm);
            return;
        }

        if (targetWindow is not null)
        {
            return;
        }

        // Dropped elsewhere: open the tab in a new window
        _ = Task.Run(() =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                if (MainWindowInitializer is null || Application.Current.DataContext is not CoreViewModel core)
                {
                    return;
                }

                var newWindow = MainWindowInitializer.CreateMainWindow();
                if (newWindow.DataContext is not MainWindowViewModel newVm)
                {
                    return;
                }
                newWindow.Position = new PixelPoint(e.ScreenPosition.X - 100, e.ScreenPosition.Y - 50);
                newWindow.Width = Width;
                newWindow.Height = Height;
                WindowFunctions.DetachedWindowStartup(core, newWindow);

                // Fix null DataContext
                if (tab.CurrentView.CurrentValue is Control control)
                {
                    control.DataContext = tab;
                }

                desktop.MainWindow = newWindow;

                TabNavigationInitializer.InitializeDetachedWindow(this, parentVm, newVm, tab, core);
            }, DispatcherPriority.Send);
        });
    }

    private bool ContainsScreenPoint(PixelPoint screenPosition)
    {
        var clientPoint = this.PointToClient(screenPosition);
        return new Rect(0, 0, ClientSize.Width, ClientSize.Height).Contains(clientPoint);
    }

    /// <summary>
    /// Moves a tab from one main window to another and selects it there.
    /// </summary>
    internal static void MoveTab(TabViewModel tab, MainWindowViewModel source, MainWindowViewModel target)
    {
        source.WindowTabs.RemoveTab(tab);

        // The drag preview may already have added it
        if (!target.WindowTabs.Tabs.Value.Contains(tab))
        {
            target.WindowTabs.Tabs.Value.Add(tab);
        }

        target.WindowTabs.SelectTab(tab);
        tab.ParentWindowContext = target;

        // Refresh bindings
        if (tab.CurrentView.CurrentValue is Control control)
        {
            control.DataContext = tab;
        }
    }

    #endregion

    public void SetClipboardServices(IPlatformSpecificService? platformService = null, CoreViewModel? core = null)
    {
        core ??= Application.Current?.DataContext as CoreViewModel;
        platformService ??= core?.PlatformService;

        ClipboardCopyService = new ClipboardCopyService(
            Clipboard,
            StorageProvider,
            platformService,
            () => _ = AnimationsHelper.CopyAnimation(this));

        ClipboardPasteService = new ClipboardPasteService(
            Clipboard,
            this,
            core);
    }

    public void SetClipboardCopyService(IPlatformSpecificService? platformService = null) =>
        SetClipboardServices(platformService);

    private void OnDeactivated(object? sender, EventArgs e)
    {
        Settings.StartUp.LastFile = FileHistoryManager.CurrentEntry;
        
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        core.SharedCache.ForceDisposalQueue();
    }

    private void OnScalingChanged(object? sender, EventArgs e)
    {
        ScreenHelper.UpdateScreenSize(this);
        WindowFitter.Refit();
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (Application.Current!.DataContext is not CoreViewModel core || Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        core.MainWindows.ActiveWindow.Value = DataContext as MainWindowViewModel;
        desktop.MainWindow = this;
    }
    
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            await WindowFunctions.WindowClosingBehavior(this).ConfigureAwait(true);
            base.OnClosing(e);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(MainWindow), nameof(OnClosing), ex);
            Environment.Exit(1);
        }
    }
    
    protected override void OnClosed(EventArgs e)
    {
        FrameProvider?.Dispose();
        Resized -= WindowSizeChanged;
        Disposables?.Dispose();
        Loaded -= OnLoaded;
        Activated -= OnActivated;
        Deactivated -= OnDeactivated;
        SharedBottomBar?.Dispose();
        base.OnClosed(e);
    }

    #region Dialog

    /// <summary>
    /// Handles close action based on current application state
    /// </summary>
    public async Task HandleShouldClosing(MainWindowViewModel vm)
    {
        // Handle cropping mode
        if (CropManager.IsCropping(this))
        {
            CropManager.CloseCropControl(vm);
            return;
        }

        // Handle slideshow
        if (Slideshow.IsRunning)
        {
            Slideshow.StopSlideshow(vm);
            return;
        }
        
        // Handle window close
        await Dispatcher.UIThread.InvokeAsync(CloseWithOptionalDialog);
    }

    public void CloseWithOptionalDialog()
    {
        if (Settings.UIProperties.ShowConfirmationOnEsc)
        {
            UIHelper.GetMainView?.MainPanel.Children.Add(new CloseDialog());
        }
        else
        {
            Close();
        }
    }

    public bool ContainsFileSearchDialog() =>
        UIHelper.GetMainView.MainPanel.Children.OfType<FileSearchDialog>().Any();
    
    public bool ContainsNavigationDialog() =>
        UIHelper.GetMainView.MainPanel.Children.OfType<NavigationDialog>().Any();
    
    public bool ContainsRenameDialog() =>
        UIHelper.GetMainView.MainPanel.Children.OfType<RenameDialog>().Any();
    
    public bool ContainsQuickEditingDialog() =>
        UIHelper.GetMainView.MainPanel.Children.OfType<QuickEditingDialog>().Any();
    public bool ContainsQuickSettingsDialog() =>
        UIHelper.GetMainView.MainPanel.Children.OfType<QuickSettingsDialog>().Any();
    public bool ContainsFavoriteDialog() =>
        UIHelper.GetMainView.MainPanel.Children.OfType<FavoriteDialog>().Any();

    public void AddFileSearchDialog()
    {
        if (ContainsFileSearchDialog() || UIHelper.GetMainView.DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        vm.TopTitlebarViewModel.CloseDropDownMenu();
        UIHelper.GetMainView.MainPanel.Children.Add(new FileSearchDialog());
    }

    public void AddNavigationDialog()
    {
        if (ContainsNavigationDialog() || UIHelper.GetMainView.DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        vm.TopTitlebarViewModel.CloseDropDownMenu();
        UIHelper.GetMainView.MainPanel.Children.Add(new NavigationDialog());
    }
    
    public void AddRenameDialog()
    {
        if (ContainsRenameDialog() || UIHelper.GetMainView.DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        vm.TopTitlebarViewModel.CloseDropDownMenu();
        UIHelper.GetMainView.MainPanel.Children.Add(new RenameDialog());
    }
    
    public void AddQuickEditingDialog()
    {
        if (ContainsQuickEditingDialog() || UIHelper.GetMainView.DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        vm.TopTitlebarViewModel.CloseDropDownMenu();
        UIHelper.GetMainView.MainPanel.Children.Add(new QuickEditingDialog());
    }
    
    public void AddQuickSettingsDialog()
    {
        if (ContainsQuickSettingsDialog() || UIHelper.GetMainView.DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        vm.TopTitlebarViewModel.CloseDropDownMenu();
        UIHelper.GetMainView.MainPanel.Children.Add(new QuickSettingsDialog());
    }
    
    public void AddGalleryItemSizeSlider()
    {
        if (UIHelper.GetMainView.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.TopTitlebarViewModel.CloseDropDownMenu();
        UIHelper.GetMainView.MainPanel.Children.Add(new GalleryItemSizeSlider());
    }
    
    public void AddFavoriteDialog(string? initialPath = null)
    {
        if (ContainsFavoriteDialog() || UIHelper.GetMainView.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.TopTitlebarViewModel.CloseDropDownMenu();
        UIHelper.GetMainView.MainPanel.Children.Add(new FavoriteDialog(initialPath));
    }

    #endregion

    #region Sizing

    public void SetLayoutSizeAndVisibility(double width)
    {
        SharedBottomBar.ResponsiveNavigationBtnSize(width);
        SharedTitleBar.SharedDropDownMenuButton.IsVisible = width > SizeDefaults.MainTitleDropDownBtnBp;
        SharedTitleBar.SharedSearchButton.IsVisible = width > SizeDefaults.MainTitleDropDownBtnBp;
        
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (vm.TopTitlebarViewModel.DropDownMenu.IsFileMenuVisible.CurrentValue)
        {
            switch (width)
            {
                case < SizeDefaults.FullBtnBp and > SizeDefaults.SearchResetAndRotateBtnBp:
                    UIHelper.GetFileMenu.Margin = new Thickness(0, 0, 115, 0);
                    break;
                case <= SizeDefaults.BottomMenusBp:
                    vm.TopTitlebarViewModel.DropDownMenu.IsFileMenuVisible.Value = false;
                    AddQuickEditingDialog();
                    break;
                default:
                    UIHelper.GetFileMenu.Margin = new Thickness(0, 0, 147, 0);
                    break;
            }
        }
        if (vm.TopTitlebarViewModel.DropDownMenu.IsSettingsMenuVisible.CurrentValue)
        {
            switch (width)
            {
                case < SizeDefaults.FullBtnBp and > SizeDefaults.SearchResetAndRotateBtnBp:
                    UIHelper.GetSettingsMenu.Margin = new Thickness(0, 0, -102, 0);
                    break;
                case <= SizeDefaults.BottomMenusBp:
                    vm.TopTitlebarViewModel.DropDownMenu.IsSettingsMenuVisible.Value = false;
                    AddQuickSettingsDialog();
                    break;
                default:
                    UIHelper.GetSettingsMenu.Margin = new Thickness(0, 0, -40, 0);
                    break;
            }
        }
    }
    
    // Window has been resized
    private void WindowSizeChanged(object? sender, WindowResizedEventArgs e)
    {
        if (SharedBottomBar is null || SharedTitleBar is null)
        {
            return;
        }

        if (e.Reason is not WindowResizeReason.User || IsChangingWindowState)
        {
            return;
        }

        if (SharedTitleBar.IsPointerOver && OperatingSystem.IsMacOS())
        {
            // Traffic light button clicked, don't change window state
            return;
        }

        if (sender is MainWindow { WindowState: WindowState.FullScreen })
        {
            // Don't reset when leaving fullscreen
            return;
        }
        // Resizing by hand turns AutoFit off for this window only
        _ = WindowModeSwitcher.ResizedByHand();
        WindowFitter.Refit();
    }
    
    // Window is being resized
    private void HandleWindowResize(AvaloniaPropertyChangedEventArgs<Size> size)
    {
        if (IsChangingWindowState || WindowState != WindowState.Normal || size.Sender is not MainWindow)
        {
            return;
        }
        
        WindowResizing.HandleWindowResize(this, size);
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        if (vm.WindowTabs.ActiveTab.CurrentValue.Gallery.IsDockedGalleryVisible.CurrentValue
            && vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is ImageViewer imageViewer)
        {
            Dispatcher.UIThread.Post(() =>
            {
                imageViewer.GalleryView.GalleryItemsControl.ScrollToCenterOfCurrentItem();
            }, DispatcherPriority.Render);
        }
        var newWidth = size.NewValue.Value.Width;
        if (newWidth == Bounds.Width || 
            size.OldValue.Value.Width >= SizeDefaults.FullBtnBp && size.NewValue.Value.Width >= SizeDefaults.FullBtnBp)
        {
            return;
        }
        SetLayoutSizeAndVisibility(newWidth);
    }
    
    #endregion
}
