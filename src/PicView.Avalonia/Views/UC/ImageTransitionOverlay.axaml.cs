using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace PicView.Avalonia.Views.UC;

public partial class ImageTransitionOverlay : UserControl
{
    public ImageTransitionOverlay()
    {
        InitializeComponent();
    }

    public void SetBitmaps(Bitmap? outgoing, Bitmap? incoming)
    {
        Outgoing.Opacity = 1;
        Incoming.Opacity = 0;

        Outgoing.RenderTransform = null;
        Incoming.RenderTransform = null;

        Outgoing.Source = outgoing;
        Incoming.Source = incoming;
    }

    public async Task RunRotateAsync(
        double degrees,
        TimeSpan duration,
        double? targetWidth = null,
        double? targetHeight = null,
        CancellationToken ct = default)
    {
        if (!Dispatcher.UIThread.CheckAccess())
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

        ct.ThrowIfCancellationRequested();

        // Outgoing only
        Outgoing.Opacity = 1;
        Outgoing.RenderTransform = null;

        // Ensure bounds are valid
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

        var cx = Outgoing.Bounds.Width * 0.5;
        var cy = Outgoing.Bounds.Height * 0.5;

        // Capture start size (same logic as RunFadeAsync)
        var startW = Stage.Width;
        var startH = Stage.Height;

        if (double.IsNaN(startW) || startW <= 0) startW = Stage.Bounds.Width;
        if (double.IsNaN(startH) || startH <= 0) startH = Stage.Bounds.Height;

        var endW = targetWidth  ?? startW;
        var endH = targetHeight ?? startH;

        // Lock stage size for the duration of the animation
        Stage.Width = startW;
        Stage.Height = startH;

        var sw = Stopwatch.StartNew();
        var totalMs = Math.Max(1.0, duration.TotalMilliseconds);

        while (sw.Elapsed.TotalMilliseconds < totalMs)
        {
            ct.ThrowIfCancellationRequested();

            var t = sw.Elapsed.TotalMilliseconds / totalMs; // 0..1

            // (3) Snappier easing for rotate
            var easedRot = EaseOutBack(t);

            // Rotation
            var angle = degrees * easedRot;

            // (1) Scale pop: 1.0 -> 1.05 -> 1.0
            var pop = ScalePop(t, peak: 1.05);

            // Transform
            Outgoing.RenderTransform = new MatrixTransform(CreatePivotRotateScaleMatrix(angle, pop, pop, cx, cy));

            // (2) Opacity dip
            Outgoing.Opacity = OpacityDip(t, minOpacity: 0.88);

            // Animate size alongside rotation
            Stage.Width  = Lerp(startW, endW, easedRot);
            Stage.Height = Lerp(startH, endH, easedRot);

            await Task.Delay(16, ct); // ~60fps
        }

        // Final state
        Outgoing.RenderTransform = new MatrixTransform(CreatePivotRotateScaleMatrix(degrees, 1.0, 1.0, cx, cy));
        Outgoing.Opacity = 1.0;

        Stage.Width = endW;
        Stage.Height = endH;
    }



    public async Task RunFadeAsync(
        TimeSpan duration,
        double? targetWidth = null,
        double? targetHeight = null,
        Transform? outgoingTransform = null,
        Transform? incomingTransform = null,
        CancellationToken ct = default)
    {
        // Must run on UI thread
        if (!Dispatcher.UIThread.CheckAccess())
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

        ct.ThrowIfCancellationRequested();

        if (outgoingTransform is not null)
            Outgoing.RenderTransform = outgoingTransform;

        if (incomingTransform is not null)
            Incoming.RenderTransform = incomingTransform;

        // Capture start size (use Stage.Width/Height if set, otherwise Bounds)
        // Bounds is the actual on-screen size.
        var startW = Stage.Width;
        var startH = Stage.Height;

        if (double.IsNaN(startW) || startW <= 0) startW = Outgoing.Bounds.Width;
        if (double.IsNaN(startH) || startH <= 0) startH = Outgoing.Bounds.Height;

        // If no target provided, keep size
        var endW = targetWidth  ?? startW;
        var endH = targetHeight ?? startH;

        // Lock stage to explicit size during animation to avoid layout jitter
        Stage.Width = startW;
        Stage.Height = startH;

        var sw = Stopwatch.StartNew();
        var totalMs = Math.Max(1.0, duration.TotalMilliseconds);

        while (sw.Elapsed.TotalMilliseconds < totalMs)
        {
            ct.ThrowIfCancellationRequested();

            var t = sw.Elapsed.TotalMilliseconds / totalMs;

            // ease-out cubic for fade
            var eased = 1 - Math.Pow(1 - t, 3);

            Outgoing.Opacity = 1 - eased;
            Incoming.Opacity = eased;

            // Match easing for size (or use a different one if you want)
            Stage.Width  = Lerp(startW, endW, eased);
            Stage.Height = Lerp(startH, endH, eased);

            await Task.Delay(16, ct);
        }

        Outgoing.Opacity = 0;
        Incoming.Opacity = 1;

        Stage.Width = endW;
        Stage.Height = endH;

        Outgoing.RenderTransform = null;
        Incoming.RenderTransform = null;
    }


