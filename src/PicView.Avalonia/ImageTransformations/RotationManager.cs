using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.ImageTransformations;

public static class RotationManager
{
    public static void ResetZoomAndRotations(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is ImageViewer imageViewer)
        {
            imageViewer.ResetZoomSlim();
            imageViewer.Rotate(0);
        }
        
        if (vm.WindowModeState.CurrentValue.AutoFit)
        {
            WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);
        }
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
            WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);
        }
    }
    
    public static void Rotate(MainWindowViewModel vm, int angle, MainWindow mainWindow)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is not ImageViewer imageViewer)
        {
            return;
        }
        
        imageViewer.Rotate(angle);
        
        if (vm.WindowModeState.CurrentValue.AutoFit)
        {
            WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);
        }
    }
    
    public static void RotateRight(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is not ImageViewer imageViewer)
        {
            return;
        }
        
        imageViewer.Rotate(true);
        
        if (vm.WindowModeState.CurrentValue.AutoFit)
        {
            WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);
        }
    }

    public static void RotateLeft(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is not ImageViewer imageViewer)
        {
            return;
        }
        
        imageViewer.Rotate(false);
        
        if (vm.WindowModeState.CurrentValue.AutoFit)
        {
            WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);
        }
    }
    
    public static void Flip(MainWindowViewModel vm)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is ImageViewer imageViewer)
        {
            imageViewer.Flip(true);
        }
    }
}