using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using PicView.Core.DebugTools;

namespace PicView.Core.MacOS.Thumbnails;

/// <summary>
/// AOT-compatible macOS thumbnail extraction via ImageIO (CGImageSource).
/// Uses LibraryImport for source-generated interop and returns raw BGRA pixel data,
/// matching the layout produced by the Windows Shell thumbnail implementation.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class ShellThumbnailNative
{
    #region Native Libraries

    private const string CoreFoundationLib =
        "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    private const string CoreGraphicsLib =
        "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    private const string ImageIOLib =
        "/System/Library/Frameworks/ImageIO.framework/ImageIO";

    #endregion

    #region Native Structs and Constants

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
    }

    /// <summary>kCGImageAlphaPremultipliedFirst | kCGBitmapByteOrder32Little, which yields BGRA.</summary>
    private const uint BgraPremultipliedBitmapInfo = 2u | (2u << 12);

    /// <summary>kCFNumberIntType</summary>
    private const int CFNumberIntType = 9;

    /// <summary>Fallback thumbnail size when no valid size is requested.</summary>
    private const int DefaultMaxPixelSize = 256;

    /// <summary>Upper bound to avoid allocating unreasonably large buffers.</summary>
    private const int MaxAllowedPixelSize = 4096;

    #endregion

    #region Native Methods

    [LibraryImport(CoreFoundationLib)]
    private static partial void CFRelease(IntPtr cf);

    [LibraryImport(CoreFoundationLib)]
    private static unsafe partial IntPtr CFURLCreateFromFileSystemRepresentation(
        IntPtr allocator,
        byte* buffer,
        nint bufLen,
        [MarshalAs(UnmanagedType.U1)] bool isDirectory);

    [LibraryImport(CoreFoundationLib)]
    private static unsafe partial IntPtr CFNumberCreate(IntPtr allocator, int theType, void* valuePtr);

    [LibraryImport(CoreFoundationLib)]
    private static unsafe partial IntPtr CFDictionaryCreate(
        IntPtr allocator,
        IntPtr* keys,
        IntPtr* values,
        nint numValues,
        IntPtr keyCallBacks,
        IntPtr valueCallBacks);

    [LibraryImport(ImageIOLib)]
    private static partial IntPtr CGImageSourceCreateWithURL(IntPtr url, IntPtr options);

    [LibraryImport(ImageIOLib)]
    private static partial IntPtr CGImageSourceCreateThumbnailAtIndex(IntPtr isrc, nuint index, IntPtr options);

    [LibraryImport(CoreGraphicsLib)]
    private static partial nuint CGImageGetWidth(IntPtr image);

    [LibraryImport(CoreGraphicsLib)]
    private static partial nuint CGImageGetHeight(IntPtr image);

    [LibraryImport(CoreGraphicsLib)]
    private static partial void CGImageRelease(IntPtr image);

    [LibraryImport(CoreGraphicsLib)]
    private static partial IntPtr CGColorSpaceCreateDeviceRGB();

    [LibraryImport(CoreGraphicsLib)]
    private static partial void CGColorSpaceRelease(IntPtr space);

    [LibraryImport(CoreGraphicsLib)]
    private static unsafe partial IntPtr CGBitmapContextCreate(
        void* data,
        nuint width,
        nuint height,
        nuint bitsPerComponent,
        nuint bytesPerRow,
        IntPtr space,
        uint bitmapInfo);

    [LibraryImport(CoreGraphicsLib)]
    private static partial void CGContextDrawImage(IntPtr context, CGRect rect, IntPtr image);

    [LibraryImport(CoreGraphicsLib)]
    private static partial void CGContextRelease(IntPtr context);

    #endregion

    #region Public API

    /// <summary>
    /// Extracts a macOS thumbnail for the given file path as raw BGRA pixel data.
    /// </summary>
    /// <param name="path">Absolute file path.</param>
    /// <param name="width">Requested thumbnail width. Pass 0 to only constrain by height.</param>
    /// <param name="height">Requested thumbnail height. Pass 0 to only constrain by width.</param>
    /// <param name="pixelWidth">Actual width of the returned thumbnail in pixels.</param>
    /// <param name="pixelHeight">Actual height of the returned thumbnail in pixels.</param>
    /// <returns>Raw BGRA pixel data, or null on failure.</returns>
    public static byte[]? GetShellThumbnailBytes(string path, int width, int height,
        out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = 0;
        pixelHeight = 0;

        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        try
        {
            return CreateThumbnail(path, GetMaxPixelSize(width, height), out pixelWidth, out pixelHeight);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ShellThumbnailNative), nameof(GetShellThumbnailBytes), e);
            return null;
        }
    }

    #endregion

    #region Private Helpers

    private static int GetMaxPixelSize(int width, int height)
    {
        var size = Math.Max(width, height);
        if (size <= 0)
        {
            size = DefaultMaxPixelSize;
        }

        return Math.Min(size, MaxAllowedPixelSize);
    }

    private static unsafe byte[]? CreateThumbnail(string path, int maxPixelSize,
        out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = 0;
        pixelHeight = 0;

        var url = CreateFileUrl(path);
        if (url == IntPtr.Zero)
        {
            return null;
        }

        var imageSource = IntPtr.Zero;
        var options = IntPtr.Zero;
        var cgImage = IntPtr.Zero;

        try
        {
            imageSource = CGImageSourceCreateWithURL(url, IntPtr.Zero);
            if (imageSource == IntPtr.Zero)
            {
                return null;
            }

            options = CreateThumbnailOptions(maxPixelSize);
            if (options == IntPtr.Zero)
            {
                return null;
            }

            cgImage = CGImageSourceCreateThumbnailAtIndex(imageSource, 0, options);
            if (cgImage == IntPtr.Zero)
            {
                return null;
            }

            return ExtractBgraPixels(cgImage, out pixelWidth, out pixelHeight);
        }
        finally
        {
            if (cgImage != IntPtr.Zero)
            {
                CGImageRelease(cgImage);
            }

            if (options != IntPtr.Zero)
            {
                CFRelease(options);
            }

            if (imageSource != IntPtr.Zero)
            {
                CFRelease(imageSource);
            }

            CFRelease(url);
        }
    }

    private static unsafe IntPtr CreateFileUrl(string path)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(path);
        fixed (byte* pBytes = bytes)
        {
            return CFURLCreateFromFileSystemRepresentation(IntPtr.Zero, pBytes, bytes.Length, false);
        }
    }

    /// <summary>
    /// Builds the CFDictionary of options passed to CGImageSourceCreateThumbnailAtIndex.
    /// </summary>
    private static unsafe IntPtr CreateThumbnailOptions(int maxPixelSize)
    {
        var maxSizeKey = ImageIOSymbols.ThumbnailMaxPixelSizeKey;
        var createAlwaysKey = ImageIOSymbols.CreateThumbnailFromImageAlwaysKey;
        var transformKey = ImageIOSymbols.CreateThumbnailWithTransformKey;
        var cacheKey = ImageIOSymbols.ShouldCacheKey;
        var booleanTrue = ImageIOSymbols.BooleanTrue;
        var booleanFalse = ImageIOSymbols.BooleanFalse;

        if (maxSizeKey == IntPtr.Zero || createAlwaysKey == IntPtr.Zero ||
            transformKey == IntPtr.Zero || booleanTrue == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var maxSizeNumber = CFNumberCreate(IntPtr.Zero, CFNumberIntType, &maxPixelSize);
        if (maxSizeNumber == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        try
        {
            var count = cacheKey != IntPtr.Zero && booleanFalse != IntPtr.Zero ? 4 : 3;

            var keys = stackalloc IntPtr[4];
            var values = stackalloc IntPtr[4];

            keys[0] = maxSizeKey;
            values[0] = maxSizeNumber;
            keys[1] = createAlwaysKey;
            values[1] = booleanTrue;
            keys[2] = transformKey;
            values[2] = booleanTrue;
            if (count == 4)
            {
                keys[3] = cacheKey;
                values[3] = booleanFalse;
            }

            return CFDictionaryCreate(IntPtr.Zero, keys, values, count,
                ImageIOSymbols.TypeDictionaryKeyCallBacks, ImageIOSymbols.TypeDictionaryValueCallBacks);
        }
        finally
        {
            CFRelease(maxSizeNumber);
        }
    }

    /// <summary>
    /// Renders a CGImage into a top-down BGRA bitmap buffer.
    /// </summary>
    private static unsafe byte[]? ExtractBgraPixels(IntPtr cgImage, out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = 0;
        pixelHeight = 0;

        var imageWidth = (int)CGImageGetWidth(cgImage);
        var imageHeight = (int)CGImageGetHeight(cgImage);

        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return null;
        }

        var stride = imageWidth * 4;
        var pixels = new byte[stride * imageHeight];

        var colorSpace = CGColorSpaceCreateDeviceRGB();
        if (colorSpace == IntPtr.Zero)
        {
            return null;
        }

        var context = IntPtr.Zero;

        try
        {
            fixed (byte* pPixels = pixels)
            {
                context = CGBitmapContextCreate(pPixels, (nuint)imageWidth, (nuint)imageHeight, 8,
                    (nuint)stride, colorSpace, BgraPremultipliedBitmapInfo);
                if (context == IntPtr.Zero)
                {
                    return null;
                }

                var rect = new CGRect { X = 0, Y = 0, Width = imageWidth, Height = imageHeight };
                CGContextDrawImage(context, rect, cgImage);
            }

            pixelWidth = imageWidth;
            pixelHeight = imageHeight;
            return pixels;
        }
        finally
        {
            if (context != IntPtr.Zero)
            {
                CGContextRelease(context);
            }

            CGColorSpaceRelease(colorSpace);
        }
    }

    #endregion

    #region Exported Symbols

    /// <summary>
    /// Resolves the CFString option keys and CFBoolean constants exported as data symbols,
    /// since they cannot be referenced through P/Invoke.
    /// </summary>
    private static class ImageIOSymbols
    {
        internal static readonly IntPtr ThumbnailMaxPixelSizeKey =
            ReadPointer(ImageIOLib, "kCGImageSourceThumbnailMaxPixelSize");

        internal static readonly IntPtr CreateThumbnailFromImageAlwaysKey =
            ReadPointer(ImageIOLib, "kCGImageSourceCreateThumbnailFromImageAlways");

        internal static readonly IntPtr CreateThumbnailWithTransformKey =
            ReadPointer(ImageIOLib, "kCGImageSourceCreateThumbnailWithTransform");

        internal static readonly IntPtr ShouldCacheKey =
            ReadPointer(ImageIOLib, "kCGImageSourceShouldCache");

        internal static readonly IntPtr BooleanTrue =
            ReadPointer(CoreFoundationLib, "kCFBooleanTrue");

        internal static readonly IntPtr BooleanFalse =
            ReadPointer(CoreFoundationLib, "kCFBooleanFalse");

        internal static readonly IntPtr TypeDictionaryKeyCallBacks =
            GetExport(CoreFoundationLib, "kCFTypeDictionaryKeyCallBacks");

        internal static readonly IntPtr TypeDictionaryValueCallBacks =
            GetExport(CoreFoundationLib, "kCFTypeDictionaryValueCallBacks");

        /// <summary>
        /// Returns the address of an exported data symbol.
        /// </summary>
        private static IntPtr GetExport(string library, string symbol)
        {
            try
            {
                if (!NativeLibrary.TryLoad(library, out var handle))
                {
                    return IntPtr.Zero;
                }

                return NativeLibrary.TryGetExport(handle, symbol, out var address) ? address : IntPtr.Zero;
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(ImageIOSymbols), nameof(GetExport), e);
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Reads the pointer value stored in an exported data symbol, such as a CFStringRef constant.
        /// </summary>
        private static unsafe IntPtr ReadPointer(string library, string symbol)
        {
            var address = GetExport(library, symbol);
            return address == IntPtr.Zero ? IntPtr.Zero : *(IntPtr*)address;
        }
    }

    #endregion
}
