using ImageMagick;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Saving;
using PicView.Core.ViewModels;
using PicView.Tests.Config;
using PicView.Tests.Navigation;
using R3;
using Orientation = PicView.Core.ImageTransformations.Orientation;

namespace PicView.Tests.Resizing;

/// <summary>
/// The resize dialog's Save and Save As, which write through the save module.
/// </summary>
[Collection("Sequential")]
public class ResizeImageViewModelSaveTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewResizeTests_" + Guid.NewGuid());
    private readonly List<IDisposable> _disposables = [];
    private readonly FrameProvider _previousFrameProvider = ObservableSystem.DefaultFrameProvider;

    public ResizeImageViewModelSaveTests()
    {
        SetDefaults();
        // The dialog follows the shown image each frame; no frame runs here, so the size it asks for is the test's
        ObservableSystem.DefaultFrameProvider = new NoFrames();
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        ObservableSystem.DefaultFrameProvider = _previousFrameProvider;
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
    public async Task SaveImage_SameFormatTurned_TurnsTheFileOnceAcrossTwoSaves()
    {
        var path = WriteImage("a.png", 4, 2);
        var (_, resizeVm, tab) = await CreateAsync(path);
        tab.Orientation.TurnRight();
        AskFor(resizeVm, 2, 4);

        await resizeVm.SaveImage();
        await resizeVm.SaveImage();

        // Turned a second time, the 4x2 file would fit the 2x4 box as 2x1
        AssertSize(path, 2, 4);
        Assert.Equal(Orientation.Upright, tab.Orientation.Current.CurrentValue);
    }

    [Fact]
    public async Task SaveImage_SameFormat_ShowsTheNewPixels()
    {
        var path = WriteImage("a.png", 4, 2);
        var (_, resizeVm, tab) = await CreateAsync(path);
        AskFor(resizeVm, 2, 1);

        await resizeVm.SaveImage();

        AssertSize(path, 2, 1);
        Assert.Equal(path, tab.Model.FileInfo?.FullName);
        Assert.Equal((2u, 1u), (tab.Model.PixelWidth, tab.Model.PixelHeight));
    }

    [Fact]
    public async Task SaveImage_OtherFormatTurned_ShowsUprightWithResetZoomOff()
    {
        var wasReset = Settings.Zoom.ResetZoomOnChange;
        Settings.Zoom.ResetZoomOnChange = false;
        try
        {
            var path = WriteImage("a.png", 4, 2);
            var (_, resizeVm, tab) = await CreateAsync(path);
            tab.Orientation.TurnRight();
            AskFor(resizeVm, 2, 4);
            // WebP
            resizeVm.SelectedConversionIndex.Value = 3;

            await resizeVm.SaveImage();

            var converted = Path.ChangeExtension(path, ".webp");
            Assert.Equal(converted, tab.Model.FileInfo?.FullName);
            Assert.Equal(Orientation.Upright, tab.Orientation.Current.CurrentValue);
            Assert.False(File.Exists(path));
            AssertSize(converted, 2, 4);
        }
        finally
        {
            Settings.Zoom.ResetZoomOnChange = wasReset;
        }
    }

    [Fact]
    public async Task SaveImage_SingleImage_AsksForAPathAndWrites()
    {
        var (mainVm, resizeVm, tab) = await CreateAsync(null);
        mainVm.Core.Writer.SingleImagePixels = new FixedPixels(4, 2);
        var destination = Path.Combine(_testDirectory, "single.png");
        var asked = 0;
        resizeVm.PickFileAction = (_, _) =>
        {
            asked++;
            return ValueTask.FromResult<string?>(destination);
        };
        var closed = false;
        resizeVm.CloseAction = () => closed = true;
        AskFor(resizeVm, 2, 1);

        await resizeVm.SaveImage();

        Assert.Equal(1, asked);
        AssertSize(destination, 2, 1);
        Assert.True(closed);
        Assert.Null(tab.FileInfo.CurrentValue);
    }

    [Fact]
    public async Task SaveImage_WithEffects_KeepsThemInTheFile()
    {
        var path = WriteImage("a.png", 4, 2);
        var (mainVm, resizeVm, tab) = await CreateAsync(path);
        mainVm.Core.Effects = await RenderBlueAsync(tab);
        AskFor(resizeVm, 2, 1);

        await resizeVm.SaveImage();

        AssertSize(path, 2, 1);
        Assert.Equal(Rgb(MagickColors.Blue), PixelAt(path));
    }

    [Fact]
    public async Task SaveImage_OtherFormatNameTaken_ShowsMessageAndKeepsBothFiles()
    {
        var path = WriteImage("a.png", 4, 2);
        var taken = WriteImage("a.webp", 3, 3);
        var takenBytes = await File.ReadAllBytesAsync(taken, TestContext.Current.CancellationToken);
        var (_, resizeVm, tab) = await CreateAsync(path);
        var messages = new List<string>();
        resizeVm.ShowMessageAction = messages.Add;
        var closed = false;
        resizeVm.CloseAction = () => closed = true;
        AskFor(resizeVm, 2, 1);
        // WebP
        resizeVm.SelectedConversionIndex.Value = 3;
        // Read before the save: a test elsewhere may load another language while this one runs
        var fileAlreadyExists = TranslationManager.Translation.FileAlreadyExists;

        await resizeVm.SaveImage();

        var message = Assert.Single(messages);
        Assert.EndsWith(Environment.NewLine + "a.webp", message, StringComparison.Ordinal);
        Assert.Equal(fileAlreadyExists + Environment.NewLine + "a.webp", message);
        Assert.False(closed);
        AssertSize(path, 4, 2);
        Assert.Equal(takenBytes, await File.ReadAllBytesAsync(taken, TestContext.Current.CancellationToken));
        Assert.Equal(path, tab.Model.FileInfo?.FullName);
    }

    [Fact]
    public async Task SaveImage_TabSwitchedDuringWrite_ShowsInTheWritingTab()
    {
        var path = WriteImage("a.png", 4, 2);
        var (mainVm, resizeVm, tab) = await CreateAsync(path);
        AskFor(resizeVm, 2, 1);

        // SaveImage takes the tab before its first await, and the write runs off this thread, so the other tab is
        // active while the write and its follow-up run
        var saving = resizeVm.SaveImage();
        var other = mainVm.WindowTabs.CreateTab();
        Assert.Same(other, mainVm.WindowTabs.ActiveTab.CurrentValue);
        await saving.WaitAsync(Timeout, TestContext.Current.CancellationToken);

        AssertSize(path, 2, 1);
        Assert.Equal((2u, 1u), (tab.Model.PixelWidth, tab.Model.PixelHeight));
        Assert.Equal(path, tab.Model.FileInfo?.FullName);
        Assert.Null(other.FileInfo.CurrentValue);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("0", "0")]
    [InlineData("abc", "0")]
    public async Task SaveImage_NoSize_WritesNothingAndStaysOpen(string width, string height)
    {
        var path = WriteImage("a.png", 4, 2);
        var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        var (_, resizeVm, tab) = await CreateAsync(path);
        tab.Orientation.TurnRight();
        var closed = false;
        resizeVm.CloseAction = () => closed = true;
        var asked = false;
        resizeVm.PickFileAction = (_, _) =>
        {
            asked = true;
            return ValueTask.FromResult<string?>(Path.Combine(_testDirectory, "copy.png"));
        };
        resizeVm.IsKeepingAspectRatio.Value = false;
        resizeVm.DesiredPixelWidth.Value = width;
        resizeVm.DesiredPixelHeight.Value = height;

        await resizeVm.SaveImage();
        await resizeVm.SaveImageAs();

        Assert.False(closed);
        Assert.False(asked);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        Assert.Equal(1, tab.Orientation.Current.CurrentValue.QuarterTurns);
    }

    // A Main Window whose active tab loaded the file at path, or shows no file when it is null, and a resize dialog
    // for it. The cache reads each file's size, as a decode would
    private async Task<(MainWindowViewModel MainVm, ResizeImageViewModel ResizeVm, TabViewModel Tab)> CreateAsync(
        string? path)
    {
        var core = new CoreViewModel(null, file =>
        {
            var info = new MagickImageInfo(file);
            return new ImageModel { FileInfo = file, PixelWidth = info.Width, PixelHeight = info.Height };
        });
        core.InitializeNavigation(TestTabServices.For(core.SharedCache), new NavigationService());
        var mainVm = new MainWindowViewModel(core, new TranslationViewModel(), StartedSettings.New(),
            new GallerySharedSettingsViewModel());
        _disposables.Add(mainVm);
        var tab = mainVm.WindowTabs.ActiveTab.CurrentValue;
        _disposables.Add(tab);
        if (path is not null)
        {
            await tab.Load.LoadAsync(path);
        }

        var resizeVm = new ResizeImageViewModel();
        _disposables.Add(resizeVm);
        resizeVm.Initialize(mainVm);
        return (mainVm, resizeVm, tab);
    }

    // Asks for exactly width x height, as typed with the aspect unlinked
    private static void AskFor(ResizeImageViewModel resizeVm, uint width, uint height)
    {
        resizeVm.IsKeepingAspectRatio.Value = false;
        resizeVm.DesiredPixelWidth.Value = width.ToString();
        resizeVm.DesiredPixelHeight.Value = height.ToString();
    }

    // An effects view model whose Effects Render of the tab's file is blue, rendered for a non-default sharpen
    private async Task<EffectsViewModel> RenderBlueAsync(TabViewModel tab)
    {
        var effects = new EffectsViewModel(tab.FileInfo, (file, _, _) =>
        {
            using var shown = new MagickImage(file);
            return Task.FromResult<MagickImage?>(new MagickImage(MagickColors.Blue, shown.Width, shown.Height));
        });
        _disposables.Add(effects);
        var published = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = effects.Rendered.Subscribe(_ => published.TrySetResult());

        effects.Sharpen.Value = 5;

        await published.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        return effects;
    }

    private string WriteImage(string name, uint width, uint height)
    {
        var path = Path.Combine(_testDirectory, name);
        using var image = new MagickImage(MagickColors.Red, width, height);
        image.Write(path);
        return path;
    }

    private static void AssertSize(string path, uint width, uint height)
    {
        using var image = new MagickImage(path);
        Assert.Equal((width, height), (image.Width, image.Height));
    }

    // The red, green and blue of the top-left pixel
    private static (byte R, byte G, byte B) PixelAt(string path)
    {
        using var image = new MagickImage(path);
        using var pixels = image.GetPixels();
        var color = pixels.GetPixel(0, 0).ToColor()!;
        return (color.R, color.G, color.B);
    }

    private static (byte R, byte G, byte B) Rgb(MagickColor color) => (color.R, color.G, color.B);

    private sealed class FixedPixels(uint width, uint height) : ISingleImagePixels
    {
        public MagickImage? Read(TabViewModel tab) => new(MagickColors.Green, width, height);
    }

    // Runs no frame
    private sealed class NoFrames : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) { }
    }
}
