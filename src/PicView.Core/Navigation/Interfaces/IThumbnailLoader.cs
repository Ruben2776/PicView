using ImageMagick;

namespace PicView.Core.Navigation.Interfaces;

public interface IThumbnailLoader
{
    ValueTask<object?> GetThumbnailAsync(FileInfo file);
    ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, MagickImage? magick = null);
    object? GetExifThumbnail(FileInfo file);
    object? GetThumbQuick(FileInfo file);
}
