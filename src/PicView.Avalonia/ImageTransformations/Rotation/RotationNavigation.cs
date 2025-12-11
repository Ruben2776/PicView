using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Gallery;

namespace PicView.Avalonia.ImageTransformations.Rotation;

public static class RotationNavigation
{
    public static async Task RotateRight(MainViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => { vm.ImageViewer.Rotate(false); });
    }

    public static async Task RotateTo(MainViewModel? vm, int angle)
    {
        await Dispatcher.UIThread.InvokeAsync(() => { vm.ImageViewer.Rotate(angle); });
        vm.PicViewer.RotationAngle.Value = angle;
        await WindowResizing.SetSizeAsync(vm);
    }


    public static async Task RotateLeft(MainViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => { vm.ImageViewer.Rotate(true); });
    }

    public static void Flip(MainViewModel vm)
    {
        Dispatcher.UIThread.Invoke(() => { vm.ImageViewer.Flip(true); });

        if (vm.PicViewer.ScaleX.CurrentValue == 1)
        {
            vm.PicViewer.ScaleX.Value = -1;
            vm.Translation.IsFlipped.Value = vm.Translation.UnFlip.CurrentValue;
        }
        else
        {
            vm.PicViewer.ScaleX.Value = 1;
            vm.Translation.IsFlipped.Value = vm.Translation.Flip.CurrentValue;
        }
    }

    /// <summary>
    /// Navigates up or rotates the image based on current state
    /// </summary>
    public static async Task NavigateUp(MainViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            GalleryNavigation.NavigateGallery(Direction.Up, vm);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (vm.GlobalSettings.IsScrollingEnabled.CurrentValue)
            {
                vm.ImageViewer.ImageScrollViewer.LineUp();
            }
            else
            {
                vm.ImageViewer.Rotate(true);
            }
        });
    }

    /// <summary>
    /// Navigates down or rotates the image based on current state
    /// </summary>
    public static async Task NavigateDown(MainViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            GalleryNavigation.NavigateGallery(Direction.Down, vm);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (vm.GlobalSettings.IsScrollingEnabled.CurrentValue)
            {
                vm.ImageViewer.ImageScrollViewer.LineDown();
            }
            else
            {
                vm.ImageViewer.Rotate(false);
            }
        });
    }
}