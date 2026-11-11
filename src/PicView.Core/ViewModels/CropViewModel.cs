using PicView.Core.Sizing;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// A tab's open crop. Its selection is held as a whole-pixel rectangle of the image, the one truth, and the values
/// the crop surface draws are derived from it through the tab's Fit Scale (ADR-0027). <see cref="SetSelection"/> is
/// the only way in.
/// </summary>
public class CropViewModel(TabZoom zoom, uint imagePixelWidth, uint imagePixelHeight) : IDisposable
{
    private readonly BindableReactiveProperty<int> _pixelX = new();
    private readonly BindableReactiveProperty<int> _pixelY = new();
    private readonly BindableReactiveProperty<uint> _pixelSelectionWidth = new();
    private readonly BindableReactiveProperty<uint> _pixelSelectionHeight = new();
    private readonly BindableReactiveProperty<double> _selectionX = new();
    private readonly BindableReactiveProperty<double> _selectionY = new();
    private readonly BindableReactiveProperty<double> _selectionWidth = new();
    private readonly BindableReactiveProperty<double> _selectionHeight = new();

    public ReactiveCommand CropImageCommand { get; } = new();
    public ReactiveCommand CopyCropImageCommand { get; } = new();
    public ReactiveCommand CloseCropCommand { get; } = new();

    /// <summary>The selection's left edge, in image pixels.</summary>
    public IReadOnlyBindableReactiveProperty<int> PixelX => _pixelX;

    /// <summary>The selection's top edge, in image pixels.</summary>
    public IReadOnlyBindableReactiveProperty<int> PixelY => _pixelY;

    public IReadOnlyBindableReactiveProperty<uint> PixelSelectionWidth => _pixelSelectionWidth;
    public IReadOnlyBindableReactiveProperty<uint> PixelSelectionHeight => _pixelSelectionHeight;

    /// <summary>The selection's left edge as the crop surface draws it: <see cref="PixelX"/> times the Fit Scale.</summary>
    public IReadOnlyBindableReactiveProperty<double> SelectionX => _selectionX;

    public IReadOnlyBindableReactiveProperty<double> SelectionY => _selectionY;
    public IReadOnlyBindableReactiveProperty<double> SelectionWidth => _selectionWidth;
    public IReadOnlyBindableReactiveProperty<double> SelectionHeight => _selectionHeight;

    /// <summary>
    /// The tab's Fit Scale, which the crop area is drawn at, so a selection in view units divides by it to give pixels.
    /// </summary>
    public double AspectRatio => zoom.FitScale;

    /// <summary>
    /// Sets the selection from a rectangle in view units: rounded to whole image pixels, kept inside the image and
    /// at least one pixel each way. The view values then follow the pixels, so the drawn rectangle sits on them.
    /// </summary>
    public void SetSelection(double x, double y, double width, double height)
    {
        var scale = AspectRatio;
        if (scale <= 0 || double.IsNaN(scale) || double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(width) ||
            double.IsNaN(height) || imagePixelWidth == 0 || imagePixelHeight == 0)
        {
            return;
        }

        var (pixelX, pixelWidth) = ToPixels(x, width, scale, imagePixelWidth);
        var (pixelY, pixelHeight) = ToPixels(y, height, scale, imagePixelHeight);
        _pixelX.Value = pixelX;
        _pixelY.Value = pixelY;
        _pixelSelectionWidth.Value = pixelWidth;
        _pixelSelectionHeight.Value = pixelHeight;
        Refit();
    }

    /// <summary>
    /// Derives the view values again from the pixels, as when the Fit Scale changed under an open crop.
    /// </summary>
    public void Refit()
    {
        var scale = AspectRatio;
        _selectionX.Value = _pixelX.Value * scale;
        _selectionY.Value = _pixelY.Value * scale;
        _selectionWidth.Value = _pixelSelectionWidth.Value * scale;
        _selectionHeight.Value = _pixelSelectionHeight.Value * scale;
    }

    private static (int Start, uint Length) ToPixels(double start, double length, double scale, uint imageLength)
    {
        // Each edge is rounded on its own, so resizing by one edge never moves the opposite one
        var first = Math.Clamp(Math.Round(start / scale, MidpointRounding.AwayFromZero), 0, imageLength - 1);
        var end = Math.Clamp(Math.Round((start + length) / scale, MidpointRounding.AwayFromZero), first + 1,
            imageLength);
        return ((int)first, (uint)(end - first));
    }

    public void Dispose()
    {
        CropImageCommand.Dispose();
        CopyCropImageCommand.Dispose();
        CloseCropCommand.Dispose();
        _pixelX.Dispose();
        _pixelY.Dispose();
        _pixelSelectionWidth.Dispose();
        _pixelSelectionHeight.Dispose();
        _selectionX.Dispose();
        _selectionY.Dispose();
        _selectionWidth.Dispose();
        _selectionHeight.Dispose();
        GC.SuppressFinalize(this);
    }
}
