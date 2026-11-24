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
    /// Sets the Windows keywords (tags) in the EXIF metadata of an image file.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="value">The keywords, separated by semicolons as Windows expects.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> AddKeywords(FileInfo? fileInfo, string? value) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            if (value is null)
            {
                return false;
            }

            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.XPKeywords, Encoding.Unicode.GetBytes(value));
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(AddKeywords));

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
        value is null ? Task.FromResult(false) : SetUserCommentAsync(fileInfo, value, allowEncode: true);

    /// <summary>
    /// Sets the user comment of an image file, with the Exif character code that fits the text.
    /// </summary>
    /// <param name="fileInfo">The image file to update.</param>
    /// <param name="text">The comment text.</param>
    /// <param name="allowEncode">
    /// Whether a file whose metadata cannot be rewritten in place may be encoded again. A JPEG that does not fit
    /// its segments is then left untouched.
    /// </param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the operation succeeded; otherwise, false.</returns>
    public static Task<bool> SetUserCommentAsync(FileInfo? fileInfo, string text, bool allowEncode) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            var profile = magickImage.GetExifProfile() ?? new ExifProfile();
            var useUtf8 = string.CompareOrdinal(ExifReader.GetExifVersion(profile), "0300") >= 0;
            profile.SetValue(ExifTag.UserComment, UserCommentEncoder.Encode(text, useUtf8));
            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(SetUserCommentAsync), allowEncode);

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
    /// Writes <paramref name="code"/> to the enumerated <paramref name="tag"/> of an image file, such as Contrast, as
    /// Image Information's dropdown of that tag picks it. A <see langword="null"/> code removes the tag.
    /// </summary>
    /// <returns>True if the file was written; otherwise, false.</returns>
    public static Task<bool> SetChoiceAsync(FileInfo? fileInfo, ExifTag<ushort> tag, ushort? code) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            var profile = magickImage.GetExifProfile();
            if (code is null)
            {
                if (profile is null || !profile.RemoveValue(tag))
                {
                    return false;
                }

                // An empty profile is not written, which would leave the old one in the file
                if (profile.Values.Count is 0)
                {
                    magickImage.RemoveProfile(profile);
                    return true;
                }
            }
            else
            {
                profile ??= new ExifProfile();
                profile.SetValue(tag, code.Value);
            }

            // The encoder writes the resolution unit from the image's density, over the profile's
            if (tag == ExifTag.ResolutionUnit)
            {
                var density = magickImage.Density;
                magickImage.Density = new Density(density.X, density.Y, code switch
                {
                    2 => DensityUnit.PixelsPerInch,
                    3 => DensityUnit.PixelsPerCentimeter,
                    _ => DensityUnit.Undefined
                });
            }

            magickImage.SetProfile(profile);
            return true;
        }, nameof(ExifWriter), nameof(SetChoiceAsync));

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
    /// Removes the metadata from the specified image file: Exif, XMP, IPTC and comments. The colour profile is kept.
    /// </summary>
    /// <param name="fileInfo">The file information of the image from which the EXIF data will be removed.</param>
    /// <returns>A task representing the asynchronous operation, containing a boolean value that indicates whether the EXIF data was successfully removed.</returns>
    public static Task<bool> RemoveImageMetaData(FileInfo fileInfo) =>
        ExifFunctions.TryUpdateImageProfileAsync(fileInfo, magickImage =>
        {
            // Strip drops the colour profile too, which would change the colours of a wide-gamut image
            var colorProfile = magickImage.GetColorProfile();
            magickImage.Strip();
            if (colorProfile is not null)
            {
                magickImage.SetProfile(colorProfile);
            }

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