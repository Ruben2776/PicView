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
        internal static Task<string[]?> RetrieveData(FileInfo? fileInfo) => Task.Run(async () =>
        {
            if (fileInfo is not null && ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex] != fileInfo.FullName)
            {
                return null;
            }

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
            if (preloadValue is null)
            {
                await Preloader.AddAsync(ChangeImage.Navigation.FolderIndex).ConfigureAwait(false);

                if (preloadValue is null)
                {
                    preloadValue = new Preloader.PreloadValue(null, false, fileInfo);
                    await UILogic.ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                    {
                        preloadValue.bitmapSource = ImageDecoder.GetRenderedBitmapFrame();
                    });
                }
            }
            while (preloadValue.isLoading)
            {
                await Task.Delay(200).ConfigureAwait(false);

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

            string altitude = String.Empty;
            string altitudeValue = String.Empty;

            string googleLink = String.Empty;
            string bingLink = String.Empty;

            string title = String.Empty;
            string titleValue = String.Empty;

            string subject = String.Empty;
            string subjectValue = String.Empty;

            string authors = String.Empty;
            string authorsValue = String.Empty;

            string dateTaken = String.Empty;
            string dateTakenValue = String.Empty;

            string programName = String.Empty;
            string programNameValue = String.Empty;

            string copyrightName = String.Empty;
            string copyrightValue = String.Empty;

            string resolutionUnit = String.Empty;
            string resolutionUnitValue = String.Empty;

            string colorRepresentation = String.Empty;
            string colorRepresentationValue = String.Empty;

            string compression = String.Empty;
            string compressionValue = String.Empty;

            string compressionBits = String.Empty;
            string compressionBitsValue = String.Empty;

            string cameraMaker = String.Empty;
            string cameroMakerValue = String.Empty;

            string cameraModel = String.Empty;
            string cameroModelValue = String.Empty;

            string fstop = String.Empty;
            string fstopValue = String.Empty;

            string exposure = String.Empty;
            string exposureValue = String.Empty;

            string isoSpeed = String.Empty;
            string isoSpeedValue = String.Empty;

            string exposureBias = String.Empty;
            string exposureBiasValue = String.Empty;

            string focal = String.Empty;
            string focalValue = String.Empty;

            string maxAperture = String.Empty;
            string maxApertureValue = String.Empty;

            string flashMode = String.Empty;
            string flashModeValue = String.Empty;

            string flashEnergy = String.Empty;
            string flashEnergyValue = String.Empty;

            string flength35 = String.Empty;
            string flength35Value = String.Empty;

            string meteringMode = String.Empty;
            string meteringModeValue = String.Empty;

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
            altitude = so.Properties.GetProperty(SystemProperties.System.GPS.Altitude).Description.DisplayName;

            var _title = so.Properties.GetProperty(SystemProperties.System.Title);
            title = _title.Description.DisplayName;
            if (_title.ValueAsObject is not null)
            {
                titleValue = _title.ValueAsObject.ToString();
            }

            var _subject = so.Properties.GetProperty(SystemProperties.System.Subject);
            subject = _subject.Description.DisplayName;
            if (_subject.ValueAsObject is not null)
            {
                subjectValue = _subject.ValueAsObject.ToString();
            }

            var _author = so.Properties.GetProperty(SystemProperties.System.Author);
            authors = _author.Description.DisplayName;
            if (_author.ValueAsObject is not null)
            {
                var authorsArray = (string[])_author.ValueAsObject;
                for (int i = 0; i < authorsArray.Length; i++)
                {
                    if (i == 0)
                    {
                        authorsValue = authorsArray[0];
                    }
                    else
                    {
                        authorsValue += ", " + authorsArray[i];
                    }
                }
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

            var _copyright = so.Properties.GetProperty(SystemProperties.System.Copyright);
            copyrightName = _copyright.Description.DisplayName;
            if (_copyright.ValueAsObject is not null)
            {
                copyrightValue = _copyright.ValueAsObject.ToString();
            }

            var _resolutionUnit = so.Properties.GetProperty(SystemProperties.System.Image.ResolutionUnit);
            resolutionUnit = _resolutionUnit.Description.DisplayName;
            if (_resolutionUnit.ValueAsObject is not null)
            {
                resolutionUnitValue = _resolutionUnit.ValueAsObject.ToString();
            }

            colorRepresentation = so.Properties.GetProperty(SystemProperties.System.Image.ColorSpace).Description.DisplayName;

            compression = so.Properties.GetProperty(SystemProperties.System.Image.Compression).Description.DisplayName;
            compressionBits = so.Properties.GetProperty(SystemProperties.System.Image.CompressedBitsPerPixel).Description.DisplayName;

            var manu = so.Properties.GetProperty(SystemProperties.System.Photo.CameraManufacturer);
            cameraMaker = manu.Description.DisplayName;
            if (manu.ValueAsObject is not null)
            {
                cameroMakerValue = manu.ValueAsObject.ToString();
            }

            var cam = so.Properties.GetProperty(SystemProperties.System.Photo.CameraModel);
            cameraModel = cam.Description.DisplayName;
            if (cam.ValueAsObject is not null)
            {
                cameroModelValue = cam.ValueAsObject.ToString();
            }

            fstop = so.Properties.GetProperty(SystemProperties.System.Photo.FNumber).Description.DisplayName;

            var expo = so.Properties.GetProperty(SystemProperties.System.Photo.ExposureTime);
            exposure = expo.Description.DisplayName;
            if (expo.ValueAsObject is not null)
            {
                exposureValue = expo.ValueAsObject.ToString();
            }

            var iso = so.Properties.GetProperty(SystemProperties.System.Photo.ISOSpeed);
            isoSpeed = iso.Description.DisplayName;
            if (iso.ValueAsObject is not null)
            {
                isoSpeedValue = iso.ValueAsObject.ToString();
            }

            meteringMode = so.Properties.GetProperty(SystemProperties.System.Photo.MeteringMode).Description.DisplayName;

            exposureBias = so.Properties.GetProperty(SystemProperties.System.Photo.ExposureBias).Description.DisplayName;

            maxAperture = so.Properties.GetProperty(SystemProperties.System.Photo.MaxAperture).Description.DisplayName;

            focal = so.Properties.GetProperty(SystemProperties.System.Photo.FocalLength).Description.DisplayName;

            flashMode = so.Properties.GetProperty(SystemProperties.System.Photo.Flash).Description.DisplayName;

            flashEnergy = so.Properties.GetProperty(SystemProperties.System.Photo.FlashEnergy).Description.DisplayName;

            var f35 = so.Properties.GetProperty(SystemProperties.System.Photo.FocalLengthInFilm);
            flength35 = f35.Description.DisplayName;
            if (f35.ValueAsObject is not null)
            {
                flength35Value = f35.ValueAsObject.ToString();
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
                var gpsAltitude = exifData?.GetValue(ExifTag.GPSAltitude);
                if (gpsAltitude is not null)
                {
                    altitudeValue = gpsAltitude.Value.ToString();
                }

                var colorSpace = exifData.GetValue(ExifTag.ColorSpace);
                if (colorSpace is not null)
                {
                    colorRepresentationValue = colorSpace.ToString();
                }

                var compr = exifData.GetValue(ExifTag.Compression);
                if (compr is not null)
                {
                    compressionValue = compr.ToString();
                }

                var comprBits = exifData.GetValue(ExifTag.CompressedBitsPerPixel);
                if (comprBits is not null)
                {
                    compressionBitsValue = comprBits.ToString();
                }

                var fNumber = exifData?.GetValue(ExifTag.FNumber);
                if (fNumber is not null)
                {
                    fstopValue = fNumber.ToString();
                }

                var bias = exifData.GetValue(ExifTag.ExposureBiasValue);
                if (bias is not null)
                {
                    exposureBiasValue = bias.ToString();
                }

                var maxApart = exifData.GetValue(ExifTag.MaxApertureValue);
                if (maxApart is not null)
                {
                    maxApertureValue = maxApart.ToString();
                }

                var fcal = exifData.GetValue(ExifTag.FocalLength);
                if (fcal is not null)
                {
                    focalValue = fcal.ToString();
                }

                var flash = exifData.GetValue(ExifTag.Flash);
                if (flash is not null)
                {
                    flashModeValue = flash.ToString();
                }

                var fenergy = exifData.GetValue(ExifTag.FlashEnergy);
                if (fenergy is not null)
                {
                    flashEnergyValue = fenergy.ToString();
                }

                var metering = exifData.GetValue(ExifTag.MeteringMode);
                if (metering is not null)
                {
                    meteringModeValue = metering.ToString();
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
                altitude, altitudeValue,

                title, titleValue,
                subject, subjectValue,

                authors, authorsValue,
                dateTaken, dateTakenValue,

                programName, programNameValue,
                copyrightName,copyrightValue,

                resolutionUnit, resolutionUnitValue,
                colorRepresentation, colorRepresentationValue,

                compression, compressionValue,
                compressionBits, compressionBitsValue,

                cameraMaker, cameroMakerValue,
                cameraModel, cameroModelValue,

                fstop, fstopValue,
                exposure, exposureValue,

                isoSpeed, isoSpeedValue,
                exposureBias, exposureBiasValue,

                maxAperture, maxApertureValue,

                focal, focalValue,
                flength35, flength35Value,

                flashMode, flashModeValue,
                flashEnergy, flashEnergyValue,

                meteringMode, meteringModeValue
            };
        });

        private static double GetCoordinates(string gpsRef, Rational[] rationals)
        {
            if (rationals[0].Denominator == 0 || rationals[1].Denominator == 0 || rationals[2].Denominator == 0)
            {
                return 0;
            }

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