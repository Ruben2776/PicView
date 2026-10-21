using System.Runtime.InteropServices;
using PicView.Core.DebugTools;

namespace PicView.Core.WindowsNT.Printing;

public static class Win32Print
{
    public static List<string> GetAvailablePrinters()
    {
        var printers = new List<string>();
        const uint flags = NativeMethods.PRINTER_ENUM_LOCAL | NativeMethods.PRINTER_ENUM_CONNECTIONS;

        NativeMethods.EnumPrintersW(flags, null, 2, IntPtr.Zero, 0, out var pcbNeeded, out var pcReturned);
        if (pcbNeeded == 0)
        {
            return printers;
        }

        var pAddr = Marshal.AllocHGlobal((int)pcbNeeded);
        try
        {
            if (NativeMethods.EnumPrintersW(flags, null, 2, pAddr, pcbNeeded, out _, out pcReturned))
            {
                var structSize = Marshal.SizeOf<NativeMethods.PRINTER_INFO_2>();
                for (var i = 0; i < pcReturned; i++)
                {
                    var info = Marshal.PtrToStructure<NativeMethods.PRINTER_INFO_2>(IntPtr.Add(pAddr, i * structSize));
                    if (!string.IsNullOrEmpty(info.pPrinterName))
                    {
                        printers.Add(info.pPrinterName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(Win32Print), nameof(GetAvailablePrinters), ex);
        }
        finally
        {
            Marshal.FreeHGlobal(pAddr);
        }

        return printers;
    }

    public static string? GetDefaultPrinter()
    {
        uint pcchBuffer = 0;
        NativeMethods.GetDefaultPrinter(IntPtr.Zero, ref pcchBuffer);
        if (pcchBuffer == 0)
        {
            return null;
        }

        var pAddr = Marshal.AllocHGlobal((int)pcchBuffer * 2);
        try
        {
            if (NativeMethods.GetDefaultPrinter(pAddr, ref pcchBuffer))
            {
                return Marshal.PtrToStringUni(pAddr);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(Win32Print), nameof(GetDefaultPrinter), ex);
        }
        finally
        {
            Marshal.FreeHGlobal(pAddr);
        }
        return null;
    }

    public static List<string> GetPaperSizes(string printerName)
    {
        var names = new List<string>();
        var count = NativeMethods.DeviceCapabilitiesW(printerName, null!, NativeMethods.DC_PAPERNAMES, IntPtr.Zero, IntPtr.Zero);
        if (count <= 0)
        {
            return names;
        }

        // DC_PAPERNAMES returns an array of char[64]
        var pAddr = Marshal.AllocHGlobal(count * 64 * 2);
        try
        {
            if (NativeMethods.DeviceCapabilitiesW(printerName, null!, NativeMethods.DC_PAPERNAMES, pAddr, IntPtr.Zero) != -1)
            {
                for (var i = 0; i < count; i++)
                {
                    var name = Marshal.PtrToStringUni(IntPtr.Add(pAddr, i * 64 * 2), 64).TrimEnd('\0', ' ');
                    if (!string.IsNullOrEmpty(name))
                    {
                        names.Add(name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(Win32Print), nameof(GetPaperSizes), ex);
        }
        finally
        {
            Marshal.FreeHGlobal(pAddr);
        }

        return names;
    }

    /// <summary>
    /// Prints a page at its real size, on the paper and in the orientation it was laid out for.
    /// </summary>
    /// <param name="paperSize">The paper, as <see cref="GetPaperSizes"/> names it.</param>
    /// <param name="pixels">The page: rows of 32-bit BGRA pixels, top row first, <paramref name="width"/> * 4 bytes each.</param>
    /// <returns>Whether the printer took the job.</returns>
    public static bool Print(string printerName, string jobTitle, int copies, string paperSize, bool isLandscape,
        int width, int height, IntPtr pixels)
    {
        // Without these the printer uses its own paper and orientation, and the page is stretched over it
        var devMode = CreateDevMode(printerName, paperSize, isLandscape);
        var hdc = NativeMethods.CreateDCW("WINSPOOL", printerName, null, devMode);
        if (hdc == IntPtr.Zero)
        {
            DebugHelper.LogDebug(nameof(Win32Print), nameof(Print), "Failed to create Printer DC");
            Marshal.FreeHGlobal(devMode);
            return false;
        }

        try
        {
            var di = new NativeMethods.DOCINFO
            {
                cbSize = Marshal.SizeOf<NativeMethods.DOCINFO>(),
                lpszDocName = jobTitle
            };

            if (NativeMethods.StartDocW(hdc, ref di) <= 0)
            {
                DebugHelper.LogDebug(nameof(Win32Print), nameof(Print), "Failed to start the document");
                return false;
            }

            for (var i = 0; i < copies; i++)
            {
                if (NativeMethods.StartPage(hdc) <= 0)
                {
                    continue;
                }

                // The printer draws from the corner of the area it can print, so the page starts that far up and to
                // the left, to cover the whole sheet as it was laid out
                var sheetWidth = NativeMethods.GetDeviceCaps(hdc, NativeMethods.PHYSICALWIDTH);
                var sheetHeight = NativeMethods.GetDeviceCaps(hdc, NativeMethods.PHYSICALHEIGHT);
                var offsetX = NativeMethods.GetDeviceCaps(hdc, NativeMethods.PHYSICALOFFSETX);
                var offsetY = NativeMethods.GetDeviceCaps(hdc, NativeMethods.PHYSICALOFFSETY);

                var bmi = new NativeMethods.BITMAPINFOHEADER
                {
                    biSize = (uint)Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = -height, // Negative for top-down
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0 // BI_RGB
                };

                NativeMethods.StretchDIBits(
                    hdc,
                    -offsetX, -offsetY, sheetWidth, sheetHeight,
                    0, 0, width, height,
                    pixels,
                    ref bmi,
                    NativeMethods.DIB_RGB_COLORS,
                    NativeMethods.SRCCOPY);

                NativeMethods.EndPage(hdc);
            }
            NativeMethods.EndDoc(hdc);
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(Win32Print), nameof(Print), ex);
            return false;
        }
        finally
        {
            NativeMethods.DeleteDC(hdc);
            Marshal.FreeHGlobal(devMode);
        }
    }

    /// <summary>
    /// The printer's settings for a job, changed to the paper and orientation, which the caller frees with
    /// <see cref="Marshal.FreeHGlobal"/>. <see cref="IntPtr.Zero"/> when the printer would not give them.
    /// </summary>
    private static IntPtr CreateDevMode(string printerName, string paperSize, bool isLandscape)
    {
        if (!NativeMethods.OpenPrinter(printerName, out var printer, IntPtr.Zero))
        {
            DebugHelper.LogDebug(nameof(Win32Print), nameof(CreateDevMode), "Failed to open the printer");
            return IntPtr.Zero;
        }

        var devMode = IntPtr.Zero;
        try
        {
            var size = NativeMethods.DocumentProperties(IntPtr.Zero, printer, printerName, IntPtr.Zero, IntPtr.Zero, 0);
            if (size <= 0)
            {
                return IntPtr.Zero;
            }

            devMode = Marshal.AllocHGlobal(size);
            if (NativeMethods.DocumentProperties(IntPtr.Zero, printer, printerName, devMode, IntPtr.Zero,
                    NativeMethods.DM_OUT_BUFFER) < 0)
            {
                Marshal.FreeHGlobal(devMode);
                return IntPtr.Zero;
            }

            var fields = (uint)Marshal.ReadInt32(devMode, NativeMethods.DEVMODE_FIELDS_OFFSET) |
                         NativeMethods.DM_ORIENTATION;
            Marshal.WriteInt16(devMode, NativeMethods.DEVMODE_ORIENTATION_OFFSET,
                isLandscape ? NativeMethods.DMORIENT_LANDSCAPE : NativeMethods.DMORIENT_PORTRAIT);
            if (FindPaperId(printerName, paperSize) is { } paperId)
            {
                Marshal.WriteInt16(devMode, NativeMethods.DEVMODE_PAPERSIZE_OFFSET, paperId);
                fields |= NativeMethods.DM_PAPERSIZE;
            }

            Marshal.WriteInt32(devMode, NativeMethods.DEVMODE_FIELDS_OFFSET, (int)fields);

            // The driver merges the changes into the rest of its settings
            if (NativeMethods.DocumentProperties(IntPtr.Zero, printer, printerName, devMode, devMode,
                    NativeMethods.DM_IN_BUFFER | NativeMethods.DM_OUT_BUFFER) < 0)
            {
                Marshal.FreeHGlobal(devMode);
                return IntPtr.Zero;
            }

            return devMode;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(Win32Print), nameof(CreateDevMode), ex);
            Marshal.FreeHGlobal(devMode);
            return IntPtr.Zero;
        }
        finally
        {
            NativeMethods.ClosePrinter(printer);
        }
    }

    /// <summary>
    /// The printer's id for the paper named <paramref name="paperSize"/>, or <see langword="null"/> when it has none.
    /// </summary>
    private static short? FindPaperId(string printerName, string paperSize)
    {
        var count = NativeMethods.DeviceCapabilitiesW(printerName, null!, NativeMethods.DC_PAPERNAMES, IntPtr.Zero, IntPtr.Zero);
        if (count <= 0)
        {
            return null;
        }

        // DC_PAPERNAMES gives a char[64] for each paper, and DC_PAPERS the WORD id of each, in the same order
        var names = Marshal.AllocHGlobal(count * 64 * 2);
        var ids = Marshal.AllocHGlobal(count * 2);
        try
        {
            if (NativeMethods.DeviceCapabilitiesW(printerName, null!, NativeMethods.DC_PAPERNAMES, names, IntPtr.Zero) == -1 ||
                NativeMethods.DeviceCapabilitiesW(printerName, null!, NativeMethods.DC_PAPERS, ids, IntPtr.Zero) == -1)
            {
                return null;
            }

            for (var i = 0; i < count; i++)
            {
                var name = Marshal.PtrToStringUni(IntPtr.Add(names, i * 64 * 2), 64).TrimEnd('\0', ' ');
                if (string.Equals(name, paperSize, StringComparison.Ordinal))
                {
                    return Marshal.ReadInt16(ids, i * 2);
                }
            }

            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(names);
            Marshal.FreeHGlobal(ids);
        }
    }
}