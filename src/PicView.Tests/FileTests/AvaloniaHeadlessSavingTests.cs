using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia;
using PicView.Avalonia.FileSystem;
using PicView.Core.ViewModels;

namespace PicView.Tests.FileTests;

public class AvaloniaHeadlessSavingTests
{
    private static bool _setupDone = false;
    
    public AvaloniaHeadlessSavingTests()
    {
        if (!_setupDone)
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
            
            LoadSettings();

            Core.Localization.TranslationManager.Init();
            if (Core.Localization.TranslationManager.Translation != null)
            {
                Core.Localization.TranslationManager.Translation.Folder = "Folder";
                Core.Localization.TranslationManager.Translation.SaveAs = "SaveAs";
                Core.Localization.TranslationManager.Translation.OpenFileDialog = "Open";
            }

            _setupDone = true;
        }
    }

    private T RunWithDispatcher<T>(ValueTask<T> task)
    {
        while (!task.IsCompleted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        return task.GetAwaiter().GetResult();
    }

    private MainWindowViewModel CreateDummyVm()
    {
        // Try creating with null dependencies since they might just be saved to properties
        return new MainWindowViewModel(null!, null!, null!, null!);
    }

    [Fact]
    public void ConvertJpgToWebp_ReducesFileSize()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var sourceJpg = Path.Combine(tempDir, "test.jpg");
        var destWebp = Path.Combine(tempDir, "test.webp");

        try
        {
            // 1. Create a dummy JPEG using MagickImage
            // We'll create something with a pattern/noise so it isn't ultra-compressible and 1 byte in both formats
            using (var image = new MagickImage(MagickColors.Red, 800, 600))
            {
                image.AddNoise(NoiseType.Gaussian);
                image.Format = MagickFormat.Jpeg;
                image.Write(sourceJpg);
            }

            Assert.True(File.Exists(sourceJpg));
            var originalSize = new FileInfo(sourceJpg).Length;
            Assert.True(originalSize > 0, "Original JPEG size should be greater than 0");

            // 2. Initialize CoreViewModel and assign to Application.Current.DataContext
            var core = new CoreViewModel(null, _ => new ValueTask<Core.Models.ImageModel>((Core.Models.ImageModel)null!));
            Application.Current.DataContext = core;

            var vm = CreateDummyVm();
            var window = new Window();
            var pickerService = new FilePickerService(window.StorageProvider);
            var savingService = new FileSavingService(pickerService);

            // 3. Save file again as test.webp
            var result = RunWithDispatcher(savingService.SaveFileAsync(sourceJpg, destWebp, vm));

            // 4. Assert it was saved successfully and file size is lower
            Assert.True(result, "SaveFileAsync should return true");
            Assert.True(File.Exists(destWebp), "WebP file should exist on the device");

            var newSize = new FileInfo(destWebp).Length;
            // WebP usually compresses noisy RGB images better than JPEG, but we test < originalSize.
            // If they are very similar, at least ensure new file exists and is smaller.
            Assert.True(newSize < originalSize, $"WebP size ({newSize}) should be smaller than JPEG size ({originalSize})");
        }
        finally
        {
            Application.Current.DataContext = null;
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
