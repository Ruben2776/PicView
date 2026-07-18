using System.Globalization;
using System.Text;
using ImageMagick;
using PicView.Core.DebugTools;

namespace PicView.Core.Exif;

/// <summary>
/// Provides functionality for modifying EXIF (Exchangeable Image File Format) metadata in image files.
/// </summary>
public static class ExifWriter
{
    /// <summary>
    /// Sets the EXIF rating of an image file to the specified value.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The rating to set, ranging from 0 to 5.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> SetExifRatingAsync(FileInfo? fileInfo, ushort? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null || fileInfo is null)
            {
                return false;
            }

            if (!fileInfo.Exists)
            {
                return false;
            }

            var rating = value.Value;
            if (rating > 5)
            {
                DebugHelper.LogDebug(nameof(ExifWriter), nameof(SetExifRatingAsync),
                    $"Rating '{rating}' is not a valid value.");
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.Rating, rating);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(SetExifRatingAsync));

    /// <summary>
    /// Sets the "Date Taken" metadata of an image file to the specified value.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The date and time to set as the "Date Taken" value.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> SetDateTaken(FileInfo? fileInfo, DateTime value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (fileInfo is null)
            {
                return false;
            }

            if (!fileInfo.Exists)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.DateTime, value.ToString(CultureInfo.InvariantCulture));
            profile.SetValue(ExifTag.DateTimeOriginal, value.ToString(CultureInfo.InvariantCulture));
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(SetDateTaken));


    /// <summary>
    /// Adds a subject to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The subject text to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddSubject(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.XPSubject, Encoding.Unicode.GetBytes(value));
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddSubject));

    /// <summary>
    /// Adds a title to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The title text to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddTitle(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.XPTitle, Encoding.Unicode.GetBytes(value));
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddTitle));

    /// <summary>
    /// Adds a user comment to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The comment text to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddComment(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.UserComment, Encoding.ASCII.GetBytes(value));
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddComment));

    /// <summary>
    /// Adds the resolution unit to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The resolution unit to set (e.g., 2 for inches, 3 for centimeters).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddResolutionUnit(FileInfo? fileInfo, ushort? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.ResolutionUnit, value.Value);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddResolutionUnit));

    /// <summary>
    /// Adds the color space information to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The color space value to set (e.g., 1 for sRGB).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddColorSpace(FileInfo? fileInfo, ushort? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.ColorSpace, value.Value);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddResolutionUnit));

    /// <summary>
    /// Adds the compression type to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The compression value to set.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddCompression(FileInfo? fileInfo, ushort? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.Compression, value.Value);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddCompression));


    /// <summary>
    /// Adds the compressed bits per pixel to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The compressed bits per pixel value to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddCompressedBitsPerPixel(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
            {
                if (value is null)
                {
                    return false;
                }

                if (!ExifFunctions.TryParseRational(value, out var rational))
                {
                    return false;
                }

                var profile = magickImage.GetExifProfile() ?? new ExifProfile();
                profile.SetValue(ExifTag.CompressedBitsPerPixel, rational);
                magickImage.SetProfile(profile);
                return true;
            },
            nameof(ExifWriter), nameof(AddCompressedBitsPerPixel));

    /// <summary>
    /// Adds the camera manufacturer to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The camera manufacturer name to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddCameraMaker(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.Make, value);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddCameraMaker));

    /// <summary>
    /// Adds the camera model to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The camera model name to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddCameraModel(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.Model, value);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddCameraModel));

    /// <summary>
    /// Adds the F-number to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The F-number value to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddFNumber(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
            {
                if (value is null)
                {
                    return false;
                }

                if (!ExifFunctions.TryParseRational(value, out var rational))
                {
                    // The string value is not in a valid format
                    DebugHelper.LogDebug(nameof(ExifWriter), nameof(AddFNumber),
                        $"Could not parse '{value}' to SignedRational.");
                    return false;
                }

                var profile = magickImage.GetExifProfile() ?? new ExifProfile();
                profile.SetValue(ExifTag.FNumber, rational);
                magickImage.SetProfile(profile);
                return true;
            },
            nameof(ExifWriter), nameof(AddFNumber));

    /// <summary>
    /// Adds the maximum aperture value to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The maximum aperture value to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddMaxAperture(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
            {
                if (value is null)
                {
                    return false;
                }

                if (!ExifFunctions.TryParseRational(value, out var rational))
                {
                    // The string value is not in a valid format
                    DebugHelper.LogDebug(nameof(ExifWriter), nameof(AddMaxAperture),
                        $"Could not parse '{value}' to SignedRational.");
                    return false;
                }

                var profile = magickImage.GetExifProfile() ?? new ExifProfile();
                profile.SetValue(ExifTag.MaxApertureValue, rational);
                magickImage.SetProfile(profile);
                return true;
            },
            nameof(ExifWriter), nameof(AddMaxAperture));

    /// <summary>
    /// Adds the exposure bias value to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The exposure bias value to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddExposureBias(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
            {
                if (value is null)
                {
                    return false;
                }

                if (!ExifFunctions.TryParseSignedRational(value, out var rational))
                {
                    return false;
                }

                var profile = magickImage.GetExifProfile() ?? new ExifProfile();
                profile.SetValue(ExifTag.ExposureBiasValue, rational);
                magickImage.SetProfile(profile);
                return true;
            },
            nameof(ExifWriter), nameof(AddExposureBias));

    /// <summary>
    /// Adds the exposure time to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The exposure time value to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddExposureTime(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
            {
                if (value is null)
                {
                    return false;
                }

                if (!ExifFunctions.TryParseRational(value, out var rational))
                {
                    return false;
                }

                var profile = magickImage.GetExifProfile() ?? new ExifProfile();
                profile.SetValue(ExifTag.ExposureTime, rational);
                magickImage.SetProfile(profile);
                return true;
            },
            nameof(ExifWriter), nameof(AddExposureTime));

    /// <summary>
    /// Adds the exposure program to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The exposure program value to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddExposureProgram(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (!ushort.TryParse(value, out var program))
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.ExposureProgram, program);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddExposureProgram));

    /// <summary>
    /// Adds the digital zoom ratio to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The digital zoom ratio to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddDigitalZoom(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            if (!ExifFunctions.TryParseRational(value, out var rational))
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.DigitalZoomRatio, rational);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddDigitalZoom));

    /// <summary>
    /// Adds the focal length to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The focal length to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddFocalLength(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
            {
                if (value is null)
                {
                    return false;
                }

                if (!ExifFunctions.TryParseRational(value, out var rational))
                {
                    return false;
                }

                var profile = magickImage.GetExifProfile() ?? new ExifProfile();
                profile.SetValue(ExifTag.FocalLength, rational);
                magickImage.SetProfile(profile);
                return true;
            },
            nameof(ExifWriter), nameof(AddFocalLength));

    /// <summary>
    /// Adds the 35mm equivalent focal length to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The 35mm equivalent focal length to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddFocalLength35mm(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (!ushort.TryParse(value, out var length))
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.FocalLengthIn35mmFilm, length);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddFocalLength35mm));

    /// <summary>
    /// Adds the ISO speed to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The ISO speed rating to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddIsoSpeed(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (!ushort.TryParse(value, out var speed))
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.ISOSpeedRatings, [speed]);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddIsoSpeed));

    /// <summary>
    /// Adds the metering mode to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The metering mode to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddMeteringMode(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (!ushort.TryParse(value, out var mode))
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.MeteringMode, mode);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddMeteringMode));

    /// <summary>
    /// Adds the contrast setting to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The contrast setting to add (e.g., 0 = Normal, 1 = Soft, 2 = Hard).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddContrast(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (!ushort.TryParse(value, out var contrast))
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.Contrast, contrast);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddContrast));


    /// <summary>
    /// Adds the saturation level to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The saturation level to add (e.g., 0 = Normal, 1 = Low, 2 = High).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddSaturation(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (!ushort.TryParse(value, out var saturation))
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.Saturation, saturation);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddSaturation));

    /// <summary>
    /// Adds the sharpness level to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The sharpness level to add (e.g., 0 = Normal, 1 = Soft, 2 = Hard).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddSharpness(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (!ushort.TryParse(value, out var sharpness))
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.Sharpness, sharpness);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddSharpness));

    /// <summary>
    /// Adds the white balance mode to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The white balance mode to add (e.g., 0 = Auto, 1 = Manual).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddWhiteBalance(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (!ushort.TryParse(value, out var whiteBalance))
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.WhiteBalance, whiteBalance);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddWhiteBalance));

    /// <summary>
    /// Adds the flash energy to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The flash energy value to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddFlashEnergy(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
            {
                if (value is null)
                {
                    return false;
                }

                if (!ExifFunctions.TryParseRational(value, out var rational))
                {
                    return false;
                }

                var profile = magickImage.GetExifProfile() ?? new ExifProfile();
                profile.SetValue(ExifTag.FlashEnergy, rational);
                magickImage.SetProfile(profile);
                return true;
            },
            nameof(ExifWriter), nameof(AddFlashEnergy));

    /// <summary>
    /// Adds the flash mode to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The flash mode to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddFlashMode(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (!ushort.TryParse(value, out var flash))
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.Flash, flash);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddFlashMode));

    /// <summary>
    /// Adds the light source to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The light source to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddLightSource(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (!ushort.TryParse(value, out var source))
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.LightSource, source);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddLightSource));

    /// <summary>
    /// Adds the brightness value to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The brightness value to add, as a string representing a signed rational number (e.g., "-1/2").</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddBrightness(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false; // No value to set
            }

            if (!ExifFunctions.TryParseSignedRational(value, out var brightnessValue))
            {
                // The string value is not in a valid format
                DebugHelper.LogDebug(nameof(ExifWriter), nameof(AddBrightness),
                    $"Could not parse '{value}' to SignedRational.");
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.BrightnessValue, brightnessValue);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddBrightness));

    /// <summary>
    /// Adds the photometric interpretation to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The photometric interpretation value to add (e.g., 2 for RGB, 6 for YCbCr).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddPhotometricInterpretation(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (!ushort.TryParse(value, out var interpretation))
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.PhotometricInterpretation, interpretation);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddPhotometricInterpretation));

    /// <summary>
    /// Adds the lens manufacturer to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The lens manufacturer name to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddLensMaker(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.LensMake, value);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddLensMaker));

    /// <summary>
    /// Adds the lens model to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The lens model name to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddLensModel(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.LensModel, value);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddLensModel));

    /// <summary>
    /// Adds the EXIF version to the metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The EXIF version string to add (e.g., "0230").</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddExifVersion(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.ExifVersion, Encoding.ASCII.GetBytes(value));
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddExifVersion));

    /// <summary>
    /// Removes the EXIF metadata from the specified image file.
    /// </summary>
    /// <param name="fileInfo">The file information of the image from which the EXIF data will be removed.</param>
    /// <returns>A task representing the asynchronous operation, containing a boolean value that indicates whether the EXIF data was successfully removed.</returns>
    public static Task<bool> RemoveImageMetaData(FileInfo fileInfo) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            var profile = magickImage.GetExifProfile();
            if (profile is null)
            {
                return false;
            }

            magickImage.Strip();
            return true;
        }, nameof(ExifWriter), nameof(RemoveImageMetaData));

    /// <summary>
    /// Adds authors metadata to the EXIF profile of the specified image file.
    /// </summary>
    /// <param name="fileInfo">The file information of the image to update.</param>
    /// <param name="value">The author(s) to add to the EXIF profile.</param>
    /// <returns>A task representing the asynchronous operation, containing a boolean indicator of success or failure.</returns>
    public static Task<bool> AddAuthors(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.Artist, value);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddAuthors));

    /// <summary>
    /// Adds or updates the copyright information in the EXIF metadata of the specified image file.
    /// </summary>
    /// <param name="fileInfo">The file information of the image to update.</param>
    /// <param name="value">The copyright text to be added or updated in the EXIF metadata.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddCopyright(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.Copyright, value);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddCopyright));

    /// <summary>
    /// Adds the software name to the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The software name to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddSoftware(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.Software, value);
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddSoftware));
}