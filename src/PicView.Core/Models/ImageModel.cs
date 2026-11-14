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

    /// <summary>
    /// The SVG file a Single Image was drawn from, when the model names no <see cref="FileInfo"/>: the temp file
    /// that holds pasted SVG code. It is there to read the SVG's source again, never to browse from.
    /// </summary>
    public string? SvgSourcePath { get; set; }

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