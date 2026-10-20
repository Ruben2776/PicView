using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ImageTransformations;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.ImageTransformations;

/// <summary>
/// Turns and flips the image the window's active tab shows, through the tab's Orientation, and resets its zoom.
/// </summary>
public static class RotationManager
{
    public static void ResetZoomAndRotations(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is ImageViewer imageViewer)
        {
            imageViewer.ResetZoomSlim();
        }

        Turn(vm, mainWindow, static orientation => orientation.TurnTo(0));
    }
    
    public static void ResetZoom(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is not ImageViewer imageViewer)
        {
            return;
        }
        
        imageViewer.ResetZoom(Settings.Zoom.IsZoomAnimated);
        if (vm.WindowModeState.CurrentValue.AutoFit)
        {
            mainWindow.WindowFitter.Refit();
        }
    }

    /// <param name="quarterTurns">Clockwise quarter turns from upright.</param>
    public static void Rotate(MainWindowViewModel vm, int quarterTurns, MainWindow mainWindow) =>
        Turn(vm, mainWindow, orientation => orientation.TurnTo(quarterTurns));

    public static void RotateRight(MainWindowViewModel vm, MainWindow mainWindow) =>
        Turn(vm, mainWindow, static orientation => orientation.TurnRight());

    public static void RotateLeft(MainWindowViewModel vm, MainWindow mainWindow) =>
        Turn(vm, mainWindow, static orientation => orientation.TurnLeft());

    public static void Flip(MainWindowViewModel vm)
    {
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        if (tab.CurrentView.CurrentValue is not ImageViewer { HasImage: true } imageViewer)
        {
            return;
        }

        imageViewer.AnimateNextFlip();
        tab.Orientation.Flip();
    }

    /// <summary>
    /// Turns the shown image and fits the window to its new shape, once.
    /// </summary>
    private static void Turn(MainWindowViewModel vm, MainWindow mainWindow, Action<TabOrientation> turn)
    {
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        if (tab.CurrentView.CurrentValue is not ImageViewer { HasImage: true })
        {
            return;
        }

        turn(tab.Orientation);
        mainWindow.WindowFitter.Refit();
        if (OperatingSystem.IsMacOS() && vm.IsFullscreen.Value)
        {
            // Sometimes the window is off-center after rotating on macOS fullscreen view
            WindowFunctions.CenterWindowOnScreen(mainWindow);
        }
    }
}
