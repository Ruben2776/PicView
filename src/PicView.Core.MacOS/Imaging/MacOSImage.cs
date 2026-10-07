using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using PicView.Core.DebugTools;

namespace PicView.Core.MacOS.Imaging;

/// <summary>
/// AOT-compatible macOS native image decoding fallback via ImageIO (CGImageSource).
/// Uses LibraryImport for source-generated interop and returns raw BGRA pixel data,
/// matching the layout produced by the Windows WIC fallback implementation.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class MacOSImage
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

    /// <summary>Upper bound to avoid allocating unreasonably large buffers.</summary>
    private const int MaxAllowedPixelSize = 16384;

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

    [LibraryImport(ImageIOLib)]
    private static partial IntPtr CGImageSourceCreateWithURL(IntPtr url, IntPtr options);

    [LibraryImport(ImageIOLib)]
    private static partial IntPtr CGImageSourceCreateImageAtIndex(IntPtr isrc, nuint index, IntPtr options);

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
    /// Decodes an image file using macOS's native ImageIO (CGImageSource) and returns raw BGRA pixel data.
    /// Acts as the macOS counterpart to the Windows WIC fallback decoder.
    /// </summary>
    /// <param name="path">Absolute file path to the image.</param>
    /// <param name="pixelWidth">Actual width of the decoded image in pixels.</param>
    /// <param name="pixelHeight">Actual height of the decoded image in pixels.</param>
    /// <returns>Raw 32-bit premultiplied BGRA pixel data, or null on failure.</returns>
    public static byte[]? GetPixelsFromNativeImagingComponent(string path, out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = 0;
        pixelHeight = 0;

        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        try
        {
            return DecodeImage(path, out pixelWidth, out pixelHeight);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MacOSImage), nameof(GetPixelsFromNativeImagingComponent), e);
            pixelWidth = 0;
            pixelHeight = 0;
            return null;
        }
    }

    #endregion

    #region Private Helpers

    private static unsafe IntPtr CreateFileUrl(string path)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(path);
        fixed (byte* pBytes = bytes)
        {
            return CFURLCreateFromFileSystemRepresentation(IntPtr.Zero, pBytes, bytes.Length, false);
        }
    }

    private static unsafe byte[]? DecodeImage(string path, out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = 0;
        pixelHeight = 0;

        var url = CreateFileUrl(path);
        if (url == IntPtr.Zero)
        {
            return null;
        }

        var imageSource = IntPtr.Zero;
        var cgImage = IntPtr.Zero;

        try
        {
            imageSource = CGImageSourceCreateWithURL(url, IntPtr.Zero);
            if (imageSource == IntPtr.Zero)
            {
                return null;
            }

            cgImage = CGImageSourceCreateImageAtIndex(imageSource, 0, IntPtr.Zero);
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

            if (imageSource != IntPtr.Zero)
            {
                CFRelease(imageSource);
            }

            CFRelease(url);
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

        if (imageWidth <= 0 || imageHeight <= 0 || imageWidth > MaxAllowedPixelSize || imageHeight > MaxAllowedPixelSize)
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
}
