using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.Crop;
using PicView.Avalonia.DragAndDrop;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Services;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.Main;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.DebugTools;
using PicView.Core.FileHistory;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;
using R3.Avalonia;

namespace PicView.Avalonia.CustomControls;

public class MainWindow : Window, IMainWindow
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

    protected MainWindow()
    {
        UIHelper = new UIControlHelper();
        FrameProvider = new AvaloniaRenderingFrameProvider(GetTopLevel(this));
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Resized += WindowSizeChanged;
        
        // Set initial layout size and visibility, so that the UI is responsive at first startup also
        SetLayoutSizeAndVisibility(Bounds.Width);
        
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
    }

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
        WindowResizing.SetSize(this, WindowResizeReason.DpiChange);
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
            await WindowFunctions.WindowClosingBehavior(this);
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

    
    public bool IsDialogOpen { get; set; }
    
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
                    UIHelper.GetMainView.MainPanel.Children.Add(new QuickEditingDialog());
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
                    UIHelper.GetMainView.MainPanel.Children.Add(new QuickSettingsDialog());
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

        if (e.Reason is WindowResizeReason.User && !IsChangingWindowState)
        {
            if (SharedTitleBar.IsPointerOver && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Traffic light button clicked, don't change window state
                return;
            }

            if (sender is MainWindow { WindowState: WindowState.FullScreen })
            {
                // Don't reset when leaving fullscreen
                return;
            }
            // User manually resized (not maximize or restore), reset to manual window
            Dispatcher.CurrentDispatcher.Post(WindowFunctions.SetManualWindows);
        }
        WindowResizing.SetSize(this, e.Reason);
    }
    
    // Window is being resized
    private void HandleWindowResize(AvaloniaPropertyChangedEventArgs<Size> size)
    {
        if (IsChangingWindowState || WindowState != WindowState.Normal || size.Sender is not MainWindow)
        {
            return;
        }
        
        WindowResizing.HandleWindowResize(this, size);
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