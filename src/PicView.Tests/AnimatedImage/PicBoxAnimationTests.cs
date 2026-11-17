using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.AnimatedImage;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using PicView.Core.Animation;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using static PicView.Tests.UI.TabViewsHarness;

namespace PicView.Tests.AnimatedImage;

[Collection("Sequential")]
public class PicBoxAnimationTests
{
    private int _opens;

    public PicBoxAnimationTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    private static AnimationPlayback Playback(string name) => new(new FileInfo(name), ImageType.AnimatedGif);

    private AnimationPlayer CountingOpen(AnimationPlayback playback)
    {
        Interlocked.Increment(ref _opens);
        return new AnimationPlayer(playback, new FakeAnimationDecoder(100, 100, 100));
    }

    private (Window Window, PicBox Box) Show()
    {
        var box = new PicBox
        {
            Source = new WriteableBitmap(new PixelSize(4, 4), new Vector(96, 96)),
            OpenPlayer = CountingOpen
        };
        var window = new Window { Width = 200, Height = 200, Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, box);
    }

    private static async Task SettleOpening(PicBox box)
    {
        await box.Opening.ConfigureAwait(true);
        Dispatcher.UIThread.RunJobs();
    }

    // A message sent to the custom visual reaches its handler, on the compositor, by the second render tick
    private static void DeliverHandlerMessages()
    {
        for (var i = 0; i < 2; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public async Task Playback_Set_OpensOnce()
    {
        var (window, box) = Show();
        var playback = Playback("a.gif");

        box.Playback = playback;
        await SettleOpening(box);

        Assert.Equal(1, _opens);
        Assert.Same(playback, box.Player?.Playback);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Layout_SeveralPassesAndResizes_DoesNotOpenAgain()
    {
        var (window, box) = Show();
        box.Playback = Playback("a.gif");
        await SettleOpening(box);
        var player = box.Player;

        foreach (var size in new[] { 300, 150, 250 })
        {
            window.Width = size;
            window.Height = size;
            window.UpdateLayout();
            box.InvalidateVisual();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            await SettleOpening(box);
        }

        Assert.Equal(1, _opens);
        Assert.NotNull(player);
        Assert.Same(player, box.Player);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Playback_Changed_OpensTheNewOne()
    {
        var (window, box) = Show();
        box.Playback = Playback("a.gif");
        await SettleOpening(box);
        var first = box.Player;
        var second = Playback("b.gif");

        box.Playback = second;
        await SettleOpening(box);
        DeliverHandlerMessages();

        Assert.Equal(2, _opens);
        Assert.Same(second, box.Player?.Playback);
        Assert.True(first?.IsDisposed);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Playback_SetToNull_LeavesNoPlayer()
    {
        var (window, box) = Show();
        box.Playback = Playback("a.gif");
        await SettleOpening(box);
        var player = box.Player;

        box.Playback = null;
        await SettleOpening(box);
        DeliverHandlerMessages();

        Assert.Null(box.Player);
        Assert.True(player?.IsDisposed);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Playback_ChangedBeforeOpenFinishes_DisposesTheStaleOne()
    {
        var (window, box) = Show();
        var gate = new TaskCompletionSource();
        var first = Playback("a.gif");
        AnimationPlayer? stale = null;
        box.OpenPlayer = playback =>
        {
            if (playback != first)
            {
                return CountingOpen(playback);
            }

            gate.Task.Wait();
            stale = CountingOpen(playback);
            return stale;
        };

        box.Playback = first;
        var firstOpening = box.Opening;
        var second = Playback("b.gif");
        box.Playback = second;
        gate.SetResult();
        await firstOpening;
        await SettleOpening(box);

        Assert.NotNull(stale);
        Assert.True(stale.IsDisposed);
        Assert.Same(second, box.Player?.Playback);
        window.Close();
    }

    [AvaloniaFact]
    public async Task OpenPlayer_Throws_LeavesNoPlayerAndKeepsSource()
    {
        var (window, box) = Show();
        var source = box.Source;
        box.OpenPlayer = _ => throw new IOException("Cannot open.");

        box.Playback = Playback("a.gif");
        var exception = await Record.ExceptionAsync(() => SettleOpening(box));

        Assert.Null(exception);
        Assert.Null(box.Player);
        Assert.Same(source, box.Source);
        window.Close();
    }

    [AvaloniaFact]
    public async Task DetachedFromTree_LeavesNoPlayer()
    {
        var (window, box) = Show();
        box.Playback = Playback("a.gif");
        await SettleOpening(box);
        var player = box.Player;

        window.Content = null;
        await SettleOpening(box);
        DeliverHandlerMessages();

        Assert.Null(box.Player);
        Assert.True(player?.IsDisposed);
        window.Close();
    }

    [AvaloniaFact]
    public async Task ReattachedToTree_OpensAgainForTheSamePlayback()
    {
        var (window, box) = Show();
        var playback = Playback("a.gif");
        box.Playback = playback;
        await SettleOpening(box);
        playback.Step(1);

        window.Content = null;
        await SettleOpening(box);
        window.Content = box;
        await SettleOpening(box);

        Assert.Equal(2, _opens);
        Assert.Same(playback, box.Player?.Playback);
        Assert.Equal(1, playback.CurrentFrame);
        window.Close();
    }

    [AvaloniaFact]
    public async Task OpenPlayer_RunsOffTheUIThread()
    {
        var (window, box) = Show();
        bool? onUIThread = null;
        box.OpenPlayer = playback =>
        {
            onUIThread = Dispatcher.UIThread.CheckAccess();
            return CountingOpen(playback);
        };

        box.Playback = Playback("a.gif");
        await SettleOpening(box);

        Assert.False(onUIThread);
        window.Close();
    }

    [AvaloniaFact]
    public void ImageViewerAndPreviewer_AnimatedImageShown_OnlyTheViewerHasAPlayback()
        => WithWindow((window, _, tab) =>
        {
            tab.Display(new ImageModel
            {
                FileInfo = new FileInfo("a.gif"),
                Image = new WriteableBitmap(new PixelSize(2, 2), new Vector(96, 96)),
                PixelWidth = 2,
                PixelHeight = 2,
                ImageType = ImageType.AnimatedGif
            });
            Settle(window);

            var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
            Assert.NotNull(tab.Animation.Primary.Value);
            Assert.Same(tab.Animation.Primary.Value, viewer.MainImage.Playback);
            // The harness window does not host the tab's view, so the boxes are looked for under the viewer
            var stillBoxes = viewer.GetLogicalDescendants()
                .OfType<PicBox>()
                .Where(box => box.FindLogicalAncestorOfType<ZoomPreviewer>() is not null
                              || box.FindLogicalAncestorOfType<CropControl>() is not null)
                .ToList();
            Assert.Equal(2, stillBoxes.Count);
            Assert.All(stillBoxes, box => Assert.Null(box.Playback));
        });
}
