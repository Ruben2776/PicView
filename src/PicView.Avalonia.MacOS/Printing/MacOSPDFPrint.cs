// using System.Runtime.InteropServices;
// using Avalonia.Media.Imaging;
// using Avalonia;
// using Avalonia.Controls;
// using Avalonia.Controls.ApplicationLifetimes;
// using Avalonia.Platform;
// using Avalonia.Platform.Storage;
// using Avalonia.Threading;
// using CoreGraphics;
// using PicView.Avalonia.Printing;
// using PicView.Avalonia.UI;
// using PicView.Avalonia.ViewModels;
// using PicView.Core.Localization;
// using PicView.Core.Printing;
//
// namespace PicView.Avalonia.MacOS.Printing;
//
// /// <summary>
// /// Handles PDF creation from images on macOS using CoreGraphics
// /// </summary>
// public static partial class MacOSPDFPrint
// {
//     private const float PreviewDpi = 96f;
//     private const double MmPerInch = 25.4;
//     private const double PointsPerInch = 72.0;
//     private const string CoreGraphicsLib = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
//     private const string CoreFoundationLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
//
//     // CoreGraphics P/Invoke declarations
//     [LibraryImport(CoreGraphicsLib)]
//     private static partial IntPtr CGPDFContextCreateWithURL(IntPtr url, ref CGRect mediaBox, IntPtr auxiliaryInfo);
//
//     [LibraryImport(CoreGraphicsLib)]
//     private static partial void CGPDFContextBeginPage(IntPtr context, IntPtr pageInfo);
//
//     [LibraryImport(CoreGraphicsLib)]
//     private static partial void CGPDFContextEndPage(IntPtr context);
//
//     [LibraryImport(CoreGraphicsLib)]
//     private static partial void CGPDFContextClose(IntPtr context);
//
//     [LibraryImport(CoreGraphicsLib)]
//     private static partial void CGContextDrawImage(IntPtr context, CGRect rect, IntPtr image);
//
//     [LibraryImport(CoreGraphicsLib)]
//     private static partial void CGContextRelease(IntPtr context);
//
//     [LibraryImport(CoreGraphicsLib)]
//     private static partial IntPtr CGColorSpaceCreateDeviceRGB();
//
//     [LibraryImport(CoreGraphicsLib)]
//     private static partial void CGColorSpaceRelease(IntPtr colorSpace);
//
//     [LibraryImport(CoreGraphicsLib)]
//     private static partial IntPtr CGDataProviderCreateWithData(
//         IntPtr info, IntPtr data, IntPtr size, IntPtr releaseCallback);
//
//     [LibraryImport(CoreGraphicsLib)]
//     private static partial void CGDataProviderRelease(IntPtr provider);
//
//     [LibraryImport(CoreGraphicsLib)]
//     private static partial IntPtr CGImageCreate(
//         IntPtr width, IntPtr height, IntPtr bitsPerComponent, IntPtr bitsPerPixel,
//         IntPtr bytesPerRow, IntPtr colorSpace, uint bitmapInfo,
//         IntPtr provider, IntPtr decode, [MarshalAs(UnmanagedType.I1)] bool shouldInterpolate,
//         int intent);
//
//     [LibraryImport(CoreGraphicsLib)]
//     private static partial void CGImageRelease(IntPtr image);
//
//     // CoreFoundation for URL creation
//     [LibraryImport(CoreFoundationLib)]
//     private static partial IntPtr CFURLCreateFromFileSystemRepresentation(
//         IntPtr allocator, IntPtr buffer, IntPtr bufLen, [MarshalAs(UnmanagedType.I1)] bool isDirectory);
//
//     [LibraryImport(CoreFoundationLib)]
//     private static partial void CFRelease(IntPtr cf);
//
//     /// <summary>
//     /// Creates a PDF file from the given image with print settings
//     /// </summary>
//     public static bool CreatePDF(string outputPath, PrintSettings settings, Bitmap avaloniaBmp)
//     {
//         IntPtr pdfContext = IntPtr.Zero;
//         IntPtr pdfUrl = IntPtr.Zero;
//         IntPtr cgImage = IntPtr.Zero;
//
//         try
//         {
//             if (settings.ColorMode.Value == (int)ColorModes.BlackAndWhite)
//             {
//                 avaloniaBmp = MacOSPrintEngine.ToGrayScale(avaloniaBmp, PreviewDpi);
//             }
//
//             var paperInfo = MacOSPrintEngine.ResolvePaper(settings.PaperSize.Value,
//                 settings.Orientation.Value == (int)Orientations.Landscape);
//
//             if (paperInfo is null)
//             {
//                 return false;
//             }
//
//             // Get paper size in points
//             var paperWidthPoints = paperInfo.WidthMm / MmPerInch * PointsPerInch;
//             var paperHeightPoints = paperInfo.HeightMm / MmPerInch * PointsPerInch;
//
//             if (settings.Orientation.Value == (int)Orientations.Landscape)
//             {
//                 (paperWidthPoints, paperHeightPoints) = (paperHeightPoints, paperWidthPoints);
//             }
//
//             var pageRect = new CGRect(0, 0, paperWidthPoints, paperHeightPoints);
//
//             // Create CFURL from file path
//             pdfUrl = CreateCFURLFromPath(outputPath);
//             if (pdfUrl == IntPtr.Zero)
//             {
//                 return false;
//             }
//
//             // Create PDF context
//             var rect = pageRect;
//             pdfContext = CGPDFContextCreateWithURL(pdfUrl, ref rect, IntPtr.Zero);
//             if (pdfContext == IntPtr.Zero)
//             {
//                 return false;
//             }
//
//             // Start page
//             CGPDFContextBeginPage(pdfContext, IntPtr.Zero);
//
//             // Calculate content rect with margins
//             var leftMargin = settings.MarginLeft.Value / MmPerInch * PointsPerInch;
//             var rightMargin = settings.MarginRight.Value / MmPerInch * PointsPerInch;
//             var topMargin = settings.MarginTop.Value / MmPerInch * PointsPerInch;
//             var bottomMargin = settings.MarginBottom.Value / MmPerInch * PointsPerInch;
//
//             var contentWidth = paperWidthPoints - (leftMargin + rightMargin);
//             var contentHeight = paperHeightPoints - (topMargin + bottomMargin);
//
//             // Convert Avalonia bitmap to CGImage
//             cgImage = AvaloniaToCGImage(avaloniaBmp);
//             if (cgImage == IntPtr.Zero)
//             {
//                 return false;
//             }
//
//             // Get CGImage dimensions
//             var imgWidth = avaloniaBmp.PixelSize.Width;
//             var imgHeight = avaloniaBmp.PixelSize.Height;
//
//             // Calculate image dimensions in points
//             var imgW = imgWidth * PointsPerInch / PreviewDpi;
//             var imgH = imgHeight * PointsPerInch / PreviewDpi;
//
//             // Calculate scale
//             double scaleFactorX = contentWidth / imgW;
//             double scaleFactorY = contentHeight / imgH;
//
//             var scale = settings.ScaleMode.Value switch
//             {
//                 (int)ScaleModes.Fill => Math.Max(scaleFactorX, scaleFactorY),
//                 (int)ScaleModes.Fit => Math.Min(scaleFactorX, scaleFactorY),
//                 (int)ScaleModes.Stretch => 0,
//                 _ => 1.0
//             };
//
//             CGRect destRect;
//             if (settings.ScaleMode.Value == (int)ScaleModes.Stretch)
//             {
//                 destRect = new CGRect(leftMargin, bottomMargin, contentWidth, contentHeight);
//             }
//             else
//             {
//                 var scaledWidth = imgW * scale;
//                 var scaledHeight = imgH * scale;
//                 var x = leftMargin + (contentWidth - scaledWidth) / 2;
//                 var y = bottomMargin + (contentHeight - scaledHeight) / 2;
//                 destRect = new CGRect(x, y, scaledWidth, scaledHeight);
//             }
//
//             // Draw image to PDF
//             CGContextDrawImage(pdfContext, destRect, cgImage);
//
//             // End page
//             CGPDFContextEndPage(pdfContext);
//             CGPDFContextClose(pdfContext);
//
//             return true;
//         }
//         catch (Exception ex)
//         {
//             System.Diagnostics.Debug.WriteLine($"PDF creation failed: {ex.Message}");
//             return false;
//         }
//         finally
//         {
//             // Clean up resources
//             if (cgImage != IntPtr.Zero)
//             {
//                 CGImageRelease(cgImage);
//             }
//             if (pdfContext != IntPtr.Zero)
//             {
//                 CGContextRelease(pdfContext);
//             }
//             if (pdfUrl != IntPtr.Zero)
//             {
//                 CFRelease(pdfUrl);
//             }
//         }
//     }
//
//     /// <summary>
//     /// Shows the native macOS save dialog for PDF
//     /// </summary>
//     public static async Task<bool> ShowSaveToPDFDialog(PrintSettings settings, Bitmap avaloniaBmp)
//     {
//         try
//         {
//             if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
//                 desktop.MainWindow?.StorageProvider is not { } provider)
//             {
//                 return false;
//             }
//             
//             string? fileName = null;
//             if (UIHelper.GetMainView.DataContext is MainViewModel vm)
//             {
//                 fileName = Path.GetFileNameWithoutExtension(vm.PicViewer.FileInfo?.Value?.Name)
//                                            ?? "export";
//             }
//             
//             var options = new FilePickerSaveOptions
//             {
//                 Title = TranslationManager.Translation.SaveAs + " - PicView",
//                 SuggestedFileName = fileName + ".pdf",
//                 FileTypeChoices =
//                 [
//                     new FilePickerFileType("PDF") { Patterns = ["*.pdf"] }
//                 ]
//             };
//         
//             var chosenFile = await Dispatcher.UIThread.InvokeAsync(() => provider.SaveFilePickerAsync(options));
//             return chosenFile is not null && CreatePDF(chosenFile.Path.LocalPath, settings, avaloniaBmp);
//         }
//         catch
//         {
//             return false;
//         }
//     }
//
//     private static IntPtr AvaloniaToCGImage(Bitmap avaloniaBmp)
//     {
//         var width = avaloniaBmp.PixelSize.Width;
//         var height = avaloniaBmp.PixelSize.Height;
//         var stride = width * 4;
//         var bufferSize = stride * height;
//
//         var buffer = Marshal.AllocHGlobal(bufferSize);
//         IntPtr colorSpace = IntPtr.Zero;
//         IntPtr provider = IntPtr.Zero;
//
//         try
//         {
//             avaloniaBmp.CopyPixels(
//                 new PixelRect(0, 0, width, height),
//                 buffer,
//                 bufferSize,
//                 stride);
//
//             // Create color space
//             colorSpace = CGColorSpaceCreateDeviceRGB();
//             if (colorSpace == IntPtr.Zero)
//             {
//                 Marshal.FreeHGlobal(buffer);
//                 return IntPtr.Zero;
//             }
//
//             // Create data provider (note: we don't free buffer here as CGImage takes ownership)
//             provider = CGDataProviderCreateWithData(
//                 IntPtr.Zero,
//                 buffer,
//                 new IntPtr(bufferSize),
//                 IntPtr.Zero);
//
//             if (provider == IntPtr.Zero)
//             {
//                 CGColorSpaceRelease(colorSpace);
//                 Marshal.FreeHGlobal(buffer);
//                 return IntPtr.Zero;
//             }
//
//             // Create CGImage
//             // kCGBitmapByteOrder32Little | kCGImageAlphaFirst = 0x4001
//             const uint bitmapInfo = 0x4001;
//
//             var image = CGImageCreate(
//                 new IntPtr(width),
//                 new IntPtr(height),
//                 new IntPtr(8),      // bits per component
//                 new IntPtr(32),     // bits per pixel
//                 new IntPtr(stride),
//                 colorSpace,
//                 bitmapInfo,
//                 provider,
//                 IntPtr.Zero,
//                 false,
//                 0); // kCGRenderingIntentDefault
//
//             // Clean up intermediate objects
//             CGDataProviderRelease(provider);
//             CGColorSpaceRelease(colorSpace);
//
//             return image;
//         }
//         catch
//         {
//             if (provider != IntPtr.Zero)
//                 CGDataProviderRelease(provider);
//             if (colorSpace != IntPtr.Zero)
//                 CGColorSpaceRelease(colorSpace);
//             Marshal.FreeHGlobal(buffer);
//             return IntPtr.Zero;
//         }
//     }
//
//     private static IntPtr CreateCFURLFromPath(string path)
//     {
//         var pathBytes = System.Text.Encoding.UTF8.GetBytes(path);
//         var pathPtr = Marshal.AllocHGlobal(pathBytes.Length);
//
//         try
//         {
//             Marshal.Copy(pathBytes, 0, pathPtr, pathBytes.Length);
//             var url = CFURLCreateFromFileSystemRepresentation(
//                 IntPtr.Zero,
//                 pathPtr,
//                 new IntPtr(pathBytes.Length),
//                 false);
//
//             return url;
//         }
//         finally
//         {
//             Marshal.FreeHGlobal(pathPtr);
//         }
//     }
// }