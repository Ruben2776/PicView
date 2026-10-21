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

    public static bool Print(string printerName, string jobTitle, int copies, int width, int height, IntPtr pixels)
    {
        var hdc = NativeMethods.CreateDCW("WINSPOOL", printerName, null, IntPtr.Zero);
        if (hdc == IntPtr.Zero)
        {
            DebugHelper.LogDebug(nameof(Win32Print), nameof(Print), "Failed to create Printer DC");
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
                return true;
            }

            for (var i = 0; i < copies; i++)
            {
                if (NativeMethods.StartPage(hdc) <= 0)
                {
                    continue;
                }

                var printableWidth = NativeMethods.GetDeviceCaps(hdc, NativeMethods.PHYSICALWIDTH);
                var printableHeight = NativeMethods.GetDeviceCaps(hdc, NativeMethods.PHYSICALHEIGHT);

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
                    0, 0, printableWidth, printableHeight,
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
        }
    }
}