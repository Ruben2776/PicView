using ImageMagick;
using PicView.Core.BatchResize;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;
using Properties = PicView.Core.Config.BatchResizeWindowConfig.BatchResizeWindowProperties;

namespace PicView.Tests.Resizing;

[Collection("Sequential")]
public class BatchResizeViewModelTests : IDisposable
{
    private readonly string _folder;
    private readonly List<string> _located = [];

    public BatchResizeViewModelTests()
    {
        SetDefaults();
        TranslationManager.Init();
        TranslationManager.LoadLanguage("en");
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        _folder = Path.Combine(Path.GetTempPath(), $"BatchResizeViewModelTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, true);
        }
        catch (IOException)
        {
            // A file still held open is left for the OS to clean up
        }
    }

    [Theory]
    [InlineData(ResizeMode.None, false, false, false, false)]
    [InlineData(ResizeMode.Percentage, true, false, false, false)]
    [InlineData(ResizeMode.WidthAndHeight, false, true, false, false)]
    [InlineData(ResizeMode.Width, false, false, true, false)]
    [InlineData(ResizeMode.Height, false, false, false, true)]
    public void Resize_ChosenMode_ShowsOnlyItsFields(ResizeMode mode, bool percentage, bool widthAndHeight,
        bool width, bool height)
    {
        using var vm = CreateViewModel();

        vm.Resize.Value = mode;

        Assert.Equal(percentage, vm.IsPercentageResizing.Value);
        Assert.Equal(widthAndHeight, vm.IsWidthAndHeightResizing.Value);
        Assert.Equal(width, vm.IsWidthResizing.Value);
        Assert.Equal(height, vm.IsHeightResizing.Value);
    }

    [Fact]
    public void Prefill_WithAFile_ListsItsFolderAndSizesToTheImage()
    {
        var file = new FileInfo(Path.Combine(_folder, "photo.png"));
        using var vm = CreateViewModel(getFiles: _ => [file, new FileInfo(Path.Combine(_folder, "other.png"))]);

        vm.Prefill(file, 200, 100);

        Assert.Equal(2, vm.SelectedFiles.Value.Count);
        Assert.Equal(Path.Combine(_folder, TranslationManager.Translation.BatchResize!), vm.OutputFolder.Value);
        Assert.Equal((200u, 100u), (vm.Width.Value, vm.Height.Value));
    }

    [Fact]
    public void Width_WhileKeepingAspectRatio_SetsTheHeightFromTheImagesRatio()
    {
        using var vm = CreateViewModel();
        vm.Prefill(null, 200, 100);

        vm.Width.Value = 100;

        Assert.Equal(50u, vm.Height.Value);
    }

    [Fact]
    public void Height_WithoutKeepingAspectRatio_LeavesTheWidth()
    {
        using var vm = CreateViewModel();
        vm.Prefill(null, 200, 100);
        vm.IsKeepingAspectRatio.Value = false;

        vm.Height.Value = 10;

        Assert.Equal(200u, vm.Width.Value);
    }

    [Fact]
    public void ThumbnailAmount_Three_AddsLargeMediumAndSmallRows()
    {
        using var vm = CreateViewModel();

        vm.ThumbnailAmount.Value = 3;

        Assert.Equal([("large", 70d), ("medium", 50d), ("small", 30d)],
            vm.Thumbnails.Select(row => (row.Subfolder.Value, row.Value.Value)));
        Assert.All(vm.Thumbnails, row => Assert.Equal(ThumbnailSize.Percentage, row.Size.Value));
    }

    [Fact]
    public void CreateJob_HoldsEveryChosenSetting()
    {
        var file = new FileInfo(Path.Combine(_folder, "photo.png"));
        using var vm = CreateViewModel();
        vm.SelectedFiles.Value.Add(file);
        vm.OutputFolder.Value = _folder;
        vm.Conversion.Value = ConversionTarget.Webp;
        vm.Compression.Value = CompressionMode.Lossy;
        vm.IsQualityEnabled.Value = true;
        vm.Quality.Value = 60;
        vm.Resize.Value = ResizeMode.Percentage;
        vm.Percentage.Value = 40;
        vm.ThumbnailAmount.Value = 1;
        vm.Thumbnails[0].Size.Value = ThumbnailSize.Width;
        vm.Thumbnails[0].Value.Value = 120;
        vm.Thumbnails[0].Subfolder.Value = "thumbs";

        var job = vm.CreateJob();

        Assert.Equal([file], job.Files);
        Assert.Equal(_folder, job.OutputFolder);
        Assert.Equal(ConversionTarget.Webp, job.Conversion);
        Assert.Equal(CompressionMode.Lossy, job.Compression);
        Assert.Equal(60u, job.Quality);
        Assert.Equal(ResizeMode.Percentage, job.Resize);
        Assert.Equal(40, job.Percentage);
        Assert.Equal([new BatchThumbnail("thumbs", ThumbnailSize.Width, 120)], job.Thumbnails);
    }

    [Fact]
    public void CreateJob_WithQualityOff_LeavesQualityToTheFormat()
    {
        using var vm = CreateViewModel();
        vm.Quality.Value = 60;
        vm.IsQualityEnabled.Value = false;

        Assert.Null(vm.CreateJob().Quality);
    }

