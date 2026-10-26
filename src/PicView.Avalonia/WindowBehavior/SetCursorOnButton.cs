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
    /// <param name="IsClicked">Whether the button was clicked and the window has not resized since.</param>
    /// <param name="Clear">Marks the click as handled.</param>
    /// <param name="Find">Finds the button in the window.</param>
    /// <param name="Offset">Where on the button the cursor goes, for the window's new width.</param>
    private readonly record struct ClickedButton(
        Func<MainWindowViewModel, bool> IsClicked,
        Action<MainWindowViewModel> Clear,
        Func<MainWindow, MainWindowViewModel, Control?> Find,
        Func<double, Point> Offset);

    private static readonly ClickedButton[] Buttons =
    [
        new(static vm => vm.IsNavigationButtonLeftClicked,
            static vm => vm.IsNavigationButtonLeftClicked = false,
            static (window, _) => window.UIHelper.GetBottomBar.PreviousButton,
            NavigationButtonOffset),
        new(static vm => vm.IsNavigationButtonRightClicked,
            static vm => vm.IsNavigationButtonRightClicked = false,
            static (window, _) => window.UIHelper.GetBottomBar.NextButton,
            NavigationButtonOffset),
        new(static vm => vm.IsBottomToolbarRightRotationClicked,
            static vm => vm.IsBottomToolbarRightRotationClicked = false,
            static (window, _) => window.UIHelper.GetBottomBar.RotateRightButton,
            static _ => new Point(20, 10)),
        new(static vm => vm.IsBottomToolbarLeftRotationClicked,
            static vm => vm.IsBottomToolbarLeftRotationClicked = false,
            static (window, _) => window.UIHelper.GetBottomBar.RotateLeftButton,
            static _ => new Point(20, 10)),
        new(static vm => Hoverbar(vm).IsHoverNavigationButtonNextClicked,
            static vm => Hoverbar(vm).IsHoverNavigationButtonNextClicked = false,
            static (_, _) => UIHelper.GetHoverBar().NextButton,
            static _ => new Point(50, 10)),
        new(static vm => Hoverbar(vm).IsHoverNavigationButtonPreviousClicked,
            static vm => Hoverbar(vm).IsHoverNavigationButtonPreviousClicked = false,
            static (_, _) => UIHelper.GetHoverBar().PreviousButton,
            static _ => new Point(50, 10)),
        new(static vm => vm.IsClickArrowLeftClicked,
            static vm => vm.IsClickArrowLeftClicked = false,
            static (_, vm) => UIHelper.GetClickArrowLeft(vm),
            static _ => new Point(15, 95)),
        new(static vm => vm.IsClickArrowRightClicked,
            static vm => vm.IsClickArrowRightClicked = false,
            static (_, vm) => UIHelper.GetClickArrowRight(vm),
            static _ => new Point(65, 95)),
        new(static vm => Hoverbar(vm).IsHoverRotateRightClicked,
            static vm => Hoverbar(vm).IsHoverRotateRightClicked = false,
            static (_, _) => UIHelper.GetHoverBar().RotateRightButton,
            static _ => new Point(11, 7)),
        new(static vm => Hoverbar(vm).IsHoverRotateLeftClicked,
            static vm => Hoverbar(vm).IsHoverRotateLeftClicked = false,
            static (_, _) => UIHelper.GetHoverBar().RotateLeftButton,
            static _ => new Point(11, 7))
    ];

    /// <summary>
    /// Puts the cursor back on every button whose click resized the window, now that it has resized.
    /// </summary>
    public static void AfterResize(MainWindow window, MainWindowViewModel vm, double windowWidth)
    {
        foreach (var button in Buttons)
        {
            if (!button.IsClicked(vm))
            {
                continue;
            }

            if (button.Find(window, vm) is { } control && Application.Current.DataContext is CoreViewModel core)
            {
                var offset = button.Offset(windowWidth);
                Dispatcher.CurrentDispatcher.Post(() =>
                {
                    var screenPoint = control.PointToScreen(offset);
                    core.PlatformService.SetCursorPos(screenPoint.X, screenPoint.Y);
                }, DispatcherPriority.Loaded + 1);
            }

            button.Clear(vm);
        }
    }

    // The navigation buttons sit further in once the bottom bar is wide enough to show its search, reset and rotate
    // buttons
    private static Point NavigationButtonOffset(double windowWidth) =>
        new(windowWidth > SizeDefaults.SearchResetAndRotateBtnBp ? 50 : 15, 10);

    private static HoverbarViewModel Hoverbar(MainWindowViewModel vm) =>
        vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar;
}
