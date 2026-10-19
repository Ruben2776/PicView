using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.UI;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.WindowBehavior;

public static class WindowResizing
{
    #region Window Resize Handling

    private static bool KeepWindowSize(Window window, AvaloniaPropertyChangedEventArgs<Size> size)
    {
        var oldSize = size.OldValue.Value;
        var newSize = size.NewValue.Value;
        
        if (!size.OldValue.HasValue || !size.NewValue.HasValue ||
            !Equals(size.Sender, window) || oldSize.Width is 0 || oldSize.Height is 0 ||
            newSize.Width is 0 || newSize.Height is 0)
        {
            return false;
        }

        var x = (oldSize.Width - newSize.Width) / 2;
        var y = (oldSize.Height - newSize.Height) / 2;

        window.Position = new PixelPoint(window.Position.X + (int)x, window.Position.Y + (int)y);
        
        return true;
    }
    
    public static void FastCenterWindow(Window window)
    {
        var screen = ScreenHelper.ScreenSize;

        // Get the size of the window
        var windowSize = window.ClientSize;

        var x = screen.X;
        var y = screen.Y;

        // Calculate the position to center the window on the screen
        var centeredX = x + (screen.WorkingAreaWidth - windowSize.Width) / 2;
        var centeredY = y + (screen.WorkingAreaHeight - windowSize.Height) / 2;

        // Set the window's new position
        window.Position = new PixelPoint((int)centeredX, (int)centeredY);
    }

    public static void HandleWindowResize(MainWindow mainWindow, AvaloniaPropertyChangedEventArgs<Size> size)
    {
        if (mainWindow.DataContext is not MainWindowViewModel { WindowModeState.CurrentValue.AutoFit: true } mainWindowVm)
        {
            return;
        }

        if (Settings.WindowProperties.KeepCentered)
        {
            FastCenterWindow(mainWindow);
        }
        else
        {
            var isWindowResized = KeepWindowSize(mainWindow, size);
            if (!isWindowResized)
            {
                return;
            }
        }
        
        var nextNavX = size.NewValue.Value.Width > SizeDefaults.SearchResetAndRotateBtnBp ? 50 : 15;

        RepositionCursorIfTriggered(mainWindowVm.IsNavigationButtonLeftClicked,
            clicked => mainWindowVm.IsNavigationButtonLeftClicked = clicked,
            () => mainWindow.UIHelper.GetBottomBar.PreviousButton,
            new Point(nextNavX, 10));

        RepositionCursorIfTriggered(mainWindowVm.IsNavigationButtonRightClicked,
            clicked => mainWindowVm.IsNavigationButtonRightClicked = clicked,
            () => mainWindow.UIHelper.GetBottomBar.NextButton,
            new Point(nextNavX, 10));

        RepositionCursorIfTriggered(mainWindowVm.IsBottomToolbarRightRotationClicked,
            clicked => mainWindowVm.IsBottomToolbarRightRotationClicked = clicked,
            () => mainWindow.UIHelper.GetBottomBar.RotateRightButton,
            new Point(20, 10));

        RepositionCursorIfTriggered(mainWindowVm.IsBottomToolbarLeftRotationClicked,
            clicked => mainWindowVm.IsBottomToolbarLeftRotationClicked = clicked,
            () => mainWindow.UIHelper.GetBottomBar.RotateLeftButton,
            new Point(20, 10));

        RepositionCursorIfTriggered(mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverNavigationButtonNextClicked,
            clicked => mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverNavigationButtonNextClicked = clicked,
            () => UIHelper.GetHoverBar().NextButton,
            new Point(50, 10));

        RepositionCursorIfTriggered(mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverNavigationButtonPreviousClicked,
            clicked => mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverNavigationButtonPreviousClicked = clicked,
            () => UIHelper.GetHoverBar().PreviousButton,
            new Point(50, 10));

        RepositionCursorIfTriggered(mainWindowVm.IsClickArrowLeftClicked,
            clicked => mainWindowVm.IsClickArrowLeftClicked = clicked,
            () => UIHelper.GetClickArrowLeft(mainWindowVm),
            new Point(15, 95));
        
        RepositionCursorIfTriggered(mainWindowVm.IsClickArrowRightClicked,
            clicked => mainWindowVm.IsClickArrowRightClicked = clicked,
            () => UIHelper.GetClickArrowRight(mainWindowVm),
            new Point(65, 95));

        RepositionCursorIfTriggered(mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverRotateRightClicked,
            clicked => mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverRotateRightClicked = clicked,
            () => UIHelper.GetHoverBar().RotateRightButton,
            new Point(11, 7));

        RepositionCursorIfTriggered(mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverRotateLeftClicked,
            clicked => mainWindowVm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverRotateLeftClicked = clicked,
            () => UIHelper.GetHoverBar().RotateLeftButton,
            new Point(11, 7));
    }

