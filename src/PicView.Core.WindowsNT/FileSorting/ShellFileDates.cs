using System.Runtime.InteropServices;
using PicView.Core.FileSorting;

namespace PicView.Core.WindowsNT.FileSorting;

/// <summary>
/// The date Windows Explorer shows for a file in its Date column (System.ItemDate), read from the file's shell item.
/// Each read opens the file through its property handler, about 3 ms a file, so the dates of a folder are read in the
/// background (ADR-0051). Called through the vtable, as <see cref="ExplorerFolderOrder"/> is, and from pool threads.
/// </summary>
public sealed unsafe partial class ShellFileDates : IFileDateSource
{
    private static readonly Guid IidShellItem2 = new(0x7E9FB0D3, 0x919F, 0x4307, 0xAB, 0x2E, 0x9B, 0x18, 0x60, 0x31, 0xC, 0x93) /* 7E9FB0D3-919F-4307-AB2E-9B1860310C93 */;

    // After IUnknown's three and IShellItem's five, the eighth of IShellItem2's own
    private const int ShellItem2GetFileTime = 15;

    private const int Ok = 0;

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHCreateItemFromParsingName(string path, nint bindContext, in Guid iid, out nint item);

    /// <inheritdoc />
    public DateTime? DateOf(string path)
    {
        if (!OperatingSystem.IsWindows() ||
            SHCreateItemFromParsingName(path, 0, IidShellItem2, out var item) != Ok || item == 0)
        {
            return null;
        }

        try
        {
            var key = ExplorerSort.Date;
            long fileTime;
            var getFileTime = (delegate* unmanaged[Stdcall]<nint, ShellPropertyKey*, long*, int>)
                (*(void***)item)[ShellItem2GetFileTime];
            // A file Explorer shows no date for, as one whose creation time is not set, has no such property
            if (getFileTime(item, &key, &fileTime) != Ok || fileTime <= 0)
            {
                return DateTime.MinValue;
            }

            return DateTime.FromFileTimeUtc(fileTime);
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTime.MinValue;
        }
        finally
        {
            Marshal.Release(item);
        }
    }
}
