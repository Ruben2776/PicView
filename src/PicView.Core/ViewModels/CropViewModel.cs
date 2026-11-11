using PicView.Core.ImageTransformations;
using PicView.Core.Sizing;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// A tab's open crop. Its selection is held as a whole-pixel rectangle of the image as shown (turned and flipped by
/// the tab's Orientation), the one truth, and the values the crop surface draws are derived from it through the tab's
/// Fit Scale (ADR-0027, ADR-0028). <see cref="SetSelection"/> is the only way in besides the tab's Orientation, which
/// the selection follows while the crop is open.
/// </summary>
public class CropViewModel : IDisposable
{
    private readonly TabZoom _zoom;
    private readonly uint _sourcePixelWidth;
    private readonly uint _sourcePixelHeight;
    private readonly Subject<Unit> _turned = new();
    private readonly IDisposable? _orientationSubscription;
    private readonly BindableReactiveProperty<double> _shownWidth = new();
    private readonly BindableReactiveProperty<double> _shownHeight = new();
    private readonly BindableReactiveProperty<int> _pixelX = new();
    private readonly BindableReactiveProperty<int> _pixelY = new();
    private readonly BindableReactiveProperty<uint> _pixelSelectionWidth = new();
    private readonly BindableReactiveProperty<uint> _pixelSelectionHeight = new();
    private readonly BindableReactiveProperty<double> _selectionX = new();
    private readonly BindableReactiveProperty<double> _selectionY = new();
    private readonly BindableReactiveProperty<double> _selectionWidth = new();
    private readonly BindableReactiveProperty<double> _selectionHeight = new();

    /// <param name="zoom">The tab's zoom, whose Fit Scale the crop area is drawn at.</param>
    /// <param name="imagePixelWidth">The width of the unturned source image, in pixels.</param>
    /// <param name="imagePixelHeight">The height of the unturned source image, in pixels.</param>
    /// <param name="orientation">The tab's Orientation to follow; without it the image is always shown upright.</param>
    public CropViewModel(TabZoom zoom, uint imagePixelWidth, uint imagePixelHeight,
        TabOrientation? orientation = null)
    {
        _zoom = zoom;
        _sourcePixelWidth = imagePixelWidth;
        _sourcePixelHeight = imagePixelHeight;
        Orientation = orientation?.Current.CurrentValue ?? Orientation.Upright;
        (ShownPixelWidth, ShownPixelHeight) = OrientedRect.ShownSize(Orientation, imagePixelWidth, imagePixelHeight);
        Refit();
        _orientationSubscription = orientation?.Current.Subscribe(Follow);
    }

    public ReactiveCommand CropImageCommand { get; } = new();

    /// <summary>The Orientation the selection is currently shown under.</summary>
    public Orientation Orientation { get; private set; }

    /// <summary>The width of the image as shown, in pixels.</summary>
    public uint ShownPixelWidth { get; private set; }

    /// <summary>The height of the image as shown, in pixels.</summary>
    public uint ShownPixelHeight { get; private set; }

    /// <summary>The image as shown, as the crop surface draws it: <see cref="ShownPixelWidth"/> times the Fit Scale.</summary>
    public IReadOnlyBindableReactiveProperty<double> ShownWidth => _shownWidth;

    public IReadOnlyBindableReactiveProperty<double> ShownHeight => _shownHeight;

    /// <summary>
    /// Raised after the selection has been remapped and refitted because the tab's Orientation changed.
    /// </summary>
    public Observable<Unit> Turned => _turned;

    /// <summary>The current selection in the unturned source image, which is what gets cropped.</summary>
    public PixelArea SourceSelection => OrientedRect.ToSource(CurrentArea(), Orientation, _sourcePixelWidth,
        _sourcePixelHeight);
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
    public double AspectRatio => _zoom.FitScale;

    /// <summary>
    /// Sets the selection from a rectangle in view units: rounded to whole image pixels, kept inside the image and
    /// at least one pixel each way. The view values then follow the pixels, so the drawn rectangle sits on them.
    /// </summary>
    public void SetSelection(double x, double y, double width, double height)
    {
        var scale = AspectRatio;
        if (scale <= 0 || double.IsNaN(scale) || double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(width) ||
            double.IsNaN(height) || ShownPixelWidth == 0 || ShownPixelHeight == 0)
        {
            return;
        }

        var (pixelX, pixelWidth) = ToPixels(x, width, scale, ShownPixelWidth);
        var (pixelY, pixelHeight) = ToPixels(y, height, scale, ShownPixelHeight);
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
        _shownWidth.Value = ShownPixelWidth * scale;
        _shownHeight.Value = ShownPixelHeight * scale;
    }

    private PixelArea CurrentArea() =>
        new(_pixelX.Value, _pixelY.Value, _pixelSelectionWidth.Value, _pixelSelectionHeight.Value);

    private void Follow(Orientation orientation)
    {
        if (orientation == Orientation)
        {
            return;
        }

        var source = OrientedRect.ToSource(CurrentArea(), Orientation, _sourcePixelWidth, _sourcePixelHeight);
        var shown = OrientedRect.FromSource(source, orientation, _sourcePixelWidth, _sourcePixelHeight);
        Orientation = orientation;
        (ShownPixelWidth, ShownPixelHeight) = OrientedRect.ShownSize(orientation, _sourcePixelWidth,
            _sourcePixelHeight);
        _pixelX.Value = shown.X;
        _pixelY.Value = shown.Y;
        _pixelSelectionWidth.Value = shown.Width;
        _pixelSelectionHeight.Value = shown.Height;
        Refit();
        _turned.OnNext(Unit.Default);
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
        _orientationSubscription?.Dispose();
        _turned.Dispose();
        _shownWidth.Dispose();
        _shownHeight.Dispose();
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
