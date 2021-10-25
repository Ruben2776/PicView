using ImageMagick;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using PicView.ChangeImage;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace PicView.ImageHandling
{
    internal static class GetImageData
    {
        internal static Task<string[]?> RetrieveData(FileInfo? fileInfo) => Task.Run(() =>
        {
            string name, directoryname, fullname, creationtime, lastwritetime, lastaccesstime;

            if (fileInfo is null)
            {
                name = string.Empty;
                directoryname = string.Empty;
                fullname = string.Empty;
                creationtime = string.Empty;
                lastwritetime = string.Empty;
                lastaccesstime = String.Empty;
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
                    lastaccesstime = fileInfo.LastAccessTime.ToString(CultureInfo.CurrentCulture);
                }
                catch (Exception)
                {
                    name = string.Empty;
                    directoryname = string.Empty;
                    fullname = string.Empty;
                    creationtime = string.Empty;
                    lastwritetime = string.Empty;
                    lastaccesstime = string.Empty;
                }
            }

            var preloadValue = Preloader.Get(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex]);
            while (preloadValue.isLoading)
            {
                _ = Preloader.AddAsync(ChangeImage.Navigation.FolderIndex);

                if (preloadValue == null) { return null; }
            }

            if (preloadValue == null) { return null; }

            var inchesWidth = preloadValue.bitmapSource.PixelWidth / preloadValue.bitmapSource.DpiX;
            var inchesHeight = preloadValue.bitmapSource.PixelHeight / preloadValue.bitmapSource.DpiY;
            var cmWidth = inchesWidth * 2.54;
            var cmHeight = inchesHeight * 2.54;

            var firstRatio = preloadValue.bitmapSource.PixelWidth / UILogic.TransformImage.ZoomLogic.GCD(preloadValue.bitmapSource.PixelWidth, preloadValue.bitmapSource.PixelHeight);
            var secondRatio = preloadValue.bitmapSource.PixelHeight / UILogic.TransformImage.ZoomLogic.GCD(preloadValue.bitmapSource.PixelWidth, preloadValue.bitmapSource.PixelHeight);
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

            string megaPixels = ((float)preloadValue.bitmapSource.PixelHeight * preloadValue.bitmapSource.PixelWidth / 1000000)
                    .ToString("0.##", CultureInfo.CurrentCulture) + " " + Application.Current.Resources["MegaPixels"];

            string printSizeCm = cmWidth.ToString("0.##", CultureInfo.CurrentCulture) + " x " + cmHeight.ToString("0.##", CultureInfo.CurrentCulture)
                    + " " + Application.Current.Resources["Centimeters"];

            string printSizeInch = inchesWidth.ToString("0.##", CultureInfo.CurrentCulture) + " x " + inchesHeight.ToString("0.##", CultureInfo.CurrentCulture)
                    + " " + Application.Current.Resources["Inches"];

            object bitdepth, stars;
            string dpi = String.Empty;

            // exif
            string gps = String.Empty;

            string latitude = String.Empty;
            string latitudeValue = String.Empty;

            string longitude = String.Empty;
            string longitudeValue = String.Empty;

            string googleLink = String.Empty;
            string bingLink = String.Empty;

            string authors = String.Empty;
            string authorsValue = String.Empty;

            string dateTaken = String.Empty;
            string dateTakenValue = String.Empty;

            string programName = String.Empty;
            string programNameValue = String.Empty;

            string copyrightName = String.Empty;
            string copyrightValue = String.Empty;

            var so = ShellObject.FromParsingName(fileInfo.FullName);
            bitdepth = so.Properties.GetProperty(SystemProperties.System.Image.BitDepth).ValueAsObject;
            stars = so.Properties.GetProperty(SystemProperties.System.Rating).ValueAsObject;

            var dpiX = so.Properties.GetProperty(SystemProperties.System.Image.HorizontalResolution).ValueAsObject;
            var dpiY = so.Properties.GetProperty(SystemProperties.System.Image.VerticalResolution).ValueAsObject;
            if (dpiX is not null && dpiY is not null)
            {
                dpi = Math.Round((double)dpiX) + " x " + Math.Round((double)dpiY) + " " + Application.Current.Resources["Dpi"];
            }

            if (bitdepth == null)
            {
                bitdepth = string.Empty;
            }

            if (stars is null)
            {
                stars = string.Empty;
            }

            var magickImage = new MagickImage();
            try
            {
                magickImage.Read(fileInfo);
            }
            catch (Exception)
            {
                return new string[] {
                        name,
                        directoryname,
                        fullname,
                        creationtime,
                        lastwritetime,
                        lastaccesstime,

                        bitdepth.ToString(),

                        preloadValue.bitmapSource.PixelWidth.ToString(),
                        preloadValue.bitmapSource.PixelHeight.ToString(),

                        dpi,

                        megaPixels,

                        printSizeCm,
                        printSizeInch,

                        ratioText,

                        stars.ToString(),
                    };
            }

            var exifData = magickImage.GetExifProfile();
            magickImage.Dispose();

            latitude = so.Properties.GetProperty(SystemProperties.System.GPS.Latitude).Description.DisplayName;
            longitude = so.Properties.GetProperty(SystemProperties.System.GPS.Longitude).Description.DisplayName;

            var _author = so.Properties.GetProperty(SystemProperties.System.Author);
            authors = _author.Description.DisplayName;
            if (_author.ValueAsObject is not null)
            {
                authorsValue = _author.ValueAsObject.ToString();
            }

            var _dateTaken = so.Properties.GetProperty(SystemProperties.System.Photo.DateTaken);
            dateTaken = _dateTaken.Description.DisplayName;
            if (_dateTaken.ValueAsObject is not null)
            {
                dateTakenValue = _dateTaken.ValueAsObject.ToString();
            }

            var _program = so.Properties.GetProperty(SystemProperties.System.ApplicationName);
            programName = _program.Description.DisplayName;
            if (_program.ValueAsObject is not null)
            {
                programNameValue = _program.ValueAsObject.ToString();
            }

            var _copyright = so.Properties.GetProperty(SystemProperties.System.ApplicationName);
            copyrightName = _copyright.Description.DisplayName;
            if (_copyright.ValueAsObject is not null)
            {
                copyrightValue = _copyright.ValueAsObject.ToString();
            }

            if (exifData is not null)
            {
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

            return new string[]
            {
                // Fileinfo
                name,
                directoryname,
                fullname,
                creationtime,
                lastwritetime,
                lastaccesstime,

                bitdepth.ToString(),

                preloadValue.bitmapSource.PixelWidth.ToString(),
                preloadValue.bitmapSource.PixelHeight.ToString(),

                dpi,

                megaPixels,

                printSizeCm,
                printSizeInch,

                ratioText,

                stars.ToString(),

                latitude, latitudeValue,
                longitude, longitudeValue,
                bingLink, googleLink,

                authors, authorsValue,
                dateTaken, dateTakenValue,

                programName, programNameValue,
                copyrightName,copyrightValue,
            };
        });

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