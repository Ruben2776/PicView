using System.Buffers;
using ImageMagick;
using PicView.Core.FileHandling;

namespace PicView.Core.ImageReading;

public static class MagickPerformanceReader
{
    /// <summary>
    /// Reads the whole file into a pooled buffer and decodes it from a span.
    /// The work is synchronous; the async overload only exists for existing callers.
    /// </summary>
    public static MagickImage ReadMagickImageWithSpan(FileInfo fileInfo, MagickImage? image = null)
    {
        if (fileInfo.Length >= 2147483648)
        {
            return ReadMagickWithFileStream(fileInfo, image);
        }

        using var stream = File.OpenRead(fileInfo.FullName);

        // Get the length of the file to ensure the buffer is large enough.
        var fileLength = (int)stream.Length;
        if (fileLength == 0)
        {
            throw new InvalidDataException("Image file is empty.");
        }

        // Rent a buffer that is guaranteed to be large enough for the whole file.
        var buffer = ArrayPool<byte>.Shared.Rent(fileLength);

        try
        {
            // ReadExactly guarantees the entire file is read into the buffer.
            var imageSpan = buffer.AsSpan(0, fileLength);
            stream.ReadExactly(imageSpan);

            image ??= new MagickImage();
            image.Read(imageSpan);

            return image;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static ValueTask<MagickImage> ReadMagickImageWithSpanAsync(FileInfo fileInfo, MagickImage? image = null) =>
        ValueTask.FromResult(ReadMagickImageWithSpan(fileInfo, image));

    /// <summary>
    /// Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
    /// </summary>
    public static MagickImage ReadMagickWithFileStream(FileInfo fileInfo, MagickImage? image = null)
    {
        using var fileStream = FileStreamUtils.GetOptimizedFileStream(fileInfo);
        image.Read(fileStream);
        return image;
    }

    public static ValueTask<MagickImage> ReadMagickWithFileStreamAsync(FileInfo fileInfo, MagickImage? image = null) =>
        ValueTask.FromResult(ReadMagickWithFileStream(fileInfo, image));
}
