using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace PicView.Core.WindowsNT.Imaging;

/// <summary>
/// AOT-compatible Windows Imaging Component (WIC) image decoding fallback.
/// Uses direct COM vtable interop and LibraryImport.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class Win32Image
{
    #region Native Methods and Constants

    private const uint GENERIC_READ = 0x80000000;
    private const uint WICDecodeMetadataCacheOnDemand = 0x00000000;
    private const uint WINCODEC_SDK_VERSION1 = 0x0236;
    private const uint WINCODEC_SDK_VERSION2 = 0x0237;

    private const int WICBitmapDitherTypeNone = 0;
    private const int WICBitmapPaletteTypeCustom = 0;

    private static readonly Guid GUID_WICPixelFormat32bppPBGRA = new("6fddc324-4e03-4bfe-b185-3d77768dc910");
    private static readonly Guid GUID_WICPixelFormat32bppBGRA = new("6fddc324-4e03-4bfe-b185-3d77768dc90f");
    private static readonly Guid CLSID_WICImagingFactory2 = new("317d06e8-5f24-433d-bdf7-79ce68d8abc2");
    private static readonly Guid IID_IWICImagingFactory2 = new("7b816b45-1996-4476-b132-de9e247c8af0");
    private static readonly Guid CLSID_WICImagingFactory1 = new("cacaf262-e232-4972-9585-33ec0872c0d7");
    private static readonly Guid IID_IWICImagingFactory1 = new("ec5ec888-8100-4e39-b747-740d2c622c45");

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [LibraryImport("ole32.dll")]
    private static partial void CoUninitialize();

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(
        in Guid rclsid,
        IntPtr pUnkOuter,
        uint dwClsContext,
        in Guid riid,
        out IntPtr ppv);

    [LibraryImport("windowscodecs.dll")]
    private static partial int WICCreateImagingFactory_Proxy(
        uint SDKVersion,
        out IntPtr ppIImagingFactory);

    #endregion

    #region Public API

    /// <summary>
    /// Decodes an image file using the Windows Imaging Component (WIC) and returns raw BGRA/PBGRA pixel data.
    /// </summary>
    /// <param name="path">Absolute file path to the image.</param>
    /// <param name="pixelWidth">Actual width of the decoded image in pixels.</param>
    /// <param name="pixelHeight">Actual height of the decoded image in pixels.</param>
    /// <returns>Raw 32-bit premultiplied BGRA pixel data, or null on failure.</returns>
    public static unsafe byte[]? GetPixelsFromNativeImagingComponent(string path, out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = 0;
        pixelHeight = 0;

        var coInitHr = CoInitializeEx(IntPtr.Zero, 0 /* COINIT_MULTITHREADED */);
        var mustUninitialize = coInitHr >= 0;

        var factoryPtr = IntPtr.Zero;
        var decoderPtr = IntPtr.Zero;
        var framePtr = IntPtr.Zero;
        var converterPtr = IntPtr.Zero;

        try
        {
            var hr = WICCreateImagingFactory_Proxy(WINCODEC_SDK_VERSION2, out factoryPtr);
            if (hr != 0 || factoryPtr == IntPtr.Zero)
            {
                hr = WICCreateImagingFactory_Proxy(WINCODEC_SDK_VERSION1, out factoryPtr);
            }

            if (hr != 0 || factoryPtr == IntPtr.Zero)
            {
                hr = CoCreateInstance(in CLSID_WICImagingFactory2, IntPtr.Zero, 1 /* CLSCTX_INPROC_SERVER */, in IID_IWICImagingFactory2, out factoryPtr);
            }

            if (hr != 0 || factoryPtr == IntPtr.Zero)
            {
                hr = CoCreateInstance(in CLSID_WICImagingFactory1, IntPtr.Zero, 1 /* CLSCTX_INPROC_SERVER */, in IID_IWICImagingFactory1, out factoryPtr);
            }

            if (hr != 0 || factoryPtr == IntPtr.Zero)
            {
                return null;
            }

            var factoryVtbl = *(void***)factoryPtr;

            // IWICImagingFactory slot 3: CreateDecoderFromFilename
            var createDecoderFromFilename = (delegate* unmanaged[Stdcall]<void*, char*, void*, uint, uint, void**, int>)factoryVtbl[3];
            fixed (char* pPath = path)
            {
                void* pDecoder = null;
                hr = createDecoderFromFilename((void*)factoryPtr, pPath, null, GENERIC_READ, WICDecodeMetadataCacheOnDemand, &pDecoder);
                decoderPtr = (IntPtr)pDecoder;
            }

            if (hr != 0 || decoderPtr == IntPtr.Zero)
            {
                return null;
            }

            var decoderVtbl = *(void***)decoderPtr;

            // IWICBitmapDecoder slot 12: GetFrameCount
            var getFrameCount = (delegate* unmanaged[Stdcall]<void*, uint*, int>)decoderVtbl[12];
            uint frameCount = 0;
            hr = getFrameCount((void*)decoderPtr, &frameCount);
            if (hr != 0 || frameCount == 0)
            {
                return null;
            }

            // IWICBitmapDecoder slot 13: GetFrame
            var getFrame = (delegate* unmanaged[Stdcall]<void*, uint, void**, int>)decoderVtbl[13];
            void* pFrame = null;
            hr = getFrame((void*)decoderPtr, 0, &pFrame);
            framePtr = (IntPtr)pFrame;
            if (hr != 0 || framePtr == IntPtr.Zero)
            {
                return null;
            }

            // IWICImagingFactory slot 10: CreateFormatConverter
            // IWICFormatConverter slot 8: Initialize
            // A null palette with WICBitmapPaletteTypeCustom makes WIC use the source palette for indexed images.
            // A converter can't be re-initialized after a failed Initialize, so a fresh one is created per attempt.
            var createFormatConverter = (delegate* unmanaged[Stdcall]<void*, void**, int>)factoryVtbl[10];
            void** converterVtbl = null;
            for (var attempt = 0; attempt < 2; attempt++)
            {
                if (converterPtr != IntPtr.Zero)
                {
                    Marshal.Release(converterPtr);
                    converterPtr = IntPtr.Zero;
                }

                void* pConverter = null;
                hr = createFormatConverter((void*)factoryPtr, &pConverter);
                converterPtr = (IntPtr)pConverter;
                if (hr != 0 || converterPtr == IntPtr.Zero)
                {
                    return null;
                }

                converterVtbl = *(void***)converterPtr;
                var initializeConverter = (delegate* unmanaged[Stdcall]<void*, void*, Guid*, int, void*, double, int, int>)converterVtbl[8];
                var targetFormat = attempt == 0 ? GUID_WICPixelFormat32bppPBGRA : GUID_WICPixelFormat32bppBGRA;
                hr = initializeConverter((void*)converterPtr, (void*)framePtr, &targetFormat, WICBitmapDitherTypeNone, null, 0.0, WICBitmapPaletteTypeCustom);
                if (hr == 0)
                {
                    break;
                }
            }

            if (hr != 0 || converterVtbl is null)
            {
                return null;
            }

            // IWICBitmapSource slot 3: GetSize
            var getSize = (delegate* unmanaged[Stdcall]<void*, uint*, uint*, int>)converterVtbl[3];
            uint width = 0;
            uint height = 0;
            hr = getSize((void*)converterPtr, &width, &height);
            if (hr != 0 || width == 0 || height == 0 || width > int.MaxValue / 4 || (long)width * height * 4 > int.MaxValue)
            {
                return null;
            }

            var stride = (int)width * 4;
            var totalBytes = stride * (int)height;
            var pixels = new byte[totalBytes];

            // IWICBitmapSource slot 7: CopyPixels
            var copyPixels = (delegate* unmanaged[Stdcall]<void*, void*, uint, uint, byte*, int>)converterVtbl[7];
            fixed (byte* pPixels = pixels)
            {
                hr = copyPixels((void*)converterPtr, null, (uint)stride, (uint)totalBytes, pPixels);
            }

            if (hr != 0)
            {
                return null;
            }

            pixelWidth = (int)width;
            pixelHeight = (int)height;
            return pixels;
        }
        catch
        {
            pixelWidth = 0;
            pixelHeight = 0;
            return null;
        }
        finally
        {
            if (converterPtr != IntPtr.Zero)
            {
                Marshal.Release(converterPtr);
            }

            if (framePtr != IntPtr.Zero)
            {
                Marshal.Release(framePtr);
            }

            if (decoderPtr != IntPtr.Zero)
            {
                Marshal.Release(decoderPtr);
            }

            if (factoryPtr != IntPtr.Zero)
            {
                Marshal.Release(factoryPtr);
            }

            if (mustUninitialize)
            {
                CoUninitialize();
            }
        }
    }

    #endregion
}
