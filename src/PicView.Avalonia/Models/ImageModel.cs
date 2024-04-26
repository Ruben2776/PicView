using Avalonia.Media;
using Avalonia.Media.Imaging;
using PicView.Avalonia.Navigation;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.Models;

public class ImageModel : IDisposable
{
    private bool _disposedValue;

    public object? Image { get; set; }
    public FileInfo? FileInfo { get; set; }
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
    public EXIFHelper.EXIFOrientation? EXIFOrientation { get; set; }
    public ImageType ImageType { get; set; }

    public double Rotation
    {
        get
        {
            if (!EXIFOrientation.HasValue)
            {
                return 0;
            }

            return EXIFOrientation switch
            {
                EXIFHelper.EXIFOrientation.None or EXIFHelper.EXIFOrientation.Normal
                    or EXIFHelper.EXIFOrientation.Flipped => 0,
                EXIFHelper.EXIFOrientation.Rotated180 or EXIFHelper.EXIFOrientation.Rotated180Flipped => 180,
                EXIFHelper.EXIFOrientation.Rotated270Flipped or EXIFHelper.EXIFOrientation.Rotated270 => 270,
                EXIFHelper.EXIFOrientation.Rotated90 or EXIFHelper.EXIFOrientation.Rotated90Flipped => 90,
                _ => 0
            };
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            if (Image is Bitmap bmp)
            {
                bmp.Dispose();
            }
            FileInfo = null;
        }

        // TODO: free unmanaged resources (unmanaged objects) and override finalizer
        // TODO: set large fields to null
        _disposedValue = true;
    }

    // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~ImageModel()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}