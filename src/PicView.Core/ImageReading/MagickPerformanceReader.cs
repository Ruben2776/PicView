using System.Buffers;
using ImageMagick;
using PicView.Core.FileHandling;

namespace PicView.Core.ImageReading;

public static class MagickPerformanceReader
{
    public static async ValueTask<MagickImage> ReadMagickImageWithSpanAsync(FileInfo fileInfo,
        MagickImage? image = null)
    {
        if (fileInfo.Length >= 2147483648)
        {
            return await ReadMagickWithFileStreamAsync(fileInfo, image).ConfigureAwait(false);
        }

        var stream = File.OpenRead(fileInfo.FullName);
        await using (stream.ConfigureAwait(false))
        {

            // Get the length of the file to ensure the buffer is large enough.
            var fileLength = (int)stream.Length;
            if (fileLength == 0)
            {
                // Handle empty files if necessary, perhaps by throwing an exception.
                throw new InvalidDataException("Image file is empty.");
            }

            // Rent a buffer that is guaranteed to be large enough for the whole file.
            var buffer = ArrayPool<byte>.Shared.Rent(fileLength);

            try
            {
                // Use ReadExactly to guarantee the entire file is read into the buffer.
                // This is safer than a single .Read() call.
                stream.ReadExactly(buffer.AsSpan(0, fileLength));

                // Create a span covering the exact file data.
                ReadOnlySpan<byte> imageSpan = buffer.AsSpan(0, fileLength);

                image ??= new MagickImage();
                image.Read(imageSpan); // Read from the complete image data.

                return image;
            }
            finally
            {
                // Always return the buffer to the pool.
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    public static async ValueTask<MagickImage> ReadMagickWithFileStreamAsync(FileInfo fileInfo, MagickImage? image = null)
        {
            // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
            var fileStream = FileStreamUtils.GetOptimizedFileStream(fileInfo);
            await using (fileStream.ConfigureAwait(false))
            {
                // ReSharper disable once MethodHasAsyncOverload
#pragma warning disable MA0042
                image.Read(fileStream);
#pragma warning restore MA0042
                return image;
            }
        }
}