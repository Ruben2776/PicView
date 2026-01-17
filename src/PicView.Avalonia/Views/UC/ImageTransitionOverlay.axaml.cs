using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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

    public void SetHost(UserControl host) => Host.Content = host;

    public void LockStageSize(double width, double height)
    {
        // Lock to prevent SizeToContent recalculations during transform animations
        Stage.Width = width;
        Stage.Height = height;
    }


    public void SetBitmaps(Bitmap outgoing, Bitmap incoming)
    {
        Outgoing.Source = outgoing;
        Incoming.Source = incoming;

        Outgoing.Opacity = 1;
        Incoming.Opacity = 0;

        Outgoing.RenderTransform = null;
        Incoming.RenderTransform = null;
    }

    public async Task RunFadeAsync(
        TimeSpan duration,
        Transform? outgoingTransform = null,
        Transform? incomingTransform = null,
        CancellationToken ct = default)
    {
        // must run on UI thread
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(() => { });
        }

        ct.ThrowIfCancellationRequested();

        if (outgoingTransform is not null)
            Outgoing.RenderTransform = outgoingTransform;

        if (incomingTransform is not null)
            Incoming.RenderTransform = incomingTransform;

        // Version-proof "animation": step opacity over time
        var sw = Stopwatch.StartNew();
        var totalMs = Math.Max(1.0, duration.TotalMilliseconds);

        while (sw.Elapsed.TotalMilliseconds < totalMs)
        {
            ct.ThrowIfCancellationRequested();

            var t = sw.Elapsed.TotalMilliseconds / totalMs;
            // ease-out cubic
            var eased = 1 - Math.Pow(1 - t, 3);

            Outgoing.Opacity = 1 - eased;
            Incoming.Opacity = eased;

            // ~60fps
            await Task.Delay(16, ct);
        }

        Outgoing.Opacity = 0;
        Incoming.Opacity = 1;

        // reset transforms
        Outgoing.RenderTransform = null;
        Incoming.RenderTransform = null;
    }

public async Task RunFlipAsync(
    bool horizontal,
    TimeSpan duration,
    CancellationToken ct = default)
{
    // Must run on UI thread
    if (!Dispatcher.UIThread.CheckAccess())
        await Dispatcher.UIThread.InvokeAsync(() => { });

    ct.ThrowIfCancellationRequested();

    // Ensure baseline state
    Outgoing.Opacity = 1;
    Incoming.Opacity = 0;

    // We animate RenderTransform as ScaleTransform.
    // Horizontal flip: ScaleX animates; Vertical flip: ScaleY animates.
    var sw = Stopwatch.StartNew();
    var totalMs = Math.Max(1.0, duration.TotalMilliseconds);

    while (sw.Elapsed.TotalMilliseconds < totalMs)
    {
        ct.ThrowIfCancellationRequested();

        var t = sw.Elapsed.TotalMilliseconds / totalMs; // 0..1
        // ease-in-out (smooth squeeze + expand)
        var eased = EaseInOutCubic(t);

        if (eased < 0.5)
        {
            // Phase 1 (0..0.5): outgoing scales down to 0
            var p = eased / 0.5;          // 0..1
            var scale = Lerp(1.0, 0.0, p);

            Outgoing.Opacity = 1;
            Incoming.Opacity = 0;

            Outgoing.RenderTransform = horizontal
                ? new ScaleTransform(scale, 1)
                : new ScaleTransform(1, scale);
        }
        else
        {
            // Phase 2 (0.5..1): incoming scales up from 0 to 1, mirrored
            var p = (eased - 0.5) / 0.5;  // 0..1
            var scale = Lerp(0.0, 1.0, p);

            Outgoing.Opacity = 0;
            Incoming.Opacity = 1;

            // Mirrored on the relevant axis
            Incoming.RenderTransform = horizontal
                ? new ScaleTransform(-scale, 1) // negative X = flipped
                : new ScaleTransform(1, -scale); // negative Y = flipped
        }

        await Task.Delay(16, ct); // ~60fps
    }

    // Final state
    Outgoing.Opacity = 0;
    Incoming.Opacity = 1;

    Outgoing.RenderTransform = null;
    Incoming.RenderTransform = null;
}

private static double Lerp(double a, double b, double t) => a + (b - a) * t;

private static double EaseInOutCubic(double t)
{
    // 0..1
    return t < 0.5
        ? 4 * t * t * t
        : 1 - Math.Pow(-2 * t + 2, 3) / 2;
}

}
