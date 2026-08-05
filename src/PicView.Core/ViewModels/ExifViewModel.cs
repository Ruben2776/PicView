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

        SetExifRating0Command = new ReactiveCommand<FileInfo>(async (s, _) => await SetRating(s, 0).ConfigureAwait(false));
        SetExifRating1Command = new ReactiveCommand<FileInfo>(async (s, _) => await SetRating(s, 1).ConfigureAwait(false));
        SetExifRating2Command = new ReactiveCommand<FileInfo>(async (s, _) => await SetRating(s, 2).ConfigureAwait(false));
        SetExifRating3Command = new ReactiveCommand<FileInfo>(async (s, _) => await SetRating(s, 3).ConfigureAwait(false));
        SetExifRating4Command = new ReactiveCommand<FileInfo>(async (s, _) => await SetRating(s, 4).ConfigureAwait(false));
        SetExifRating5Command = new ReactiveCommand<FileInfo>(async (s, _) => await SetRating(s, 5).ConfigureAwait(false));

        RemoveImageMetaDataCommand = new ReactiveCommand<FileInfo>(async (f, _) => await RemoveImageMetaData(f).ConfigureAwait(false));
        
        SetDateTakenCommand = new ReactiveCommand<FileInfo>(async (f, _) => await SetDateTaken(f).ConfigureAwait(false));

        SetAuthorsCommand = new ReactiveCommand<string>(async (s, _) => { Authors.Value = s; await AddExifPropertyAsync(ExifWriter.AddAuthors, s).ConfigureAwait(false); });
        SetCopyrightCommand = new ReactiveCommand<string>(async (s, _) => { Copyright.Value = s; await AddExifPropertyAsync(ExifWriter.AddCopyright, s).ConfigureAwait(false); });
        SetSoftwareCommand = new ReactiveCommand<string>(async (s, _) => { Software.Value = s; await AddExifPropertyAsync(ExifWriter.AddSoftware, s).ConfigureAwait(false); });
        SetSubjectCommand = new ReactiveCommand<string>(async (s, _) => { Subject.Value = s; await AddExifPropertyAsync(ExifWriter.AddSubject, s).ConfigureAwait(false); });
        SetTitleCommand = new ReactiveCommand<string>(async (s, _) => { Title.Value = s; await AddExifPropertyAsync(ExifWriter.AddTitle, s).ConfigureAwait(false); });
        SetCommentCommand = new ReactiveCommand<string>(async (s, _) => { Comment.Value = s; await AddExifPropertyAsync(ExifWriter.AddComment, s).ConfigureAwait(false); });
        SetLatitudeCommand = new ReactiveCommand<string>(async (s, _) => { Latitude.Value = s; await AddExifPropertyAsync(GpsHelper.AddLatitude, s).ConfigureAwait(false); });
        SetLongitudeCommand = new ReactiveCommand<string>(async (s, _) => { Longitude.Value = s; await AddExifPropertyAsync(GpsHelper.AddLongitude, s).ConfigureAwait(false); });
        SetAltitudeCommand = new ReactiveCommand<string>(async (s, _) => { Altitude.Value = s; await AddExifPropertyAsync(GpsHelper.AddAltitude, s).ConfigureAwait(false); });
        SetCompressedBitsPixelCommand = new ReactiveCommand<string>(async (s, _) => { CompressedBitsPixel.Value = s; await AddExifPropertyAsync(ExifWriter.AddCompressedBitsPerPixel, s).ConfigureAwait(false); });
        SetCameraMakerCommand = new ReactiveCommand<string>(async (s, _) => { CameraMaker.Value = s; await AddExifPropertyAsync(ExifWriter.AddCameraMaker, s).ConfigureAwait(false); });
        SetCameraModelCommand = new ReactiveCommand<string>(async (s, _) => { CameraModel.Value = s; await AddExifPropertyAsync(ExifWriter.AddCameraModel, s).ConfigureAwait(false); });
        SetFNumberCommand = new ReactiveCommand<string>(async (s, _) => { FNumber.Value = s; await AddExifPropertyAsync(ExifWriter.AddFNumber, s).ConfigureAwait(false); });
        SetMaxApertureCommand = new ReactiveCommand<string>(async (s, _) => { MaxAperture.Value = s; await AddExifPropertyAsync(ExifWriter.AddMaxAperture, s).ConfigureAwait(false); });
        SetExposureBiasCommand = new ReactiveCommand<string>(async (s, _) => { ExposureBias.Value = s; await AddExifPropertyAsync(ExifWriter.AddExposureBias, s).ConfigureAwait(false); });
        SetExposureTimeCommand = new ReactiveCommand<string>(async (s, _) => { ExposureTime.Value = s; await AddExifPropertyAsync(ExifWriter.AddExposureTime, s).ConfigureAwait(false); });
        SetExposureProgramCommand = new ReactiveCommand<string>(async (s, _) => { ExposureProgram.Value = s; await AddExifPropertyAsync(ExifWriter.AddExposureProgram, s).ConfigureAwait(false); });
        SetDigitalZoomCommand = new ReactiveCommand<string>(async (s, _) => { DigitalZoom.Value = s; await AddExifPropertyAsync(ExifWriter.AddDigitalZoom, s).ConfigureAwait(false); });
        SetFocalLengthCommand = new ReactiveCommand<string>(async (s, _) => { FocalLength.Value = s; await AddExifPropertyAsync(ExifWriter.AddFocalLength, s).ConfigureAwait(false); });
        SetFocalLength35MmCommand = new ReactiveCommand<string>(async (s, _) => { FocalLength35Mm.Value = s; await AddExifPropertyAsync(ExifWriter.AddFocalLength35mm, s).ConfigureAwait(false); });
        SetISOSpeedCommand = new ReactiveCommand<string>(async (s, _) => { ISOSpeed.Value = s; await AddExifPropertyAsync(ExifWriter.AddIsoSpeed, s).ConfigureAwait(false); });
        SetMeteringModeCommand = new ReactiveCommand<string>(async (s, _) => { MeteringMode.Value = s; await AddExifPropertyAsync(ExifWriter.AddMeteringMode, s).ConfigureAwait(false); });
        SetContrastCommand = new ReactiveCommand<string>(async (s, _) => { Contrast.Value = s; await AddExifPropertyAsync(ExifWriter.AddContrast, s).ConfigureAwait(false); });
        SetSaturationCommand = new ReactiveCommand<string>(async (s, _) => { Saturation.Value = s; await AddExifPropertyAsync(ExifWriter.AddSaturation, s).ConfigureAwait(false); });
        SetSharpnessCommand = new ReactiveCommand<string>(async (s, _) => { Sharpness.Value = s; await AddExifPropertyAsync(ExifWriter.AddSharpness, s).ConfigureAwait(false); });
        SetWhiteBalanceCommand = new ReactiveCommand<string>(async (s, _) => { WhiteBalance.Value = s; await AddExifPropertyAsync(ExifWriter.AddWhiteBalance, s).ConfigureAwait(false); });
        SetFlashEnergyCommand = new ReactiveCommand<string>(async (s, _) => { FlashEnergy.Value = s; await AddExifPropertyAsync(ExifWriter.AddFlashEnergy, s).ConfigureAwait(false); });
        SetFlashModeCommand = new ReactiveCommand<string>(async (s, _) => { FlashMode.Value = s; await AddExifPropertyAsync(ExifWriter.AddFlashMode, s).ConfigureAwait(false); });
        SetLightSourceCommand = new ReactiveCommand<string>(async (s, _) => { LightSource.Value = s; await AddExifPropertyAsync(ExifWriter.AddLightSource, s).ConfigureAwait(false); });
        SetBrightnessCommand = new ReactiveCommand<string>(async (s, _) => { Brightness.Value = s; await AddExifPropertyAsync(ExifWriter.AddBrightness, s).ConfigureAwait(false); });
        SetPhotometricInterpretationCommand = new ReactiveCommand<string>(async (s, _) => { PhotometricInterpretation.Value = s; await AddExifPropertyAsync(ExifWriter.AddPhotometricInterpretation, s).ConfigureAwait(false); });
        SetLensMakerCommand = new ReactiveCommand<string>(async (s, _) => { LensMaker.Value = s; await AddExifPropertyAsync(ExifWriter.AddLensMaker, s).ConfigureAwait(false); });
        SetLensModelCommand = new ReactiveCommand<string>(async (s, _) => { LensModel.Value = s; await AddExifPropertyAsync(ExifWriter.AddLensModel, s).ConfigureAwait(false); });
        SetExifVersionCommand = new ReactiveCommand<string>(async (s, _) => { ExifVersion.Value = s; await AddExifPropertyAsync(ExifWriter.AddExifVersion, s).ConfigureAwait(false); });

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

    public ReactiveCommand<FileInfo>? SetExifRating0Command { get; set; }
    public ReactiveCommand<FileInfo>? SetExifRating1Command { get; set; }
    public ReactiveCommand<FileInfo>? SetExifRating2Command { get; set; }
    public ReactiveCommand<FileInfo>? SetExifRating3Command { get; set; }
    public ReactiveCommand<FileInfo>? SetExifRating4Command { get; set; }
    public ReactiveCommand<FileInfo>? SetExifRating5Command { get; set; }
    public ReactiveCommand<FileInfo> RemoveImageMetaDataCommand { get; set; }
    public ReactiveCommand<FileInfo> SetDateTakenCommand { get; set; }
    public ReactiveCommand<string> SetAuthorsCommand { get; set; }
    public ReactiveCommand<string> SetCopyrightCommand { get; set; }
    public ReactiveCommand<string> SetSoftwareCommand { get; set; }
    public ReactiveCommand<string> SetSubjectCommand { get; set; }
    public ReactiveCommand<string> SetTitleCommand { get; set; }
    public ReactiveCommand<string> SetCommentCommand { get; set; }
    public ReactiveCommand<string> SetLatitudeCommand { get; set; }
    public ReactiveCommand<string> SetLongitudeCommand { get; set; }
    public ReactiveCommand<string> SetAltitudeCommand { get; set; }
    public ReactiveCommand<string> SetCompressedBitsPixelCommand { get; set; }
    public ReactiveCommand<string> SetCameraMakerCommand { get; set; }
    public ReactiveCommand<string> SetCameraModelCommand { get; set; }
    public ReactiveCommand<string> SetFNumberCommand { get; set; }
    public ReactiveCommand<string> SetMaxApertureCommand { get; set; }
    public ReactiveCommand<string> SetExposureBiasCommand { get; set; }
    public ReactiveCommand<string> SetExposureTimeCommand { get; set; }
    public ReactiveCommand<string> SetExposureProgramCommand { get; set; }
    public ReactiveCommand<string> SetDigitalZoomCommand { get; set; }
    public ReactiveCommand<string> SetFocalLengthCommand { get; set; }
    public ReactiveCommand<string> SetFocalLength35MmCommand { get; set; }
    public ReactiveCommand<string> SetISOSpeedCommand { get; set; }
    public ReactiveCommand<string> SetMeteringModeCommand { get; set; }
    public ReactiveCommand<string> SetContrastCommand { get; set; }
    public ReactiveCommand<string> SetSaturationCommand { get; set; }
    public ReactiveCommand<string> SetSharpnessCommand { get; set; }
    public ReactiveCommand<string> SetWhiteBalanceCommand { get; set; }
    public ReactiveCommand<string> SetFlashEnergyCommand { get; set; }
    public ReactiveCommand<string> SetFlashModeCommand { get; set; }
    public ReactiveCommand<string> SetLightSourceCommand { get; set; }
    public ReactiveCommand<string> SetBrightnessCommand { get; set; }
    public ReactiveCommand<string> SetPhotometricInterpretationCommand { get; set; }
    public ReactiveCommand<string> SetLensMakerCommand { get; set; }
    public ReactiveCommand<string> SetLensModelCommand { get; set; }
    public ReactiveCommand<string> SetExifVersionCommand { get; set; }

    public BindableReactiveProperty<uint> PixelWidth { get; } = new();
    public BindableReactiveProperty<uint> PixelHeight { get; } = new();
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
    
    public BindableReactiveProperty<MagickFormat?> ImageFormat { get; } = new();

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
            PixelHeight,
            PixelWidth,
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
            WhiteBalance,
            ImageFormat,
            SetAuthorsCommand,
            SetCopyrightCommand,
            SetSoftwareCommand,
            SetSubjectCommand,
            SetTitleCommand,
            SetCommentCommand,
            SetLatitudeCommand,
            SetLongitudeCommand,
            SetAltitudeCommand,
            SetCompressedBitsPixelCommand,
            SetCameraMakerCommand,
            SetCameraModelCommand,
            SetFNumberCommand,
            SetMaxApertureCommand,
            SetExposureBiasCommand,
            SetExposureTimeCommand,
            SetExposureProgramCommand,
            SetDigitalZoomCommand,
            SetFocalLengthCommand,
            SetFocalLength35MmCommand,
            SetISOSpeedCommand,
            SetMeteringModeCommand,
            SetContrastCommand,
            SetSaturationCommand,
            SetSharpnessCommand,
            SetWhiteBalanceCommand,
            SetFlashEnergyCommand,
            SetFlashModeCommand,
            SetLightSourceCommand,
            SetBrightnessCommand,
            SetPhotometricInterpretationCommand,
            SetLensMakerCommand,
            SetLensModelCommand,
            SetExifVersionCommand);
        
        GC.SuppressFinalize(this);
    }

    private FileInfo? _fileInfo;

