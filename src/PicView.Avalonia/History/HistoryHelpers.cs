using System;
using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Core.ViewModels;
using PicView.Core.Extensions;
using PicView.Avalonia.Extensions;

namespace PicView.Avalonia.History;

public static class HistoryHelpers
{
    public static MagickImage? EnsureMagickFrame(PicViewerModel model)
    {
        if (model.MagickFrame.Value is MagickImage magick)
            return magick;

        if (model.ImageSource.Value is Bitmap bmp)
        {
            magick = (MagickImage)bmp.ToMagickImage();
            model.MagickFrame.Value = magick;
            return magick;
        }

        return null;
    }

    public static Bitmap CloneAndResizeToBitmap(this MagickImage source, int maxWidth)
    {
        using var clone = (MagickImage)source.Clone();
        clone.Resize((uint)maxWidth, 0);
        return clone.ToAvaloniaBitmap();
    }
}
