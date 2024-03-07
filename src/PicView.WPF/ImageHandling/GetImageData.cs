using ImageMagick;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Navigation;
using PicView.WPF.ChangeImage;
using PicView.WPF.UILogic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PicView.WPF.ImageHandling;

internal static class GetImageData
{
    public struct ImageData
    {
        public string FileName { get; set; }
        public string DirectoryName { get; set; }
        public string Path { get; set; }
        public string CreationTime { get; set; }
        public string LastWriteTime { get; set; }
        public string LastAccessTime { get; set; }
        public string BitDepth { get; set; }
        public string PixelWidth { get; set; }
        public string PixelHeight { get; set; }
        public string Dpi { get; set; }
        public string MegaPixels { get; set; }
        public string PrintSizeCm { get; set; }
        public string PrintSizeInch { get; set; }
        public string AspectRatio { get; set; }
        public uint ExifRating { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string BingLink { get; set; }
        public string GoogleLink { get; set; }
        public string Altitude { get; set; }
        public string Title { get; set; }
        public string Subject { get; set; }
        public string Authors { get; set; }
        public string DateTaken { get; set; }
        public string Software { get; set; }
        public string Copyright { get; set; }
        public string ResolutionUnit { get; set; }
        public string ColorRepresentation { get; set; }
        public string Compression { get; set; }
        public string CompressedBitsPixel { get; set; }
        public string CameraMaker { get; set; }
        public string CameraModel { get; set; }
        public string Fstop { get; set; }
        public string ExposureTime { get; set; }
        public string ISOSpeed { get; set; }
        public string ExposureBias { get; set; }
        public string MaxAperture { get; set; }
        public string FocalLength { get; set; }
        public string FocalLength35mm { get; set; }
        public string FlashMode { get; set; }
        public string FlashEnergy { get; set; }
        public string MeteringMode { get; set; }
        public string LensMaker { get; set; }
        public string LensModel { get; set; }
        public string CamSerialNumber { get; set; }
        public string Contrast { get; set; }
        public string Brightness { get; set; }
        public string LightSource { get; set; }
        public string ExposureProgram { get; set; }
        public string Saturation { get; set; }
        public string Sharpness { get; set; }
        public string WhiteBalance { get; set; }
        public string PhotometricInterpretation { get; set; }
        public string DigitalZoom { get; set; }
        public string ExifVersion { get; set; }
    }

