using System.Globalization;
using System.Numerics;

namespace PicView.Core.Extensions;

public static class FileExtensions
{
    private static ReadOnlySpan<char> Suffixes => ['B', 'K', 'M', 'G', 'T', 'P', 'E'];

    /// <summary>
    /// Returns the human-readable file size for an arbitrary, 64-bit file size
    /// The default format is "0.## XB", e.g. "4.2 KB" or "1.43 GB"
    /// </summary>
    /// <param name="fileSize">FileInfo.Length</param>
    /// <returns>A formatted string representing the file size with a suffix (e.g., "4.2 KB").</returns>
    public static string GetReadableFileSize(this long fileSize)
    {
        if (fileSize <= 0)
        {
            return "0 B";
        }

        var magnitude = BitOperations.Log2((ulong)fileSize) / 10;

        return magnitude is 0 ? FormatBytes(fileSize) : FormatWithSuffix(fileSize, magnitude);
    }

    /// <summary>
    /// Formats a file size in bytes into a human-readable string in the "X B" format.
    /// </summary>
    /// <param name="bytes">The size of the file in bytes to format.</param>
    /// <returns>A string representing the file size in bytes with the " B" suffix (e.g., "1024 B").</returns>
    private static string FormatBytes(long bytes)
    {
        Span<char> buffer = stackalloc char[16];
        if (!bytes.TryFormat(buffer, out var charsWritten))
        {
            return "0 B";
        }

        buffer[charsWritten++] = ' ';
        buffer[charsWritten++] = 'B';
        return new string(buffer[..charsWritten]);
    }

    /// <summary>
    /// Formats the given file size into a human-readable string with the appropriate suffix,
    /// supporting up to exabytes (EB).
    /// </summary>
    /// <param name="fileSize">The size of the file in bytes.</param>
    /// <param name="magnitude">The magnitude of the size, determining the suffix (e.g., KB, MB).</param>
    /// <returns>A formatted string representing the file size with a suffix (e.g., "4.2 KB").</returns>
    private static string FormatWithSuffix(long fileSize, int magnitude)
    {
        Span<char> buffer = stackalloc char[16];

        var divisor = 1L << (magnitude * 10);
        var whole = fileSize / divisor;
        var fraction = fileSize % divisor;

        // Format whole part
        whole.TryFormat(buffer, out var charsWritten);

        // Handle fractional part only if needed
        if (fraction > 0) // Only show decimals for non-byte sizes
        {
            // Calculate two decimal places efficiently
            var decimalValue = fraction * 100 / divisor;
            if (decimalValue > 0)
            {
                // Add dot or comma, depending on culture
                buffer[charsWritten++] = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

                // Format first decimal digit
                var firstDigit = decimalValue / 10;
                buffer[charsWritten++] = (char)('0' + firstDigit);

                // Format second digit only if non-zero
                var secondDigit = decimalValue % 10;
                if (secondDigit > 0)
                {
                    buffer[charsWritten++] = (char)('0' + secondDigit);
                }
            }
        }

        // Append suffix
        buffer[charsWritten++] = ' ';
        buffer[charsWritten++] = Suffixes[magnitude];
        buffer[charsWritten++] = 'B';

        return new string(buffer[..charsWritten]);
    }
}