#pragma warning disable MA0051
    public void UpdateExifValues(ImageModel model, MagickImage? magick = null)
#pragma warning restore MA0051
    {
        _fileInfo = model.FileInfo;
        var shouldDispose = magick != null;

        var fileInfo = model.FileInfo;

        var pixelWidth = PixelWidth.Value = model.PixelWidth;
        var pixelHeight = PixelHeight.Value = model.PixelHeight;
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
                var metadataNames = magick.AttributeNames.Concat(magick.ArtifactNames).Distinct(StringComparer.OrdinalIgnoreCase);

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
                        if (key.Contains("date", StringComparison.OrdinalIgnoreCase) &&
                            (key.Contains("create", StringComparison.OrdinalIgnoreCase) 
                             || key.Contains("original", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (DateTime.TryParse(val, CultureInfo.InvariantCulture, out var date))
                            {
                                profile.SetValue(ExifTag.DateTimeOriginal, 
                                    date.ToString("yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture));
                            }
                        }
                        // --- Camera Details ---
                        else if (key.Contains("camera.model.name", StringComparison.OrdinalIgnoreCase)
                                 || key.Contains("exif:model", StringComparison.OrdinalIgnoreCase))
                        {
                            profile.SetValue(ExifTag.Model, val);
                        }
                        else if (key.Contains("camera.make.name", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:make", StringComparison.OrdinalIgnoreCase))
                        {
                            profile.SetValue(ExifTag.Make, val);
                        }
                        // --- Exposure Settings ---
                        else if (key.Contains("exposure.time", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:exposuretime", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ExifFunctions.TryParseRational(val, out var rational))
                            {
                                profile.SetValue(ExifTag.ExposureTime, rational);
                            }
                        }
                        else if (key.Contains("f.number", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:fnumber", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ExifFunctions.TryParseRational(val, out var rational))
                            {
                                profile.SetValue(ExifTag.FNumber, rational);
                            }
                        }
                        else if (key.Contains("iso", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:isospeedratings", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ushort.TryParse(val, out var iso))
                            {
                                profile.SetValue(ExifTag.ISOSpeedRatings, [iso]);
                            }
                        }
                        else if (key.Contains("exposure.bias", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:exposurebiasvalue", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ExifFunctions.TryParseSignedRational(val, out var rational))
                            {
                                profile.SetValue(ExifTag.ExposureBiasValue, rational);
                            }
                        }
                        // --- Optics ---
                        else if (key.Contains("focal.length", StringComparison.OrdinalIgnoreCase) 
                                 && !key.Contains("35mm", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ExifFunctions.TryParseRational(val, out var rational))
                            {
                                profile.SetValue(ExifTag.FocalLength, rational);
                            }
                        }
                        else if (key.Contains("focal.length.in.35mm", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:focallengthin35mmfilm", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ushort.TryParse(val, out var num))
                            {
                                profile.SetValue(ExifTag.FocalLengthIn35mmFilm, num);
                            }
                        }
                        else if (key.Contains("lens.model", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:lensmodel", StringComparison.OrdinalIgnoreCase))
                        {
                            profile.SetValue(ExifTag.LensModel, val);
                        }

                        // --- GPS Coordinates (Lat/Long are Rational Arrays) ---
                        else if (key.Contains("gps.latitude", StringComparison.OrdinalIgnoreCase) 
                                 && !key.Contains("ref", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = val.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
                            var rationals = parts
                                .Select(p => ExifFunctions.TryParseRational(p, out var r) ? r : default).ToArray();
                            if (rationals.Length > 0)
                            {
                                profile.SetValue(ExifTag.GPSLatitude, rationals);
                            }
                        }
                        else if (key.Contains("gps.longitude", StringComparison.OrdinalIgnoreCase) 
                                 && !key.Contains("ref", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = val.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
                            var rationals = parts
                                .Select(p => ExifFunctions.TryParseRational(p, out var r) ? r : default).ToArray();
                            if (rationals.Length > 0)
                            {
                                profile.SetValue(ExifTag.GPSLongitude, rationals);
                            }
                        }
                        else if (key.Contains("gps.latituderef", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:gpslatituderef", StringComparison.OrdinalIgnoreCase))
                        {
                            profile.SetValue(ExifTag.GPSLatitudeRef, val.Trim());
                        }
                        else if (key.Contains("gps.longituderef", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:gpslongituderef", StringComparison.OrdinalIgnoreCase))
                        {
                            profile.SetValue(ExifTag.GPSLongitudeRef, val.Trim());
                        }

                        // --- Shooting Info ---
                        else if (key.Contains("white.balance", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:whitebalance", StringComparison.OrdinalIgnoreCase))
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
                        if (key.Contains("artist", StringComparison.OrdinalIgnoreCase) 
                            || key.Contains("author", StringComparison.OrdinalIgnoreCase) 
                            || key.Contains("exif:artist", StringComparison.OrdinalIgnoreCase))
                        {
                            profile.SetValue(ExifTag.Artist, val);
                        }
                        else if (key.Contains("copyright", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:copyright", StringComparison.OrdinalIgnoreCase))
                        {
                            profile.SetValue(ExifTag.Copyright, val);
                        }
                        else if (key.Contains("software", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:software", StringComparison.OrdinalIgnoreCase))
                        {
                            profile.SetValue(ExifTag.Software, val);
                        }

                        // --- Titles & Comments ---
                        else if (key.Contains("comment", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:usercomment", StringComparison.OrdinalIgnoreCase))
                        {
                            // ExifReader expects ASCII/UNICODE prefixing for UserComment
                            var bytes = Encoding.ASCII.GetBytes("ASCII\0\0\0" + val);
                            profile.SetValue(ExifTag.UserComment, bytes);
                        }
                        else if (key.Contains("description", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:imagedescription", StringComparison.OrdinalIgnoreCase))
                        {
                            profile.SetValue(ExifTag.ImageDescription, val);
                        }

                        // --- Rating ---
                        else if (key.Contains("rating", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:rating", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ushort.TryParse(val, out var rating))
                            {
                                profile.SetValue(ExifTag.Rating, rating);
                            }
                        }

                        // --- Color Representation ---
                        else if (key.Contains("colorspace", StringComparison.OrdinalIgnoreCase) 
                                 || key.Contains("exif:colorspace", StringComparison.OrdinalIgnoreCase))
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

            // Read UserComment before any other EXIF tag. Magick.NET normalizes the
            // profile byte order on first access, which legacy Unicode decoding needs.
            Comment.Value = ExifReader.GetUserComment(profile);

            if (profile != null)
            {
                DpiY.Value = profile.GetValue(ExifTag.YResolution)?.Value.ToDouble() ?? magick.Density.X;
                DpiX.Value = profile.GetValue(ExifTag.XResolution)?.Value.ToDouble() ?? magick.Density.Y;
                var depth = profile.GetValue(ExifTag.BitsPerSample)?.Value;
                if (depth is not null)
                {
                    var x = depth.Aggregate(0, (current, value) => current + value);
                    BitDepth.Value = x.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    BitDepth.Value = (magick.Depth * 3).ToString();
                }
            }
            else
            {
#if DEBUG
                foreach (var artifactName in magick.ArtifactNames)
                {
                    DebugHelper.LogDebug(nameof(ExifViewModel), nameof(UpdateExifValues), artifactName);
                }
#endif
                
                DpiY.Value = magick.Density.X;
                DpiX.Value = magick.Density.Y;
                BitDepth.Value = (magick.Depth * 3).ToString();
            }

            var orientation = ExifOrientationHelper.GetImageOrientation(magick);
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

            ImageFormat.Value = magick.Format;

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
                profile?.GetValue(ExifTag.CompressedBitsPerPixel)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            CameraMaker.Value = profile?.GetValue(ExifTag.Make)?.Value ?? string.Empty;
            CameraModel.Value = profile?.GetValue(ExifTag.Model)?.Value ?? string.Empty;
            ExposureProgram.Value = ExifReader.GetExposureProgram(profile);
            ExposureTime.Value = profile?.GetValue(ExifTag.ExposureTime)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            FNumber.Value = profile?.GetValue(ExifTag.FNumber)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            MaxAperture.Value = profile?.GetValue(ExifTag.MaxApertureValue)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ExposureBias.Value = profile?.GetValue(ExifTag.ExposureBiasValue)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            DigitalZoom.Value = profile?.GetValue(ExifTag.DigitalZoomRatio)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            FocalLength35Mm.Value = profile?.GetValue(ExifTag.FocalLengthIn35mmFilm)?.Value.ToString() ?? string.Empty;
            FocalLength.Value = profile?.GetValue(ExifTag.FocalLength)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ISOSpeed.Value = ExifReader.GetISOSpeed(profile);
            MeteringMode.Value = profile?.GetValue(ExifTag.MeteringMode)?.Value.ToString() ?? string.Empty;
            Contrast.Value = ExifReader.GetContrast(profile);
            Saturation.Value = ExifReader.GetSaturation(profile);
            Sharpness.Value = ExifReader.GetSharpness(profile);
            WhiteBalance.Value = ExifReader.GetWhiteBalance(profile);
            FlashMode.Value = ExifReader.GetFlashMode(profile);
            FlashEnergy.Value = profile?.GetValue(ExifTag.FlashEnergy)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            LightSource.Value = ExifReader.GetLightSource(profile);
            Brightness.Value = profile?.GetValue(ExifTag.BrightnessValue)?.Value.ToString(CultureInfo.CurrentCulture) ??
                               null;
            PhotometricInterpretation.Value = ExifReader.GetPhotometricInterpretation(profile);
            ExifVersion.Value = ExifReader.GetExifVersion(profile);
            LensModel.Value = profile?.GetValue(ExifTag.LensModel)?.Value ?? string.Empty;
            LensMaker.Value = profile?.GetValue(ExifTag.LensMake)?.Value ?? string.Empty;
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

    private async Task<bool> SetRating(FileInfo fileInfo, ushort rating)
    {
        var isRated = await ExifWriter.SetExifRatingAsync(fileInfo, rating).ConfigureAwait(false);
        if (!isRated)
        {
            return false;
        }

        ExifRating.Value = rating;
        return true;
    }

    private async Task SetDateTaken(FileInfo fileInfo)
    {
        if (DateTaken.Value != null)
        {
            await ExifWriter.SetDateTaken(fileInfo, DateTaken.Value.Value).ConfigureAwait(false);
        }
    }

    private async Task AddExifPropertyAsync<T>(Func<FileInfo?, T, Task<bool>> addAction, T value)
    {
        if (_fileInfo is not null)
        {
            await addAction(_fileInfo, value).ConfigureAwait(false);
        }
    }
    
    private async Task RemoveImageMetaData(FileInfo fileInfo)
    {
        if (!fileInfo.Exists)
        {
            return;
        }
        await ExifWriter.RemoveImageMetaData(fileInfo).ConfigureAwait(false);
        
        // Remove UI displayed fields
        ExifRating.Value = 0;
        DateTaken.Value = null;
        Copyright.Value = string.Empty;
        Authors.Value = string.Empty;
        Software.Value = string.Empty;
        Subject.Value = string.Empty;
        Title.Value = string.Empty;
        Comment.Value = string.Empty;
        Latitude.Value = null;
        Longitude.Value = null;
        Altitude.Value = null;
        CompressedBitsPixel.Value = string.Empty;
        CameraMaker.Value = string.Empty;
        CameraModel.Value = string.Empty;
        FNumber.Value = string.Empty;
        MaxAperture.Value = string.Empty;
        ExposureBias.Value = string.Empty;
        ExposureTime.Value = string.Empty;
        DigitalZoom.Value = string.Empty;
        FocalLength.Value = string.Empty;
        FocalLength35Mm.Value = string.Empty;
        ISOSpeed.Value = string.Empty;
        MeteringMode.Value = string.Empty;
        Contrast.Value = string.Empty;
        Saturation.Value = string.Empty;
        Sharpness.Value = string.Empty;
        WhiteBalance.Value = string.Empty;
        FlashEnergy.Value = string.Empty;
        FlashMode.Value = string.Empty;
        LightSource.Value = string.Empty;
        Brightness.Value = string.Empty;
        PhotometricInterpretation.Value = string.Empty;
        LensMaker.Value = string.Empty;
        LensModel.Value = string.Empty;
        ExifVersion.Value = string.Empty;
        DateTaken.Value = null;
    }
}
