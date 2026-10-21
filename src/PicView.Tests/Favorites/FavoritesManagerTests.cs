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

    private string CreateFolder(string name)
    {
        var path = Path.Combine(_tempRoot, name);
        Directory.CreateDirectory(path);
        return path;
    }

    private async Task<List<string>> AddFolders(int count)
    {
        var folders = new List<string>(count);
        for (var i = 1; i <= count; i++)
        {
            var folder = CreateFolder($"Folder{i}");
            await FavoritesManager.AddAsync(folder);
            folders.Add(folder);
        }

        return folders;
    }

    [Fact]
    public async Task AddAsync_WithExistingDirectoryAndTrailingSeparator_NormalizesAndPersists()
    {
        var folderPath = CreateFolder("Photos");

        await FavoritesManager.AddAsync(folderPath + Path.DirectorySeparatorChar);

        Assert.Equal(1, FavoritesManager.Count);
        Assert.Equal(folderPath, FavoritesManager.AllEntries[0].FilePath);
        Assert.True(FavoritesManager.Contains(folderPath));
        Assert.True(FavoritesManager.Contains(folderPath + Path.DirectorySeparatorChar));

        // Re-initialize from persisted file
        FavoritesManager.Initialize(_configPath);
        Assert.Equal(1, FavoritesManager.Count);
        Assert.Equal(folderPath, FavoritesManager.AllEntries[0].FilePath);
        Assert.Equal(1, FavoritesManager.AllEntries[0].Slot);
    }

    [Fact]
    public async Task AddAsync_WithExistingFilePath_BookmarksParentFolder()
    {
        var folderPath = CreateFolder("Wallpapers");
        var imageFile = Path.Combine(folderPath, "sunset.jpg");
        await File.WriteAllTextAsync(imageFile, "dummy", TestContext.Current.CancellationToken);

        await FavoritesManager.AddAsync(imageFile);

        Assert.Equal(1, FavoritesManager.Count);
        Assert.Equal(folderPath, FavoritesManager.AllEntries[0].FilePath);
    }

    [Fact]
    public async Task AddAsync_WithNonExistentDirectory_IgnoresEntry()
    {
        var missingPath = Path.Combine(_tempRoot, "DoesNotExist");

        var slot = await FavoritesManager.AddAsync(missingPath);

        Assert.Null(slot);
        Assert.Equal(0, FavoritesManager.Count);
        Assert.False(FavoritesManager.Contains(missingPath));
    }

    [Fact]
    public async Task AddAsync_TakesLowestFreeSlot()
    {
        var folders = await AddFolders(3);
        await FavoritesManager.RemoveAsync(folders[1]);

        var slot = await FavoritesManager.AddAsync(CreateFolder("Newcomer"));

        Assert.Equal(2, slot);
        Assert.Equal([1, 3, 2], FavoritesManager.AllEntries.Select(e => e.Slot));
    }

    [Fact]
    public async Task AddAsync_WhenAllSlotsAreUsed_AddsFavoriteWithoutSlot()
    {
        await AddFolders(FavoritesManager.MaxSlots);

        var slot = await FavoritesManager.AddAsync(CreateFolder("Extra"));

        Assert.Null(slot);
        Assert.Equal(FavoritesManager.MaxSlots + 1, FavoritesManager.Count);
        Assert.Null(FavoritesManager.AllEntries[^1].Slot);
        Assert.Null(FavoritesManager.NextFreeSlot());
    }

    [Fact]
    public async Task AddAsync_DuplicateDirectory_KeepsExistingSlotAndPosition()
    {
        var folders = await AddFolders(2);

        var slot = await FavoritesManager.AddAsync(folders[0] + Path.DirectorySeparatorChar);

        Assert.Equal(1, slot);
        Assert.Equal(2, FavoritesManager.Count);
        Assert.Equal(folders, FavoritesManager.AllEntries.Select(e => e.FilePath));
    }

    [Fact]
    public async Task RemoveAsync_SlotFreed_DoesNotPromoteFavoriteWithoutSlot()
    {
        var folders = await AddFolders(FavoritesManager.MaxSlots + 1);

        await FavoritesManager.RemoveAsync(folders[2]);

        Assert.Null(FavoritesManager.SlotOf(folders[^1]));
        Assert.Null(FavoritesManager.PathForSlot(3));
        Assert.Equal(3, FavoritesManager.NextFreeSlot());
    }

    [Fact]
    public async Task PathForSlot_And_SlotOf_FindEachOther()
    {
        var folders = await AddFolders(2);

        Assert.Equal(folders[1], FavoritesManager.PathForSlot(2));
        Assert.Equal(2, FavoritesManager.SlotOf(folders[1] + Path.DirectorySeparatorChar));
        Assert.Null(FavoritesManager.PathForSlot(3));
        Assert.Null(FavoritesManager.SlotOf(Path.Combine(_tempRoot, "NotAFavorite")));
    }

    [Fact]
    public async Task Initialize_WithInvalidOrDuplicateSlots_ClearsThem()
    {
        var a = CreateFolder("A");
        var b = CreateFolder("B");
        var c = CreateFolder("C");
        var d = CreateFolder("D");
        var json = $$"""
                     { "Entries": [
                       { "Slot": 0, "FilePath": {{JsonString(a)}} },
                       { "Slot": 2, "FilePath": {{JsonString(b)}}, "Keybind": "Ctrl+D1" },
                       { "Slot": 2, "FilePath": {{JsonString(c)}} },
                       { "Slot": 11, "FilePath": {{JsonString(d)}} }
                     ] }
                     """;
        await File.WriteAllTextAsync(_configPath, json, TestContext.Current.CancellationToken);

        FavoritesManager.Initialize(_configPath);
        await FavoritesManager.SaveToFileAsync();

        Assert.Equal([null, 2, null, null], FavoritesManager.AllEntries.Select(e => e.Slot));
        var saved = await File.ReadAllTextAsync(_configPath, TestContext.Current.CancellationToken);
        Assert.DoesNotContain("Keybind", saved, StringComparison.Ordinal);
    }

    private static string JsonString(string value) =>
        System.Text.Json.JsonSerializer.Serialize(value, FavoritesTestJson.Default.String);

    [Fact]
    public async Task RemoveAsync_ExistingAndDeletedFolder_RemovesEntry()
    {
        var folderPath = CreateFolder("ToRemove");

        await FavoritesManager.AddAsync(folderPath);
        Assert.Equal(1, FavoritesManager.Count);

        // Even if the directory is deleted on disk later, RemoveAsync still removes the bookmark
        Directory.Delete(folderPath);
        var removed = await FavoritesManager.RemoveAsync(folderPath);

        Assert.True(removed);
        Assert.Equal(0, FavoritesManager.Count);
    }

    [Fact]
    public async Task FavoritesViewModel_UpdateFavorites_SetsFolderNameSlotAndIsCurrentItem()
    {
        var folderA = CreateFolder("FolderA");
        var folderB = CreateFolder("FolderB");
        var activeFile = Path.Combine(folderB, "pic.png");
        await File.WriteAllTextAsync(activeFile, "img", TestContext.Current.CancellationToken);

        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(vm);
        tab.Model = new ImageModel { FileInfo = new FileInfo(activeFile) };
        vm.WindowTabs.ActiveTab.Value = tab;

        var favoritesVm = new FavoritesViewModel(vm);
        await favoritesVm.AddFavorite(folderA);
        await favoritesVm.AddFavorite(folderB + Path.DirectorySeparatorChar);

        Assert.Equal(2, favoritesVm.Entries.Count);
        Assert.Equal("FolderA", favoritesVm.Entries[0].FolderName.CurrentValue);
        Assert.Equal(1, favoritesVm.Entries[0].Slot.CurrentValue);
        Assert.False(favoritesVm.Entries[0].IsCurrentItem.CurrentValue);

        Assert.Equal("FolderB", favoritesVm.Entries[1].FolderName.CurrentValue);
        Assert.Equal(2, favoritesVm.Entries[1].Slot.CurrentValue);
        Assert.True(favoritesVm.Entries[1].IsCurrentItem.CurrentValue);
    }

    [Fact]
    public void EditCommand_OnFavoritesEntryViewModel_ExecutesEditFavoritesDialogCommandWithEntry()
    {
        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var favoritesVm = new FavoritesViewModel(vm);
        vm.Favorites = favoritesVm;

        FavoriteEntry? editedEntry = null;
        favoritesVm.EditFavoritesDialogCommand.Subscribe(entry => editedEntry = entry);

        var entryVm = new FavoritesEntryViewModel();
        var folderPath = Path.Combine(_tempRoot, "MyFavoritesFolder");
        entryVm.Initialize(folderPath, "MyFavoritesFolder", 4, false, vm);

        entryVm.OpenEditDialogCommand.Execute(Unit.Default);

        Assert.NotNull(editedEntry);
        Assert.Equal(folderPath, editedEntry.Value.FilePath);
        Assert.Equal(4, editedEntry.Value.Slot);
    }
}

[System.Text.Json.Serialization.JsonSerializable(typeof(string))]
internal partial class FavoritesTestJson : System.Text.Json.Serialization.JsonSerializerContext;
