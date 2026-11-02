using System.Collections.Concurrent;
using PicView.Core.Preloading;

namespace PicView.Tests.Preloading;

public class PreloaderTests
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    public PreloaderTests()
    {
        SetDefaults();
    }

    [Fact]
    public async Task Preload_AskedAgainWhileRunning_RunsAgainWithTheNewerListOnceItEnds()
    {
        var loaded = new ConcurrentQueue<string>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var preloader = new Preloader(async (_, file) =>
        {
            loaded.Enqueue(file.Name);
            await release.Task;
        });
        FileInfo[] two = [new("a.jpg"), new("b.jpg")];
        FileInfo[] three = [new("a.jpg"), new("b.jpg"), new("c.jpg")];

        preloader.Preload(1, two, 0, reversed: false, CancellationToken.None);
        await WaitUntil(() => loaded.Contains("b.jpg"));
        // A page was listed after the shown one while the first preload still runs, as an archive's pages are
        preloader.Preload(1, three, 0, reversed: false, CancellationToken.None);
        release.SetResult();

        await WaitUntil(() => loaded.Contains("c.jpg"));
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + WaitLimit;
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting");
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }
}
