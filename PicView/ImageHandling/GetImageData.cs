using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using PicView.ChangeImage;
using System;
using System.Globalization;
using System.IO;
using System.Windows;

namespace PicView.ImageHandling
{
    internal static class GetImageData
    {
        internal static string[] RetrieveData(string file)
        {
            string name, directoryname, fullname, creationtime, lastwritetime;

            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(file);
                name = fileInfo.Name;
                directoryname = fileInfo.DirectoryName;
                fullname = fileInfo.FullName;
                creationtime = fileInfo.CreationTime.ToString(CultureInfo.CurrentCulture);
                lastwritetime = fileInfo.LastWriteTime.ToString(CultureInfo.CurrentCulture);
            }
            catch (Exception)
            {
                name = string.Empty;
                directoryname = string.Empty;
                fullname = string.Empty;
                creationtime = string.Empty;
                lastwritetime = string.Empty;
            }

            var image = Preloader.Get(Navigation.Pics[Navigation.FolderIndex]).bitmapSource;

            var inchesWidth = image.PixelWidth / image.DpiX;
            var inchesHeight = image.PixelHeight / image.DpiY;
            var cmWidth = inchesWidth * 2.54;
            var cmHeight = inchesHeight * 2.54;

            var firstRatio = image.PixelWidth / UILogic.TransformImage.ZoomLogic.GCD(image.PixelWidth, image.PixelHeight);
            var secondRatio = image.PixelHeight / UILogic.TransformImage.ZoomLogic.GCD(image.PixelWidth, image.PixelHeight);
            string ratioText;
            if (firstRatio == secondRatio)
            {
                ratioText = $"{firstRatio}:{secondRatio} ({Application.Current.Resources["Square"]})";
            }
            else if (firstRatio > secondRatio)
            {
                ratioText = $"{firstRatio}:{secondRatio} ({Application.Current.Resources["Landscape"]})";
            }
            else
            {
                ratioText = $"{firstRatio}:{secondRatio} ({Application.Current.Resources["Portrait"]})";
            }

            object bitdepth, dpiX, dpiY;
            string dpi;

            try
            {
                var so = ShellObject.FromParsingName(file);
                bitdepth = so.Properties.GetProperty(SystemProperties.System.Image.BitDepth).ValueAsObject;
                dpiX = so.Properties.GetProperty(SystemProperties.System.Image.HorizontalResolution).ValueAsObject;
                dpiY = so.Properties.GetProperty(SystemProperties.System.Image.VerticalResolution).ValueAsObject;
                so.Dispose();
            }
            catch (Exception)
            {
                bitdepth = string.Empty;
                dpiX = string.Empty;
                dpiY = string.Empty;
            }

            if (bitdepth == null)
            {
                bitdepth = string.Empty;
            }

            if (dpiX == null)
            {
                dpi = string.Empty;
            }
            else
            {
                dpi = Math.Round((double)dpiX) + " x " + Math.Round((double)dpiY) + " " + Application.Current.Resources["Dpi"];
            }

            return new string[]
            {
                // Fileinfo
                name,
                directoryname,
                fullname,
                creationtime,
                lastwritetime,

                // Resolution
                image.PixelWidth + " x " + image.PixelHeight + " " + Application.Current.Resources["Pixels"],

                // DPI
                dpi,

                // Bit dpeth
                bitdepth.ToString(),

                // Megapixels
                ((float)image.PixelHeight * image.PixelWidth / 1000000)
                    .ToString("0.##", CultureInfo.CurrentCulture) + " " + Application.Current.Resources["MegaPixels"],

                // Print size cm
                cmWidth.ToString("0.##", CultureInfo.CurrentCulture) + " x " + cmHeight.ToString("0.##", CultureInfo.CurrentCulture)
                    + " " + Application.Current.Resources["Centimeters"],

                // Print size inch
                inchesWidth.ToString("0.##", CultureInfo.CurrentCulture) + " x " + inchesHeight.ToString("0.##", CultureInfo.CurrentCulture)
                    + " " + Application.Current.Resources["Inches"],

                // Aspect ratio
                ratioText
            };
        }
    }
}