using PicView.Core.Sizing;
using R3;

namespace PicView.Core.ViewModels;

public class CropViewModel(TabZoom zoom) : IDisposable
{
    public ReactiveCommand CropImageCommand { get; } = new();
    public ReactiveCommand CopyCropImageCommand { get; } = new();
    public ReactiveCommand CloseCropCommand { get; } = new();
    
    public BindableReactiveProperty<int> SelectionX { get; } = new();
    public BindableReactiveProperty<int> SelectionY { get; } = new();

    public BindableReactiveProperty<double> SelectionWidth { get; } = new();
    public BindableReactiveProperty<double> SelectionHeight { get; } = new();
    
    public BindableReactiveProperty<uint> PixelSelectionWidth { get; } = new();
    public BindableReactiveProperty<uint> PixelSelectionHeight { get; } = new();
    
    /// <summary>
    /// The tab's Fit Scale, which the crop area is drawn at, so a selection in view units divides by it to give pixels.
    /// </summary>
    public double AspectRatio => zoom.FitScale;
    
    public void SetSelectionWidth(uint value)
    {
        SelectionWidth.Value = value;
        PixelSelectionWidth.Value = Convert.ToUInt32(value / AspectRatio);
    }

    public void SetSelectionHeight(uint value)
    {
        SelectionHeight.Value = value;
        PixelSelectionHeight.Value = Convert.ToUInt32(value / AspectRatio);
    }


    public void Dispose()
    {
        CropImageCommand.Dispose();
        CopyCropImageCommand.Dispose();
        CloseCropCommand.Dispose();
        SelectionX.Dispose();
        SelectionY.Dispose();
        SelectionWidth.Dispose();
        PixelSelectionWidth.Dispose();
        SelectionHeight.Dispose();
        PixelSelectionHeight.Dispose();
        GC.SuppressFinalize(this);
    }
}