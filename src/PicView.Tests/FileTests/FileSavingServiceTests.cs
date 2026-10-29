using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.FileSystem;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

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
        var tab = new TabViewModel(vm);
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
        tab.Orientation.TurnTo(1);
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
        var tab = new TabViewModel(vm);
        tab.FileInfo.Value = new FileInfo(path);
        tab.Orientation.TurnTo(1);
        var picker = new StubSavePathPicker(null);
        var service = new FileSavingService(picker, () => null);

        var isSaved = RunWithDispatcher(service.SaveAsync(tab));

        Assert.True(isSaved);
        Assert.Null(picker.SuggestedName);
        using var saved = new MagickImage(path);
        Assert.Equal(6u, saved.Width);
    }

    /// <summary>
    /// A black 3x2 PNG whose top-left pixel is red.
    /// </summary>
    private string WriteMarkedPng(string name)
    {
        var path = Path.Combine(_tempDir, name);
        using var image = new MagickImage(MagickColors.Black, 3, 2);
        using (var pixels = image.GetPixels())
        {
            pixels.SetPixel(0, 0, [255, 0, 0]);
        }
        image.Write(path);
        return path;
    }

    [AvaloniaTheory]
    [InlineData(0, 3, 2, 2, 0)]
    [InlineData(1, 2, 3, 0, 0)]
    [InlineData(2, 3, 2, 0, 1)]
    [InlineData(3, 2, 3, 1, 2)]
    public void SaveToAsync_FlippedTab_SavesItTurnedThenMirrored(int quarterTurns, int width, int height,
        int markedX, int markedY)
    {
        var source = WriteMarkedPng("source.png");
        var destination = Path.Combine(_tempDir, "flipped.png");
        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(vm);
        tab.FileInfo.Value = new FileInfo(source);
        tab.Orientation.TurnTo(quarterTurns);
        tab.Orientation.Flip();
        var service = new FileSavingService(new StubSavePathPicker(null), () => null);

        var isSaved = RunWithDispatcher(service.SaveToAsync(tab, destination));

        Assert.True(isSaved);
        using var saved = new MagickImage(destination);
        Assert.Equal((uint)width, saved.Width);
        Assert.Equal((uint)height, saved.Height);
        using var pixels = saved.GetPixels();
        Assert.Equal(MagickColors.Red, pixels.GetPixel(markedX, markedY).ToColor());
    }

    /// <summary>
    /// Effects whose Effects Render of <paramref name="file"/> is a 3x2 blue image.
    /// </summary>
    private static EffectsViewModel CreateEffectsWithRender(FileInfo file)
    {
        var effects = new EffectsViewModel(new ReactiveProperty<FileInfo?>(file),
            (_, _, _) => Task.FromResult<MagickImage?>(new MagickImage(MagickColors.Blue, 3, 2)));
        var published = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = effects.Rendered.Subscribe(_ => published.TrySetResult());
        effects.Sharpen.Value = 5;
        published.Task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        return effects;
    }

    private string WriteRedPng(string name)
    {
        var path = Path.Combine(_tempDir, name);
        using var image = new MagickImage(MagickColors.Red, 8, 6);
        image.Write(path);
        return path;
    }

    [AvaloniaFact]
    public void SaveToAsync_EffectsRenderOfTabFile_SavesEffectsRender()
    {
        var source = WriteRedPng("source.png");
        var destination = Path.Combine(_tempDir, "copy.png");
        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(vm);
        tab.FileInfo.Value = new FileInfo(source);
        using var effects = CreateEffectsWithRender(new FileInfo(source));
        var service = new FileSavingService(new StubSavePathPicker(null), () => effects);

        var isSaved = RunWithDispatcher(service.SaveToAsync(tab, destination));

        Assert.True(isSaved);
        using var saved = new MagickImage(destination);
        Assert.Equal(3u, saved.Width);
    }

    [AvaloniaFact]
    public void SaveToAsync_EffectsRenderOfOtherFile_SavesFile()
    {
        var source = WriteRedPng("source.png");
        var destination = Path.Combine(_tempDir, "copy.jpg");
        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(vm);
        tab.FileInfo.Value = new FileInfo(source);
        using var effects = CreateEffectsWithRender(new FileInfo(Path.Combine(_tempDir, "other.png")));
        var service = new FileSavingService(new StubSavePathPicker(null), () => effects);

        var isSaved = RunWithDispatcher(service.SaveToAsync(tab, destination));

        Assert.True(isSaved);
        using var saved = new MagickImage(destination);
        Assert.Equal(MagickFormat.Jpeg, saved.Format);
        Assert.Equal(8u, saved.Width);
    }

    [AvaloniaFact]
    public void ExportCommonFormatAsync_UntouchedPng_ReturnsFilePath()
    {
        var source = WriteRedPng("source.png");
        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(vm);
        tab.FileInfo.Value = new FileInfo(source);
        var service = new FileSavingService(new StubSavePathPicker(null), () => null);

        var exported = RunWithDispatcher(service.ExportCommonFormatAsync(tab));

        Assert.Equal(source, exported);
    }

    [AvaloniaFact]
    public void ExportCommonFormatAsync_RotatedTab_ExportsRotatedPng()
    {
        var source = WriteRedPng("source.png");
        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(vm);
        tab.FileInfo.Value = new FileInfo(source);
        tab.Orientation.TurnTo(1);
        var service = new FileSavingService(new StubSavePathPicker(null), () => null);

        var exported = RunWithDispatcher(service.ExportCommonFormatAsync(tab));

        try
        {
            Assert.NotNull(exported);
            Assert.NotEqual(source, exported);
            using var image = new MagickImage(exported);
            Assert.Equal(MagickFormat.Png, image.Format);
            Assert.Equal(6u, image.Width);
        }
        finally
        {
            File.Delete(exported!);
        }
    }

    [Theory]
    [InlineData("Clipboard image", "Clipboard image 2026-10-17 230047.png")]
    [InlineData("a/b:c*?", "abc 2026-10-17 230047.png")]
    [InlineData("", "PicView 2026-10-17 230047.png")]
    public void GetSuggestedFileName_NotAFile_UsesTitleAndDate(string title, string expected)
    {
        var tab = new TabViewModel(null!);
        tab.TabTitle.Value = title;

        var name = FileSavingService.GetSuggestedFileName(tab, new DateTime(2026, 10, 17, 23, 0, 47));

        Assert.Equal(expected, name);
    }

    [Fact]
    public void GetSuggestedFileName_File_UsesFileName()
    {
        var tab = new TabViewModel(null!);
        tab.FileInfo.Value = new FileInfo(Path.Combine(_tempDir, "photo.jpg"));

        var name = FileSavingService.GetSuggestedFileName(tab, DateTime.Now);

        Assert.Equal("photo.jpg", name);
    }
}
