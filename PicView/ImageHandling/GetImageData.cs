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
        internal static async Task<string[]>? RetrieveDataAsync(FileInfo? fileInfo, bool exif)
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

            object bitdepth, dpiX, dpiY, stars;
            string dpi;
            bool skip = false;

            // exif
            string altitude = String.Empty;
            string altitudeValue = String.Empty;

            string latitude = String.Empty;
            string latitudeValue = String.Empty;

            string longitude = String.Empty;
            string longitudeValue = String.Empty;

            try
            {
                var so = ShellObject.FromParsingName(fileInfo.FullName);
                bitdepth = so.Properties.GetProperty(SystemProperties.System.Image.BitDepth).ValueAsObject;
                dpiX = so.Properties.GetProperty(SystemProperties.System.Image.HorizontalResolution).ValueAsObject;
                dpiY = so.Properties.GetProperty(SystemProperties.System.Image.VerticalResolution).ValueAsObject;
                stars = so.Properties.GetProperty(SystemProperties.System.Rating).ValueAsObject;

                if (exif)
                {
                    var getaltitude = so.Properties.GetProperty(SystemProperties.System.GPS.Altitude);
                    altitude = getaltitude.Description.DisplayName;
                    var altitudeObject = getaltitude.ValueAsObject;
                    if (altitudeObject is not null)
                    {
                        var altitudes = (double[])altitudeObject;
                        latitudeValue = altitudes[0].ToString() + ", " + altitudes[1].ToString() + ", " + altitudes[2].ToString();
                    }

                    var getLatitude = so.Properties.GetProperty(SystemProperties.System.GPS.Latitude);
                    latitude = getLatitude.Description.DisplayName;
                    var latitudeObject = getLatitude.ValueAsObject;
                    if (latitudeObject is not null)
                    {
                        var latitudes = (double[])latitudeObject;
                        latitudeValue = latitudes[0].ToString() + ", " + latitudes[1].ToString() + ", " + latitudes[2].ToString();
                    }

                    var getLongitude = so.Properties.GetProperty(SystemProperties.System.GPS.Longitude);
                    longitude = getLongitude.Description.DisplayName;
                    var longitudeObject = getLongitude.ValueAsObject;
                    if (longitudeObject is not null)
                    {
                        var longitudes = (double[])longitudeObject;
                        longitudeValue = longitudes[0].ToString() + ", " + longitudes[1].ToString() + ", " + longitudes[2].ToString();
                    }
                }

                so.Dispose();
            }
            catch (Exception)
            {
                bitdepth = string.Empty;
                dpiX = string.Empty;
                dpiY = string.Empty;
                skip = true;
                stars = string.Empty;
            }

            if (bitdepth == null)
            {
                bitdepth = string.Empty;
            }

            if (skip || dpiX == null)
            {
                dpi = string.Empty;
            }
            else
            {
                dpi = Math.Round((double)dpiX) + " x " + Math.Round((double)dpiY) + " " + Application.Current.Resources["Dpi"];
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
                ratioText,

                stars.ToString(),

                altitude, altitudeValue,

                latitude, latitudeValue,

                longitude, longitudeValue,
            };
        }
    }
}