    private static void RepositionCursorIfTriggered(
        bool isTriggered,
        Action<bool> setTrigger,
        Func<Control?> controlProvider,
        Point offset)
    {
        if (!isTriggered)   
        {
            return;
        }
        var control = controlProvider();
        if (control is not null && Application.Current.DataContext is CoreViewModel core)
        {
            Dispatcher.CurrentDispatcher.Post(() =>
            {
                var screenPoint = control.PointToScreen(offset);
                core.PlatformService.SetCursorPos(screenPoint.X, screenPoint.Y);
            }, DispatcherPriority.Loaded + 1);
        }

        setTrigger(false);
    }

    #endregion
    
    #region Set Window Size

    public static void SetSize(MainWindow mainWindow, WindowResizeReason reason)
    {
        var vm = mainWindow.DataContext as MainWindowViewModel;
        var size = GetSize(mainWindow, vm);

        if (size is null)
        {
            return;
        }

        SetSize(size.Value, reason, mainWindow, vm);
    }

    public static void SetSize(double width, double height, double secondWidth, double secondHeight, WindowResizeReason reason, MainWindow mainWindow, MainWindowViewModel vm)
    {
        var size = GetSize(width, height, secondWidth, secondHeight, vm.WindowTabs.ActiveTab.CurrentValue.RotationAngle.CurrentValue, mainWindow, vm);

        if (size is null || size.Value.WindowWidth is 0 || size.Value.WindowHeight is 0)
        {
            return;
        }

        SetSize(size.Value, reason, mainWindow, vm);
    }

