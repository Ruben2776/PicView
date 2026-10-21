using System.Runtime.InteropServices;
using PicView.Core.Printing;

namespace PicView.Core.MacOS.Printing;

public static partial class CupsPaperQuery
{
    // ─────────────────────────────────────────────────────────────────────────────
    //   PUBLIC: Query supported sizes
    // ─────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<string> GetPaperSizes(string printerName)
    {
        // Try PPD → fallback → PaperSizeHelper. The PPD is read once, as CUPS fetches it again for each read.
        var ppdSizes = TryGetSizesFromPpd(printerName).ToArray();
        return ppdSizes.Length > 0 ? ppdSizes : PaperSizeHelper.GetAllNames();
    }


    // ─────────────────────────────────────────────────────────────────────────────
    //   PPD PARSING
    // ─────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<string> TryGetSizesFromPpd(string printerName)
    {
        var ppdFilePtr = NativeMethods.cupsGetPPD(printerName);
        if (ppdFilePtr == IntPtr.Zero)
            yield break;

        var path = Marshal.PtrToStringAnsi(ppdFilePtr);
        if (string.IsNullOrWhiteSpace(path))
            yield break;

        var ppdPtr = NativeMethods.ppdOpenFile(path);
        if (ppdPtr == IntPtr.Zero)
            yield break;

        try
        {
            var ppd = Marshal.PtrToStructure<NativeMethods.ppd_file_t>(ppdPtr);
            var sizePtr = ppd.sizes;
            var count = ppd.num_sizes;
            var entrySize = Marshal.SizeOf<NativeMethods.ppd_size_t>();

            for (var i = 0; i < count; i++)
            {
                var entryPtr = IntPtr.Add(sizePtr, i * entrySize);
                var entry = Marshal.PtrToStructure<NativeMethods.ppd_size_t>(entryPtr);

                var name = Marshal.PtrToStringAnsi(entry.name);
                if (!string.IsNullOrWhiteSpace(name))
                    yield return name;
            }
        }
        finally
        {
            NativeMethods.ppdClose(ppdPtr);
        }
    }
}