    internal static ImageData? RetrieveData(FileInfo? fileInfo)
    {
        BitmapSource? img = null;
        Application.Current.Dispatcher.Invoke(() =>
        {
            img = ConfigureWindows.GetMainWindow.MainImage.Source as BitmapSource;
        });

        if (img is null)
        {
            return null;
        }

        var imageData = new ImageData
        {
            PixelWidth = img.PixelWidth.ToString(CultureInfo.CurrentCulture),
            PixelHeight = img.PixelHeight.ToString(CultureInfo.CurrentCulture),
            Dpi = $"{img.DpiX} x {img.DpiY}",
        };

        if (fileInfo is null)
        {
            return imageData;
        }

        if (!fileInfo.Exists)
        {
            return imageData;
        }

        if (ErrorHandling.CheckOutOfRange() == false)
        {
            if (Navigation.Pics[Navigation.FolderIndex] != fileInfo.FullName)
            {
                return imageData;
            }
        }
        else
        {
            return imageData;
        }

        try
        {
            imageData.FileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            imageData.DirectoryName = fileInfo.DirectoryName ?? "";
            imageData.Path = fileInfo.FullName;
            imageData.CreationTime = fileInfo.CreationTime.ToString(CultureInfo.CurrentCulture);
            imageData.LastWriteTime = fileInfo.LastWriteTime.ToString(CultureInfo.CurrentCulture);
            imageData.LastAccessTime = fileInfo.LastAccessTime.ToString(CultureInfo.CurrentCulture);
        }
        catch (Exception)
        {
            imageData.FileName = string.Empty;
            imageData.DirectoryName = string.Empty;
            imageData.Path = string.Empty;
            imageData.CreationTime = string.Empty;
            imageData.LastWriteTime = string.Empty;
            imageData.LastAccessTime = string.Empty;
        }

        using var magick = new MagickImage();
        try
        {
            magick.Ping(Navigation.Pics[Navigation.FolderIndex]);
            var profile = magick.GetExifProfile();
            double dpiX = 0, dpiY = 0;

            if (profile != null)
            {
                dpiY = profile?.GetValue(ExifTag.YResolution)?.Value.ToDouble() ?? 0;
                dpiX = profile?.GetValue(ExifTag.XResolution)?.Value.ToDouble() ?? 0;
                var depth = profile?.GetValue(ExifTag.BitsPerSample)?.Value;
                if (depth is not null)
                {
                    var x = depth.Aggregate(0, (current, value) => current + value);
                    imageData.BitDepth = x.ToString();
                }
                else
                {
                    imageData.BitDepth = (magick.Depth * 3).ToString();
                }
            }

            if (dpiX is 0)
            {
                dpiX = img?.DpiX ?? 0;
                dpiY = img?.DpiX ?? 0;
            }

            if (string.IsNullOrEmpty(imageData.BitDepth))
            {
                imageData.BitDepth = (magick.Depth * 3).ToString();
            }

            if (dpiX == 0)
            {
                imageData.PrintSizeCm = imageData.PrintSizeInch = imageData.MegaPixels = imageData.ResolutionUnit = string.Empty;
            }
            else
            {
                var inchesWidth = img.PixelWidth / dpiX;
                var inchesHeight = img.PixelHeight / dpiY;
                imageData.PrintSizeInch =
                    $"{inchesWidth.ToString("0.##", CultureInfo.CurrentCulture)} x {inchesHeight.ToString("0.##", CultureInfo.CurrentCulture)} {TranslationHelper.GetTranslation("Inches")}";

                var cmWidth = img.PixelWidth / dpiX * 2.54;
                var cmHeight = img.PixelHeight / dpiY * 2.54;
                imageData.PrintSizeCm =
                    $"{cmWidth.ToString("0.##", CultureInfo.CurrentCulture)} x {cmHeight.ToString("0.##", CultureInfo.CurrentCulture)} {TranslationHelper.GetTranslation("Centimeters")}";
                imageData.MegaPixels =
                    $"{((float)img.PixelHeight * img.PixelWidth / 1000000).ToString("0.##", CultureInfo.CurrentCulture)} {TranslationHelper.GetTranslation("MegaPixels")}";

                imageData.ResolutionUnit = $"{dpiX} x {dpiY} {TranslationHelper.GetTranslation("Dpi")}";
            }

            var firstRatio = img.PixelWidth / TitleHelper.GCD(img.PixelWidth, img.PixelHeight);
            var secondRatio = img.PixelHeight / TitleHelper.GCD(img.PixelWidth, img.PixelHeight);

            if (firstRatio == secondRatio)
            {
                imageData.AspectRatio = $"{firstRatio}:{secondRatio} ({TranslationHelper.GetTranslation("Square")})";
            }
            else if (firstRatio > secondRatio)
            {
                imageData.AspectRatio =
                    $"{firstRatio}:{secondRatio} ({TranslationHelper.GetTranslation("Landscape")})";
            }
            else
            {
                imageData.AspectRatio = $"{firstRatio}:{secondRatio} ({TranslationHelper.GetTranslation("Portrait")})";
            }

            imageData.ExifRating = profile?.GetValue(ExifTag.Rating)?.Value ?? 0;

            var gpsValues = EXIFHelper.GetGPSValues(profile);

            if (gpsValues is not null)
            {
                imageData.Latitude = gpsValues[0] ?? "";
                imageData.Longitude = gpsValues[1] ?? "";

                imageData.GoogleLink = gpsValues[2] ?? "";
                imageData.BingLink = gpsValues[3] ?? "";
            }
            else
            {
                imageData.Latitude = imageData.Longitude = imageData.GoogleLink = imageData.BingLink = string.Empty;
            }

            var altitude = profile?.GetValue(ExifTag.GPSAltitude)?.Value;
            imageData.Altitude = altitude.HasValue
                ? $"{altitude.Value.ToDouble()} {TranslationHelper.GetTranslation("Meters")}"
                : string.Empty;
            var getAuthors = profile?.GetValue(ExifTag.Artist)?.Value;
            imageData.Authors = getAuthors ?? string.Empty;
            imageData.DateTaken = EXIFHelper.GetDateTaken(profile);
            imageData.Copyright = profile?.GetValue(ExifTag.Copyright)?.Value ?? string.Empty;
            imageData.Title = profile?.GetValue(ExifTag.XPTitle)?.Value.ToString() ?? string.Empty;
            imageData.Subject = profile?.GetValue(ExifTag.XPSubject)?.Value.ToString() ?? string.Empty;
            imageData.Software = profile?.GetValue(ExifTag.Software)?.Value ?? string.Empty;
            imageData.ResolutionUnit = EXIFHelper.GetResolutionUnit(profile);
            imageData.ColorRepresentation = EXIFHelper.GetColorSpace(profile);
            imageData.Compression = profile?.GetValue(ExifTag.Compression)?.Value.ToString() ?? string.Empty;
            imageData.CompressedBitsPixel = profile?.GetValue(ExifTag.CompressedBitsPerPixel)?.Value.ToString() ?? string.Empty;
            imageData.CameraMaker = profile?.GetValue(ExifTag.Make)?.Value ?? string.Empty;
            imageData.CameraModel = profile?.GetValue(ExifTag.Model)?.Value ?? string.Empty;
            imageData.ExposureProgram = EXIFHelper.GetExposureProgram(profile);
            imageData.ExposureTime = profile?.GetValue(ExifTag.ExposureTime)?.Value.ToString() ?? string.Empty;
            imageData.Fstop = profile?.GetValue(ExifTag.FNumber)?.Value.ToString() ?? string.Empty;
            imageData.MaxAperture = profile?.GetValue(ExifTag.MaxApertureValue)?.Value.ToString() ?? string.Empty;
            imageData.ExposureBias = profile?.GetValue(ExifTag.ExposureBiasValue)?.Value.ToString() ?? string.Empty;
            imageData.DigitalZoom = profile?.GetValue(ExifTag.DigitalZoomRatio)?.Value.ToString() ?? string.Empty;
            imageData.FocalLength35mm = profile?.GetValue(ExifTag.FocalLengthIn35mmFilm)?.Value.ToString() ?? string.Empty;
            imageData.FocalLength = profile?.GetValue(ExifTag.FocalLength)?.Value.ToString() ?? string.Empty;
            imageData.ISOSpeed = EXIFHelper.GetISOSpeed(profile);
            imageData.MeteringMode = profile?.GetValue(ExifTag.MeteringMode)?.Value.ToString() ?? string.Empty;
            imageData.Contrast = EXIFHelper.GetContrast(profile);
            imageData.Saturation = EXIFHelper.GetSaturation(profile);
            imageData.Sharpness = EXIFHelper.GetSharpness(profile);
            imageData.WhiteBalance = EXIFHelper.GetWhiteBalance(profile);
            imageData.FlashMode = EXIFHelper.GetFlashMode(profile);
            imageData.FlashEnergy = profile?.GetValue(ExifTag.FlashEnergy)?.Value.ToString() ?? string.Empty;
            imageData.LightSource = EXIFHelper.GetLightSource(profile);
            imageData.Brightness = profile?.GetValue(ExifTag.BrightnessValue)?.Value.ToString() ?? string.Empty;
            imageData.PhotometricInterpretation = EXIFHelper.GetPhotometricInterpretation(profile);
            imageData.ExifVersion = EXIFHelper.GetExifVersion(profile);
            imageData.LensModel = profile?.GetValue(ExifTag.LensModel)?.Value ?? string.Empty;
            imageData.LensMaker = profile?.GetValue(ExifTag.LensMake)?.Value ?? string.Empty;
        }
        catch
        {
            return imageData;
        }

        return imageData;
    }
}