using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;

public static class ImageTransitionService
{
    public static async Task AnimateAndCommitAsync(
        MainViewModel vm,
        Bitmap newBitmap,
        TimeSpan duration,
        Func<Transform?>? outgoingTransform = null,
        Func<Transform?>? incomingTransform = null,
        CancellationToken ct = default)
    {
        // Capture current view (what the window is currently hosting)
        var oldView = vm.MainWindow.CurrentView.Value;
        if (oldView is null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
                vm.ImageViewer.ApplyBitmapAndRefresh(newBitmap, vm));
            return;
        }

        // Capture the currently displayed bitmap
        if (vm.PicViewer.ImageSource.Value is not Bitmap oldBitmap)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
                vm.ImageViewer.ApplyBitmapAndRefresh(newBitmap, vm));
            return;
        }

        // Capture size so SizeToContent window doesn't shrink/expand during transforms
        double w = oldView.Bounds.Width;
        double h = oldView.Bounds.Height;

        // Bounds can be 0 during early layout; fallback to main window bounds
        if (w <= 0 || h <= 0)
        {
            w = vm.MainWindow.CurrentView.CurrentValue.Bounds.Width;
            h = vm.MainWindow.CurrentView.CurrentValue.Bounds.Height;
        }

        // Final fallback: don't lock if we still have no size
        var canLock = w > 0 && h > 0;

        ImageTransitionOverlay? overlay = null;

        // Swap CurrentView to overlay on UI thread
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            overlay = new ImageTransitionOverlay();
            overlay.SetHost(oldView);
            overlay.SetBitmaps(oldBitmap, newBitmap);

            if (canLock)
                overlay.LockStageSize(w, h);

            vm.MainWindow.CurrentView.Value = overlay;
        }, DispatcherPriority.Render);

        try
        {
            // Run fade (must be on UI thread; method itself enforces UI thread usage)
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                ct.ThrowIfCancellationRequested();

                await overlay!.RunFadeAsync(
                    duration,
                    outgoingTransform?.Invoke(),
                    incomingTransform?.Invoke(),
                    ct);
            }, DispatcherPriority.Render);

            // Restore original view and commit the real image state
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                vm.MainWindow.CurrentView.Value = oldView;

                // Commit: this updates PicViewer.ImageSource.Value and refreshes UI
                vm.ImageViewer.ApplyBitmapAndRefresh(newBitmap, vm);
                vm.PicViewer.HasChanges.Value = true;
            }, DispatcherPriority.Render);
        }
        finally
        {
            // If something went wrong, make sure we restore view
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (vm.MainWindow.CurrentView.Value == overlay)
                    vm.MainWindow.CurrentView.Value = oldView;
            }, DispatcherPriority.Render);
        }
    }


    public static async Task AnimateFlipAndCommitAsync(
    MainViewModel vm,
    Bitmap newBitmap,
    bool horizontal,
    TimeSpan duration,
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

    // Capture size so SizeToContent window doesn't shrink/expand during transforms
    double w = oldView.Bounds.Width;
    double h = oldView.Bounds.Height;

    // Bounds can be 0 during early layout; fallback to main window bounds
    if (w <= 0 || h <= 0)
    {
        w = vm.MainWindow.CurrentView.CurrentValue.Bounds.Width;
        h = vm.MainWindow.CurrentView.CurrentValue.Bounds.Height;
    }

    // Final fallback: don't lock if we still have no size
    var canLock = w > 0 && h > 0;

    ImageTransitionOverlay? overlay = null;

    await Dispatcher.UIThread.InvokeAsync(() =>
    {
        overlay = new ImageTransitionOverlay();
        overlay.SetHost(oldView);
        overlay.SetBitmaps(oldBitmap, newBitmap);

        if (canLock)
                overlay.LockStageSize(w, h);

        //vm.MainWindow.CurrentView.Value = overlay;
        UIHelper.GetMainView.MainGrid.Children.Add(overlay);
    }, DispatcherPriority.Render);

    try
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await overlay!.RunFlipAsync(horizontal, duration, ct);
        }, DispatcherPriority.Render);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            UIHelper.GetMainView.MainGrid.Children.Remove(overlay);
            vm.MainWindow.CurrentView.Value = oldView;
            vm.ImageViewer.ApplyBitmapAndRefresh(newBitmap, vm);
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
