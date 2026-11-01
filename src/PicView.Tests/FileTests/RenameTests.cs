using ImageMagick;
using PicView.Avalonia.UI;
using PicView.Core.FileHandling;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Tests.FileTests;

[Collection("Sequential")]
public class RenameTests : IDisposable
{
    private readonly string _testDirectory;

    public RenameTests()
    {
        SetDefaults();
        _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewRenameTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void RenameFile_TargetExists_KeepsBothFiles()
    {
        var source = WriteText("a.txt", "source");
        var target = WriteText("b.txt", "target");

        var isRenamed = FileHelper.RenameFile(source, target);

        Assert.False(isRenamed);
        Assert.Equal("source", File.ReadAllText(source));
        Assert.Equal("target", File.ReadAllText(target));
    }

    [Fact]
    public void RenameFile_TargetFree_MovesTheFile()
    {
        var source = WriteText("a.txt", "source");
        var target = Path.Combine(_testDirectory, "b.txt");

        var isRenamed = FileHelper.RenameFile(source, target);

        Assert.True(isRenamed);
        Assert.False(File.Exists(source));
        Assert.Equal("source", File.ReadAllText(target));
    }

    [Fact]
    public void RenameFile_OnlyCaseChanges_RenamesTheFile()
    {
        var source = WriteText("a.txt", "source");
        var target = Path.Combine(_testDirectory, "A.txt");

        var isRenamed = FileHelper.RenameFile(source, target);

        Assert.True(isRenamed);
        Assert.Contains("A.txt", Directory.GetFiles(_testDirectory).Select(Path.GetFileName));
        Assert.Equal("source", File.ReadAllText(target));
    }

    [Fact]
    public async Task RenameAction_TargetFree_RenamesAndUpdatesTheTab()
    {
        var source = WriteImage("a.png", MagickColors.Red);
        var target = Path.Combine(_testDirectory, "b.png");
        var (vm, tab) = CreateTab(source);

        var isRenamed = await RenameHelper.RenameAction(vm, "b.png");

        Assert.True(isRenamed);
        Assert.False(File.Exists(source));
        Assert.True(File.Exists(target));
        Assert.Equal(target, tab.FileInfo.CurrentValue!.FullName);
        Assert.Null(vm.ToolTip!.ToolTipMessageSource.CurrentValue);
    }

    [Fact]
    public async Task RenameAction_SameExtensionTargetExists_KeepsBothFilesAndTheTab()
    {
        var source = WriteImage("a.png", MagickColors.Red);
        var target = WriteImage("b.png", MagickColors.Blue);
        var targetBytes = await File.ReadAllBytesAsync(target, TestContext.Current.CancellationToken);
        var (vm, tab) = CreateTab(source);

        var isRenamed = await RenameHelper.RenameAction(vm, "b.png");

        Assert.False(isRenamed);
        Assert.True(File.Exists(source));
        Assert.Equal(targetBytes, await File.ReadAllBytesAsync(target, TestContext.Current.CancellationToken));
        Assert.Equal(source, tab.FileInfo.CurrentValue!.FullName);
        Assert.Contains("b.", vm.ToolTip!.ToolTipMessageSource.CurrentValue);
    }

    [Fact]
    public async Task RenameAction_NewExtensionTargetExists_KeepsBothFilesAndTheTab()
    {
        var source = WriteImage("a.png", MagickColors.Red);
        var target = WriteImage("b.jpg", MagickColors.Blue);
        var targetBytes = await File.ReadAllBytesAsync(target, TestContext.Current.CancellationToken);
        var (vm, tab) = CreateTab(source);

        var isRenamed = await RenameHelper.RenameAction(vm, "b.jpg");

        Assert.False(isRenamed);
        Assert.True(File.Exists(source));
        Assert.Equal(targetBytes, await File.ReadAllBytesAsync(target, TestContext.Current.CancellationToken));
        Assert.Equal(source, tab.FileInfo.CurrentValue!.FullName);
        Assert.Contains("b.", vm.ToolTip!.ToolTipMessageSource.CurrentValue);
    }

    [Fact]
    public async Task RenameAction_SameExtension_KeepsTheTabOnItsEntryUnderTheNewName()
    {
        var after = WriteImage("c.png", MagickColors.Blue);
        var (vm, tab) = CreateTabInCore(WriteImage("a.png", MagickColors.Red), after);

        await RenameHelper.RenameAction(vm, "b.png");

        Assert.Equal([Path.Combine(_testDirectory, "b.png"), after], tab.ViewHistory.Entries.Select(e => e.Path));
        Assert.Equal(0, tab.ViewHistory.Cursor);
    }

    [Fact]
    public async Task RenameAction_NewExtension_KeepsTheTabOnItsEntryUnderTheNewName()
    {
        // The converted file loads and shows. Had the history not followed the rename first, that showing would be a new
        // view, which drops the entry after the cursor
        var after = WriteImage("c.png", MagickColors.Blue);
        var (vm, tab) = CreateTabInCore(WriteImage("a.png", MagickColors.Red), after);

        await RenameHelper.RenameAction(vm, "b.jpg");

        Assert.Equal([Path.Combine(_testDirectory, "b.jpg"), after], tab.ViewHistory.Entries.Select(e => e.Path));
        Assert.Equal(0, tab.ViewHistory.Cursor);
        Assert.Equal(Path.Combine(_testDirectory, "b.jpg"), tab.Model.FileInfo?.FullName);
    }

    // The active tab of a Main Window in a core whose navigation loads files, as in PicView. The tab showed path, then
    // after, and went back to path
    private (MainWindowViewModel Vm, TabViewModel Tab) CreateTabInCore(string path, string after)
    {
        var core = new CoreViewModel(null, file => ValueTask.FromResult(new ImageModel { FileInfo = file, Image = new object() }));
        var thumbnails = new NoThumbnails();
        var folderWatcher = new NoFolderWatcher();
        // The navigation needs no image loader or platform for a file in a folder
        core.InitializeNavigation(new NavigationService(null!, core.SharedCache, folderWatcher, null!, string.CompareOrdinal),
            thumbnails, folderWatcher);
        var vm = new MainWindowViewModel(core, new TranslationViewModel(), null!, new GlobalSettingsViewModel(),
            new GallerySharedSettingsViewModel()) { ToolTip = new ToolTipViewModel() };
        core.MainWindows.MainWindows.Add(vm);
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Initialize(core.SharedCache, core.SharedThumbnailCache, thumbnails);
        tab.Display(new ImageModel { FileInfo = new FileInfo(path), Image = new object() });
        tab.Display(new ImageModel { FileInfo = new FileInfo(after), Image = new object() });
        var back = tab.ViewHistory.BeginStep(forwards: false)!.Value;
        tab.Display(new ImageModel { FileInfo = new FileInfo(path), Image = new object() });
        tab.ViewHistory.EndStep(back);
        return (vm, tab);
    }

    private sealed class NoFolderWatcher : IFileWatcherService
    {
        public void Watch(TabViewModel tab, string? directory = null) { }
        public void Unwatch(TabViewModel tab) { }
        public void RestartWatches() { }
    }

    private sealed class NoThumbnails : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) =>
            ValueTask.FromResult<object?>(null);
        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private (MainWindowViewModel Vm, TabViewModel Tab) CreateTab(string path)
    {
        var vm = new MainWindowViewModel(null!, new TranslationViewModel(), null!, new GlobalSettingsViewModel(),
            new GallerySharedSettingsViewModel()) { ToolTip = new ToolTipViewModel() };
        var tab = vm.WindowTabs.ActiveTab.Value;
        var fileInfo = new FileInfo(path);
        tab.FileInfo.Value = fileInfo;
        tab.Model.FileInfo = fileInfo;
        return (vm, tab);
    }

    private string WriteText(string name, string content)
    {
        var path = Path.Combine(_testDirectory, name);
        File.WriteAllText(path, content);
        return path;
    }

    private string WriteImage(string name, MagickColor color)
    {
        var path = Path.Combine(_testDirectory, name);
        using var image = new MagickImage(color, 4, 4);
        image.Write(path);
        return path;
    }
}
