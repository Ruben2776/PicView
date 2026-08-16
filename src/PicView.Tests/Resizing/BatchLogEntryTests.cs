using PicView.Core.Models;

namespace PicView.Tests.Resizing;

public class BatchLogEntryTests
{
    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var entry = new BatchLogEntry
        {
            FileName = "photo.jpg",
            OldSize = "2.5 MB",
            NewSize = "1.1 MB"
        };

        Assert.Equal("photo.jpg 2.5 MB -> 1.1 MB", entry.ToString());
    }

    [Fact]
    public void ToString_WithEmptyStrings_FormatsCorrectly()
    {
        var entry = new BatchLogEntry();

        Assert.Equal("  -> ", entry.ToString());
    }
}
