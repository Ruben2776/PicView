using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
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