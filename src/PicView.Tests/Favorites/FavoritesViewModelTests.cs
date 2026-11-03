using PicView.Core.Favorites;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Favorites;

[Collection("Sequential")]
public class FavoritesViewModelTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "PicView_FavoritesVm_" + Guid.NewGuid().ToString("N"));
    private readonly MainWindowViewModel _window;
    private readonly FavoritesViewModel _favorites;

    public FavoritesViewModelTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        Directory.CreateDirectory(_tempRoot);
        FavoritesManager.Initialize(Path.Combine(_tempRoot, "Favorites.json"));
        _window = new MainWindowViewModel(null!, null!, null!, null!);
        _favorites = new FavoritesViewModel(_window);
    }

    public void Dispose()
    {
        _favorites.Dispose();
        try
        {
            Directory.Delete(_tempRoot, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task ShowFavorites_BeforeAnyUpdate_BuildsTheList()
    {
        await FavoritesManager.AddAsync(CreateFolder("A"));

        _favorites.ShowFavorites();

        Assert.Single(_favorites.Entries);
        Assert.True(_favorites.IsCached);
    }

    [Fact]
    public async Task ShowFavorites_WhenCached_KeepsTheSameEntries()
    {
        await FavoritesManager.AddAsync(CreateFolder("A"));
        _favorites.UpdateFavorites();
        var entry = Assert.Single(_favorites.Entries);
        var changes = 0;
        _favorites.Entries.CollectionChanged += (in _) => changes++;

        _favorites.ShowFavorites();

        Assert.Same(entry, Assert.Single(_favorites.Entries));
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task ShowFavorites_AfterInvalidate_RebuildsTheList()
    {
        await FavoritesManager.AddAsync(CreateFolder("A"));
        _favorites.UpdateFavorites();
        var entry = Assert.Single(_favorites.Entries);

        _favorites.Invalidate();
        Assert.False(_favorites.IsCached);
        _favorites.ShowFavorites();

        Assert.NotSame(entry, Assert.Single(_favorites.Entries));
        Assert.True(_favorites.IsCached);
    }

    [Fact]
    public async Task ShowFavorites_WhenCached_MarksTheActiveTabsFolder()
    {
        var a = CreateFolder("A");
        var b = CreateFolder("B");
        await FavoritesManager.AddAsync(a);
        await FavoritesManager.AddAsync(b);
        _favorites.UpdateFavorites();
        var before = _favorites.Entries.ToArray();

        _window.WindowTabs.ActiveTab.Value.Display(Model(Path.Combine(b, "x.jpg")));
        _favorites.ShowFavorites();

        Assert.Equal(before, _favorites.Entries);
        Assert.Equal([false, true], _favorites.Entries.Select(e => e.IsCurrentItem.CurrentValue));
    }

    private string CreateFolder(string name)
    {
        var path = Path.Combine(_tempRoot, name);
        Directory.CreateDirectory(path);
        return path;
    }

    private static ImageModel Model(string path) => new() { FileInfo = new FileInfo(path), Image = new object() };

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
