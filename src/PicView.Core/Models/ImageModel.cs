using System.Diagnostics;
using PicView.Core.ImageDecoding;
using PicView.Core.MotionPhoto;
using PicView.Core.Navigation.Tiff;

namespace PicView.Core.Models;

[DebuggerDisplay("{FileInfo.Name}, {PixelWidth}x{PixelHeight}")]
public class ImageModel : IDisposable
{
    public object? Image { get; set; }
    public FileInfo? FileInfo { get; set; }
    public uint PixelWidth { get; set; }
    public uint PixelHeight { get; set; }
    public ImageType ImageType { get; set; }
    public TiffNavigationInfo? TiffNavigation { get; set; }
    public MotionPhotoInfo? MotionPhoto { get; set; }
    
    public void Dispose()
    {
        if (Image is IDisposable img)
        {
            img.Dispose();
        }

        if (TiffNavigation is not null)
        {
            foreach (var page in TiffNavigation.Pages)
            {
                if (page is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        GC.SuppressFinalize(this);
    }
}