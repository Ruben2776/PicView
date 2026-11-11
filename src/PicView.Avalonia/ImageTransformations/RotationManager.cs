using Avalonia.Threading;
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
        switch (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.Value)
        {
            case ImageViewer imageViewer:
                imageViewer.ResetZoomSlim();
                break;
            case CropControl cropControl:
                cropControl.ResetZoom();
                break;
        }

        Turn(vm, mainWindow, static orientation => orientation.TurnTo(0));
    }
    
    public static void ResetZoom(MainWindowViewModel vm)
    {
        switch (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.Value)
        {
            case ImageViewer imageViewer:
                imageViewer.ResetZoom(Settings.Zoom.IsZoomAnimated);
                break;
            // The crop view zooms too (ADR-0027)
            case CropControl cropControl:
                cropControl.ResetZoom();
                break;
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
        var view = tab.CurrentView.Value;
        if (view is not (ImageViewer { HasImage: true } or CropControl))
        {
            return;
        }

        if (view is ImageViewer imageViewer)
        {
            imageViewer.AnimateNextFlip();
        }

        tab.Orientation.Flip();
    }

    /// <summary>
    /// Turns the shown image and fits the window to its new shape, once.
    /// </summary>
    private static void Turn(MainWindowViewModel vm, MainWindow mainWindow, Action<TabOrientation> turn)
    {
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        if (tab.CurrentView.Value is not (ImageViewer { HasImage: true } or CropControl))
        {
            return;
        }

        // The window's fitter fits it to the turned image
        turn(tab.Orientation);
        if (OperatingSystem.IsMacOS() && vm.IsFullscreen.Value)
        {
            // Workaround, outside ADR-0026's rule: Placement does nothing in Fullscreen. This re-centers the macOS
            // fullscreen window after a turn (after the Fit), because it sometimes ends up off-center there, a quirk
            // nobody can reproduce, so it stays marked as one, in the spirit of ADR-0006
            Dispatcher.UIThread.Post(() => WindowFunctions.CenterWindowOnScreen(mainWindow),
                DispatcherPriority.Background);
        }
    }
}
