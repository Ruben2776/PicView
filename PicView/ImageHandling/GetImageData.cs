using ImageMagick;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using PicView.ChangeImage;
using PicView.UILogic;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PicView.ImageHandling
{
    internal static class GetImageData
    {
        internal static async Task<string[]>? RetrieveDataAsync(FileInfo? fileInfo)
        {
            string name, directoryname, fullname, creationtime, lastwritetime;

            if (fileInfo is null)
            {
                name = string.Empty;
                directoryname = string.Empty;
                fullname = string.Empty;
                creationtime = string.Empty;
                lastwritetime = string.Empty;
            }
            else
            {
                try
                {
                    name = Path.GetFileNameWithoutExtension(fileInfo.Name);
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
            }

            BitmapSource? image = null;
            var source = Preloader.Get(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex]);
            if (source != null)
            {
                image = source.bitmapSource;
            }
            else
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                {
                    image = ImageDecoder.GetRenderedBitmapFrame();
                });
            }

            if (image == null)
            {
#pragma warning disable CS8603 // Possible null reference return.
                return null;
#pragma warning restore CS8603 // Possible null reference return.
            }

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

            object bitdepth, stars;
            string dpi = String.Empty;

            // exif
            string latitude = String.Empty;
            string latitudeValue = String.Empty;

            string longitude = String.Empty;
            string longitudeValue = String.Empty;

            string googleLink = String.Empty;
            string bingLink = String.Empty;

            try
            {
                var so = ShellObject.FromParsingName(fileInfo.FullName);
                bitdepth = so.Properties.GetProperty(SystemProperties.System.Image.BitDepth).ValueAsObject;
                stars = so.Properties.GetProperty(SystemProperties.System.Rating).ValueAsObject;

                var magickImage = new MagickImage(fileInfo);
                var exifData = magickImage.GetExifProfile();
                magickImage.Dispose();

                if (exifData is null)
                {
                    var dpiX = so.Properties.GetProperty(SystemProperties.System.Image.HorizontalResolution).ValueAsObject;
                    var dpiY = so.Properties.GetProperty(SystemProperties.System.Image.VerticalResolution).ValueAsObject;
                    dpi = Math.Round((double)dpiX) + " x " + Math.Round((double)dpiY) + " " + Application.Current.Resources["Dpi"];
                }
                else
                {
                    var dpiX = exifData.GetValue(ExifTag.XResolution);
                    var dpiY = exifData.GetValue(ExifTag.YResolution);

                    if (dpiX is not null && dpiY is not null)
                    {
                        dpi = dpiX.Value + " x " + dpiY.Value + " " + Application.Current.Resources["Dpi"];
                    }

                    latitude = so.Properties.GetProperty(SystemProperties.System.GPS.Latitude).Description.DisplayName;
                    longitude = so.Properties.GetProperty(SystemProperties.System.GPS.Longitude).Description.DisplayName;

                    var gpsLong = exifData.GetValue(ExifTag.GPSLongitude);
                    var gpsLongRef = exifData.GetValue(ExifTag.GPSLongitudeRef);
                    var gpsLatitude = exifData.GetValue(ExifTag.GPSLatitude);
                    var gpsLatitudeRef = exifData.GetValue(ExifTag.GPSLatitudeRef);

                    if (gpsLong is not null && gpsLongRef is not null && gpsLatitude is not null && gpsLatitudeRef is not null)
                    {
                        latitudeValue = GetCoordinates(gpsLatitudeRef.ToString(), gpsLatitude.Value).ToString();
                        longitudeValue = GetCoordinates(gpsLongRef.ToString(), gpsLong.Value).ToString();

                        googleLink = @"https://www.google.com/maps/search/?api=1&query=" + latitudeValue + "," + longitudeValue;
                        bingLink = @"https://bing.com/maps/default.aspx?cp=" + latitudeValue + "~" + longitudeValue + "&style=o&lvl=1&dir=0&scene=1140291";
                    }
                }

                so.Dispose();
            }
            catch (Exception)
            {
                bitdepth = string.Empty;
                stars = string.Empty;
            }

            if (bitdepth == null)
            {
                bitdepth = string.Empty;
            }

            if (stars is null)
            {
                stars = string.Empty;
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
                source.bitmapSource.PixelWidth + " x " + source.bitmapSource.PixelHeight + " " + Application.Current.Resources["Pixels"],

                // DPI
                dpi,

                // Bit dpeth
                bitdepth.ToString(),

                // Megapixels
                ((float)source.bitmapSource.PixelHeight * source.bitmapSource.PixelWidth / 1000000)
                    .ToString("0.##", CultureInfo.CurrentCulture) + " " + Application.Current.Resources["MegaPixels"],

                // Print size cm
                cmWidth.ToString("0.##", CultureInfo.CurrentCulture) + " x " + cmHeight.ToString("0.##", CultureInfo.CurrentCulture)
                    + " " + Application.Current.Resources["Centimeters"],

                // Print size inch
                inchesWidth.ToString("0.##", CultureInfo.CurrentCulture) + " x " + inchesHeight.ToString("0.##", CultureInfo.CurrentCulture)
                    + " " + Application.Current.Resources["Inches"],

                // Aspect ratio
                ratioText,

                stars.ToString(),

                latitude, latitudeValue,

                longitude, longitudeValue,

                bingLink, googleLink
            };
        }

        private static double GetCoordinates(string gpsRef, Rational[] rationals)
        {
            double degrees = rationals[0].Numerator / rationals[0].Denominator;
            double minutes = rationals[1].Numerator / rationals[1].Denominator;
            double seconds = rationals[2].Numerator / rationals[2].Denominator;

            double coordinate = degrees + (minutes / 60d) + (seconds / 3600d);
            if (gpsRef == "S" || gpsRef == "W")
                coordinate *= -1;
            return coordinate;
        }
    }
}