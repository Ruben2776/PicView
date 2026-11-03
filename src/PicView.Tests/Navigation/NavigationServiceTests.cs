using ImageMagick;
using PicView.Core.FileSorting;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

// The loads moved to the tab's Load, whose tests are TabLoadTests
[Collection("Sequential")]
public class NavigationServiceTests : IDisposable
{
    private readonly SharedImageCache _cache;
    private readonly MockThumbnailLoader _mockThumbnailLoader;
    private readonly NavigationService _navigationService;
    private readonly string _testDirectory;

    public NavigationServiceTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();

        _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);

        _cache = new SharedImageCache(file => ValueTask.FromResult(new ImageModel { FileInfo = file }));
        _mockThumbnailLoader = new MockThumbnailLoader();

        _navigationService = new NavigationService(string.CompareOrdinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task SortAsync_Descending_SortsTheFilesAndKeepsTheShownOne()
    {
        // Arrange
        var tab = CreateTab();
        var file1 = new FileInfo(Path.Combine(_testDirectory, "1.jpg"));
        var file2 = new FileInfo(Path.Combine(_testDirectory, "2.jpg"));
        var file3 = new FileInfo(Path.Combine(_testDirectory, "3.jpg"));

        await File.Create(file1.FullName).DisposeAsync();
        await File.Create(file2.FullName).DisposeAsync();
        await File.Create(file3.FullName).DisposeAsync();

        tab.Model = new ImageModel { FileInfo = file1 };
        tab.ImageIterator.Initialize([file1, file2, file3]);

        var cts = new CancellationTokenSource();

        // Act - sort descending
        Settings.Sorting.SortPreference = (int)SortFilesBy.Name;
        await _navigationService.SortAsync(tab, ascending: false, cts);

        // Assert
        Assert.Equal(3, tab.ImageIterator.Files.Count);
        Assert.Equal(file3.FullName, tab.ImageIterator.Files[0].FullName);
        Assert.Equal(file2.FullName, tab.ImageIterator.Files[1].FullName);
        Assert.Equal(file1.FullName, tab.ImageIterator.Files[2].FullName);

        Assert.Equal(2, tab.ImageIterator.CurrentIndex);
    }

    private TabViewModel CreateTab()
    {
        var dummyParent = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(dummyParent, TestTabServices.For(_cache, new MockThumbnailCache(), _mockThumbnailLoader));
        // Initialize with mocks to avoid null refs
        tab.Initialize();
        tab.ImageIterator.Initialize([]);
        return tab;
    }

    private class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) =>
            ValueTask.FromResult<object?>(null);

        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private class MockThumbnailCache : IThumbnailCache
    {
        public void Add(uint ownerId, string path, object thumbnail) { }
        public bool TryGet(string path, out object? thumbnail) { thumbnail = null; return false; }
        public void Remove(string path) { }
        public void RemoveOwner(uint ownerId) { }
        public void Clear() { }
        public bool IsEmpty => true;
    }

    private class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
