using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Sizing;

public static class ImageTransitionService
{    
    public static async Task AnimateFlipAndCommitAsync(MainViewModel vm, Bitmap? newBitmap, Orientation flipOrientation, CancellationToken cancellationToken = default) =>
        await AnimateAndCommitAsync(
            vm,
            flipOrientation == Orientation.Horizontal ? TransformType.FlipHorizontal : TransformType.FlipVertical,
            newBitmap,
            TimeSpan.FromMilliseconds(300),
            ct: cancellationToken);
    

    public static async Task AnimateRotateAndCommitAsync(MainViewModel vm, Bitmap newBitmap, double angle, CancellationToken cancellationToken = default) => 
        await AnimateAndCommitAsync(
            vm,
            TransformType.Rotate,
            newBitmap,
            TimeSpan.FromMilliseconds(300),
            angle,
            ct: cancellationToken);

    public static async Task AnimateFadeAndCommitAsync(MainViewModel vm, Bitmap? newBitmap, CancellationToken cancellationToken = default) => 
        await AnimateAndCommitAsync(
            vm,
            TransformType.Fade,
            newBitmap,
            TimeSpan.FromMilliseconds(500),
            ct: cancellationToken);
    



    private enum TransformType
    {
        Rotate,
        FlipHorizontal,
        FlipVertical,
        Fade
    }


    private static async Task AnimateAndCommitAsync(
        MainViewModel vm,
        TransformType transformType,
        Bitmap? newBitmap,
        TimeSpan duration,
        double arg1 = 0.0,
        CancellationToken ct = default)
    {
        var oldView = vm.MainWindow.CurrentView.Value;
        if (oldView is null)
        {
            await Dispatcher.UIThread.InvokeAsync(() => vm.ImageViewer.ApplyBitmapAndRefresh(newBitmap, vm));
            return;
        }

        if (vm.PicViewer.ImageSource.Value is not Bitmap oldBitmap)
        {
            await Dispatcher.UIThread.InvokeAsync(() => vm.ImageViewer.ApplyBitmapAndRefresh(newBitmap, vm));
            return;
        }

        bool canLock = false;        
        double w = oldView.Bounds.Width;
        double h = oldView.Bounds.Height;
        ImageSize? newBitmapSize = null;

        if(newBitmap is not null)
            newBitmapSize = WindowResizing.GetSize(newBitmap.Size.Width, newBitmap.Size.Height, 0, 0, vm);

        if (w <= 0 || h <= 0)
        {
            w = vm.MainWindow.CurrentView.CurrentValue.Bounds.Width;
            h = vm.MainWindow.CurrentView.CurrentValue.Bounds.Height;
        }

        ImageTransitionOverlay? overlay = null;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                overlay = new ImageTransitionOverlay();
                overlay.SetBitmaps(oldBitmap, newBitmap);
                overlay.SetStageSize(w, h);

                vm.MainWindow.CurrentView.Value = overlay;

                await (transformType switch
                {
                    TransformType.Rotate         => overlay!.RunRotateAsync(arg1, duration, !canLock ? newBitmap.Size.Width : null, !canLock ? newBitmap.Size.Height : null, ct),
                    TransformType.FlipHorizontal => overlay!.RunFlipAsync(true,  duration, ct),
                    TransformType.FlipVertical   => overlay!.RunFlipAsync(false, duration, ct),
                    TransformType.Fade           => overlay!.RunFadeAsync(duration, !canLock ? newBitmap.Size.Width : null, !canLock ? newBitmap.Size.Height : null, null, null, ct),
                    _                            => Task.CompletedTask
                });
            }, DispatcherPriority.Render);

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                UIHelper.GetMainView.MainGrid.Children.Remove(overlay);
                vm.MainWindow.CurrentView.Value = oldView;
                await vm.ImageViewer.ApplyBitmapAndRefresh(newBitmap, vm);
                vm.PicViewer.HasChanges.Value = true;
            }, DispatcherPriority.Render);
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (vm.MainWindow.CurrentView.Value == overlay)
                    vm.MainWindow.CurrentView.Value = oldView;
            }, DispatcherPriority.Render);
        }
    }

}
