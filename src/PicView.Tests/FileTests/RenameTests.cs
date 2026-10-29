using ImageMagick;
using PicView.Avalonia.UI;
using PicView.Core.FileHandling;
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
