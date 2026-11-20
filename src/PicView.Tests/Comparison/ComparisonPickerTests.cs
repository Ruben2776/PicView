using PicView.Avalonia.Comparison;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;

namespace PicView.Tests.Comparison;

/// <summary>
/// Picking an image for A or B of a tab's comparison: the picked file is loaded and shown in the slot, unless the
/// pick is cancelled, the image cannot render or the comparison is gone once it is loaded.
/// </summary>
[Collection("Sequential")]
public class ComparisonPickerTests
{
    private readonly TabViewModel _tab;

    public ComparisonPickerTests()
    {
        SetDefaults();
        var cache = new SharedImageCache(static f => Model(f.Name));
        _tab = new TabViewModel(null!, TestTabServices.For(cache));
    }

    private sealed class DisposeProbe : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private static ImageModel Model(string name) => new()
    {
        FileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "PicViewComparisonPickerTests", name)),
        Image = new DisposeProbe(),
        PixelWidth = 1,
        PixelHeight = 1
    };

    private static bool IsDisposed(ImageModel model) => ((DisposeProbe)model.Image!).IsDisposed;

    private void Compare()
    {
        _tab.Display(Model("a.jpg"), Model("b.jpg"));
        _tab.SetComparing(true);
    }

    private static Func<Task<string?>> Selects(string? name) =>
        () => Task.FromResult(name is null ? null : Path.Combine(Path.GetTempPath(), name));

    [AvaloniaFact]
    public async Task PickAsync_B_SetsTheOverride()
    {
        Compare();

        await ComparisonPicker.PickAsync(_tab, null, ComparisonSlot.B, Selects("c.jpg"),
            static file => ValueTask.FromResult(Model(file.Name)));

        Assert.Equal("c.jpg", _tab.Comparison!.FileInfoB.Value!.Name);
        Assert.Equal("a.jpg", _tab.Comparison.FileInfoA.Value!.Name);
    }

    [AvaloniaFact]
    public async Task PickAsync_Cancelled_ChangesNothing()
    {
        Compare();
        var loads = 0;

        await ComparisonPicker.PickAsync(_tab, null, ComparisonSlot.B, Selects(null), file =>
        {
            loads++;
            return ValueTask.FromResult(Model(file.Name));
        });

        Assert.Equal(0, loads);
        Assert.Equal("b.jpg", _tab.Comparison!.FileInfoB.Value!.Name);
    }

    [AvaloniaFact]
    public async Task PickAsync_ImageCannotRender_DisposesAndKeepsB()
    {
        Compare();
        var unrenderable = new ImageModel { FileInfo = new FileInfo("c.jpg") };

        await ComparisonPicker.PickAsync(_tab, null, ComparisonSlot.B, Selects("c.jpg"),
            _ => ValueTask.FromResult(unrenderable));

        Assert.Equal("b.jpg", _tab.Comparison!.FileInfoB.Value!.Name);
    }

    [AvaloniaFact]
    public async Task PickAsync_NotComparing_DoesNothing()
    {
        _tab.Display(Model("a.jpg"), Model("b.jpg"));
        var loads = 0;

        await ComparisonPicker.PickAsync(_tab, null, ComparisonSlot.B, Selects("c.jpg"), file =>
        {
            loads++;
            return ValueTask.FromResult(Model(file.Name));
        });

        Assert.Null(_tab.Comparison);
        Assert.Equal(0, loads);
    }

    [AvaloniaFact]
    public async Task PickAsync_ComparisonGoneWhenLoaded_DisposesTheModel()
    {
        Compare();
        var picked = Model("c.jpg");

        await ComparisonPicker.PickAsync(_tab, null, ComparisonSlot.B, Selects("c.jpg"), _ =>
        {
            _tab.SetComparing(false);
            return ValueTask.FromResult(picked);
        });

        Assert.Null(_tab.Comparison);
        Assert.True(IsDisposed(picked));
    }
}
