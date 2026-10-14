using PicView.Core.Favorites;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

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
    public async Task Version_ChangesOnEveryMutation()
    {
        var folderPath = Path.Combine(_tempRoot, "Photos");
        Directory.CreateDirectory(folderPath);
        var versions = new List<int> { FavoritesManager.Version };

        await FavoritesManager.AddAsync(folderPath, "Ctrl+D1");
        versions.Add(FavoritesManager.Version);
        FavoritesManager.SetKeybind(folderPath, "Ctrl+D2");
        versions.Add(FavoritesManager.Version);
        await FavoritesManager.RemoveAsync(folderPath);
        versions.Add(FavoritesManager.Version);
        await FavoritesManager.ClearAsync();
        versions.Add(FavoritesManager.Version);
        FavoritesManager.Initialize(_configPath);
        versions.Add(FavoritesManager.Version);

        Assert.Equal(versions.Count, versions.Distinct().Count());
    }

    [Fact]
    public async Task AddAsync_WithExistingDirectoryAndTrailingSeparator_NormalizesAndPersists()
    {
        var folderPath = Path.Combine(_tempRoot, "Photos");
        Directory.CreateDirectory(folderPath);

        await FavoritesManager.AddAsync(folderPath + Path.DirectorySeparatorChar, "Ctrl+1");

        Assert.Equal(1, FavoritesManager.Count);
        Assert.Equal(folderPath, FavoritesManager.AllEntries[0].FilePath);
        Assert.Equal("Ctrl+1", FavoritesManager.AllEntries[0].Keybind);
        Assert.True(FavoritesManager.Contains(folderPath));
        Assert.True(FavoritesManager.Contains(folderPath + Path.DirectorySeparatorChar));

        // Re-initialize from persisted file
        FavoritesManager.Initialize(_configPath);
        Assert.Equal(1, FavoritesManager.Count);
        Assert.Equal(folderPath, FavoritesManager.AllEntries[0].FilePath);
        Assert.Equal("Ctrl+1", FavoritesManager.AllEntries[0].Keybind);
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
        Assert.Equal("Alt+W", FavoritesManager.AllEntries[0].Keybind);
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
    public async Task AddAsync_DuplicateDirectory_UpdatesKeybindInPlace()
    {
        var folderPath = Path.Combine(_tempRoot, "Albums");
        Directory.CreateDirectory(folderPath);

        await FavoritesManager.AddAsync(folderPath, "Ctrl+1");
        await FavoritesManager.AddAsync(folderPath + Path.DirectorySeparatorChar, "Ctrl+9");

        Assert.Equal(1, FavoritesManager.Count);
        Assert.Equal("Ctrl+9", FavoritesManager.AllEntries[0].Keybind);
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

    [Fact]
    public async Task AddAsync_WithDuplicateKeybindAcrossDifferentFolders_EvictsPreviousKeybind()
    {
        var folderA = Path.Combine(_tempRoot, "FolderA");
        var folderB = Path.Combine(_tempRoot, "FolderB");
        Directory.CreateDirectory(folderA);
        Directory.CreateDirectory(folderB);

        await FavoritesManager.AddAsync(folderA, "Ctrl + D1");
        await FavoritesManager.AddAsync(folderB, "Ctrl + D1");

        Assert.Equal(2, FavoritesManager.Count);
        Assert.Equal(string.Empty, FavoritesManager.AllEntries[0].Keybind);
        Assert.Equal("Ctrl + D1", FavoritesManager.AllEntries[1].Keybind);

        // Re-initialize from disk to verify persistence of evicted Keybind
        FavoritesManager.Initialize(_configPath);
        Assert.Equal(2, FavoritesManager.Count);
        Assert.Equal(string.Empty, FavoritesManager.AllEntries[0].Keybind);
        Assert.Equal("Ctrl + D1", FavoritesManager.AllEntries[1].Keybind);
    }

    [Fact]
    public async Task SetKeybind_WhenAssigningExistingKeybindToEarlierEntry_EvictsLaterEntryOnSave()
    {
        var folderA = Path.Combine(_tempRoot, "FolderA");
        var folderB = Path.Combine(_tempRoot, "FolderB");
        Directory.CreateDirectory(folderA);
        Directory.CreateDirectory(folderB);

        await FavoritesManager.AddAsync(folderA, string.Empty);
        await FavoritesManager.AddAsync(folderB, "F9");

        FavoritesManager.SetKeybind(folderA, "F9");
        await FavoritesManager.SaveToFileAsync();

        Assert.Equal("F9", FavoritesManager.AllEntries[0].Keybind);
        Assert.Equal(string.Empty, FavoritesManager.AllEntries[1].Keybind);
    }

    [Fact]
    public void EditCommand_OnFavoritesEntryViewModel_ExecutesEditFavoritesDialogCommandWithPreselectedValues()
    {
        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var favoritesVm = new FavoritesViewModel(vm);
        vm.Favorites = favoritesVm;

        FavoriteEntry? editedEntry = null;
        favoritesVm.EditFavoritesDialogCommand.Subscribe(entry => editedEntry = entry);

        var entryVm = new FavoritesEntryViewModel();
        var folderPath = Path.Combine(_tempRoot, "MyFavoritesFolder");
        entryVm.Initialize(folderPath, "MyFavoritesFolder", "Ctrl + F", false, vm);

        entryVm.OpenEditDialogCommand.Execute(Unit.Default);

        Assert.NotNull(editedEntry);
        Assert.Equal(folderPath, editedEntry.Value.FilePath);
        Assert.Equal("Ctrl + F", editedEntry.Value.Keybind);
    }

    [Fact]
    public async Task AddOrEditAsync_WithExistingFilePath_UpdatesKeybindInPlaceAndEvictsDuplicates()
    {
        var folderA = Path.Combine(_tempRoot, "FolderA");
        var folderB = Path.Combine(_tempRoot, "FolderB");
        Directory.CreateDirectory(folderA);
        Directory.CreateDirectory(folderB);

        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var favoritesVm = new FavoritesViewModel(vm);
        vm.Favorites = favoritesVm;

        await favoritesVm.AddFavorite(folderA, "Ctrl+1");
        await favoritesVm.AddFavorite(folderB, "Ctrl+2");

        Assert.Equal(2, favoritesVm.Entries.Count);
        Assert.Equal("Ctrl+1", favoritesVm.Entries[0].Keybind.CurrentValue);
        Assert.Equal("Ctrl+2", favoritesVm.Entries[1].Keybind.CurrentValue);

        // Edit folderA to take Ctrl+2 (which should update folderA and evict duplicate from folderB)
        await favoritesVm.AddOrEditAsync(new FavoriteEntry(folderA, "Ctrl+2"));

        Assert.Equal(2, favoritesVm.Entries.Count);
        Assert.Equal("Ctrl+2", favoritesVm.Entries[0].Keybind.CurrentValue);
        Assert.Equal(string.Empty, favoritesVm.Entries[1].Keybind.CurrentValue);

        // Verify persistence in FavoritesManager
        Assert.Equal("Ctrl+2", FavoritesManager.AllEntries[0].Keybind);
        Assert.Equal(string.Empty, FavoritesManager.AllEntries[1].Keybind);
    }

    [Fact]
    public async Task AddOrEditAsync_WithNewFilePath_AddsNewFavoriteEntry()
    {
        var folder = Path.Combine(_tempRoot, "NewFolder");
        Directory.CreateDirectory(folder);

        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var favoritesVm = new FavoritesViewModel(vm);
        vm.Favorites = favoritesVm;

        Assert.Empty(favoritesVm.Entries);

        await favoritesVm.AddOrEditAsync(folder, "Alt+N");

        Assert.Single(favoritesVm.Entries);
        Assert.Equal(folder, favoritesVm.Entries[0].FilePath.CurrentValue);
        Assert.Equal("Alt+N", favoritesVm.Entries[0].Keybind.CurrentValue);
        Assert.True(FavoritesManager.Contains(folder));
    }

    [Fact]
    public void AddOrEditCommand_ExecutesAndUpsertsFavorite()
    {
        var folder = Path.Combine(_tempRoot, "UpsertFolder");
        Directory.CreateDirectory(folder);

        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var favoritesVm = new FavoritesViewModel(vm);
        vm.Favorites = favoritesVm;

        favoritesVm.AddOrEditCommand.Execute(new FavoriteEntry(folder, "Ctrl+U"));

        var spun = SpinWait.SpinUntil(() => favoritesVm.Entries.Count == 1, 3000);
        Assert.True(spun);
        Assert.Equal(folder, favoritesVm.Entries[0].FilePath.CurrentValue);
        Assert.Equal("Ctrl+U", favoritesVm.Entries[0].Keybind.CurrentValue);
    }
}
