using System.Globalization;
using System.Text;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.Exif;
using PicView.Core.Localization;
using PicView.Core.Metadata;
using PicView.Core.Models;
using PicView.Core.ProcessHandling;
using PicView.Core.Sizing;
using PicView.Core.Titles;
using R3;

namespace PicView.Core.ViewModels;

public class ExifViewModel : IDisposable
{
    /// <param name="languageChanged">
    /// <see cref="TranslationViewModel.LanguageChanged"/>, so the translated text this keeps follows a language change.
    /// </param>
    public ExifViewModel(Observable<Unit>? languageChanged = null)
    {
        _languageSubscription = languageChanged?.Subscribe(_ => Rephrase(),
            DebugHelper.LogError(nameof(ExifViewModel), nameof(Rephrase)));

        OpenGoogleLinkCommand = new ReactiveCommand(OpenGoogleMaps);
        OpenBingLinkCommand = new ReactiveCommand(OpenBingMaps);

        SetExifRating0Command = new ReactiveCommand<FileInfo>(async (s, _) => await SetRating(s, 0).ConfigureAwait(false));
        SetExifRating1Command = new ReactiveCommand<FileInfo>(async (s, _) => await SetRating(s, 1).ConfigureAwait(false));
        SetExifRating2Command = new ReactiveCommand<FileInfo>(async (s, _) => await SetRating(s, 2).ConfigureAwait(false));
        SetExifRating3Command = new ReactiveCommand<FileInfo>(async (s, _) => await SetRating(s, 3).ConfigureAwait(false));
        SetExifRating4Command = new ReactiveCommand<FileInfo>(async (s, _) => await SetRating(s, 4).ConfigureAwait(false));
        SetExifRating5Command = new ReactiveCommand<FileInfo>(async (s, _) => await SetRating(s, 5).ConfigureAwait(false));

        // The file whose values are shown, as for every edit, and shown again from the file afterwards
        RemoveImageMetaDataCommand = new ReactiveCommand<FileInfo>(async (_, _) => await CommitAsync(ExifWriter.RemoveImageMetaData).ConfigureAwait(false));
        
        SetDateTakenCommand = new ReactiveCommand<FileInfo>(async (_, _) => await SetDateTaken().ConfigureAwait(false));

        SetAuthorsCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddAuthors(f, s)).ConfigureAwait(false));
        SetCopyrightCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddCopyright(f, s)).ConfigureAwait(false));
        SetSoftwareCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddSoftware(f, s)).ConfigureAwait(false));
        SetSubjectCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddSubject(f, s)).ConfigureAwait(false));
        SetKeywordsCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddKeywords(f, s)).ConfigureAwait(false));
        SetTitleCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddTitle(f, s)).ConfigureAwait(false));
        SetCommentCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddComment(f, s)).ConfigureAwait(false));
        SetLatitudeCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => GpsHelper.AddLatitude(f, s)).ConfigureAwait(false));
        SetLongitudeCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => GpsHelper.AddLongitude(f, s)).ConfigureAwait(false));
        SetAltitudeCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => GpsHelper.AddAltitude(f, s)).ConfigureAwait(false));
        SetCompressedBitsPixelCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddCompressedBitsPerPixel(f, s)).ConfigureAwait(false));
        SetCameraMakerCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddCameraMaker(f, s)).ConfigureAwait(false));
        SetCameraModelCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddCameraModel(f, s)).ConfigureAwait(false));
        SetFNumberCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddFNumber(f, s)).ConfigureAwait(false));
        SetMaxApertureCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddMaxAperture(f, s)).ConfigureAwait(false));
        SetExposureBiasCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddExposureBias(f, s)).ConfigureAwait(false));
        SetExposureTimeCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddExposureTime(f, s)).ConfigureAwait(false));
        SetDigitalZoomCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddDigitalZoom(f, s)).ConfigureAwait(false));
        SetFocalLengthCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddFocalLength(f, s)).ConfigureAwait(false));
        SetFocalLength35MmCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddFocalLength35mm(f, s)).ConfigureAwait(false));
        SetISOSpeedCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddIsoSpeed(f, s)).ConfigureAwait(false));
        SetFlashEnergyCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddFlashEnergy(f, s)).ConfigureAwait(false));
        SetBrightnessCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddBrightness(f, s)).ConfigureAwait(false));
        SetLensMakerCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddLensMaker(f, s)).ConfigureAwait(false));
        SetLensModelCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddLensModel(f, s)).ConfigureAwait(false));
        SetExifVersionCommand = new ReactiveCommand<string>(async (s, _) => await CommitAsync(f => ExifWriter.AddExifVersion(f, s)).ConfigureAwait(false));

        // Parallel, so each commit takes the image shown when its box commits; CommitSdAsync runs them one at a time
        SetSdPromptCommand = new ReactiveCommand<string>(async (s, _) => await CommitSdAsync(SdField.Prompt, s).ConfigureAwait(false), AwaitOperation.Parallel);
        SetSdNegativePromptCommand = new ReactiveCommand<string>(async (s, _) => await CommitSdAsync(SdField.NegativePrompt, s).ConfigureAwait(false), AwaitOperation.Parallel);
        SetSdSettingsCommand = new ReactiveCommand<string>(async (s, _) => await CommitSdAsync(SdField.Settings, s).ConfigureAwait(false), AwaitOperation.Parallel);
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
    public ReactiveCommand<string> SetKeywordsCommand { get; set; }
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
    public ReactiveCommand<string> SetDigitalZoomCommand { get; set; }
    public ReactiveCommand<string> SetFocalLengthCommand { get; set; }
    public ReactiveCommand<string> SetFocalLength35MmCommand { get; set; }
    public ReactiveCommand<string> SetISOSpeedCommand { get; set; }
    public ReactiveCommand<string> SetFlashEnergyCommand { get; set; }
    public ReactiveCommand<string> SetBrightnessCommand { get; set; }
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

    public BindableReactiveProperty<string?> Keywords { get; } = new();

    public BindableReactiveProperty<string?> Software { get; } = new();

    // Orientation and Compression describe the image, so writing them would change nothing but the tag
    public ExifChoiceField Orientation { get; } =
        new(ExifTag.Orientation, [1, 2, 3, 4, 5, 6, 7, 8], ExifReader.OrientationLabel, isReadOnly: true);

    public ExifChoiceField ResolutionUnit { get; } =
        new(ExifTag.ResolutionUnit, [1, 2, 3], ExifReader.ResolutionUnitLabel);

    public ExifChoiceField ColorRepresentation { get; } =
        new(ExifTag.ColorSpace, [1, 2, 65535], ExifReader.ColorSpaceLabel);

    public ExifChoiceField Compression { get; } =
        new(ExifTag.Compression, ExifReader.CompressionCodes, ExifReader.CompressionLabel, isReadOnly: true);

    public BindableReactiveProperty<string?> Comment { get; } = new();

    public BindableReactiveProperty<string?> CompressedBitsPixel { get; } = new();

    public BindableReactiveProperty<string?> CameraMaker { get; } = new();

    public BindableReactiveProperty<string?> CameraModel { get; } = new();

    public ExifChoiceField ExposureProgram { get; } =
        new(ExifTag.ExposureProgram, [0, 1, 2, 3, 4, 5, 6, 7, 8], ExifReader.ExposureProgramLabel);

    public BindableReactiveProperty<string?> ExposureTime { get; } = new();

    public BindableReactiveProperty<string?> ExposureBias { get; } = new();

    public BindableReactiveProperty<string?> FNumber { get; } = new();

    public BindableReactiveProperty<string?> MaxAperture { get; } = new();

    public BindableReactiveProperty<string?> DigitalZoom { get; } = new();

    public BindableReactiveProperty<string?> FocalLength35Mm { get; } = new();

    public BindableReactiveProperty<string?> FocalLength { get; } = new();

    // ReSharper disable once InconsistentNaming
    public BindableReactiveProperty<string?> ISOSpeed { get; } = new();

    public ExifChoiceField MeteringMode { get; } =
        new(ExifTag.MeteringMode, [0, 1, 2, 3, 4, 5, 6, 255], ExifReader.MeteringModeLabel);

    public ExifChoiceField Contrast { get; } =
        new(ExifTag.Contrast, [0, 1, 2], ExifReader.SoftHardLabel);

    public ExifChoiceField Saturation { get; } =
        new(ExifTag.Saturation, [0, 1, 2], ExifReader.SaturationLabel);

    public ExifChoiceField Sharpness { get; } =
        new(ExifTag.Sharpness, [0, 1, 2], ExifReader.SoftHardLabel);

    public ExifChoiceField WhiteBalance { get; } =
        new(ExifTag.WhiteBalance, [0, 1], ExifReader.WhiteBalanceLabel);

    public ExifChoiceField FlashMode { get; } =
        new(ExifTag.Flash, [0, 1, 5, 7, 65, 69, 71, 89, 79], ExifReader.FlashModeLabel);

    public BindableReactiveProperty<string?> FlashEnergy { get; } = new();

    public ExifChoiceField LightSource { get; } =
        new(ExifTag.LightSource, [0, 1, 2, 3, 4, 9, 10, 11, 12, 13, 14, 15, 17, 18, 19, 20, 21, 22, 23, 24, 255], ExifReader.LightSourceLabel);

    public BindableReactiveProperty<string?> Brightness { get; } = new();

    public ExifChoiceField PhotometricInterpretation { get; } =
        new(ExifTag.PhotometricInterpretation, [0, 1, 2, 3, 4, 5, 6, 8, 9, 10, 32803, 32844, 32845, 32892, 65535], ExifReader.PhotometricInterpretationLabel);

    public BindableReactiveProperty<string?> ExifVersion { get; } = new();

    public BindableReactiveProperty<string?> LensModel { get; } = new();

    public BindableReactiveProperty<string?> LensMaker { get; } = new();

    public BindableReactiveProperty<bool> IsExifAvailable { get; } = new();

    public BindableReactiveProperty<MagickFormat?> ImageFormat { get; } = new();

    /// <summary>Whether the current image carries a motion photo video.</summary>
    public BindableReactiveProperty<bool> IsMotionPhoto { get; } = new();

    /// <summary>
    /// The image format name for display, with a localized " (Motion Photo)" suffix
    /// appended when the image carries a motion photo video.
    /// </summary>
    public BindableReactiveProperty<string?> ImageFormatDisplay { get; } = new();

    public void Dispose()
    {
        _languageSubscription?.Dispose();
        CancelStealthScan();
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
            Comment,
            CompressedBitsPixel,
            Compression,
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
            ImageFormatDisplay,
            ISOSpeed,
            IsExifAvailable,
            IsMotionPhoto,
            Latitude,
            LensMaker,
            LensModel,
            LightSource,
            Longitude,
            MaxAperture,
            MeteringMode,
            Orientation,
            PhotometricInterpretation,
            PixelHeight,
            PixelWidth,
            PrintSizeCm,
            PrintSizeInch,
            Resolution,
            ResolutionUnit,
            Saturation,
            Sharpness,
            SizeMp,
            Software,
            Subject,
            Keywords,
            Title,
            WhiteBalance,
            ImageFormat,
            IsSdMetadataAvailable,
            SdGenerator,
            SdPrompt,
            SdNegativePrompt,
            SdSettings,
            SdRaw,
            SdStorage,
            IsSdEditable,
            CommentLabel,
            SetSdPromptCommand,
            SetSdNegativePromptCommand,
            SetSdSettingsCommand,
            SetAuthorsCommand,
            SetCopyrightCommand,
            SetSoftwareCommand,
            SetSubjectCommand,
            SetKeywordsCommand,
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
            SetDigitalZoomCommand,
            SetFocalLengthCommand,
            SetFocalLength35MmCommand,
            SetISOSpeedCommand,
            SetFlashEnergyCommand,
            SetBrightnessCommand,
            SetLensMakerCommand,
            SetLensModelCommand,
            SetExifVersionCommand);
        
        GC.SuppressFinalize(this);
    }

    // The image whose values are shown, which every write goes to
    private ImageModel? _model;

    /// <summary>
    /// Rebuilds <see cref="ImageFormatDisplay"/> from the current format, appending the
    /// localized " (Motion Photo)" suffix when the image carries a motion photo video.
    /// </summary>
    internal void UpdateImageFormatDisplay()
    {
        var imageFormat = ImageFormat.CurrentValue;
        if (imageFormat is null)
        {
            ImageFormatDisplay.Value = null;
            return;
        }

        Span<char> formatBuffer = stackalloc char[64];
        if (!Enum.TryFormat(imageFormat.Value, formatBuffer, out var formatLength))
        {
            ImageFormatDisplay.Value = null;
            return;
        }

        var marker = TranslationManager.Translation.MotionPhoto;
        if (IsMotionPhoto.CurrentValue && !string.IsNullOrEmpty(marker))
        {
            var requiredLength = formatLength + 3 + marker.Length;
            ImageFormatDisplay.Value = string.Create(requiredLength, (imageFormat.Value, formatLength, marker), static (destination, state) =>
            {
                Enum.TryFormat(state.Value, destination[..state.formatLength], out _);
                destination[state.formatLength] = ' ';
                destination[state.formatLength + 1] = '(';
                state.marker.AsSpan().CopyTo(destination[(state.formatLength + 2)..]);
                destination[^1] = ')';
            });
        }
        else
        {
            ImageFormatDisplay.Value = new string(formatBuffer[..formatLength]);
        }
    }

    public void UpdateExifValues(ImageModel model, MagickImage? magick = null)
    {
        _model = model;
        var shouldDispose = magick != null;

        var fileInfo = model.FileInfo;

        var pixelWidth = PixelWidth.Value = model.PixelWidth;
        var pixelHeight = PixelHeight.Value = model.PixelHeight;
        IsMotionPhoto.Value = model.MotionPhoto is not null;
        try
        {
            // An image without a file, such as a clipboard or base64 image, has its pixel size and nothing else to
            // read, so the values of the image shown before are cleared
            if (magick is null && fileInfo is { Exists: true })
            {
                magick = new MagickImage();
                magick.Ping(fileInfo);
            }

            var profile = magick?.GetExifProfile();

            // Ping does not give a WebP's profile, which is read from its EXIF chunk instead of decoding the pixels
            if (profile is null && magick?.Format is MagickFormat.WebP && fileInfo is { Exists: true })
            {
                profile = WebPExifReader.ReadProfile(fileInfo);
            }

            if (profile is null && magick is not null)
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
                        // Only the Exif tag: ImageMagick's own colorspace attributes, such as jpeg:colorspace, are
                        // how the pixels are encoded, not the Exif ColorSpace
                        else if (key.Contains("exif:colorspace", StringComparison.OrdinalIgnoreCase))
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

            if (fileInfo is { Exists: true })
            {
                UpdateSdMetadataValues(model, fileInfo, Comment.CurrentValue, magick?.GetAttribute("comment"));
            }
            else
            {
                CancelStealthScan();
                ClearSdMetadataValues();
            }

            if (magick is null)
            {
                DpiY.Value = DpiX.Value = 0;
                BitDepth.Value = string.Empty;
            }
            else if (profile != null)
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

            // The image's orientation, not the tag, since some formats keep it outside the Exif profile. The codes of
            // ExifOrientation are the Exif codes
            var orientation = magick is null ? ExifOrientation.None : ExifOrientationHelper.GetImageOrientation(magick);
            Orientation.Read(orientation is ExifOrientation.None ? null : (ushort)orientation);

            ImageFormat.Value = magick?.Format;
            UpdateImageFormatDisplay();

            var meter = TranslationManager.Translation.Meter;

            if (magick is not null && string.IsNullOrEmpty(BitDepth.CurrentValue))
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
            Keywords.Value = ExifReader.GetKeywords(profile);
            Software.Value = profile?.GetValue(ExifTag.Software)?.Value ?? string.Empty;
            ResolutionUnit.Read(profile);
            ColorRepresentation.Read(profile);
            Compression.Read(profile);
            CompressedBitsPixel.Value =
                profile?.GetValue(ExifTag.CompressedBitsPerPixel)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            CameraMaker.Value = profile?.GetValue(ExifTag.Make)?.Value ?? string.Empty;
            CameraModel.Value = profile?.GetValue(ExifTag.Model)?.Value ?? string.Empty;
            ExposureProgram.Read(profile);
            ExposureTime.Value = profile?.GetValue(ExifTag.ExposureTime)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            FNumber.Value = profile?.GetValue(ExifTag.FNumber)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            MaxAperture.Value = profile?.GetValue(ExifTag.MaxApertureValue)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ExposureBias.Value = profile?.GetValue(ExifTag.ExposureBiasValue)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            DigitalZoom.Value = profile?.GetValue(ExifTag.DigitalZoomRatio)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            FocalLength35Mm.Value = profile?.GetValue(ExifTag.FocalLengthIn35mmFilm)?.Value.ToString() ?? string.Empty;
            FocalLength.Value = profile?.GetValue(ExifTag.FocalLength)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ISOSpeed.Value = ExifReader.GetISOSpeed(profile);
            MeteringMode.Read(profile);
            Contrast.Read(profile);
            Saturation.Read(profile);
            Sharpness.Read(profile);
            WhiteBalance.Read(profile);
            FlashMode.Read(profile);
            FlashEnergy.Value = profile?.GetValue(ExifTag.FlashEnergy)?.Value.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            LightSource.Read(profile);
            Brightness.Value = profile?.GetValue(ExifTag.BrightnessValue)?.Value.ToString(CultureInfo.CurrentCulture) ??
                               null;
            PhotometricInterpretation.Read(profile);
            ExifVersion.Value = ExifReader.GetExifVersion(profile);
            LensModel.Value = profile?.GetValue(ExifTag.LensModel)?.Value ?? string.Empty;
            LensMaker.Value = profile?.GetValue(ExifTag.LensMake)?.Value ?? string.Empty;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ExifViewModel), nameof(UpdateExifValues), e);

            // The AI Metadata of the image shown before must not stay editable against this one's file
            CancelStealthScan();
            ClearSdMetadataValues();
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

    private Task<bool> SetDateTaken() =>
        DateTaken.Value is { } date
            ? CommitAsync(f => ExifWriter.SetDateTaken(f, date))
            : Task.FromResult(false);

    /// <summary>
    /// Writes what the user picked in the dropdown of <paramref name="field"/> to the file shown, when it differs from
    /// what the file holds.
    /// </summary>
    public Task<bool> CommitChoiceAsync(ExifChoiceField field) => field.CommitAsync(_model?.FileInfo);

    /// <summary>
    /// Writes a tag the user typed to the file shown, with <paramref name="write"/>, then shows what the file holds,
    /// so a box never shows a value that was not written: a malformed value, or one the write failed for, goes back to
    /// the file's. Nothing is shown again when another image was shown meanwhile.
    /// </summary>
    /// <returns>True if the file was written.</returns>
    internal Task<bool> CommitAsync(Func<FileInfo, Task<bool>> write) => CommitAsync(_model, write);

    /// <summary>
    /// Writes to the file of <paramref name="model"/> with <paramref name="write"/>, then shows what the file holds
    /// when <paramref name="model"/> is still the image shown.
    /// </summary>
    /// <returns>True if the file was written.</returns>
    private async Task<bool> CommitAsync(ImageModel? model, Func<FileInfo, Task<bool>> write)
    {
        if (model is null)
        {
            return false;
        }

        var written = false;
        try
        {
            if (model.FileInfo is { } file)
            {
                written = await write(file).ConfigureAwait(false);
            }
        }
        finally
        {
            if (ReferenceEquals(_model, model))
            {
                model.FileInfo?.Refresh();
                UpdateExifValues(model);
            }
        }

        return written;
    }

    // AI Metadata: the generation data of the image shown, and its edits

    /// <summary>The part of the AI Metadata an edit replaces.</summary>
    public enum SdField
    {
        Prompt,
        NegativePrompt,
        Settings
    }

    /// <summary>True when the image carries AI Metadata.</summary>
    public BindableReactiveProperty<bool> IsSdMetadataAvailable { get; } = new();

    /// <summary>The tool that generated the image, e.g. "Automatic1111" or "ComfyUI".</summary>
    public BindableReactiveProperty<string?> SdGenerator { get; } = new();

    public BindableReactiveProperty<string?> SdPrompt { get; } = new();

    public BindableReactiveProperty<string?> SdNegativePrompt { get; } = new();

    public BindableReactiveProperty<string?> SdSettings { get; } = new();

    /// <summary>The unparsed payload, shown so nothing is lost when parsing falls short.</summary>
    public BindableReactiveProperty<string?> SdRaw { get; } = new();

    /// <summary>
    /// Where the AI Metadata came from, e.g. a PNG text chunk or hidden in the pixels, or that the pixels are being
    /// scanned for it.
    /// </summary>
    public BindableReactiveProperty<string?> SdStorage { get; } = new();

    /// <summary>True when the prompt, negative prompt and settings can be written back to the file.</summary>
    public BindableReactiveProperty<bool> IsSdEditable { get; } = new();

    /// <summary>
    /// The label of the Comment row: "Comment/Prompt" when the UserComment holds the AI Metadata, which both the
    /// Comment box and the AI Metadata boxes edit, and "Comment" otherwise.
    /// </summary>
    public BindableReactiveProperty<string?> CommentLabel { get; } = new(GetCommentLabel(isPrompt: false));

    public ReactiveCommand<string> SetSdPromptCommand { get; }
    public ReactiveCommand<string> SetSdNegativePromptCommand { get; }
    public ReactiveCommand<string> SetSdSettingsCommand { get; }

    // The AI Metadata last shown, which an edit composes the other two fields from
    private SdMetadata? _sd;

    // The image _sd was read from, which can differ from _model while another image is being read
    private ImageModel? _sdModel;

    private CancellationTokenSource? _stealthCts;

    // Rewrites CommentLabel on a language change
    private readonly IDisposable? _languageSubscription;

    // Runs SD commits one at a time, from reading _sd to reading the file again, so none composes from stale data
    private readonly SemaphoreSlim _sdCommitGate = new(1, 1);

    // Makes a scan's check that it is still current and its apply one step, so a cancelled scan never shows its result
    private readonly Lock _stealthLock = new();

    /// <summary>
    /// Writes <paramref name="value"/> over one part of the AI Metadata of the image shown, keeping the other two
    /// parts as its file holds them, then shows what the file holds.
    /// </summary>
    /// <returns>True if the file was written.</returns>
    public Task<bool> CommitSdAsync(SdField field, string? value) => CommitSdAsync(_model, field, value);

    /// <summary>
    /// Writes <paramref name="value"/> over one part of the AI Metadata of <paramref name="model"/>, keeping the
    /// other two parts as its file holds them, then shows what the file holds. Commits run one at a time, so each one
    /// composes from what the one before it wrote. Nothing is written when <paramref name="model"/> is no longer the
    /// image shown, or the data shown is not yet its own; nor for data that is not editable, a value equal to what is
    /// shown, a Settings value that is not a settings line, or one that would not read back as typed. The box then
    /// goes back to the file's value.
    /// </summary>
    /// <returns>True if the file was written.</returns>
    public async Task<bool> CommitSdAsync(ImageModel? model, SdField field, string? value)
    {
        await _sdCommitGate.WaitAsync().ConfigureAwait(false);
        try
        {
            // Another image is shown, or is being read: the edit is dropped, and that image's values are left alone
            if (model is null || !ReferenceEquals(_model, model) || !ReferenceEquals(_sdModel, model) ||
                _sd is not { } sd)
            {
                return false;
            }

            if (!sd.IsEditable ||
                string.Equals(SdMetadataParser.Normalize(value), SdMetadataParser.Normalize(GetField(sd, field)),
                    StringComparison.Ordinal) ||
                (field == SdField.Settings && !IsSettingsValue(value)))
            {
                ShowFileValue(model, field);
                return false;
            }

            var prompt = field == SdField.Prompt ? value : sd.Prompt;
            var negativePrompt = field == SdField.NegativePrompt ? value : sd.NegativePrompt;
            var settings = field == SdField.Settings ? value : sd.Settings;
            var text = SdMetadataParser.ComposeA1111(prompt, negativePrompt, settings);

            var written = await CommitAsync(model, f =>
                SdMetadataReader.ReadsBackAs(sd.Source, text, prompt, negativePrompt, settings)
                    ? SdMetadataWriter.WriteAsync(f, sd, text)
                    : Task.FromResult(false)).ConfigureAwait(false);

            // Reading the file again leaves a box whose field did not change alone, so a refused edit is undone here
            ShowFileValue(model, field);
            return written;
        }
        finally
        {
            _sdCommitGate.Release();
        }
    }

    /// <summary>Puts the box of <paramref name="field"/> back to what the file of <paramref name="model"/> holds.</summary>
    private void ShowFileValue(ImageModel model, SdField field)
    {
        if (ReferenceEquals(_sdModel, model) && _sd is { } sd)
        {
            GetBindable(field).Value = GetField(sd, field) ?? string.Empty;
        }
    }

    /// <summary>
    /// True when <paramref name="value"/> starts with a settings line. A cleared or free-text Settings box would compose
    /// a text that can still read back, only without its settings line, so the seed, sampler and model would be lost.
    /// </summary>
    private static bool IsSettingsValue(string? value)
    {
        var normalized = SdMetadataParser.Normalize(value);
        var lineEnd = normalized.IndexOf('\n');
        return SdMetadataParser.IsSettingsLine(lineEnd < 0 ? normalized : normalized[..lineEnd]);
    }

    private static string? GetField(SdMetadata sd, SdField field) => field switch
    {
        SdField.Prompt => sd.Prompt,
        SdField.NegativePrompt => sd.NegativePrompt,
        _ => sd.Settings
    };

    private BindableReactiveProperty<string?> GetBindable(SdField field) => field switch
    {
        SdField.Prompt => SdPrompt,
        SdField.NegativePrompt => SdNegativePrompt,
        _ => SdSettings
    };

    /// <summary>
    /// Fills the AI Metadata fields from the image's generation parameters, and clears
    /// them when it has none.
    /// <para>
    /// When no parameters are stored in metadata, a scan for stealth pnginfo is started in the
    /// background. That one decodes the pixels, so it must not hold up the rest of the window;
    /// it is cancelled as soon as another image is shown.
    /// </para>
    /// </summary>
    private void UpdateSdMetadataValues(ImageModel model, FileInfo fileInfo, string? userComment, string? imageComment)
    {
        CancelStealthScan();

        var sdMetadata = SdMetadataReader.Read(fileInfo, userComment, imageComment);
        if (sdMetadata is not null)
        {
            ApplySdMetadata(sdMetadata, model);
            return;
        }

        ClearSdMetadataValues();
        StartStealthScan(model, fileInfo);
    }

    /// <summary>
    /// Shows <paramref name="sdMetadata"/>, read from the file of <paramref name="model"/>. When the same image is read
    /// again, such as after PicView's own write, a box is set only when its field changed in the file, so text being
    /// typed in another box is kept.
    /// </summary>
    internal void ApplySdMetadata(SdMetadata sdMetadata, ImageModel? model)
    {
        var previous = model is not null && ReferenceEquals(_sdModel, model) ? _sd : null;
        _sd = sdMetadata;
        _sdModel = model;
        SdGenerator.Value = sdMetadata.Generator;
        ShowField(SdPrompt, previous, previous?.Prompt, sdMetadata.Prompt);
        ShowField(SdNegativePrompt, previous, previous?.NegativePrompt, sdMetadata.NegativePrompt);
        ShowField(SdSettings, previous, previous?.Settings, sdMetadata.Settings);
        SdRaw.Value = sdMetadata.Raw;
        SdStorage.Value = GetStorageLabel(sdMetadata);
        IsSdEditable.Value = sdMetadata.IsEditable;
        CommentLabel.Value = GetCommentLabel(sdMetadata.Source == SdSource.ExifUserComment);
        IsSdMetadataAvailable.Value = true;
    }

    private static void ShowField(BindableReactiveProperty<string?> box, SdMetadata? previous, string? previousValue,
        string? value)
    {
        if (previous is null || !string.Equals(previousValue, value, StringComparison.Ordinal))
        {
            box.Value = value ?? string.Empty;
        }
    }

    private static string GetCommentLabel(bool isPrompt)
    {
        var translation = TranslationManager.Translation;
        return isPrompt ? $"{translation.Comment}/{translation.Prompt}" : translation.Comment ?? string.Empty;
    }

    /// <summary>Writes <see cref="CommentLabel"/> again, in the language just loaded.</summary>
    private void Rephrase() => CommentLabel.Value = GetCommentLabel(_sd?.Source == SdSource.ExifUserComment);

    private static string GetStorageLabel(SdMetadata sdMetadata)
    {
        var translation = TranslationManager.Translation;
        return sdMetadata.Source switch
        {
            SdSource.PngText => translation.StoredInPngText ?? string.Empty,
            SdSource.ExifUserComment => translation.StoredInUserComment ?? string.Empty,
            SdSource.ImageComment => translation.StoredInImageComment ?? string.Empty,
            SdSource.Pixels when !string.IsNullOrEmpty(sdMetadata.StealthDetail) =>
                $"{translation.StoredInPixels} ({sdMetadata.StealthDetail})",
            SdSource.Pixels => translation.StoredInPixels ?? string.Empty,
            _ => string.Empty
        };
    }

    /// <summary>
    /// Scans the pixel data for hidden parameters, off the calling thread. Only images that
    /// could plausibly carry them are scanned, and only while the info window is showing this
    /// image -- <see cref="UpdateSdMetadataValues"/> is reached only from the info window.
    /// </summary>
    private void StartStealthScan(ImageModel model, FileInfo fileInfo)
    {
        if (!IsStealthCandidate(fileInfo))
        {
            return;
        }

        var cts = new CancellationTokenSource();
        _stealthCts = cts;
        var token = cts.Token;

        // The Stored in box says so until the scan ends
        SdStorage.Value = TranslationManager.Translation.ScanningHiddenMetadata ?? string.Empty;

        _ = Task.Run(() =>
        {
            var isApplied = false;
            try
            {
                var hidden = SdMetadataReader.ReadStealth(fileInfo, token);
                if (hidden is null)
                {
                    return;
                }

                lock (_stealthLock)
                {
                    // The bindables fire while _stealthLock is held. That is safe only because Avalonia marshals a
                    // binding update from another thread with Post, so no subscriber may block on the UI thread here
                    if (!token.IsCancellationRequested)
                    {
                        ApplySdMetadata(hidden, model);
                        isApplied = true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Another image was shown; its own read owns the fields now
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(ExifViewModel), nameof(StartStealthScan), e);
            }
            finally
            {
                lock (_stealthLock)
                {
                    if (ReferenceEquals(_stealthCts, cts))
                    {
                        _stealthCts = null;
                    }

                    // Nothing was found, or the scan failed: the box no longer says it is scanning
                    if (!token.IsCancellationRequested && !isApplied)
                    {
                        SdStorage.Value = string.Empty;
                    }
                }

                cts.Dispose();
            }
        }, token);
    }

    /// <summary>
    /// Stealth pnginfo only survives in lossless formats -- a JPEG re-encode destroys the low
    /// bits it lives in -- so scanning anything else is wasted work.
    /// </summary>
    private static bool IsStealthCandidate(FileInfo fileInfo) =>
        fileInfo.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
        fileInfo.Extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);

    private void CancelStealthScan()
    {
        lock (_stealthLock)
        {
            var cts = _stealthCts;
            _stealthCts = null;
            if (cts is null)
            {
                return;
            }

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The scan finished and disposed it first
            }

            // The box no longer says it is scanning; the image read next shows its own value
            SdStorage.Value = string.Empty;
        }
    }

    private void ClearSdMetadataValues()
    {
        _sd = null;
        _sdModel = null;
        IsSdMetadataAvailable.Value = false;
        IsSdEditable.Value = false;
        CommentLabel.Value = GetCommentLabel(isPrompt: false);
        SdGenerator.Value =
            SdPrompt.Value =
                SdNegativePrompt.Value =
                    SdSettings.Value =
                        SdStorage.Value =
                            SdRaw.Value = string.Empty;
    }
}
