using System.Diagnostics;
using System.Globalization;
using System.Text;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.Exif;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ProcessHandling;
using PicView.Core.Sizing;
using PicView.Core.Titles;
using R3;

namespace PicView.Core.ViewModels;

public class ExifViewModel : IDisposable
{
    public ExifViewModel()
    {
        OpenGoogleLinkCommand = new ReactiveCommand(OpenGoogleMaps);
        OpenBingLinkCommand = new ReactiveCommand(OpenBingMaps);

        SetExifRating0Command = new ReactiveCommand<string>(async (s, _) => await SetRating(s, 0));
        SetExifRating1Command = new ReactiveCommand<string>(async (s, _) => await SetRating(s, 1));
        SetExifRating2Command = new ReactiveCommand<string>(async (s, _) => await SetRating(s, 2));
        SetExifRating3Command = new ReactiveCommand<string>(async (s, _) => await SetRating(s, 3));
        SetExifRating4Command = new ReactiveCommand<string>(async (s, _) => await SetRating(s, 4));
        SetExifRating5Command = new ReactiveCommand<string>(async (s, _) => await SetRating(s, 5));

        SetDateTakenCommand = new ReactiveCommand<FileInfo>(async (f, _) => await SetDateTaken(f));

        ResolutionUnits = new BindableReactiveProperty<string[]>([
            string.Empty,
            TranslationManager.Translation.None ?? string.Empty,
            TranslationManager.Translation.Inches ?? string.Empty,
            TranslationManager.Translation.Centimeters ?? string.Empty
        ]);

        ColorRepresentations = new BindableReactiveProperty<string[]>([
            string.Empty,
            "sRGB",
            "Adobe RGB",
            TranslationManager.Translation.Uncalibrated ?? "Uncalibrated"
        ]);

        Compressions = new BindableReactiveProperty<string[]>([
            string.Empty,
            TranslationManager.GetTranslation("Uncompressed"),
            "CCITT 1D",
            "T4/Group 3 Fax",
            "T6/Group 4 Fax",
            "LZW",
            "JPEG (old-style)",
            "JPEG",
            "Adobe Deflate",
            "JBIG B&W",
            "JBIG Color",
            "JPEG",
            "Kodak 262",
            "Next",
            "Sony ARW Compressed",
            "Packed RAW",
            "Samsung SRW Compressed",
            "CCIRLEW",
            "Samsung SRW Compressed 2",
            "PackBits",
            "Thunderscan",
            "Kodak KDC Compressed",
            "IT8CTPAD",
            "IT8LW",
            "IT8MP",
            "IT8BL",
            "PixarFilm",
            "PixarLog",
            "Deflate",
            "DCS",
            "Aperio JPEG 2000 YCbCr",
            "Aperio JPEG 2000 RGB",
            "JBIG",
            "SGILog",
            "SGILog24",
            "JPEG 2000",
            "Nikon NEF Compressed",
            "JBIG2 TIFF FX",
            "Microsoft Document Imaging (MDI)\n Binary Level Codec",
            "Microsoft Document Imaging (MDI)\n Progressive Transform Codec",
            "Microsoft Document Imaging (MDI) Vector",
            "ESRI Lerc",
            "Lossy JPEG",
            "LZMA2",
            "Zstd (old)",
            "WebP (old)",
            "PNG",
            "JPEG XR",
            "Zstd",
            "WebP",
            "JPEG XL (old)",
            "JPEG XL",
            "Kodak DCR Compressed",
            "Pentax PEF Compressed"
        ]);

        Debug.Assert(TranslationManager.Translation.Flipped != null);
        Debug.Assert(TranslationManager.Translation.Normal != null);
        Orientations = new BindableReactiveProperty<string[]>([
            string.Empty,
            TranslationManager.Translation.Normal,
            TranslationManager.Translation.Flipped,
            $"{TranslationManager.Translation.Rotated} 180\u00b0",
            $"{TranslationManager.Translation.Rotated} 180\u00b0, {TranslationManager.Translation.Flipped}",
            $"{TranslationManager.Translation.Rotated} 270\u00b0, {TranslationManager.Translation.Flipped}",
            $"{TranslationManager.Translation.Rotated} 90\u00b0",
            $"{TranslationManager.Translation.Rotated} 90\u00b0, {TranslationManager.Translation.Flipped}",
            $"{TranslationManager.Translation.Rotated} 270\u00b0"
        ]);
    }

