using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.WindowBehavior;

/// <summary>
/// Puts the cursor back on the navigation or rotate button whose click resized an AutoFit window under it, so the
/// next click lands on the same button.
/// </summary>
public static class SetCursorOnButton
{
    /// <summary>
    /// Puts the cursor back on the button last clicked, now that the window has resized.
    /// </summary>
    public static void AfterResize(MainWindow window, MainWindowViewModel vm, double windowWidth)
    {
        var clicked = vm.ClickedButton;
        if (clicked is ClickedButton.None)
        {
            return;
        }

        vm.ClickedButton = ClickedButton.None;

        var (control, offset) = Find(window, vm, clicked, windowWidth);
        if (control is null || Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        Dispatcher.CurrentDispatcher.Post(() =>
        {
            var screenPoint = control.PointToScreen(offset);
            core.PlatformService.SetCursorPos(screenPoint.X, screenPoint.Y);
        }, DispatcherPriority.Loaded + 1);
    }

    // Where on which control the cursor goes, for the window's new width
    private static (Control? Control, Point Offset) Find(MainWindow window, MainWindowViewModel vm,
        ClickedButton clicked, double windowWidth) =>
        clicked switch
        {
            ClickedButton.BottomBarPrevious =>
                (window.UIHelper.GetBottomBar.PreviousButton, NavigationButtonOffset(windowWidth)),
            ClickedButton.BottomBarNext =>
                (window.UIHelper.GetBottomBar.NextButton, NavigationButtonOffset(windowWidth)),
            ClickedButton.BottomBarRotateRight => (window.UIHelper.GetBottomBar.RotateRightButton, new Point(20, 10)),
            ClickedButton.BottomBarRotateLeft => (window.UIHelper.GetBottomBar.RotateLeftButton, new Point(20, 10)),
            ClickedButton.HoverbarNext => (UIHelper.GetHoverBar(vm)?.NextButton, new Point(50, 10)),
            ClickedButton.HoverbarPrevious => (UIHelper.GetHoverBar(vm)?.PreviousButton, new Point(50, 10)),
            ClickedButton.HoverbarRotateRight => (UIHelper.GetHoverBar(vm)?.RotateRightButton, new Point(11, 7)),
            ClickedButton.HoverbarRotateLeft => (UIHelper.GetHoverBar(vm)?.RotateLeftButton, new Point(11, 7)),
            ClickedButton.ClickArrowLeft => (UIHelper.GetClickArrowLeft(vm), new Point(15, 95)),
            ClickedButton.ClickArrowRight => (UIHelper.GetClickArrowRight(vm), new Point(65, 95)),
            _ => (null, default)
        };

    // The navigation buttons sit further in once the bottom bar is wide enough to show its search, reset and rotate
    // buttons
    private static Point NavigationButtonOffset(double windowWidth) =>
        new(windowWidth > SizeDefaults.SearchResetAndRotateBtnBp ? 50 : 15, 10);
}
