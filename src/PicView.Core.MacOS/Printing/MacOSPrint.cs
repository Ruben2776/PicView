using System.Runtime.InteropServices;
using PicView.Core.DebugTools;
using PicView.Core.Localization;

namespace PicView.Core.MacOS.Printing;

public static class MacOSPrint
{
    public static List<string> GetAvailablePrinters()
    {
        // 1. Add the dummy PDF printer first
        var printers = new List<string> { TranslationManager.Translation.SaveAsPdf };
        
        var destsPtr = IntPtr.Zero;
        var count = 0;

        try
        {
            // returns the number of destinations
            count = NativeMethods.cupsGetDests(out destsPtr);

            if (count > 0 && destsPtr != IntPtr.Zero)
            {
                // Iterate through the array of structs in unmanaged memory
                var currentPtr = destsPtr;
                var structSize = Marshal.SizeOf<NativeMethods.cups_dest_t>();

                for (var i = 0; i < count; i++)
                {
                    var dest = Marshal.PtrToStructure<NativeMethods.cups_dest_t>(currentPtr);
                    
                    // CUPS returns standard ANSI/UTF-8 C-Strings
                    var name = Marshal.PtrToStringAnsi(dest.name);
                    
                    if (!string.IsNullOrEmpty(name))
                    {
                        printers.Add(name);
                    }

                    // Move to next struct
                    currentPtr = IntPtr.Add(currentPtr, structSize);
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(MacOSPrint), nameof(GetAvailablePrinters), ex);
        }
        finally
        {
            if (destsPtr != IntPtr.Zero)
            {
                // CRITICAL: Use 'count' (from CUPS), not 'printers.Count' (managed list).
                // The count passed here must exactly match what cupsGetDests returned
                // to free the memory block correctly.
                NativeMethods.cupsFreeDests(count, destsPtr);
            }
        }

        return printers;
    }

    public static void Print(string printerName, string filePath, string jobTitle, int copies)
    {
        var jobId = NativeMethods.cupsPrintFile(printerName, filePath, jobTitle, copies, IntPtr.Zero);
        
        if (jobId == 0)
        {
            DebugHelper.LogDebug(nameof(MacOSPrint), nameof(Print), "CUPS failed to submit print job.");
        }
    }
}