    public ReactiveCommand? OpenGoogleLinkCommand { get; }
    public ReactiveCommand? OpenBingLinkCommand { get; }

    public ReactiveCommand<string>? SetExifRating0Command { get; set; }
    public ReactiveCommand<string>? SetExifRating1Command { get; set; }
    public ReactiveCommand<string>? SetExifRating2Command { get; set; }
    public ReactiveCommand<string>? SetExifRating3Command { get; set; }
    public ReactiveCommand<string>? SetExifRating4Command { get; set; }
    public ReactiveCommand<string>? SetExifRating5Command { get; set; }

    public ReactiveCommand<FileInfo> SetDateTakenCommand { get; set; }

    public BindableReactiveProperty<uint> ExifRating { get; } = new();
    public BindableReactiveProperty<double> DpiX { get; } = new();

    public BindableReactiveProperty<double> DpiY { get; } = new();

    public BindableReactiveProperty<string?> PrintSizeInch { get; } = new();

    public BindableReactiveProperty<string?> PrintSizeCm { get; } = new();

    public BindableReactiveProperty<string?> SizeMp { get; } = new();

    public BindableReactiveProperty<string?> Resolution { get; } = new();

    public BindableReactiveProperty<string?> BitDepth { get; } = new();

    public BindableReactiveProperty<string?> AspectRatio { get; } = new();

    public BindableReactiveProperty<string?> Latitude { get; } = new();

    public BindableReactiveProperty<string?> Longitude { get; } = new();

    public BindableReactiveProperty<string?> Altitude { get; } = new();

    public BindableReactiveProperty<string?> GoogleLink { get; } = new();

    public BindableReactiveProperty<string?> BingLink { get; } = new();

    public BindableReactiveProperty<string?> Authors { get; } = new();

    public BindableReactiveProperty<DateTime?> DateTaken { get; } = new();

    public BindableReactiveProperty<string?> Copyright { get; } = new();

    public BindableReactiveProperty<string?> Title { get; } = new();

    public BindableReactiveProperty<string?> Subject { get; } = new();

    public BindableReactiveProperty<string?> Software { get; } = new();

    public BindableReactiveProperty<string[]> Orientations { get; }
    public BindableReactiveProperty<int> Orientation { get; } = new();
    public BindableReactiveProperty<ushort?> ResolutionUnit { get; } = new();

    public BindableReactiveProperty<string[]> ResolutionUnits { get; }


    public BindableReactiveProperty<string[]> ColorRepresentations { get; }
    public BindableReactiveProperty<ushort?> ColorRepresentation { get; } = new();

    public BindableReactiveProperty<ushort?> Compression { get; } = new();
    public BindableReactiveProperty<string[]> Compressions { get; }

    public BindableReactiveProperty<string?> Comment { get; } = new();

    public BindableReactiveProperty<string?> CompressedBitsPixel { get; } = new();

    public BindableReactiveProperty<string?> CameraMaker { get; } = new();

    public BindableReactiveProperty<string?> CameraModel { get; } = new();

    public BindableReactiveProperty<string?> ExposureProgram { get; } = new();

    public BindableReactiveProperty<string?> ExposureTime { get; } = new();

    public BindableReactiveProperty<string?> ExposureBias { get; } = new();

    public BindableReactiveProperty<string?> FNumber { get; } = new();

    public BindableReactiveProperty<string?> MaxAperture { get; } = new();

    public BindableReactiveProperty<string?> DigitalZoom { get; } = new();

    public BindableReactiveProperty<string?> FocalLength35Mm { get; } = new();

    public BindableReactiveProperty<string?> FocalLength { get; } = new();

    // ReSharper disable once InconsistentNaming
    public BindableReactiveProperty<string?> ISOSpeed { get; } = new();

    public BindableReactiveProperty<string?> MeteringMode { get; } = new();

    public BindableReactiveProperty<string?> Contrast { get; } = new();

    public BindableReactiveProperty<string?> Saturation { get; } = new();

    public BindableReactiveProperty<string?> Sharpness { get; } = new();

    public BindableReactiveProperty<string?> WhiteBalance { get; } = new();

    public BindableReactiveProperty<string?> FlashMode { get; } = new();

    public BindableReactiveProperty<string?> FlashEnergy { get; } = new();

