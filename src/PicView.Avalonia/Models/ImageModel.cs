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

            switch (EXIFOrientation)
            {
                case EXIFHelper.EXIFOrientation.None:
                case EXIFHelper.EXIFOrientation.Normal:
                case EXIFHelper.EXIFOrientation.Flipped:
                    return 0;

                case EXIFHelper.EXIFOrientation.Rotated180:
                case EXIFHelper.EXIFOrientation.Rotated180Flipped:
                    return 180;

                case EXIFHelper.EXIFOrientation.Rotated270Flipped:
                case EXIFHelper.EXIFOrientation.Rotated270:
                    return 270;

                case EXIFHelper.EXIFOrientation.Rotated90:
                case EXIFHelper.EXIFOrientation.Rotated90Flipped:
                    return 90;
            }
            return 0;
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