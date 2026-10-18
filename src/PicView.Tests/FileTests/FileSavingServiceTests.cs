using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.FileSystem;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Tests.FileTests;

/// <summary>
/// Picks a fixed path, or cancels when it is <see langword="null"/>, and remembers the suggested name.
/// </summary>
internal sealed class StubSavePathPicker(string? pickedPath) : ISavePathPicker
{
    public string? SuggestedName { get; private set; }

    public Task<string?> PickSavePathAsync(string? suggestedName, string? ext = null)
    {
        SuggestedName = suggestedName;
        return Task.FromResult(pickedPath);
    }
}

[Collection("Sequential")]
public class FileSavingServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public FileSavingServiceTests()
    {
        LoadSettings();
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private static T RunWithDispatcher<T>(ValueTask<T> task)
    {
        while (!task.IsCompleted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        return task.GetAwaiter().GetResult();
    }

    private static TabViewModel CreateClipboardTab(int width, int height)
    {
        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(null!, vm);
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        tab.DisplaySingleImage(new ImageModel
        {
            Image = bitmap,
            PixelWidth = (uint)width,
            PixelHeight = (uint)height,
            ImageType = ImageType.Bitmap
        }, SingleImageType.Clipboard);
        tab.TabTitle.Value = "Clipboard image";
        return tab;
    }

    [AvaloniaTheory]
    [InlineData("saved.png", MagickFormat.Png)]
    [InlineData("saved.jpg", MagickFormat.Jpeg)]
    [InlineData("saved.webp", MagickFormat.WebP)]
    public void SaveAsAsync_ClipboardImage_WritesPickedPathInItsFormat(string fileName, MagickFormat expected)
    {
        var destination = Path.Combine(_tempDir, fileName);
        var tab = CreateClipboardTab(8, 6);
        var service = new FileSavingService(new StubSavePathPicker(destination), () => null);

        var isSaved = RunWithDispatcher(service.SaveAsAsync(tab));

        Assert.True(isSaved);
        using var saved = new MagickImage(destination);
        Assert.Equal(expected, saved.Format);
        Assert.Equal(8u, saved.Width);
        Assert.Equal(6u, saved.Height);
    }

    [AvaloniaFact]
    public void SaveAsAsync_RotatedClipboardImage_WritesRotatedImage()
    {
        var destination = Path.Combine(_tempDir, "rotated.png");
        var tab = CreateClipboardTab(8, 6);
        tab.RotationAngle.Value = 90;
        var service = new FileSavingService(new StubSavePathPicker(destination), () => null);

        var isSaved = RunWithDispatcher(service.SaveAsAsync(tab));

        Assert.True(isSaved);
        using var saved = new MagickImage(destination);
        Assert.Equal(6u, saved.Width);
        Assert.Equal(8u, saved.Height);
    }

    [AvaloniaFact]
    public void SaveAsync_ClipboardImage_AsksWhereToSaveWithTitleAndDate()
    {
        var picker = new StubSavePathPicker(null);
        var tab = CreateClipboardTab(8, 6);
        var service = new FileSavingService(picker, () => null);

        var isSaved = RunWithDispatcher(service.SaveAsync(tab));

        Assert.False(isSaved);
        Assert.NotNull(picker.SuggestedName);
        Assert.StartsWith("Clipboard image ", picker.SuggestedName, StringComparison.Ordinal);
        Assert.EndsWith(".png", picker.SuggestedName, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void SaveAsync_File_OverwritesItInPlace()
    {
        var path = Path.Combine(_tempDir, "file.png");
        using (var image = new MagickImage(MagickColors.Red, 8, 6))
        {
            image.Write(path);
        }
        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(null!, vm);
        tab.FileInfo.Value = new FileInfo(path);
        tab.RotationAngle.Value = 90;
        var picker = new StubSavePathPicker(null);
        var service = new FileSavingService(picker, () => null);

        var isSaved = RunWithDispatcher(service.SaveAsync(tab));

        Assert.True(isSaved);
        Assert.Null(picker.SuggestedName);
        using var saved = new MagickImage(path);
        Assert.Equal(6u, saved.Width);
    }

    [AvaloniaFact]
    public void SaveToAsync_EffectsWithoutProcessedImage_SavesFile()
    {
        var source = Path.Combine(_tempDir, "source.png");
        var destination = Path.Combine(_tempDir, "copy.jpg");
        using (var image = new MagickImage(MagickColors.Red, 8, 6))
        {
            image.Write(source);
        }
        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(null!, vm);
        tab.FileInfo.Value = new FileInfo(source);
        var effects = new EffectsViewModel((_, _) => Task.FromResult<MagickImage?>(null));
        var service = new FileSavingService(new StubSavePathPicker(null), () => effects);

        var isSaved = RunWithDispatcher(service.SaveToAsync(tab, destination));

        Assert.True(isSaved);
        using var saved = new MagickImage(destination);
        Assert.Equal(MagickFormat.Jpeg, saved.Format);
        Assert.Equal(8u, saved.Width);
    }

    [Theory]
    [InlineData("Clipboard image", "Clipboard image 2026-10-17 230047.png")]
    [InlineData("a/b:c*?", "abc 2026-10-17 230047.png")]
    [InlineData("", "PicView 2026-10-17 230047.png")]
    public void GetSuggestedFileName_NotAFile_UsesTitleAndDate(string title, string expected)
    {
        var tab = new TabViewModel(_ => { }, null!);
        tab.TabTitle.Value = title;

        var name = FileSavingService.GetSuggestedFileName(tab, new DateTime(2026, 10, 17, 23, 0, 47));

        Assert.Equal(expected, name);
    }

    [Fact]
    public void GetSuggestedFileName_File_UsesFileName()
    {
        var tab = new TabViewModel(_ => { }, null!);
        tab.FileInfo.Value = new FileInfo(Path.Combine(_tempDir, "photo.jpg"));

        var name = FileSavingService.GetSuggestedFileName(tab, DateTime.Now);

        Assert.Equal("photo.jpg", name);
    }
}
