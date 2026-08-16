using ImageMagick;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Resizing;

public class BatchResizeViewModelTests
{
    public BatchResizeViewModelTests()
    {
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

    private static void CleanDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            try { Directory.Delete(path, true); } catch { /* ignore */ }
        }
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

            vm.SelectedFiles.Value = [new(img1), new(img2), new(img3)];

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

        // Mock delegates
        Task<string> SelectDirectory() => Task.FromResult(outputDir);

        Task<string?> SelectFile() => Task.FromResult<string?>(null);

        List<FileInfo> GetFiles(FileInfo _) => [new(img1), new(img2), new(img3)];
    }
}
