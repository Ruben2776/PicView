using System.Threading.Tasks;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Gallery;

namespace PicView.Avalonia.ImageTransformations.Rotation;

public static class RotationNavigation
{
    public static async Task RotateTo(MainViewModel? vm, int angle)
    {
        await vm.ImageViewer.RotateAsync(angle);
        await WindowResizing.SetSizeAsync(vm);
    }

    public static async Task RotateRight(MainViewModel? vm)
    {
        if (vm is null || GalleryFunctions.IsFullGalleryOpen)
            return;

        await vm.ImageViewer.RotateAsync(90);
    }

    public static async Task RotateLeft(MainViewModel? vm)
    {
        if (vm is null || GalleryFunctions.IsFullGalleryOpen)
            return;

        await vm.ImageViewer.RotateAsync(-90);
    }

    public static async Task Flip(MainViewModel vm, bool isHorizontal)
    {
        await vm.ImageViewer.FlipAsync(isHorizontal);

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
    /// Navigates up
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
        });
    }

    /// <summary>
    /// Navigates down
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
        });
    }
}