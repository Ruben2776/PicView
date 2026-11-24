using System.Globalization;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;

namespace PicView.Core.Exif;

public static class ExifFunctions
{
    /// <summary>
    /// Determines whether the specified format is an EXIF-supported image type.
    /// </summary>
    /// <param name="format">The MagickFormat of the image to check.</param>
    /// <returns>True if the format supports EXIF metadata; otherwise, false.</returns>
    public static bool IsExifImage(this MagickFormat? format)
    {
        if (format is null)
        {
            return false;
        }

        return format.Value switch
        {
            MagickFormat.Jpeg or
                MagickFormat.Tiff or
                MagickFormat.Dng or // Adobe Digital Negative
                MagickFormat.Cr2 or // Canon RAW
                MagickFormat.Cr3 or
                MagickFormat.Nef or // Nikon RAW
                MagickFormat.Arw or // Sony RAW
                MagickFormat.Orf or // Olympus RAW
                MagickFormat.Rw2 or // Panasonic RAW
                MagickFormat.Pef or // Pentax RAW
                MagickFormat.Raf or // Fujifilm RAW
                MagickFormat.Srw or // Samsung RAW
                MagickFormat.Heif or
                MagickFormat.Heic // High Efficiency Image Format
                => true,

            _ => false
        };
    }

    /// <summary>
    /// How long an update keeps retrying while another reader, such as the gallery or the file watcher reacting to
    /// the previous update, has the file open.
    /// </summary>
    private const int FileBusyTimeoutMs = 3000;

    private const int FileBusyRetryDelayMs = 50;

    /// <summary>
    /// Lets one update at a time touch the disk, so two updates of the same file don't race each other.
    /// </summary>
    private static readonly SemaphoreSlim UpdateGate = new(1, 1);

    /// <param name="allowEncode">
    /// Whether a file that cannot have its segments rewritten may be decoded and encoded again. When false, such a
    /// file, a non-JPEG or a JPEG the rewrite cannot do, is left untouched and false is returned.
    /// </param>
    public static Task<bool> TryUpdateImageProfileAsync(FileInfo fileInfo, Func<MagickImage, bool> updateAction,
        string callingClassName, string callingMethodName, bool allowEncode = true) =>
        RewriteFileAsync(fileInfo, original =>
        {
            // A JPEG gets its metadata segments written and keeps its image data as it is, so an edit loses no
            // quality. Any other file, or a JPEG the rewrite cannot read, is encoded again
            if (!JpegMetadataWriter.IsJpeg(original))
            {
                return allowEncode ? Encode(original, updateAction) : null;
            }

            var rewrite = RewriteJpegMetadata(original, updateAction);
            if (rewrite.IsUnchanged)
            {
                return null;
            }

            return rewrite.Bytes ?? (allowEncode ? Encode(original, updateAction) : null);
        }, callingClassName, callingMethodName);

    /// <summary>
    /// Rewrites the bytes of <paramref name="fileInfo"/> without decoding it, under the same gate and busy retry as
    /// <see cref="TryUpdateImageProfileAsync"/>.
    /// </summary>
    /// <param name="rewrite">Gets the file's bytes and returns the new ones, or null to leave the file alone.</param>
    /// <returns>False when nothing was written: no file, null or unchanged bytes, or a failure.</returns>
    internal static Task<bool> TryRewriteFileAsync(FileInfo fileInfo, Func<byte[], byte[]?> rewrite,
        string callingClassName, string callingMethodName) =>
        RewriteFileAsync(fileInfo, original =>
        {
            var bytes = rewrite(original);
            return bytes is null || bytes.AsSpan().SequenceEqual(original) ? null : bytes;
        }, callingClassName, callingMethodName);