    public async Task RunFlipAsync(
        bool horizontal,
        TimeSpan duration,
        CancellationToken ct = default)
    {
        if (!Dispatcher.UIThread.CheckAccess())
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

        ct.ThrowIfCancellationRequested();

        // Outgoing only
        Outgoing.Opacity = 1;
        Outgoing.RenderTransform = null;

        // Ensure bounds are valid
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

        var cx = Outgoing.Bounds.Width * 0.5;
        var cy = Outgoing.Bounds.Height * 0.5;

        const double eps = 0.001;

        var sw = Stopwatch.StartNew();
        var totalMs = Math.Max(1.0, duration.TotalMilliseconds);

        while (sw.Elapsed.TotalMilliseconds < totalMs)
        {
            ct.ThrowIfCancellationRequested();

            var t = sw.Elapsed.TotalMilliseconds / totalMs; // 0..1

            // Flip easing: smooth in/out + a bit of "snap"
            var eased = FlipEase(t);

            // We want axisScale: 1 -> eps -> -1
            double axisScale;
            if (eased < 0.5)
            {
                var p = eased / 0.5;                 // 0..1
                axisScale = Lerp(1.0, eps, p);       // 1..eps
            }
            else
            {
                var p = (eased - 0.5) / 0.5;         // 0..1
                axisScale = Lerp(-eps, -1.0, p);     // -eps..-1
            }

            // Scale pop (subtle): apply on the non-flipping axis
            // Horizontal flip: pop Y slightly; Vertical flip: pop X slightly.
            var pop = ScalePop(t, peak: 1.035);

            double sx, sy;
            if (horizontal)
            {
                sx = axisScale;   // flip axis
                sy = pop;         // pop on other axis
            }
            else
            {
                sx = pop;         // pop on other axis
                sy = axisScale;   // flip axis
            }

            // Opacity dip at midpoint
            Outgoing.Opacity = OpacityDip(t, minOpacity: 0.90);

            Outgoing.RenderTransform = new MatrixTransform(CreatePivotScaleMatrix(sx, sy, cx, cy));

            await Task.Delay(16, ct); // ~60fps
        }

        // Final: fully flipped (mirrored)
        double finalAxis = -1.0;
        var finalPop = 1.0;

        var finalSx = horizontal ? finalAxis : finalPop;
        var finalSy = horizontal ? finalPop  : finalAxis;

        Outgoing.RenderTransform = new MatrixTransform(CreatePivotScaleMatrix(finalSx, finalSy, cx, cy));
        Outgoing.Opacity = 1.0;
    }



    #region Helpers for transforms

    private static Matrix CreatePivotScaleMatrix(double sx, double sy, double cx, double cy)
    {
        var toOrigin = new Matrix(1, 0, 0, 1, -cx, -cy);
        var scale    = new Matrix(sx, 0, 0, sy, 0, 0);
        var back     = new Matrix(1, 0, 0, 1, cx, cy);
        return toOrigin * scale * back;
    }

    private static Matrix CreatePivotRotateScaleMatrix(double degrees, double sx, double sy, double cx, double cy)
    {
        var radians = degrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);

        var toOrigin = new Matrix(1, 0, 0, 1, -cx, -cy);

        // Scale then rotate (order matters). This order feels natural for a "pop + rotate".
        var scale  = new Matrix(sx, 0, 0, sy, 0, 0);
        var rotate = new Matrix(cos, sin, -sin, cos, 0, 0);

        var back = new Matrix(1, 0, 0, 1, cx, cy);

        return toOrigin * scale * rotate * back;
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static double EaseInOutCubic(double t)
    {
        return t < 0.5
            ? 4 * t * t * t
            : 1 - Math.Pow(-2 * t + 2, 3) / 2;
    }

    private static double FlipEase(double t)
    {
        // Base ease-in-out cubic
        double baseEase = t < 0.5
            ? 4 * t * t * t
            : 1 - Math.Pow(-2 * t + 2, 3) / 2;

        // Snap: push a little faster around midpoint without overshoot
        // Range stays 0..1
        const double snap = 0.10; // tweak 0.05..0.15
        var bump = Math.Sin(Math.PI * t); // 0..1..0
        var snapped = baseEase + (bump * (t - 0.5)) * snap;

        // Clamp
        if (snapped < 0) return 0;
        if (snapped > 1) return 1;
        return snapped;
    }

    // Rotate easing: ease-out-back (snappy but controlled)
    private static double EaseOutBack(double t)
    {
        // typical back overshoot constant
        const double c1 = 1.70158;
        const double c3 = c1 + 1.0;

        var x = t - 1.0;
        return 1.0 + c3 * x * x * x + c1 * x * x;
    }

    // Scale pop: 1 -> peak -> 1 using a smooth bump (sinusoidal)
    private static double ScalePop(double t, double peak)
    {
        var bump = Math.Sin(Math.PI * t); // 0..1..0
        return 1.0 + (peak - 1.0) * bump;
    }

    // Opacity dip: 1 -> min -> 1
    private static double OpacityDip(double t, double minOpacity)
    {
        var bump = Math.Sin(Math.PI * t); // 0..1..0
        return 1.0 - (1.0 - minOpacity) * bump;
    }

    #endregion

    public void SetStageSize(double width, double height)
    {
        if (width > 0) Stage.Width = width;
        if (height > 0) Stage.Height = height;
    }

}
