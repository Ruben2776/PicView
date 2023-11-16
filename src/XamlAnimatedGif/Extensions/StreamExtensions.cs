using System.IO;

namespace XamlAnimatedGif.Extensions;

internal static class StreamExtensions
{
    public static async Task ReadAllAsync(this Stream stream, byte[] buffer, int offset, int count,
        CancellationToken cancellationToken = default)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
#if LACKS_STREAM_MEMORY_OVERLOADS
                int n = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken);
#else
            var n = await stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead),
                cancellationToken);
#endif
            if (n == 0)
                throw new EndOfStreamException();
            totalRead += n;
        }
    }

    public static void ReadAll(this Stream stream, byte[] buffer, int offset, int count)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var n = stream.Read(buffer, offset + totalRead, count - totalRead);
            if (n == 0)
                throw new EndOfStreamException();
            totalRead += n;
        }
    }

    public static async Task<int> ReadByteAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[1];
#if LACKS_STREAM_MEMORY_OVERLOADS
            int n = await stream.ReadAsync(buffer, 0, 1, cancellationToken);
#else
        var n = await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
#endif
        if (n == 0)
            return -1;
        return buffer[0];
    }

    public static Stream AsBuffered(this Stream stream)
    {
        if (stream is BufferedStream bs)
            return bs;
        return new BufferedStream(stream);
    }

    public static async Task CopyToAsync(this Stream source, Stream destination, IProgress<long> progress,
        int bufferSize = 81920, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[bufferSize];
        int bytesRead;
        long bytesCopied = 0;
#if LACKS_STREAM_MEMORY_OVERLOADS
            while ((bytesRead =
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
#else
        while ((bytesRead = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) !=
               0)
#endif
        {
#if LACKS_STREAM_MEMORY_OVERLOADS
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
#else
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
#endif
            bytesCopied += bytesRead;
            progress?.Report(bytesCopied);
        }
    }
}