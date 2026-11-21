using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace PicView.Core.WindowsNT.Imaging;

/// <summary>
/// AOT-compatible Windows Shell thumbnail extraction via IShellItemImageFactory.
/// Uses GeneratedComInterface and LibraryImport for source-generated interop.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class ShellThumbnailNative
{
    #region COM Interface

    [GeneratedComInterface]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(SIZE size, SIIGBF flags, out IntPtr phbm);
    }

    #endregion

    #region Native Structs and Enums

    [StructLayout(LayoutKind.Sequential)]
    internal struct SIZE
    {
        public int Width;
        public int Height;
    }

    /// <summary>
    /// Flags controlling how the shell thumbnail is retrieved.
    /// </summary>
    [Flags]
    public enum SIIGBF
    {
        ResizeToFit = 0x00000000,
        BiggerSizeOk = 0x00000001,
        MemoryOnly = 0x00000002,
        IconOnly = 0x00000004,
        ThumbnailOnly = 0x00000008,
        InCacheOnly = 0x00000010,
        CropToSize = 0x00000020,
        WideThumbnails = 0x00000040,
        IconBackground = 0x00000080,
        ScaleUp = 0x00000100,
    }

    #endregion

    #region Native Methods

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHCreateItemFromParsingName(
        string pszPath,
        IntPtr pbc,
        in Guid riid,
        out IntPtr ppv);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHCreateItemFromRelativeName(
        IntPtr psiParent,
        string pszName,
        IntPtr pbc,
        in Guid riid,
        out IntPtr ppv);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(IntPtr hObject);

    [LibraryImport("gdi32.dll")]
    private static partial int GetObjectW(IntPtr hObject, int nCount, ref BITMAP lpObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAP
    {
        public int bmType;
        public int bmWidth;
        public int bmHeight;
        public int bmWidthBytes;
        public ushort bmPlanes;
        public ushort bmBitsPixel;
        public IntPtr bmBits;
    }

    #endregion

    #region Public API

    private static readonly ComWrappers s_comWrappers = new StrategyBasedComWrappers();

    private static readonly Guid ImageFactoryGuid = new("bcc18b79-ba16-442f-80c4-8a59c30c463b");
    private static readonly Guid ShellItemGuid = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");

    // The shell item of the folder this thread last asked about, so the files of a folder are parsed by their name
    // relative to it, not by their full path, which costs the shell about a hundred times more. Per thread, so an item
    // is only used on the thread that made it. A thread that ends, as idle pool threads do, drops its handle, whose
    // finalizer then releases the item on the finalizer thread, an MTA thread: so call this from MTA threads, as the
    // thread pool's are
    [ThreadStatic] private static string? t_folderPath;
    [ThreadStatic] private static ShellItemHandle? t_folderItem;

    /// <summary>
    /// Extracts a Windows Shell thumbnail for the given file path as raw BGRA pixel data.
    /// </summary>
    /// <param name="path">Absolute file path.</param>
    /// <param name="width">Requested thumbnail width.</param>
    /// <param name="height">Requested thumbnail height.</param>
    /// <param name="cacheOnly">
    /// Whether to give only a thumbnail in the Windows thumbnail cache. A file Windows has not thumbnailed then gives
    /// null at once, as does one cached smaller than asked for: the shell's own extraction, which also writes its
    /// cache, costs more than a caller that can decode the file itself. Without it, the shell extracts a thumbnail it
    /// has not cached, at least as big as asked for.
    /// </param>
    /// <param name="pixelWidth">Actual width of the returned thumbnail in pixels.</param>
    /// <param name="pixelHeight">Actual height of the returned thumbnail in pixels.</param>
    /// <returns>Raw BGRA pixel data, or null on failure.</returns>
    public static byte[]? GetShellThumbnailBytes(string path, int width, int height, bool cacheOnly,
        out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = 0;
        pixelHeight = 0;

        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var hBitmap = IntPtr.Zero;
        var shellItemPtr = IntPtr.Zero;

        try
        {
            shellItemPtr = CreateImageFactory(path);
            if (shellItemPtr == IntPtr.Zero)
            {
                return null;
            }

            // Manually query the COM interface via the generated marshaller
            var factory = (IShellItemImageFactory)s_comWrappers
                .GetOrCreateObjectForComInstance(shellItemPtr, CreateObjectFlags.None);

            var size = new SIZE { Width = width, Height = height };
            var flags = SIIGBF.ThumbnailOnly | SIIGBF.BiggerSizeOk;
            if (cacheOnly)
            {
                flags |= SIIGBF.InCacheOnly;
            }

            var hr = factory.GetImage(size, flags, out hBitmap);
            if (hr != 0 || hBitmap == IntPtr.Zero)
            {
                return null;
            }

            var pixels = ExtractBgraPixels(hBitmap, out pixelWidth, out pixelHeight);
            // From the cache, BiggerSizeOk gives whatever size is cached, which may be smaller than asked for, where an
            // extraction gives one whose longer side is at least the size asked for. An image itself smaller than that
            // is also refused, so the caller decodes it, which is cheap for an image that small
            if (cacheOnly && Math.Max(pixelWidth, pixelHeight) < Math.Max(width, height))
            {
                pixelWidth = 0;
                pixelHeight = 0;
                return null;
            }

            return pixels;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (hBitmap != IntPtr.Zero)
            {
                DeleteObject(hBitmap);
            }

            if (shellItemPtr != IntPtr.Zero)
            {
                Marshal.Release(shellItemPtr);
            }
        }
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// The image factory of the file at <paramref name="path"/>, made relative to its folder's shell item, which this
    /// thread keeps for the next file of the same folder. By its full path when the folder or the name cannot be
    /// parsed that way.
    /// </summary>
    /// <returns>The factory, which the caller releases, or zero when the shell knows no such file.</returns>
    private static IntPtr CreateImageFactory(string path)
    {
        var folder = Path.GetDirectoryName(path);
        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(folder) && !string.IsNullOrEmpty(name) && TryGetFolderItem(folder, out var folderItem) &&
            SHCreateItemFromRelativeName(folderItem, name, IntPtr.Zero, in ImageFactoryGuid, out var relative) is 0 &&
            relative != IntPtr.Zero)
        {
            return relative;
        }

        return SHCreateItemFromParsingName(path, IntPtr.Zero, in ImageFactoryGuid, out var item) is 0
            ? item
            : IntPtr.Zero;
    }

    /// <summary>
    /// The shell item of <paramref name="folder"/>: the one this thread kept, if it is of the same folder, else a new
    /// one, kept in its place.
    /// </summary>
    /// <remarks>
    /// Only this thread reads or disposes the handle it keeps, so the pointer stays valid while the caller uses it.
    /// </remarks>
    private static bool TryGetFolderItem(string folder, out IntPtr folderItem)
    {
        if (t_folderItem is { } kept && string.Equals(t_folderPath, folder, StringComparison.Ordinal))
        {
            folderItem = kept.DangerousGetHandle();
            return true;
        }

        t_folderItem?.Dispose();
        t_folderItem = null;
        t_folderPath = null;

        if (SHCreateItemFromParsingName(folder, IntPtr.Zero, in ShellItemGuid, out folderItem) is not 0 ||
            folderItem == IntPtr.Zero)
        {
            folderItem = IntPtr.Zero;
            return false;
        }

        t_folderItem = new ShellItemHandle(folderItem);
        t_folderPath = folder;
        return true;
    }

    /// <summary>
    /// A shell item reference, released when disposed or, once nothing holds the handle, by its finalizer.
    /// </summary>
    private sealed class ShellItemHandle : SafeHandle
    {
        public ShellItemHandle(IntPtr item) : base(IntPtr.Zero, ownsHandle: true) => SetHandle(item);

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            Marshal.Release(handle);
            return true;
        }
    }

    /// <summary>
    /// Reads BGRA pixel data from an HBITMAP.
    /// </summary>
    private static byte[]? ExtractBgraPixels(IntPtr hBitmap, out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = 0;
        pixelHeight = 0;

        var bmp = new BITMAP();
        if (GetObjectW(hBitmap, Marshal.SizeOf<BITMAP>(), ref bmp) == 0)
        {
            return null;
        }

        pixelWidth = bmp.bmWidth;
        pixelHeight = bmp.bmHeight;

        if (bmp.bmBitsPixel != 32 || bmp.bmBits == IntPtr.Zero)
        {
            return null;
        }

        var stride = bmp.bmWidthBytes;
        var totalBytes = stride * bmp.bmHeight;
        var pixels = new byte[totalBytes];

        Marshal.Copy(bmp.bmBits, pixels, 0, totalBytes);

        return pixels;
    }

    #endregion
}
