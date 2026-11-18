using PicView.Core.Config;

namespace PicView.Tests.Config;

public class CoalescedSaveTests
{
    [Fact]
    public async Task Soon_FiveRequestsWhileOneSaveRuns_SavesOnceMore()
    {
        var saves = 0;
        var gate = new TaskCompletionSource<bool>();
        var save = new CoalescedSave(async () => { saves++; return await gate.Task.ConfigureAwait(false); }, "test");

        for (var i = 0; i < 5; i++)
        {
            save.Soon();
        }
        Assert.Equal(1, saves);
        gate.SetResult(true);
        await save.Saving;

        Assert.Equal(2, saves);
    }

    [Fact]
    public async Task Soon_SaveThrows_NextRequestSavesAgain()
    {
        var saves = 0;
        var save = new CoalescedSave(() => { saves++; throw new IOException("disk full"); }, "test");

        save.Soon();
        await save.Saving;
        save.Soon();
        await save.Saving;

        Assert.Equal(2, saves);
    }

    [Fact]
    public void Soon_SaveEndsAtOnce_SavesOncePerRequest()
    {
        var saves = 0;
        var save = new CoalescedSave(() => { saves++; return ValueTask.FromResult(true); }, "test");

        save.Soon();
        save.Soon();

        Assert.Equal(2, saves);
    }
}