    public BindableReactiveProperty<string?> LightSource { get; } = new();

    public BindableReactiveProperty<string?> Brightness { get; } = new();

    public BindableReactiveProperty<string?> PhotometricInterpretation { get; } = new();

    public BindableReactiveProperty<string?> ExifVersion { get; } = new();

    public BindableReactiveProperty<string?> LensModel { get; } = new();

    public BindableReactiveProperty<string?> LensMaker { get; } = new();

    public BindableReactiveProperty<bool> IsExifAvailable { get; } = new();

    public void Dispose()
    {
        Disposable.Dispose(
            Altitude,
            AspectRatio,
            Authors,
            BingLink,
            BitDepth,
            Brightness,
            CameraMaker,
            CameraModel,
            ColorRepresentation,
            ColorRepresentations,
            Comment,
            CompressedBitsPixel,
            Compression,
            Compressions,
            Contrast,
            Copyright,
            DateTaken,
            DigitalZoom,
            DpiX,
            DpiY,
            ExifRating,
            ExifVersion,
            ExposureBias,
            ExposureProgram,
            ExposureTime,
            FlashEnergy,
            FlashMode,
            FNumber,
            FocalLength,
            FocalLength35Mm,
            GoogleLink,
            ISOSpeed,
            IsExifAvailable,
            Latitude,
            LensMaker,
            LensModel,
            LightSource,
            Longitude,
            MaxAperture,
            MeteringMode,
            Orientation,
            Orientations,
            PhotometricInterpretation,
            PrintSizeCm,
            PrintSizeInch,
            Resolution,
            ResolutionUnit,
            ResolutionUnits,
            Saturation,
            Sharpness,
            SizeMp,
            Software,
            Subject,
            Title,
            WhiteBalance);
    }

