using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.ImageHandling;

namespace PicView.Avalonia.ImageEffects;

public static class ImageEffectsHelper
{
    public static async Task<WriteableBitmap> GetRadialBlur(string file)
    {
        using var magick = new MagickImage();
        await magick.ReadAsync(file).ConfigureAwait(false);
        // Apply radial blur
        magick.AdaptiveBlur(10);

        // Create a new morphology settings
        var morphology = new MorphologySettings
        {
            Kernel = Kernel.DoG,
            Method = MorphologyMethod.Convolve
        };
        magick.Morphology(morphology);
        return magick.ToWriteableBitmap();
    }
}
