using ImageMagick;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.Main;
using PicView.Core.FileHandling;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Saving;
using PicView.Core.ViewModels;
using PicView.Tests.Config;
using PicView.Tests.Navigation;
using PicView.Tests.Saving;
using Orientation = PicView.Core.ImageTransformations.Orientation;

namespace PicView.Tests.FileTests;

[Collection("Sequential")]
public class ConvertAndRenameTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewConvertTests_" + Guid.NewGuid());
    private readonly RecordingPlatform _platform = new();
    private readonly List<IDisposable> _disposables = [];
    private readonly bool _wasReset;

    public ConvertAndRenameTests()
    {
        SetDefaults();
        _wasReset = Settings.Zoom.ResetZoomOnChange;
        Settings.Zoom.ResetZoomOnChange = false;
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        Settings.Zoom.ResetZoomOnChange = _wasReset;
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Best effort: a background preload of the tab's folder can still hold a file for a moment
        }
    }

    [Fact]
    public async Task Convert_Save_ReplacesTheOriginal()
    {
        var source = WriteImage("a.png", MagickColors.Red, 2, 1);
        var tab = await LoadTabAsync(source);
        var destination = Path.Combine(_testDirectory, "a.jpg");

        var result = await ConvertWriter.ConvertAsync(new ShownFileWriter(() => null), tab, destination, false);

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.False(File.Exists(source));
        Assert.Contains(_platform.Deleted, d => d.Path.EndsWith("a.png", StringComparison.Ordinal));
        Assert.Equal(destination, tab.Model.FileInfo?.FullName);
    }

    [Fact]
    public async Task Convert_NameTaken_KeepsEverything()
    {
        var source = WriteImage("a.png", MagickColors.Red, 2, 1);
        var taken = WriteImage("a.jpg", MagickColors.Green, 3, 3);
        var tab = await LoadTabAsync(source);

        var result = await ConvertWriter.ConvertAsync(new ShownFileWriter(() => null), tab, taken, false);

        Assert.Equal(WriteOutcome.NameTaken, result.Outcome);
        Assert.True(File.Exists(source));
        Assert.Empty(_platform.Deleted);
    }

    [Fact]
    public async Task Convert_SaveAs_KeepsTheOriginal()
    {
        var source = WriteImage("a.png", MagickColors.Red, 2, 1);
        var tab = await LoadTabAsync(source);
        var destination = Path.Combine(_testDirectory, "copy.jpg");

        var result = await ConvertWriter.ConvertAsync(new ShownFileWriter(() => null), tab, destination, true);

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.True(File.Exists(source));
        Assert.True(File.Exists(destination));
        Assert.Empty(_platform.Deleted);
        Assert.Equal(source, tab.FileInfo.CurrentValue!.FullName);
    }

    [Fact]
    public async Task Convert_NoConversion_ShowsTheNewPixels()
    {
        var source = WriteImage("a.png", MagickColors.Red, 2, 1);
        var tab = await LoadTabAsync(source);
        tab.Orientation.TurnRight();

        var result = await ConvertWriter.ConvertAsync(new ShownFileWriter(() => null), tab, source, false);

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        AssertSize(source, 1, 2);
        Assert.Equal(Orientation.Upright, tab.Orientation.Current.CurrentValue);
        Assert.Single(_platform.Deleted);
    }

    [Fact]
    public async Task Convert_NoConversionUnchanged_WritesNothing()
    {
        var source = WriteImage("a.png", MagickColors.Red, 2, 1);
        var tab = await LoadTabAsync(source);
        var old = await File.ReadAllBytesAsync(source, TestContext.Current.CancellationToken);

        var result = await ConvertWriter.ConvertAsync(new ShownFileWriter(() => null), tab, source, false);

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.Empty(_platform.Deleted);
        Assert.Equal(old, await File.ReadAllBytesAsync(source, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Convert_SingleImage_DoesNotThrow()
    {
        var tab = CreateTab();
        var writer = new ShownFileWriter(() => null) { SingleImagePixels = new FixedPixels(3, 2) };
        var destination = Path.Combine(_testDirectory, "single.png");

        var result = await ConvertWriter.ConvertAsync(writer, tab, destination, false);

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        AssertSize(destination, 3, 2);
    }

    [Fact]
    public async Task Convert_Turned_WritesTheTurn()
    {
        var source = WriteImage("a.png", MagickColors.Red, 2, 1);
        var tab = await LoadTabAsync(source);
        tab.Orientation.TurnRight();
        var destination = Path.Combine(_testDirectory, "a.jpg");

        var result = await ConvertWriter.ConvertAsync(new ShownFileWriter(() => null), tab, destination, false);

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.True(result.TurnApplied);
        AssertSize(destination, 1, 2);
        Assert.Equal(Orientation.Upright, tab.Orientation.Current.CurrentValue);
    }

    [Fact]
    public async Task RenameConvert_ShowsUprightAndRecyclesTheOriginal()
    {
        var source = WriteImage("a.png", MagickColors.Red, 2, 1);
        var (vm, tab) = CreateTabInCore(source);
        tab.Orientation.TurnRight();

        var isRenamed = await RenameHelper.RenameAction(vm, "b.jpg");

        var newPath = Path.Combine(_testDirectory, "b.jpg");
        Assert.True(isRenamed);
        Assert.False(File.Exists(source));
        Assert.Contains(_platform.Deleted, d => d.Path.EndsWith("a.png", StringComparison.Ordinal));
        AssertSize(newPath, 1, 2);
        Assert.Equal(newPath, tab.Model.FileInfo?.FullName);
        Assert.Equal(Orientation.Upright, tab.Orientation.Current.CurrentValue);
    }

    private (MainWindowViewModel Vm, TabViewModel Tab) CreateTabInCore(string path)
    {
        var core = new CoreViewModel(null, file => new ImageModel { FileInfo = file, Image = new object() });
        core.InitializeNavigation(TestTabServices.For(core.SharedCache, platform: _platform), new NavigationService());
        var vm = new MainWindowViewModel(core, new TranslationViewModel(), StartedSettings.New(),
            new GallerySharedSettingsViewModel()) { ToolTip = new ToolTipViewModel() };
        core.MainWindows.MainWindows.Add(vm);
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Initialize();
        tab.Display(new ImageModel { FileInfo = new FileInfo(path), Image = new object() });
        return (vm, tab);
    }

    private async Task<TabViewModel> LoadTabAsync(string path)
    {
        var tab = CreateTab();
        await tab.Load.LoadAsync(path);
        return tab;
    }

    private TabViewModel CreateTab()
    {
        var cache = new SharedImageCache(file => new ImageModel { FileInfo = file });
        var services = TestTabServices.For(cache, platform: _platform);
        var tab = new TabViewModel(new MainWindowViewModel(null!, null!, null!, null!), services);
        _disposables.Add(tab);
        return tab;
    }

    private string WriteImage(string name, MagickColor color, uint width, uint height)
    {
        var path = Path.Combine(_testDirectory, name);
        using var image = new MagickImage(color, width, height);
        image.Write(path);
        return path;
    }

    private static void AssertSize(string path, uint width, uint height)
    {
        using var image = new MagickImage(path);
        Assert.Equal((width, height), (image.Width, image.Height));
    }

    private sealed class FixedPixels(uint width, uint height) : ISingleImagePixels
    {
        public MagickImage? Read(TabViewModel tab) => new(MagickColors.Green, width, height);
    }
}