    public void UpdateExifValues(ImageModel model, MagickImage? magick = null)
    {
        var shouldDispose = magick != null;

        var fileInfo = model.FileInfo;
        var orientation = model.Orientation;
        var pixelWidth = model.PixelWidth;
        var pixelHeight = model.PixelHeight;
        try
        {
            if (fileInfo is null || !fileInfo.Exists)
            {
                return;
            }

            if (magick is null)
            {
                magick = new MagickImage();
                magick.Ping(fileInfo);
            }

            var profile = magick.GetExifProfile();

            if (profile is null)
            {
                // Check both Attributes and Artifacts as RAW metadata can reside in either
                var metadataNames = magick.AttributeNames.Concat(magick.ArtifactNames).Distinct();

                var enumerable = metadataNames as string[] ?? metadataNames.ToArray();
                if (enumerable.Length != 0)
                {
                    profile = new ExifProfile();
                    foreach (var name in enumerable)
                    {
                        var val = magick.GetAttribute(name) ?? magick.GetArtifact(name);
                        if (string.IsNullOrWhiteSpace(val))
                        {
                            continue;
                        }

                        // Normalize name to lowercase for easier matching
                        var key = name.ToLowerInvariant();

                        // --- Date and Time ---
                        if (key.Contains("date") && (key.Contains("create") || key.Contains("original")))
                        {
                            if (DateTime.TryParse(val, out var date))
                            {
                                profile.SetValue(ExifTag.DateTimeOriginal, date.ToString("yyyy:MM:dd HH:mm:ss"));
                            }
                        }
                        // --- Camera Details ---
                        else if (key.Contains("camera.model.name") || key.Contains("exif:model"))
                        {
                            profile.SetValue(ExifTag.Model, val);
                        }
                        else if (key.Contains("camera.make.name") || key.Contains("exif:make"))
                        {
                            profile.SetValue(ExifTag.Make, val);
                        }
                        // --- Exposure Settings ---
                        else if (key.Contains("exposure.time") || key.Contains("exif:exposuretime"))
                        {
                            if (ExifFunctions.TryParseRational(val, out var rational))
                            {
                                profile.SetValue(ExifTag.ExposureTime, rational);
                            }
                        }
                        else if (key.Contains("f.number") || key.Contains("exif:fnumber"))
                        {
                            if (ExifFunctions.TryParseRational(val, out var rational))
                            {
                                profile.SetValue(ExifTag.FNumber, rational);
                            }
                        }
                        else if (key.Contains("iso") || key.Contains("exif:isospeedratings"))
                        {
                            if (ushort.TryParse(val, out var iso))
                            {
                                profile.SetValue(ExifTag.ISOSpeedRatings, [iso]);
                            }
                        }
                        else if (key.Contains("exposure.bias") || key.Contains("exif:exposurebiasvalue"))
                        {
                            if (ExifFunctions.TryParseSignedRational(val, out var rational))
                            {
                                profile.SetValue(ExifTag.ExposureBiasValue, rational);
                            }
                        }
                        // --- Optics ---
                        else if (key.Contains("focal.length") && !key.Contains("35mm"))
                        {
                            if (ExifFunctions.TryParseRational(val, out var rational))
                            {
                                profile.SetValue(ExifTag.FocalLength, rational);
                            }
                        }
                        else if (key.Contains("focal.length.in.35mm") || key.Contains("exif:focallengthin35mmfilm"))
                        {
                            if (ushort.TryParse(val, out var num))
                            {
                                profile.SetValue(ExifTag.FocalLengthIn35mmFilm, num);
                            }
                        }
                        else if (key.Contains("lens.model") || key.Contains("exif:lensmodel"))
                        {
                            profile.SetValue(ExifTag.LensModel, val);
                        }

                        // --- GPS Coordinates (Lat/Long are Rational Arrays) ---
                        else if (key.Contains("gps.latitude") && !key.Contains("ref"))
                        {
                            var parts = val.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
                            var rationals = parts
                                .Select(p => ExifFunctions.TryParseRational(p, out var r) ? r : default).ToArray();
                            if (rationals.Length > 0)
                            {
                                profile.SetValue(ExifTag.GPSLatitude, rationals);
                            }
                        }
                        else if (key.Contains("gps.longitude") && !key.Contains("ref"))
                        {
                            var parts = val.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
                            var rationals = parts
                                .Select(p => ExifFunctions.TryParseRational(p, out var r) ? r : default).ToArray();
                            if (rationals.Length > 0)
                            {
                                profile.SetValue(ExifTag.GPSLongitude, rationals);
                            }
                        }
                        else if (key.Contains("gps.latituderef") || key.Contains("exif:gpslatituderef"))
                        {
                            profile.SetValue(ExifTag.GPSLatitudeRef, val.Trim());
                        }
                        else if (key.Contains("gps.longituderef") || key.Contains("exif:gpslongituderef"))
                        {
                            profile.SetValue(ExifTag.GPSLongitudeRef, val.Trim());
                        }

                        // --- Shooting Info ---
                        else if (key.Contains("white.balance") || key.Contains("exif:whitebalance"))
                        {
                            // Typically 0 = Auto, 1 = Manual in EXIF
                            if (val.Contains("auto", StringComparison.OrdinalIgnoreCase))
                            {
                                profile.SetValue(ExifTag.WhiteBalance, (ushort)0);
                            }
                            else if (val.Contains("manual", StringComparison.OrdinalIgnoreCase))
                            {
                                profile.SetValue(ExifTag.WhiteBalance, (ushort)1);
                            }
                        }

                        // --- Identification & Rights ---
                        if (key.Contains("artist") || key.Contains("author") || key.Contains("exif:artist"))
                        {
                            profile.SetValue(ExifTag.Artist, val);
                        }
                        else if (key.Contains("copyright") || key.Contains("exif:copyright"))
                        {
                            profile.SetValue(ExifTag.Copyright, val);
                        }
                        else if (key.Contains("software") || key.Contains("exif:software"))
                        {
                            profile.SetValue(ExifTag.Software, val);
                        }

                        // --- Titles & Comments ---
                        else if (key.Contains("comment") || key.Contains("exif:usercomment"))
                        {
                            // ExifReader expects ASCII/UNICODE prefixing for UserComment
                            var bytes = Encoding.ASCII.GetBytes("ASCII\0\0\0" + val);
                            profile.SetValue(ExifTag.UserComment, bytes);
                        }
                        else if (key.Contains("description") || key.Contains("exif:imagedescription"))
                        {
                            profile.SetValue(ExifTag.ImageDescription, val);
                        }

                        // --- Rating ---
                        else if (key.Contains("rating") || key.Contains("exif:rating"))
                        {
                            if (ushort.TryParse(val, out var rating))
                            {
                                profile.SetValue(ExifTag.Rating, rating);
                            }
                        }

                        // --- Color Representation ---
                        else if (key.Contains("colorspace") || key.Contains("exif:colorspace"))
                        {
                            if (val.Contains("srgb", StringComparison.OrdinalIgnoreCase))
                            {
                                profile.SetValue(ExifTag.ColorSpace, (ushort)1);
                            }
                            else if (val.Contains("adobe", StringComparison.OrdinalIgnoreCase))
                            {
                                profile.SetValue(ExifTag.ColorSpace, (ushort)2);
                            }
                            else if (ushort.TryParse(val, out var space))
                            {
                                profile.SetValue(ExifTag.ColorSpace, space);
                            }
                        }
                    }
                }
            }

            if (profile != null)
            {
                DpiY.Value = profile.GetValue(ExifTag.YResolution)?.Value.ToDouble() ?? model.DpiX;
                DpiX.Value = profile.GetValue(ExifTag.XResolution)?.Value.ToDouble() ?? model.DpiY;
                var depth = profile.GetValue(ExifTag.BitsPerSample)?.Value;
                if (depth is not null)
                {
                    var x = depth.Aggregate(0, (current, value) => current + value);
                    BitDepth.Value = x.ToString();
                }
                else
                {
                    BitDepth.Value = (magick.Depth * 3).ToString();
                }
            }
            else
            {
                foreach (var artifactName in magick.ArtifactNames)
                {
                    DebugHelper.LogDebug(nameof(ExifViewModel), nameof(UpdateExifValues), artifactName);
                }


                DpiY.Value = model.DpiX;
                DpiX.Value = model.DpiY;
                BitDepth.Value = (magick.Depth * 3).ToString();
            }

            Orientation.Value = orientation switch
            {
                ExifOrientation.Horizontal => 1,
                ExifOrientation.MirrorHorizontal => 2,
                ExifOrientation.Rotate180 => 3,
                ExifOrientation.MirrorVertical => 4,
                ExifOrientation.MirrorHorizontalRotate270Cw => 5,
                ExifOrientation.Rotate90Cw => 6,
                ExifOrientation.MirrorHorizontalRotate90Cw => 7,
                ExifOrientation.Rotated270Cw => 8,
                _ => 0
            };

            var meter = TranslationManager.Translation.Meter;

            if (string.IsNullOrEmpty(BitDepth.CurrentValue))
            {
                BitDepth.Value = (magick.Depth * 3).ToString();
            }

            if (DpiX.CurrentValue == 0 || DpiY.CurrentValue == 0) // Check for zero before division
            {
                PrintSizeCm.Value =
                    PrintSizeInch.Value =
                        SizeMp.Value =
                            Resolution.Value = string.Empty;
            }
            else
            {
                var printSizes =
                    PrintSizing.GetPrintSizes(pixelWidth, pixelHeight, DpiX.CurrentValue, DpiY.CurrentValue);

                PrintSizeCm.Value = printSizes.PrintSizeCm;
                PrintSizeInch.Value = printSizes.PrintSizeInch;
                SizeMp.Value = printSizes.SizeMp;

                Resolution.Value = $"{DpiX} x {DpiY} {TranslationManager.Translation.Dpi}";
            }

            var gcd = AspectRatioFormatter.GCD(pixelWidth, pixelHeight);
            if (gcd != 0) // Check for zero before division
            {
                AspectRatio.Value = AspectRatioFormatter.GetFormattedAspectRatio(gcd, pixelWidth, pixelHeight);
            }
            else
            {
                AspectRatio.Value = string.Empty; // Handle cases where gcd is 0
            }

            ExifRating.Value = profile?.GetValue(ExifTag.Rating)?.Value ?? 0;

            var gpsValues = GpsHelper.GetGpsValues(profile);

            if (gpsValues is not null)
            {
                Latitude.Value = gpsValues[0];
                Longitude.Value = gpsValues[1];

                GoogleLink.Value = gpsValues[2];
                BingLink.Value = gpsValues[3];
            }
            else
            {
                Latitude.Value =
                    Longitude.Value =
                        GoogleLink.Value =
                            BingLink.Value = string.Empty;
            }

            var altitude = profile?.GetValue(ExifTag.GPSAltitude)?.Value;
            Altitude.Value = altitude.HasValue
                ? $"{altitude.Value.ToDouble()} {meter}"
                : string.Empty;
            var getAuthors = profile?.GetValue(ExifTag.Artist)?.Value;
            Authors.Value = getAuthors ?? string.Empty;
            DateTaken.Value = ExifReader.GetDateTaken(profile);
            Copyright.Value = profile?.GetValue(ExifTag.Copyright)?.Value ?? string.Empty;
            Title.Value = ExifReader.GetTitle(profile);
            Subject.Value = ExifReader.GetSubject(profile);
            Software.Value = profile?.GetValue(ExifTag.Software)?.Value ?? string.Empty;
            ResolutionUnit.Value = ExifReader.GetResolutionUnit(profile);
            ColorRepresentation.Value = profile?.GetValue(ExifTag.ColorSpace)?.Value ?? 0;
            Compression.Value = profile?.GetValue(ExifTag.Compression)?.Value ?? 0;
            CompressedBitsPixel.Value =
                profile?.GetValue(ExifTag.CompressedBitsPerPixel)?.Value.ToString() ?? string.Empty;
            CameraMaker.Value = profile?.GetValue(ExifTag.Make)?.Value ?? string.Empty;
            CameraModel.Value = profile?.GetValue(ExifTag.Model)?.Value ?? string.Empty;
            ExposureProgram.Value = ExifReader.GetExposureProgram(profile);
            ExposureTime.Value = profile?.GetValue(ExifTag.ExposureTime)?.Value.ToString() ?? string.Empty;
            FNumber.Value = profile?.GetValue(ExifTag.FNumber)?.Value.ToString() ?? string.Empty;
            MaxAperture.Value = profile?.GetValue(ExifTag.MaxApertureValue)?.Value.ToString() ?? string.Empty;
            ExposureBias.Value = profile?.GetValue(ExifTag.ExposureBiasValue)?.Value.ToString() ?? string.Empty;
            DigitalZoom.Value = profile?.GetValue(ExifTag.DigitalZoomRatio)?.Value.ToString() ?? string.Empty;
            FocalLength35Mm.Value = profile?.GetValue(ExifTag.FocalLengthIn35mmFilm)?.Value.ToString() ?? string.Empty;
            FocalLength.Value = profile?.GetValue(ExifTag.FocalLength)?.Value.ToString() ?? string.Empty;
            ISOSpeed.Value = ExifReader.GetISOSpeed(profile);
            MeteringMode.Value = profile?.GetValue(ExifTag.MeteringMode)?.Value.ToString() ?? string.Empty;
            Contrast.Value = ExifReader.GetContrast(profile);
            Saturation.Value = ExifReader.GetSaturation(profile);
            Sharpness.Value = ExifReader.GetSharpness(profile);
            WhiteBalance.Value = ExifReader.GetWhiteBalance(profile);
            FlashMode.Value = ExifReader.GetFlashMode(profile);
            FlashEnergy.Value = profile?.GetValue(ExifTag.FlashEnergy)?.Value.ToString() ?? string.Empty;
            LightSource.Value = ExifReader.GetLightSource(profile);
            Brightness.Value = profile?.GetValue(ExifTag.BrightnessValue)?.Value.ToString(CultureInfo.CurrentCulture) ??
                               null;
            PhotometricInterpretation.Value = ExifReader.GetPhotometricInterpretation(profile);
            ExifVersion.Value = ExifReader.GetExifVersion(profile);
            LensModel.Value = profile?.GetValue(ExifTag.LensModel)?.Value ?? string.Empty;
            LensMaker.Value = profile?.GetValue(ExifTag.LensMake)?.Value ?? string.Empty;
            Comment.Value = ExifReader.GetUserComment(profile);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ExifViewModel), nameof(UpdateExifValues), e);
        }
        finally
        {
            if (shouldDispose)
            {
                magick.Dispose();
            }
        }
    }

    public void OpenGoogleMaps(Unit unit) => ProcessHelper.OpenLink(GoogleLink.CurrentValue);
    public void OpenBingMaps(Unit unit) => ProcessHelper.OpenLink(BingLink.CurrentValue);

    private async Task<bool> SetRating(string filePath, ushort rating)
    {
        var isRated = await ExifWriter.SetExifRatingAsync(new FileInfo(filePath), rating).ConfigureAwait(false);
        if (!isRated)
        {
            return false;
        }

        ExifRating.Value = rating;
        return true;
    }

    private async Task<bool> SetDateTaken(FileInfo fileInfo)
    {
        return await ExifWriter.SetDateTaken(fileInfo, DateTaken.Value.Value);
    }
}