    private static async Task<bool> RewriteFileAsync(FileInfo fileInfo, Func<byte[], byte[]?> rewrite,
        string callingClassName, string callingMethodName)
    {
        if (fileInfo is null || !File.Exists(fileInfo.FullName))
        {
            return false;
        }

        await UpdateGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var startTime = Environment.TickCount64;
            var original = await RetryWhileBusyAsync(() => File.ReadAllBytes(fileInfo.FullName), startTime)
                .ConfigureAwait(false);

            // No bytes means the rewrite made no changes that require saving
            var bytes = rewrite(original);
            if (bytes is null)
            {
                return false;
            }

            // Build the file before opening it, so it is truncated for as short a time as possible
            await RetryWhileBusyAsync(() =>
            {
                using var stream = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
                stream.Write(bytes);
                return true;
            }, startTime).ConfigureAwait(false);
            fileInfo.Refresh();
            return true;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(callingClassName, callingMethodName, e);
            return false;
        }
        finally
        {
            UpdateGate.Release();
        }
    }

    private readonly record struct JpegRewrite(byte[]? Bytes, bool IsUnchanged);

    /// <summary>
    /// Runs <paramref name="updateAction"/> on the metadata of <paramref name="jpeg"/>, read without its pixels, and
    /// writes the result into the file's segments. Bytes is null when the file must be encoded instead.
    /// </summary>
    private static JpegRewrite RewriteJpegMetadata(byte[] jpeg, Func<MagickImage, bool> updateAction)
    {
        using var metadata = new MagickImage();
        metadata.Ping(jpeg);
        var before = PresentMetadata(metadata);
        if (!updateAction(metadata))
        {
            return new JpegRewrite(null, IsUnchanged: true);
        }

        // Only what the update removed is left out: what Ping does not see is kept
        var removed = before & ~PresentMetadata(metadata);
        var exif = metadata.GetExifProfile()?.ToByteArray();
        var hasExif = exif is { Length: > 0 };
        return new JpegRewrite(JpegMetadataWriter.Rewrite(jpeg, exif, hasExif, removed), IsUnchanged: false);
    }

    private static JpegMetadataWriter.Drop PresentMetadata(MagickImage image)
    {
        var present = JpegMetadataWriter.Drop.None;
        if (image.GetXmpProfile() is not null)
        {
            present |= JpegMetadataWriter.Drop.Xmp;
        }

        if (image.GetIptcProfile() is not null || image.GetProfile("8bim") is not null)
        {
            present |= JpegMetadataWriter.Drop.Iptc;
        }

        if (image.GetColorProfile() is not null)
        {
            present |= JpegMetadataWriter.Drop.ColorProfile;
        }

        if (!string.IsNullOrEmpty(image.Comment))
        {
            present |= JpegMetadataWriter.Drop.Comment;
        }

        return present;
    }

    /// <summary>Decodes <paramref name="file"/>, runs <paramref name="updateAction"/> and encodes it again.</summary>
    /// <returns>The new file, or null when the update changed nothing.</returns>
    private static byte[]? Encode(byte[] file, Func<MagickImage, bool> updateAction)
    {
        using var magickImage = new MagickImage(file);
        return updateAction(magickImage) ? magickImage.ToByteArray() : null;
    }

    /// <summary>
    /// Runs <paramref name="action"/>, trying again while the file is open elsewhere, until
    /// <see cref="FileBusyTimeoutMs"/> has passed since <paramref name="startTime"/>.
    /// </summary>
    private static async Task<T> RetryWhileBusyAsync<T>(Func<T> action, long startTime)
    {
        while (true)
        {
            try
            {
                return action();
            }
            catch (Exception e) when (IsFileBusy(e) && Environment.TickCount64 - startTime < FileBusyTimeoutMs)
            {
                await Task.Delay(FileBusyRetryDelayMs).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Whether <paramref name="e"/> means another handle has the file open: a sharing violation for .NET, or
    /// "Permission denied" when ImageMagick opens it.
    /// </summary>
    private static bool IsFileBusy(Exception e) => e switch
    {
        FileNotFoundException or DirectoryNotFoundException => false,
        IOException or UnauthorizedAccessException or MagickBlobErrorException => true,
        _ => false
    };

    public static bool TryParseRational(string input, out Rational result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Handle fraction format "num/den"
        if (input.Contains('/'))
        {
            var parts = input.Split('/');
            if (parts.Length == 2 && double.TryParse(parts[0], CultureInfo.InvariantCulture, out var num) &&
                double.TryParse(parts[1], CultureInfo.InvariantCulture, out var den))
            {
                if (den == 0)
                {
                    return false;
                }

                // Only whole numbers fit the numerator and denominator. "1/0.5" would become 1/0 as a cast,
                // so any other ratio is stored by its value
                if (num is >= 0 and <= uint.MaxValue && den is > 0 and <= uint.MaxValue &&
                    num == Math.Floor(num) && den == Math.Floor(den))
                {
                    result = new Rational((uint)num, (uint)den);
                    return true;
                }

                var ratio = num / den;
                if (!double.IsFinite(ratio) || ratio < 0)
                {
                    return false;
                }

                result = new Rational(ratio);
                return true;
            }
        }

        // Handle decimal format
        if (!double.TryParse(input, CultureInfo.InvariantCulture, out var val))
        {
            return false;
        }

        result = new Rational(val);
        return true;
    }

    /// <summary>
    /// Tries to parse a string into a SignedRational.
    /// </summary>
    /// <param name="input">The string to parse (e.g., "-10/20" or "-0.5").</param>
    /// <param name="result">The parsed SignedRational.</param>
    /// <returns>True if parsing was successful, otherwise false.</returns>
    public static bool TryParseSignedRational(string input, out SignedRational result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Handle fraction format "num/den"
        if (input.Contains('/'))
        {
            var parts = input.Split('/');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var numerator) &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var denominator))
            {
                if (denominator == 0)
                {
                    return false; // Avoid division by zero
                }

                result = new SignedRational(numerator, denominator, false);
                return true;
            }
        }

        // Handle decimal format
        if (!double.TryParse(input, CultureInfo.InvariantCulture, out var val))
        {
            return false;
        }

        result = new SignedRational(val);
        return true;
    }
}