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
        // Try PPD → fallback → PaperSizeHelper
        foreach (var ppdSize in TryGetSizesFromPpd(printerName))
        {
            yield return ppdSize;
        }
        
        // If PPD fails → fallback to PaperSizeHelper
        if (TryGetSizesFromPpd(printerName).Any())
        {
            yield break;
        }

        foreach (var name in PaperSizeHelper.GetAllNames())
        {
            yield return name;
        }
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