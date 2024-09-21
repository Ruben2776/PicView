using System.Globalization;
using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Navigation;

namespace PicView.Avalonia.Navigation;

public static class ExifHandling
{
    public static void UpdateExifValues(ImageModel imageModel, MainViewModel vm)
    {
        if (vm.FileInfo is null || vm is { PixelWidth: <= 0, PixelHeight: <= 0 })
        {
            return;
        }
        using var magick = new MagickImage();
        
        try
        {
            if (!vm.FileInfo.Exists)
            {
                return;
            }
            magick.Ping(vm.FileInfo);
            var profile = magick.GetExifProfile();

            if (profile != null)
            {
                vm.DpiY = profile?.GetValue(ExifTag.YResolution)?.Value.ToDouble() ?? 0;
                vm.DpiX = profile?.GetValue(ExifTag.XResolution)?.Value.ToDouble() ?? 0;
                var depth = profile?.GetValue(ExifTag.BitsPerSample)?.Value;
                if (depth is not null)
                {
                    var x = depth.Aggregate(0, (current, value) => current + value);
                    vm.GetBitDepth = x.ToString();
                }
                else
                {
                    vm.GetBitDepth = (magick.Depth * 3).ToString();
                }
            }

            if (vm.DpiX is 0 && imageModel.ImageType is ImageType.Bitmap or ImageType.AnimatedGif or ImageType.AnimatedWebp)
            {
                if (imageModel.Image is Bitmap bmp)
                {
                    vm.DpiX = bmp?.Dpi.X ?? 0;
                    vm.DpiY = bmp?.Dpi.Y ?? 0;
                }
            }

            vm.GetOrientation = imageModel.EXIFOrientation switch
            {
                EXIFHelper.EXIFOrientation.Horizontal => TranslationHelper.Translation.Normal,
                EXIFHelper.EXIFOrientation.MirrorHorizontal => TranslationHelper.Translation.Flipped,
                EXIFHelper.EXIFOrientation.Rotate180 => $"{TranslationHelper.Translation.Rotated} 180\u00b0",
                EXIFHelper.EXIFOrientation.MirrorVertical =>
                    $"{TranslationHelper.Translation.Rotated} 180\u00b0, {TranslationHelper.Translation.Flipped}",
                EXIFHelper.EXIFOrientation.MirrorHorizontalRotate270Cw =>
                    $"{TranslationHelper.Translation.Rotated} 270\u00b0, {TranslationHelper.Translation.Flipped}",
                EXIFHelper.EXIFOrientation.Rotate90Cw => $"{TranslationHelper.Translation.Rotated} 90\u00b0",
                EXIFHelper.EXIFOrientation.MirrorHorizontalRotate90Cw =>
                    $"{TranslationHelper.Translation.Rotated} 90\u00b0, {TranslationHelper.Translation.Flipped}",
                EXIFHelper.EXIFOrientation.Rotated270Cw => $"{TranslationHelper.Translation.Rotated} 270\u00b0",
                _ => string.Empty
            };

            var meter = TranslationHelper.Translation.Meter;
            var cm = TranslationHelper.Translation.Centimeters;
            var mp = TranslationHelper.Translation.MegaPixels;
            var inches = TranslationHelper.Translation.Inches;
            var square = TranslationHelper.Translation.Square;
            var landscape = TranslationHelper.Translation.Landscape;
            var portrait = TranslationHelper.Translation.Portrait;

            if (string.IsNullOrEmpty(vm.GetBitDepth))
            {
                vm.GetBitDepth = (magick.Depth * 3).ToString();
            }

            if (vm.DpiX == 0 || vm.DpiY == 0) // Check for zero before division
            {
                vm.GetPrintSizeCm = vm.GetPrintSizeInch = vm.GetSizeMp = vm.GetResolution = string.Empty;
            }
            else
            {
                var inchesWidth = vm.PixelWidth / vm.DpiX;
                var inchesHeight = vm.PixelHeight / vm.DpiY;
                vm.GetPrintSizeInch =
                    $"{inchesWidth.ToString("0.##", CultureInfo.CurrentCulture)} x {inchesHeight.ToString("0.##", CultureInfo.CurrentCulture)} {inches}";

                var cmWidth = vm.PixelWidth / vm.DpiX * 2.54;
                var cmHeight = vm.PixelHeight / vm.DpiY * 2.54;
                vm.GetPrintSizeCm =
                    $"{cmWidth.ToString("0.##", CultureInfo.CurrentCulture)} x {cmHeight.ToString("0.##", CultureInfo.CurrentCulture)} {cm}";
                vm.GetSizeMp =
                    $"{((float)vm.PixelHeight * vm.PixelWidth / 1000000).ToString("0.##", CultureInfo.CurrentCulture)} {mp}";

                vm.GetResolution = $"{vm.DpiX} x {vm.DpiY} {TranslationHelper.Translation.Dpi}";
            }

            var gcd = ImageTitleFormatter.GCD(vm.PixelWidth, vm.PixelHeight);
            if (gcd != 0) // Check for zero before division
            {
                var firstRatio = vm.PixelWidth / gcd;
                var secondRatio = vm.PixelHeight / gcd;

                if (firstRatio == secondRatio)
                {
                    vm.GetAspectRatio = $"{firstRatio}:{secondRatio} ({square})";
                }
                else if (firstRatio > secondRatio)
                {
                    vm.GetAspectRatio =
                        $"{firstRatio}:{secondRatio} ({landscape})";
                }
                else
                {
                    vm.GetAspectRatio = $"{firstRatio}:{secondRatio} ({portrait})";
                }
            }
            else
            {
                vm.GetAspectRatio = string.Empty; // Handle cases where gcd is 0
            }

            vm.EXIFRating = profile?.GetValue(ExifTag.Rating)?.Value ?? 0;

            var gpsValues = EXIFHelper.GetGPSValues(profile);

            if (gpsValues is not null)
            {
                vm.GetLatitude = gpsValues[0];
                vm.GetLongitude = gpsValues[1];

                vm.GoogleLink = gpsValues[2];
                vm.BingLink = gpsValues[3];
            }
            else
            {
                vm.GetLatitude = vm.GetLongitude = vm.GoogleLink = vm.BingLink = string.Empty;
            }

            var altitude = profile?.GetValue(ExifTag.GPSAltitude)?.Value;
            vm.GetAltitude = altitude.HasValue
                ? $"{altitude.Value.ToDouble()} {meter}"
                : string.Empty;
            var getAuthors = profile?.GetValue(ExifTag.Artist)?.Value;
            vm.GetAuthors = getAuthors ?? string.Empty;
            vm.GetDateTaken = EXIFHelper.GetDateTaken(profile);
            vm.GetCopyright = profile?.GetValue(ExifTag.Copyright)?.Value ?? string.Empty;
            vm.GetTitle = EXIFHelper.GetTitle(profile);
            vm.GetSubject = EXIFHelper.GetSubject(profile);
            vm.GetSoftware = profile?.GetValue(ExifTag.Software)?.Value ?? string.Empty;
            vm.GetResolutionUnit = EXIFHelper.GetResolutionUnit(profile);
            vm.GetColorRepresentation = EXIFHelper.GetColorSpace(profile);
            vm.GetCompression = profile?.GetValue(ExifTag.Compression)?.Value.ToString() ?? string.Empty;
            vm.GetCompressedBitsPixel = profile?.GetValue(ExifTag.CompressedBitsPerPixel)?.Value.ToString() ??
                                        string.Empty;
            vm.GetCameraMaker = profile?.GetValue(ExifTag.Make)?.Value ?? string.Empty;
            vm.GetCameraModel = profile?.GetValue(ExifTag.Model)?.Value ?? string.Empty;
            vm.GetExposureProgram = EXIFHelper.GetExposureProgram(profile);
            vm.GetExposureTime = profile?.GetValue(ExifTag.ExposureTime)?.Value.ToString() ?? string.Empty;
            vm.GetFNumber = profile?.GetValue(ExifTag.FNumber)?.Value.ToString() ?? string.Empty;
            vm.GetMaxAperture = profile?.GetValue(ExifTag.MaxApertureValue)?.Value.ToString() ?? string.Empty;
            vm.GetExposureBias = profile?.GetValue(ExifTag.ExposureBiasValue)?.Value.ToString() ?? string.Empty;
            vm.GetDigitalZoom = profile?.GetValue(ExifTag.DigitalZoomRatio)?.Value.ToString() ?? string.Empty;
            vm.GetFocalLength35Mm = profile?.GetValue(ExifTag.FocalLengthIn35mmFilm)?.Value.ToString() ??
                                    string.Empty;
            vm.GetFocalLength = profile?.GetValue(ExifTag.FocalLength)?.Value.ToString() ?? string.Empty;
            vm.GetISOSpeed = EXIFHelper.GetISOSpeed(profile);
            vm.GetMeteringMode = profile?.GetValue(ExifTag.MeteringMode)?.Value.ToString() ?? string.Empty;
            vm.GetContrast = EXIFHelper.GetContrast(profile);
            vm.GetSaturation = EXIFHelper.GetSaturation(profile);
            vm.GetSharpness = EXIFHelper.GetSharpness(profile);
            vm.GetWhiteBalance = EXIFHelper.GetWhiteBalance(profile);
            vm.GetFlashMode = EXIFHelper.GetFlashMode(profile);
            vm.GetFlashEnergy = profile?.GetValue(ExifTag.FlashEnergy)?.Value.ToString() ?? string.Empty;
            vm.GetLightSource = EXIFHelper.GetLightSource(profile);
            vm.GetBrightness = profile?.GetValue(ExifTag.BrightnessValue)?.Value.ToString() ?? string.Empty;
            vm.GetPhotometricInterpretation = EXIFHelper.GetPhotometricInterpretation(profile);
            vm.GetExifVersion = EXIFHelper.GetExifVersion(profile);
            vm.GetLensModel = profile?.GetValue(ExifTag.LensModel)?.Value ?? string.Empty;
            vm.GetLensMaker = profile?.GetValue(ExifTag.LensMake)?.Value ?? string.Empty;
        }
        catch (Exception e)
        {
            #if DEBUG
            Console.WriteLine(e);
            TooltipHelper.ShowTooltipMessage(e);
            #endif
        }
    }
}
