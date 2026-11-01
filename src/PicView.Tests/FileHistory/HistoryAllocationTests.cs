using PicView.Core.FileHistory;

namespace PicView.Tests.FileHistory;

/// <summary>
/// A tab records every image it shows, so recording what it showed before must not allocate.
/// </summary>
[Collection("Sequential")]
public class HistoryAllocationTests : IDisposable
{
    private const int Records = 100;
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly string _configPath =
        Path.Combine(Path.GetTempPath(), "PicView_HistoryAllocation_" + Guid.NewGuid().ToString("N") + ".json");

    public HistoryAllocationTests()
    {
        SetDefaults();
        Settings.Navigation.IsFileHistoryEnabled = true;
        FileHistoryManager.Initialize(_configPath);
    }

    public void Dispose()
    {
        File.Delete(_configPath);
        _fileHistoryBackup.Dispose();
    }

    [Fact]
    public void FileHistoryAdd_PathsAlreadyListed_AllocatesNothing()
    {
        FileHistoryManager.Add("/path/a.jpg");
        FileHistoryManager.Add("/path/b.jpg");

        Assert.Equal(0, AllocatedBytes(() =>
        {
            FileHistoryManager.Add("/path/a.jpg");
            FileHistoryManager.Add("/path/b.jpg");
        }));
    }

    private static long AllocatedBytes(Action record)
    {
        record();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Records; i++)
        {
            record();
        }
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
