using ImageMagick;
using PicView.Core.BatchResize;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Resizing;

[Collection("Sequential")]
public class BatchResizeViewModelTests
{
    public BatchResizeViewModelTests()
    {
        SetDefaults();
        TranslationManager.Init();
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
    }

    private class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }

    // Helper to create temporary images
    private static string CreateTempImage(string directory, string extension, uint width = 100, uint height = 100)
    {
        var filePath = Path.Combine(directory, $"tmp_{Guid.NewGuid():N}.{extension.TrimStart('.')}" );
        using var image = new MagickImage(MagickColors.White, width, height);
        image.Format = extension.ToLowerInvariant() switch
        {
            "png" => MagickFormat.Png,
            "jpg" or "jpeg" => MagickFormat.Jpg,
            "webp" => MagickFormat.WebP,
            "avif" => MagickFormat.Avif,
            "jxl" => MagickFormat.Jxl,
            "heic" => MagickFormat.Heic,
            _ => MagickFormat.Png,
        };
        image.Write(filePath);
        return filePath;
    }

    private static string CreateNoiseJpg(string directory, uint width = 400, uint height = 300)
    {
        var filePath = Path.Combine(directory, $"noise_{Guid.NewGuid():N}.jpg");
        using var image = new MagickImage(MagickColors.White, width, height);
        image.AddNoise(NoiseType.Random);
        image.Format = MagickFormat.Jpg;
        image.Quality = 95;
        image.Write(filePath);
        return filePath;
    }

    private static void CleanDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            try { Directory.Delete(path, true); } catch { /* ignore */ }
        }
    }

    private static BatchResizeViewModel CreateViewModel(string outputDir, params string[] selectedFiles)
    {
        var vm = new BatchResizeViewModel(SelectDirectory, SelectFile, null, GetFiles);
        vm.OutputFolder.Value = outputDir;
        vm.ThumbnailAmount = 0;
        vm.Thumbs = [];
        vm.SelectedFiles.Value = [.. selectedFiles.Select(x => new FileInfo(x))];
        return vm;

        Task<string> SelectDirectory() => Task.FromResult(outputDir);
        Task<string?> SelectFile() => Task.FromResult<string?>(null);
        List<FileInfo> GetFiles(FileInfo _) => [.. selectedFiles.Select(x => new FileInfo(x))];
    }

    private static async Task RunBatchResizeAsync(BatchResizeViewModel vm)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = vm.IsFinished.Subscribe(finished =>
        {
            if (finished)
            {
                tcs.TrySetResult();
            }
        });

        vm.StartCommand.Execute(Unit.Default);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(45), TestContext.Current.CancellationToken);
    }

    private static MagickImage LoadSingleOutputImage(string outputDir)
    {
        var outputPath = Directory.GetFiles(outputDir).Single();
        return new MagickImage(outputPath);
    }

    [Fact]
    public async Task StartBatchResizeAsync_ProcessesAllFiles_UpdatesProgressAndLog()
    {
        SetDefaults();
        await TranslationManager.LoadLanguage("en");
        
        // Arrange temporary folders
        var sourceDir = Path.Combine(Path.GetTempPath(), $"BatchResizeSource_{Guid.NewGuid():N}");
        var outputDir = Path.Combine(Path.GetTempPath(), $"BatchResizeOutput_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outputDir);

        // Create 3 test images
        var img1 = CreateTempImage(sourceDir, "png");
        var img2 = CreateTempImage(sourceDir, "jpg");
        var img3 = CreateTempImage(sourceDir, "webp");

        try
        {
            var vm = new BatchResizeViewModel(SelectDirectory, SelectFile, null, GetFiles);
            vm.OutputFolder.Value = outputDir;
            // Ensure no thumbnails for this simple test
            vm.ThumbnailAmount = 0;
            vm.Thumbs = [];

            vm.SelectedFiles.Value = [new FileInfo(img1), new FileInfo(img2), new FileInfo(img3)];

            // Act
            var tcs = new TaskCompletionSource();
            using var sub = vm.IsFinished.Subscribe(finished =>
            {
                if (finished)
                {
                    tcs.TrySetResult();
                }
            });

            vm.StartCommand.Execute(Unit.Default);
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(45), TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(3, vm.ProcessedFiles?.Value?.Count ?? 0);
            Assert.Equal(3, vm.Progress.Value);
            Assert.True(vm.IsFinished.Value);
        }
        finally
        {
            // Cleanup
            CleanDirectory(sourceDir);
            CleanDirectory(outputDir);
        }

        return;

        // Mock delegates
        Task<string> SelectDirectory() => Task.FromResult(outputDir);

        Task<string?> SelectFile() => Task.FromResult<string?>(null);

        List<FileInfo> GetFiles(FileInfo _) => [new(img1), new(img2), new(img3)];
    }

    [Fact]
    public async Task StartBatchResizeAsync_CompressionModes_AffectFileSize()
    {
        SetDefaults();
        await TranslationManager.LoadLanguage("en");

        var sourceDir = Path.Combine(Path.GetTempPath(), $"BatchResizeSource_{Guid.NewGuid():N}");
        var outNone = Path.Combine(Path.GetTempPath(), $"BatchResizeOutNone_{Guid.NewGuid():N}");
        var outLossless = Path.Combine(Path.GetTempPath(), $"BatchResizeOutLossless_{Guid.NewGuid():N}");
        var outLossy = Path.Combine(Path.GetTempPath(), $"BatchResizeOutLossy_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outNone);
        Directory.CreateDirectory(outLossless);
        Directory.CreateDirectory(outLossy);
        var sourceImage = CreateNoiseJpg(sourceDir);

        try
        {
            var vmNone = CreateViewModel(outNone, sourceImage);
            vmNone.Compression.Value = CompressionMode.None;
            await RunBatchResizeAsync(vmNone);

            var vmLossless = CreateViewModel(outLossless, sourceImage);
            vmLossless.Compression.Value = CompressionMode.Lossless;
            await RunBatchResizeAsync(vmLossless);

            var vmLossy = CreateViewModel(outLossy, sourceImage);
            vmLossy.Compression.Value = CompressionMode.Lossy;
            await RunBatchResizeAsync(vmLossy);

            var noneSize = new FileInfo(Directory.GetFiles(outNone).Single()).Length;
            var losslessSize = new FileInfo(Directory.GetFiles(outLossless).Single()).Length;
            var lossySize = new FileInfo(Directory.GetFiles(outLossy).Single()).Length;

            Assert.True(losslessSize <= noneSize);
            Assert.True(lossySize <= noneSize);
            Assert.True(losslessSize < noneSize || lossySize < noneSize);
        }
        finally
        {
            CleanDirectory(sourceDir);
            CleanDirectory(outNone);
            CleanDirectory(outLossless);
            CleanDirectory(outLossy);
        }
    }

    [Fact]
    public async Task StartBatchResizeAsync_QualityIgnored_WhenDisabled()
    {
        SetDefaults();
        await TranslationManager.LoadLanguage("en");

        var sourceDir = Path.Combine(Path.GetTempPath(), $"BatchResizeSource_{Guid.NewGuid():N}");
        var outLow = Path.Combine(Path.GetTempPath(), $"BatchResizeOutLow_{Guid.NewGuid():N}");
        var outHigh = Path.Combine(Path.GetTempPath(), $"BatchResizeOutHigh_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outLow);
        Directory.CreateDirectory(outHigh);
        var sourceImage = CreateNoiseJpg(sourceDir);

        try
        {
            var vmLow = CreateViewModel(outLow, sourceImage);
            vmLow.IsQualityEnabled.Value = false;
            vmLow.Quality.Value = 10;
            await RunBatchResizeAsync(vmLow);

            var vmHigh = CreateViewModel(outHigh, sourceImage);
            vmHigh.IsQualityEnabled.Value = false;
            vmHigh.Quality.Value = 95;
            await RunBatchResizeAsync(vmHigh);

            var lowSize = new FileInfo(Directory.GetFiles(outLow).Single()).Length;
            var highSize = new FileInfo(Directory.GetFiles(outHigh).Single()).Length;

            Assert.Equal(lowSize, highSize);
        }
        finally
        {
            CleanDirectory(sourceDir);
            CleanDirectory(outLow);
            CleanDirectory(outHigh);
        }
    }

    [Fact]
    public async Task StartBatchResizeAsync_QualityApplied_WhenEnabled_ForJpeg()
    {
        SetDefaults();
        await TranslationManager.LoadLanguage("en");

        var sourceDir = Path.Combine(Path.GetTempPath(), $"BatchResizeSource_{Guid.NewGuid():N}");
        var outLow = Path.Combine(Path.GetTempPath(), $"BatchResizeOutLow_{Guid.NewGuid():N}");
        var outHigh = Path.Combine(Path.GetTempPath(), $"BatchResizeOutHigh_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outLow);
        Directory.CreateDirectory(outHigh);
        var sourceImage = CreateNoiseJpg(sourceDir);

        try
        {
            var vmLow = CreateViewModel(outLow, sourceImage);
            vmLow.IsQualityEnabled.Value = true;
            vmLow.Quality.Value = 15;
            await RunBatchResizeAsync(vmLow);

            var vmHigh = CreateViewModel(outHigh, sourceImage);
            vmHigh.IsQualityEnabled.Value = true;
            vmHigh.Quality.Value = 95;
            await RunBatchResizeAsync(vmHigh);

            var lowSize = new FileInfo(Directory.GetFiles(outLow).Single()).Length;
            var highSize = new FileInfo(Directory.GetFiles(outHigh).Single()).Length;

            Assert.True(lowSize < highSize);
        }
        finally
        {
            CleanDirectory(sourceDir);
            CleanDirectory(outLow);
            CleanDirectory(outHigh);
        }
    }

    [Fact]
    public async Task StartBatchResizeAsync_Conversion_ConvertsToSelectedTarget()
    {
        SetDefaults();
        await TranslationManager.LoadLanguage("en");

        var sourceDir = Path.Combine(Path.GetTempPath(), $"BatchResizeSource_{Guid.NewGuid():N}");
        var outputDir = Path.Combine(Path.GetTempPath(), $"BatchResizeOutput_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outputDir);
        var sourceImage = CreateTempImage(sourceDir, "png");

        try
        {
            var vm = CreateViewModel(outputDir, sourceImage);
            vm.Conversion.Value = ConversionTarget.Jpg;

            await RunBatchResizeAsync(vm);

            var outputPath = Directory.GetFiles(outputDir).Single();
            using var outputImage = new MagickImage(outputPath);

            Assert.Equal(".jpg", Path.GetExtension(outputPath), ignoreCase: true);
            Assert.Equal(MagickFormat.Jpeg, outputImage.Format);
        }
        finally
        {
            CleanDirectory(sourceDir);
            CleanDirectory(outputDir);
        }
    }

    [Fact]
    public async Task StartBatchResizeAsync_PercentageResize_ResizesByPercentage()
    {
        SetDefaults();
        await TranslationManager.LoadLanguage("en");

        var sourceDir = Path.Combine(Path.GetTempPath(), $"BatchResizeSource_{Guid.NewGuid():N}");
        var outputDir = Path.Combine(Path.GetTempPath(), $"BatchResizeOutput_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outputDir);
        var sourceImage = CreateTempImage(sourceDir, "png", 100, 100);

        try
        {
            var vm = CreateViewModel(outputDir, sourceImage);
            vm.IsPercentageResizing.Value = true;
            vm.PercentageValue.Value = 90;

            await RunBatchResizeAsync(vm);

            using var outputImage = LoadSingleOutputImage(outputDir);
            Assert.Equal((uint)90, outputImage.Width);
            Assert.Equal((uint)90, outputImage.Height);
        }
        finally
        {
            CleanDirectory(sourceDir);
            CleanDirectory(outputDir);
        }
    }

    [Fact]
    public async Task StartBatchResizeAsync_WidthAndHeight_WithAndWithoutAspectRatio_WorksAsExpected()
    {
        SetDefaults();
        await TranslationManager.LoadLanguage("en");

        var sourceDir = Path.Combine(Path.GetTempPath(), $"BatchResizeSource_{Guid.NewGuid():N}");
        var keepAspectOutput = Path.Combine(Path.GetTempPath(), $"BatchResizeKeepAspect_{Guid.NewGuid():N}");
        var ignoreAspectOutput = Path.Combine(Path.GetTempPath(), $"BatchResizeIgnoreAspect_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(keepAspectOutput);
        Directory.CreateDirectory(ignoreAspectOutput);
        var sourceImage = CreateTempImage(sourceDir, "png", 200, 100);

        try
        {
            var keepAspectVm = CreateViewModel(keepAspectOutput, sourceImage);
            keepAspectVm.IsWidthAndHeightResizing.Value = true;
            keepAspectVm.WidthValue.Value = 100;
            keepAspectVm.HeightValue.Value = 100;
            keepAspectVm.IsKeepingAspectRatio.Value = true;
            await RunBatchResizeAsync(keepAspectVm);

            var ignoreAspectVm = CreateViewModel(ignoreAspectOutput, sourceImage);
            ignoreAspectVm.IsWidthAndHeightResizing.Value = true;
            ignoreAspectVm.WidthValue.Value = 100;
            ignoreAspectVm.HeightValue.Value = 100;
            ignoreAspectVm.IsKeepingAspectRatio.Value = false;
            await RunBatchResizeAsync(ignoreAspectVm);

            using var keepAspectImage = LoadSingleOutputImage(keepAspectOutput);
            using var ignoreAspectImage = LoadSingleOutputImage(ignoreAspectOutput);

            Assert.Equal((uint)100, keepAspectImage.Width);
            Assert.Equal((uint)50, keepAspectImage.Height);

            Assert.Equal((uint)100, ignoreAspectImage.Width);
            Assert.Equal((uint)100, ignoreAspectImage.Height);
        }
        finally
        {
            CleanDirectory(sourceDir);
            CleanDirectory(keepAspectOutput);
            CleanDirectory(ignoreAspectOutput);
        }
    }

    [Fact]
    public async Task StartBatchResizeAsync_WidthAndHeightSingleAxisModes_ApplyTargetAxis()
    {
        SetDefaults();
        await TranslationManager.LoadLanguage("en");

        var sourceDir = Path.Combine(Path.GetTempPath(), $"BatchResizeSource_{Guid.NewGuid():N}");
        var widthOutput = Path.Combine(Path.GetTempPath(), $"BatchResizeWidth_{Guid.NewGuid():N}");
        var heightOutput = Path.Combine(Path.GetTempPath(), $"BatchResizeHeight_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(widthOutput);
        Directory.CreateDirectory(heightOutput);
        var sourceImage = CreateTempImage(sourceDir, "png", 200, 100);

        try
        {
            var widthVm = CreateViewModel(widthOutput, sourceImage);
            widthVm.IsWidthResizing.Value = true;
            widthVm.SingleWidthValue.Value = 80;
            await RunBatchResizeAsync(widthVm);

            var heightVm = CreateViewModel(heightOutput, sourceImage);
            heightVm.IsHeightResizing.Value = true;
            heightVm.SingleHeightValue.Value = 60;
            await RunBatchResizeAsync(heightVm);

            using var widthImage = LoadSingleOutputImage(widthOutput);
            using var heightImage = LoadSingleOutputImage(heightOutput);

            Assert.Equal((uint)80, widthImage.Width);
            Assert.Equal((uint)60, heightImage.Height);
        }
        finally
        {
            CleanDirectory(sourceDir);
            CleanDirectory(widthOutput);
            CleanDirectory(heightOutput);
        }
    }

    [Fact]
    public async Task StartBatchResizeAsync_WithThumbnails_GeneratesExpectedThumbnailCount()
    {
        SetDefaults();
        await TranslationManager.LoadLanguage("en");

        var sourceDir = Path.Combine(Path.GetTempPath(), $"BatchResizeSource_{Guid.NewGuid():N}");
        var outputDir = Path.Combine(Path.GetTempPath(), $"BatchResizeOutput_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outputDir);

        var sourceImage1 = CreateTempImage(sourceDir, "png", 200, 100);
        var sourceImage2 = CreateTempImage(sourceDir, "png", 200, 100);

        try
        {
            var vm = CreateViewModel(outputDir, sourceImage1, sourceImage2);
            vm.ThumbnailAmount = 2;
            vm.Thumbs =
            [
                new BatchThumb("thumb_50", new Percentage(50)),
                new BatchThumb("thumb_80w", width: 80)
            ];

            await RunBatchResizeAsync(vm);

            var thumb50Dir = Path.Combine(outputDir, "thumb_50");
            var thumb80wDir = Path.Combine(outputDir, "thumb_80w");
            var expectedThumbnailCount = vm.SelectedFiles.Value.Count * vm.ThumbnailAmount;

            Assert.Equal(2, Directory.GetFiles(thumb50Dir).Length);
            Assert.Equal(2, Directory.GetFiles(thumb80wDir).Length);
            Assert.Equal(expectedThumbnailCount, Directory.GetFiles(thumb50Dir, "*", SearchOption.AllDirectories).Length +
                                                 Directory.GetFiles(thumb80wDir, "*", SearchOption.AllDirectories).Length);
        }
        finally
        {
            CleanDirectory(sourceDir);
            CleanDirectory(outputDir);
        }
    }

    [Fact]
    public async Task StartBatchResizeAsync_WithThumbnailParameters_AppliesBatchThumbConfiguration()
    {
        SetDefaults();
        await TranslationManager.LoadLanguage("en");

        var sourceDir = Path.Combine(Path.GetTempPath(), $"BatchResizeSource_{Guid.NewGuid():N}");
        var outputPercentage = Path.Combine(Path.GetTempPath(), $"BatchResizeThumbPercent_{Guid.NewGuid():N}");
        var outputWidth = Path.Combine(Path.GetTempPath(), $"BatchResizeThumbWidth_{Guid.NewGuid():N}");
        var outputHeight = Path.Combine(Path.GetTempPath(), $"BatchResizeThumbHeight_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outputPercentage);
        Directory.CreateDirectory(outputWidth);
        Directory.CreateDirectory(outputHeight);

        var sourceImage = CreateTempImage(sourceDir, "png", 200, 100);

        try
        {
            var percentageVm = CreateViewModel(outputPercentage, sourceImage);
            percentageVm.ThumbnailAmount = 1;
            percentageVm.Thumbs = [new BatchThumb("thumb_percent", new Percentage(50))];
            await RunBatchResizeAsync(percentageVm);

            var widthVm = CreateViewModel(outputWidth, sourceImage);
            widthVm.ThumbnailAmount = 1;
            widthVm.Thumbs = [new BatchThumb("thumb_width", width: 80)];
            await RunBatchResizeAsync(widthVm);

            var heightVm = CreateViewModel(outputHeight, sourceImage);
            heightVm.ThumbnailAmount = 1;
            heightVm.Thumbs = [new BatchThumb("thumb_height", height: 40)];
            await RunBatchResizeAsync(heightVm);

            using var percentageThumb = new MagickImage(Directory.GetFiles(Path.Combine(outputPercentage, "thumb_percent")).Single());
            using var widthThumb = new MagickImage(Directory.GetFiles(Path.Combine(outputWidth, "thumb_width")).Single());
            using var heightThumb = new MagickImage(Directory.GetFiles(Path.Combine(outputHeight, "thumb_height")).Single());

            Assert.Equal((uint)100, percentageThumb.Width);
            Assert.Equal((uint)50, percentageThumb.Height);
            Assert.Equal((uint)80, widthThumb.Width);
            Assert.Equal((uint)40, widthThumb.Height);
            Assert.Equal((uint)80, heightThumb.Width);
            Assert.Equal((uint)40, heightThumb.Height);
        }
        finally
        {
            CleanDirectory(sourceDir);
            CleanDirectory(outputPercentage);
            CleanDirectory(outputWidth);
            CleanDirectory(outputHeight);
        }
    }
}
