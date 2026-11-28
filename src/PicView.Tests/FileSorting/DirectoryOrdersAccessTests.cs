using PicView.Core.FileSorting;

namespace PicView.Tests.FileSorting;

/// <summary>
/// Whether there is a file manager to follow, and whether the user lets PicView ask it (ADR-0050).
/// </summary>
public class DirectoryOrdersAccessTests
{
    [Fact]
    public void CanFollow_OnlyWithAFileManager()
    {
        Assert.False(new DirectoryOrders().CanFollow);
        Assert.True(new DirectoryOrders(new FileManager()).CanFollow);
    }

    [Fact]
    public void RequestAccess_WithNoFileManager_IsGranted() =>
        Assert.True(new DirectoryOrders().RequestAccess());

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RequestAccess_IsTheFileManagersAnswer(bool allows) =>
        Assert.Equal(allows, new DirectoryOrders(new FileManager { Allows = allows }).RequestAccess());

    [Fact]
    public void RequestAccess_FileManagerThatFails_IsNotARefusal() =>
        Assert.True(new DirectoryOrders(new FileManager { Throws = true }).RequestAccess());

    [Fact]
    public void RequestAccess_SourceThatDoesNotSay_IsGranted() =>
        Assert.True(new DirectoryOrders(new SilentFileManager()).RequestAccess());

    private sealed class FileManager : IFolderOrderSource
    {
        public bool Allows { get; init; } = true;
        public bool Throws { get; init; }

        public FileOrder? OrderOf(string directory) => null;

        public bool RequestAccess() => Throws ? throw new InvalidOperationException("No file manager") : Allows;
    }

    // As Windows Explorer's source, which needs no consent
    private sealed class SilentFileManager : IFolderOrderSource
    {
        public FileOrder? OrderOf(string directory) => null;
    }
}
