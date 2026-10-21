using ImageMagick;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Navigation.Tiff;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class TiffNavigationTests : IDisposable
{
    private readonly ImageIterator _iterator;
    private readonly SharedImageCache _cache;
    private readonly Dictionary<string, ImageModel> _models = new();
    private readonly TabViewModel _tab;
    private readonly List<FileInfo> _files;
    private readonly MockThumbnailCache _mockThumbnailCache;

    public TiffNavigationTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();

        SetDefaults();
        TranslationManager.Init();
        Settings.Navigation.IsFileHistoryEnabled = false;

        _files =
        [
            new FileInfo("file0.jpg"),
            new FileInfo("file1.tiff"),
            new FileInfo("file2.png")
        ];

        _cache = new SharedImageCache(f =>
            ValueTask.FromResult(_models.TryGetValue(f.FullName, out var model) ? model : new ImageModel { FileInfo = f }));
        _mockThumbnailCache = new MockThumbnailCache();
        _tab = new TabViewModel(null!);
        _iterator = new ImageIterator(_cache, _mockThumbnailCache, new MockThumbnailLoader(), _tab);
        _iterator.Initialize(_files);
    }

    [Fact]
    public async Task Next_EntersTiff_AtFirstPage()
    {
        // Arrange: Start at file0 (Index 0).
        // Target: file1 (Index 1) is a TIFF.
        
        // Mock the TIFF model for Index 1
        var tiffModel = CreateTiffModel(_files[1], 3); // 3 pages
        SetModel(1, tiffModel);
        _tab.Model = new ImageModel();

        // Act: Navigate Next (0 -> 1)
        await _iterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource());

        // Assert
        Assert.Equal(1, _iterator.CurrentIndex);
        Assert.Equal(tiffModel, _tab.Model);
        Assert.Equal(0, tiffModel.TiffNavigation!.CurrentPage);
    }
    
    [Fact]
    public async Task Previous_EntersTiff_AtLastPage()
    {
        // Arrange: Start at file2 (Index 2).
        _iterator.SetCurrentIndex(2);
        _tab.Model = new ImageModel { FileInfo = _files[2] };

        // Target: file1 (Index 1) is a TIFF.
        var tiffModel = CreateTiffModel(_files[1], 3);
        SetModel(1, tiffModel);

        // Act: Navigate Prev (2 -> 1)
        await _iterator.NavigateAsync(NavigateTo.Previous, SkipAmount.One, new CancellationTokenSource());

        // Assert
        Assert.Equal(1, _iterator.CurrentIndex);
        Assert.Equal(tiffModel, _tab.Model);
        Assert.Equal(2, tiffModel.TiffNavigation!.CurrentPage); // Last page (Count - 1)
    }
    
    [Fact]
    public async Task Next_InsideTiff_AdvancesPage_AndStaysOnFile()
    {
        // Arrange: Start at file1 (Index 1), Page 0
        var tiffModel = CreateTiffModel(_files[1], 3);
        tiffModel.TiffNavigation!.CurrentPage = 0;
        
        _iterator.SetCurrentIndex(1);
        _tab.Model = tiffModel;
        SetModel(1, tiffModel); // Cache should return same model

        // Act: Navigate Next (Target would be 2 normally)
        await _iterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource());

        // Assert
        Assert.Equal(1, _iterator.CurrentIndex); // Should stay on 1
        Assert.Equal(1, tiffModel.TiffNavigation.CurrentPage); // Page 0 -> 1
    }

    [Fact]
    public async Task Next_InsideTiff_LastPage_GoesToNextFile()
    {
        // Arrange: Start at file1 (Index 1), Page 2 (Last)
        var tiffModel = CreateTiffModel(_files[1], 3);
        tiffModel.TiffNavigation!.CurrentPage = 2;
        
        _iterator.SetCurrentIndex(1);
        _tab.Model = tiffModel;
        
        var nextModel = new ImageModel { FileInfo = _files[2] };
        SetModel(2, nextModel);

        // Act: Navigate Next (Target 2)
        await _iterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource());

        // Assert
        Assert.Equal(2, _iterator.CurrentIndex); // Moved to 2
        Assert.Equal(nextModel, _tab.Model);
    }
    
    [Fact]
    public async Task Previous_InsideTiff_DecrementsPage_AndStaysOnFile()
    {
        // Arrange: Start at file1 (Index 1), Page 1
        var tiffModel = CreateTiffModel(_files[1], 3);
        tiffModel.TiffNavigation!.CurrentPage = 1;
        
        _iterator.SetCurrentIndex(1);
        _tab.Model = tiffModel;
        SetModel(1, tiffModel);

        // Act: Navigate Prev (Target 0)
        await _iterator.NavigateAsync(NavigateTo.Previous, SkipAmount.One, new CancellationTokenSource());

        // Assert
        Assert.Equal(1, _iterator.CurrentIndex); // Stay on 1
        Assert.Equal(0, tiffModel.TiffNavigation.CurrentPage); // Page 1 -> 0
    }
    
    [Fact]
    public async Task Previous_InsideTiff_FirstPage_GoesToPrevFile()
    {
        // Arrange: Start at file1 (Index 1), Page 0
        var tiffModel = CreateTiffModel(_files[1], 3);
        tiffModel.TiffNavigation!.CurrentPage = 0;
        
        _iterator.SetCurrentIndex(1);
        _tab.Model = tiffModel;
        
        var prevModel = new ImageModel { FileInfo = _files[0] };
        SetModel(0, prevModel);

        // Act: Navigate Prev (Target 0)
        await _iterator.NavigateAsync(NavigateTo.Previous, SkipAmount.One, new CancellationTokenSource());

        // Assert
        Assert.Equal(0, _iterator.CurrentIndex); // Moved to 0
        Assert.Equal(prevModel, _tab.Model);
    }

    private ImageModel CreateTiffModel(FileInfo file, int pageCount)
    {
        var model = new ImageModel { FileInfo = file };
        var pages = new MagickImageCollection();
        for (int i = 0; i < pageCount; i++)
        {
            pages.Add(new MagickImage(MagickColors.White, 1, 1));
        }
        
        model.TiffNavigation = new TiffNavigationInfo
        {
            PageCount = pageCount,
            CurrentPage = 0,
            Pages = [pages]
        };
        model.Image = pages[0];
        return model;
    }

    public void Dispose()
    {
        
    }

    // Mocks

    private void SetModel(int index, ImageModel model) => _models[_files[index].FullName] = model;

    private class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) => ValueTask.FromResult<object?>(null);
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