    [Fact]
    public void Constructor_WithRememberedSettings_RestoresThem()
    {
        var remembered = new Properties
        {
            Conversion = nameof(ConversionTarget.Avif),
            Compression = nameof(CompressionMode.None),
            Resize = nameof(ResizeMode.Height),
            IsQualityEnabled = true,
            Quality = 55,
            KeepAspectRatio = false,
            Thumbnails = [new() { Subfolder = "tiny", Size = nameof(ThumbnailSize.Height), Value = 64 }]
        };

        using var vm = CreateViewModel(remembered);

        Assert.Equal(ConversionTarget.Avif, vm.Conversion.Value);
        Assert.Equal(CompressionMode.None, vm.Compression.Value);
        Assert.Equal(ResizeMode.Height, vm.Resize.Value);
        Assert.True(vm.IsQualityEnabled.Value);
        Assert.Equal(55u, vm.Quality.Value);
        Assert.False(vm.IsKeepingAspectRatio.Value);
        Assert.Equal(1, vm.ThumbnailAmount.Value);
        var row = Assert.Single(vm.Thumbnails);
        Assert.Equal(("tiny", ThumbnailSize.Height, 64d), (row.Subfolder.Value, row.Size.Value, row.Value.Value));
    }

    [Fact]
    public void Constructor_WithSettingsRememberedAsIndices_RestoresThem()
    {
        var remembered = new Properties { ConvertToIndex = 3, CompressionIndex = 2, ResizeIndex = 3, GenerateThumbnailsIndex = 2 };

        using var vm = CreateViewModel(remembered);

        Assert.Equal(ConversionTarget.Webp, vm.Conversion.Value);
        Assert.Equal(CompressionMode.Lossy, vm.Compression.Value);
        Assert.Equal(ResizeMode.Width, vm.Resize.Value);
        Assert.Equal(2, vm.Thumbnails.Count);
    }

    [Fact]
    public void RememberSettings_AfterChanges_WritesThemByName()
    {
        var remembered = new Properties { ConvertToIndex = 3 };
        using var vm = CreateViewModel(remembered);
        vm.Conversion.Value = ConversionTarget.Jxl;
        vm.Resize.Value = ResizeMode.WidthAndHeight;
        vm.ThumbnailAmount.Value = 2;

        vm.RememberSettings();

        Assert.Equal(nameof(ConversionTarget.Jxl), remembered.Conversion);
        Assert.Equal(nameof(ResizeMode.WidthAndHeight), remembered.Resize);
        Assert.Equal(["medium", "small"], remembered.Thumbnails!.Select(thumbnail => thumbnail.Subfolder));
        Assert.Null(remembered.ConvertToIndex);
    }

    [Fact]
    public async Task StartCommand_WithFiles_ReportsEachFileAndFinishes()
    {
        using var vm = CreateViewModel();
        vm.SelectedFiles.Value.Add(CreateImage("a.png"));
        vm.SelectedFiles.Value.Add(CreateImage("b.png"));
        vm.OutputFolder.Value = Path.Combine(_folder, "out");
        vm.ThumbnailAmount.Value = 1;

        await RunAsync(vm);

        Assert.Equal(2, vm.Progress.Value);
        Assert.Equal(2, vm.ProgressMaximum.Value);
        Assert.Equal(4, vm.ProcessedFiles.Value.Count);
        Assert.True(vm.IsRunning.Value);
    }

    [Fact]
    public async Task StartCommand_WhenTheRunFails_StillFinishes()
    {
        var blocking = Path.Combine(_folder, "file-in-the-way");
        await File.WriteAllTextAsync(blocking, "", TestContext.Current.CancellationToken);
        using var vm = CreateViewModel();
        vm.SelectedFiles.Value.Add(CreateImage("a.png"));
        vm.OutputFolder.Value = Path.Combine(blocking, "out");

        await RunAsync(vm);

        Assert.True(vm.IsFinished.Value);
        Assert.Single(vm.ProcessedFiles.Value);
    }

    [Fact]
    public void ShowInFolderCommand_LocatesTheOutputFolder()
    {
        using var vm = CreateViewModel();
        vm.OutputFolder.Value = _folder;

        vm.ShowInFolderCommand.Execute(Unit.Default);

        Assert.Equal([_folder], _located);
    }

    private static async Task RunAsync(BatchResizeViewModel vm)
    {
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = vm.IsFinished.Subscribe(isFinished =>
        {
            if (isFinished)
            {
                finished.TrySetResult();
            }
        });

        vm.StartCommand.Execute(Unit.Default);
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(45), TestContext.Current.CancellationToken);
    }

    private BatchResizeViewModel CreateViewModel(Properties? remembered = null,
        Func<FileInfo, List<FileInfo>>? getFiles = null) =>
        new(() => Task.FromResult(_folder), () => Task.FromResult<string?>(null), getFiles ?? (_ => []),
            _located.Add, remembered ?? new Properties());

    private FileInfo CreateImage(string name)
    {
        var path = Path.Combine(_folder, name);
        using var image = new MagickImage(MagickColors.White, 100, 50);
        image.Write(path);
        return new FileInfo(path);
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