    public static void SetSize(Fit size, WindowResizeReason reason, MainWindow mainWindow, MainWindowViewModel vm)
    {
        vm.WindowTabs.ActiveTab.CurrentValue.InitialZoom.Value = size.Scale;
        vm.ScrollViewerWidth.Value = size.ScrollViewerWidth;
        vm.ScrollViewerHeight.Value = size.ScrollViewerHeight;
        
        vm.ImageWidth.Value = size.Width;
        vm.ImageHeight.Value = size.Height;

        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplyWindowSize(size, reason, mainWindow, vm);
        }
        else
        {
            Dispatcher.UIThread.Post(() => ApplyWindowSize(size, reason, mainWindow, vm), DispatcherPriority.Send);
        }
    }

    /// <summary>
    /// Stops the window from following its content, before it becomes fullscreen or maximized, or turns AutoFit off.
    /// </summary>
    public static void ReleaseSize(MainWindow mainWindow, MainWindowViewModel vm)
    {
        mainWindow.SizeToContent = SizeToContent.Manual;
        vm.WindowMaxWidth.Value = vm.WindowMaxHeight.Value = double.NaN;
    }

    /// <summary>
    /// Lets the window follow its content again when AutoFit is on for it, also when there is no image to fit.
    /// </summary>
    public static void ResumeAutoFit(Window window)
    {
        if (window.DataContext is MainWindowViewModel { WindowModeState.CurrentValue.AutoFit: true })
        {
            window.SizeToContent = SizeToContent.WidthAndHeight;
        }
    }

    private static void ApplyWindowSize(Fit size, WindowResizeReason reason, MainWindow mainWindow,
        MainWindowViewModel vm)
    {
        var state = vm.WindowModeState.CurrentValue;
        var plan = WindowSizingPlan.For(state.Mode, state.AutoFit, reason, mainWindow.IsVisible, size,
            ScreenHelper.ScreenSize);

        mainWindow.SizeToContent = plan.SizeToContent;
        vm.WindowMaxWidth.Value = plan.MaxWidth;
        vm.WindowMaxHeight.Value = plan.MaxHeight;
        if (plan.Width is { } width)
        {
            mainWindow.Width = width;
        }
        if (plan.Height is { } height)
        {
            mainWindow.Height = height;
        }

        if (!plan.SettleToContentAfterRender)
        {
            return;
        }
        Dispatcher.UIThread.Post(() =>
        {
            mainWindow.SizeToContent = SizeToContent.WidthAndHeight;
            // Fixes weird window size bug where the window width is at a fixed value
            if (!double.IsNaN(mainWindow.Width))
            {
                mainWindow.Width = mainWindow.Height = double.NaN;
            }
        }, DispatcherPriority.Render);
    }

    public static Fit? GetSize(MainWindow mainWindow, MainWindowViewModel vm)
    {
        if (vm?.WindowTabs.ActiveTab?.CurrentValue is not { } tab)
        {
            return null;
        }
        double width, height, secondaryWidth, secondaryHeight;
        if (tab.Model.FileInfo is not null)
        {
            if (tab.Model.PixelHeight is not 0 && tab.Model.PixelWidth is not 0)
            {
                width = tab.Model.PixelWidth;
                height = tab.Model.PixelHeight;
            }
            else if (tab.Model.Image is Bitmap bitmap)
            {
                width = bitmap.PixelSize.Width;
                height = bitmap.PixelSize.Height;
            }
            else if (vm.WindowTabs.SharedCache?.TryGetReady(tab.Model.FileInfo, out var ready) ?? false)
            {
                width = ready.PixelWidth;
                height = ready.PixelHeight;
            }
            else
            {
                return null;
            }
        }
        else if (tab.Model?.Image is Bitmap bitmap)
        {
            width = bitmap.PixelSize.Width;
            height = bitmap.PixelSize.Height;
        }
        else
        {
            return null;
        }

        if (Settings.ImageScaling.ShowImageSideBySide && tab.SecondaryModel is { } secondary)
        {
            if (secondary.PixelWidth is not 0 && secondary.PixelHeight is not 0)
            {
                secondaryWidth = secondary.PixelWidth;
                secondaryHeight = secondary.PixelHeight;
            }
            else if (secondary.FileInfo is not null &&
                     (vm.WindowTabs.SharedCache?.TryGetReady(secondary.FileInfo, out var secondaryReady) ?? false))
            {
                secondaryWidth = secondaryReady.PixelWidth;
                secondaryHeight = secondaryReady.PixelHeight;
            }
            else
            {
                // Wait for the second image instead of resizing for a guessed pair and again once it arrives
                return null;
            }
        }
        else
        {
            secondaryWidth = secondaryHeight = 0;
        }
        
        return GetSize(width, height, secondaryWidth, secondaryHeight, tab.RotationAngle.CurrentValue, mainWindow, vm);
    }

    public static Fit? GetSize(double width, double height, double secondWidth, double secondHeight,
        double rotation, MainWindow mainWindow, MainWindowViewModel vm)
    {
        if (double.IsNaN(width) || double.IsNaN(height))
        {
            return null;
        }

        var isSideBySide = Settings.ImageScaling.ShowImageSideBySide && secondWidth > 0 && secondHeight > 0;
        var (contentWidth, contentHeight) = GetWindowContentSize();
        return ImageFitter.Fit(new FitInput
        {
            ImageWidth = width,
            ImageHeight = height,
            SecondImageWidth = isSideBySide ? secondWidth : 0,
            SecondImageHeight = isSideBySide ? secondHeight : 0,
            QuarterTurns = (int)Math.Round(rotation / 90),
            Mode = vm.WindowModeState.CurrentValue.FitMode,
            ZoomToFit = Settings.ImageScaling.ZoomToFit,
            ScrollEnabled = Settings.Zoom.ScrollEnabled,
            Screen = ScreenHelper.ScreenSize,
            ScreenMargin = Settings.WindowProperties.Margin,
            WindowContentWidth = contentWidth,
            WindowContentHeight = contentHeight,
            ReservedSpace = GetReservedSpace(vm)
        });

        (double, double) GetWindowContentSize()
        {
            return Dispatcher.CurrentDispatcher.CheckAccess() ? Get() : Dispatcher.CurrentDispatcher.Invoke(Get, DispatcherPriority.Send);

            (double, double) Get()
            {
                return (mainWindow.UIHelper.GetMainView.Bounds.Width, mainWindow.UIHelper.GetMainView.Bounds.Height);
            }
        }
    }

    private static ReservedSpace GetReservedSpace(MainWindowViewModel vm)
    {
        var (galleryWidth, galleryHeight) = GalleryHelper.GetGallerySize(vm);
        double top;
        if (vm.WindowTabs.Tabs.CurrentValue.Count > 1)
        {
            top = SizeDefaults.TabHeight + vm.TitlebarHeight.CurrentValue + 2;
        }
        else if (vm.IsTopToolbarShown.Value)
        {
            top = vm.TitlebarHeight.CurrentValue + 2;
        }
        else
        {
            top = 0;
        }

        var bottom = Settings.UIProperties.ShowBottomNavBar ? SizeDefaults.BottombarHeight : 0;
        return new ReservedSpace(top, bottom, galleryWidth, galleryHeight);
    }

    public static void SaveSize(Window window)
    {
        if (Dispatcher.CurrentDispatcher.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.CurrentDispatcher.Invoke(Set);
        }

        return;

        void Set()
        {
            var top = window.Position.Y;
            var left = window.Position.X;
            Settings.WindowProperties.Top = top;
            Settings.WindowProperties.Left = left;
            if (window.DataContext is MainWindowViewModel { WindowModeState.CurrentValue.Mode: not WindowMode.Normal })
            {
                return;
            }
            Settings.WindowProperties.Width = window.Bounds.Width;
            Settings.WindowProperties.Height = window.Bounds.Height;
        }
    }

    #endregion
}