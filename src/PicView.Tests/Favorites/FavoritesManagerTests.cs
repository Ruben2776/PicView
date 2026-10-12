using PicView.Core.Favorites;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Tests.Favorites;

[Collection("Sequential")]
public class FavoritesManagerTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _configPath;

    public FavoritesManagerTests()
    {
        SetDefaults();
        _tempRoot = Path.Combine(Path.GetTempPath(), "PicView_FavoritesTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _configPath = Path.Combine(_tempRoot, "Favorites.json");
        FavoritesManager.Initialize(_configPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try
            {
                Directory.Delete(_tempRoot, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task AddAsync_WithExistingDirectoryAndTrailingSeparator_NormalizesAndPersists()
    {
        var folderPath = Path.Combine(_tempRoot, "Photos");
        Directory.CreateDirectory(folderPath);

        await FavoritesManager.AddAsync(folderPath + Path.DirectorySeparatorChar, "Ctrl+1");

        Assert.Equal(1, FavoritesManager.Count);
        Assert.Equal(folderPath, FavoritesManager.AllEntries[0].FilePath);
        Assert.Equal("Ctrl+1", FavoritesManager.AllEntries[0].HotKey);
        Assert.True(FavoritesManager.Contains(folderPath));
        Assert.True(FavoritesManager.Contains(folderPath + Path.DirectorySeparatorChar));

        // Re-initialize from persisted file
        FavoritesManager.Initialize(_configPath);
        Assert.Equal(1, FavoritesManager.Count);
        Assert.Equal(folderPath, FavoritesManager.AllEntries[0].FilePath);
        Assert.Equal("Ctrl+1", FavoritesManager.AllEntries[0].HotKey);
    }

    [Fact]
    public async Task AddAsync_WithExistingFilePath_BookmarksParentFolder()
    {
        var folderPath = Path.Combine(_tempRoot, "Wallpapers");
        Directory.CreateDirectory(folderPath);
        var imageFile = Path.Combine(folderPath, "sunset.jpg");
        await File.WriteAllTextAsync(imageFile, "dummy", TestContext.Current.CancellationToken);

        await FavoritesManager.AddAsync(imageFile, "Alt+W");

        Assert.Equal(1, FavoritesManager.Count);
        Assert.Equal(folderPath, FavoritesManager.AllEntries[0].FilePath);
        Assert.Equal("Alt+W", FavoritesManager.AllEntries[0].HotKey);
    }

    [Fact]
    public async Task AddAsync_WithNonExistentDirectory_IgnoresEntry()
    {
        var missingPath = Path.Combine(_tempRoot, "DoesNotExist");

        await FavoritesManager.AddAsync(missingPath, "Ctrl+2");

        Assert.Equal(0, FavoritesManager.Count);
        Assert.False(FavoritesManager.Contains(missingPath));
    }

    [Fact]
    public async Task AddAsync_DuplicateDirectory_UpdatesHotKeyInPlace()
    {
        var folderPath = Path.Combine(_tempRoot, "Albums");
        Directory.CreateDirectory(folderPath);

        await FavoritesManager.AddAsync(folderPath, "Ctrl+1");
        await FavoritesManager.AddAsync(folderPath + Path.DirectorySeparatorChar, "Ctrl+9");

        Assert.Equal(1, FavoritesManager.Count);
        Assert.Equal("Ctrl+9", FavoritesManager.AllEntries[0].HotKey);
    }

    [Fact]
    public async Task RemoveAsync_ExistingAndDeletedFolder_RemovesEntry()
    {
        var folderPath = Path.Combine(_tempRoot, "ToRemove");
        Directory.CreateDirectory(folderPath);

        await FavoritesManager.AddAsync(folderPath);
        Assert.Equal(1, FavoritesManager.Count);

        // Even if the directory is deleted on disk later, RemoveAsync still removes the bookmark
        Directory.Delete(folderPath);
        var removed = await FavoritesManager.RemoveAsync(folderPath);

        Assert.True(removed);
        Assert.Equal(0, FavoritesManager.Count);
    }

    [Fact]
    public async Task FavoritesViewModel_UpdateFavorites_SetsFolderNameAndIsCurrentItem()
    {
        var folderA = Path.Combine(_tempRoot, "FolderA");
        var folderB = Path.Combine(_tempRoot, "FolderB");
        Directory.CreateDirectory(folderA);
        Directory.CreateDirectory(folderB);
        var activeFile = Path.Combine(folderB, "pic.png");
        await File.WriteAllTextAsync(activeFile, "img", TestContext.Current.CancellationToken);

        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(null!, vm);
        tab.Model = new ImageModel { FileInfo = new FileInfo(activeFile) };
        vm.WindowTabs.ActiveTab.Value = tab;

        var favoritesVm = new FavoritesViewModel(vm);
        await favoritesVm.AddFavorite(folderA, "F1");
        await favoritesVm.AddFavorite(folderB + Path.DirectorySeparatorChar, "F2");

        Assert.Equal(2, favoritesVm.Entries.Count);
        Assert.Equal("FolderA", favoritesVm.Entries[0].FolderName.CurrentValue);
        Assert.False(favoritesVm.Entries[0].IsCurrentItem.CurrentValue);

        Assert.Equal("FolderB", favoritesVm.Entries[1].FolderName.CurrentValue);
        Assert.True(favoritesVm.Entries[1].IsCurrentItem.CurrentValue);
    }
}
