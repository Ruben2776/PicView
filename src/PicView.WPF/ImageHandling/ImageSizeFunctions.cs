using System.Diagnostics;
using System.IO;
using System.Windows;
using ImageMagick;

namespace PicView.WPF.ImageHandling;

internal static class ImageSizeFunctions
{
    /// <summary>
    /// Gets the size of an image file.
    /// </summary>
    /// <param name="file">The path of the image file.</param>
    /// <returns>The size of the image if the file exists and the size can be determined; otherwise, null.</returns>
    internal static Size? GetImageSize(string file)
    {
        if (!File.Exists(file))
        {
            return null;
        }

        using var magick = new MagickImage();
        try
        {
            magick.Ping(file);
            if (magick.Width <= 0)
            {
                magick.Read(file);
            }
        }
#if DEBUG
        catch (MagickException e)
        {
            Trace.WriteLine("ImageSize returned " + file + " null\n" + e.Message);
            return null;
        }
#else
        catch (MagickException)
        {
            return null;
        }
#endif
        return new Size(magick.Width, magick.Height);
    }
}