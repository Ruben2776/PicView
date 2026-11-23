using ImageMagick;
using PicView.Core.ViewModels;

namespace PicView.Core.Saving;

/// <summary>
/// Reads the pixels of a tab's Single Image (clipboard, base64), which only the UI layer can.
/// </summary>
public interface ISingleImagePixels
{
    /// <summary>
    /// A copy of the tab's in-memory image as a MagickImage, or null when it has none. The caller owns and disposes it.
    /// </summary>
    MagickImage? Read(TabViewModel tab);
}
