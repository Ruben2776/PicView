using ImageMagick;

namespace PicView.Core.ImageDecoding;

public static class TiffManager
{
    public static MagickImageCollection? LoadTiffPages(string path)
    {
        var settings = new MagickReadSettings
        {
            // Specify that we want to read a TIFF format image
            Format = MagickFormat.Tiff
        };

        var pageCollection = new MagickImageCollection(path, settings);
        foreach (var page in pageCollection)
        {
            page.Quality = 100;
        }

        return pageCollection;
    }
}