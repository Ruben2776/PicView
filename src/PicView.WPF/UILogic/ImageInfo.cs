using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PicView.Core.Localization;
using PicView.WPF.ChangeImage;
using PicView.WPF.FileHandling;
using PicView.WPF.ImageHandling;
using PicView.WPF.Views.UserControls.Misc;

namespace PicView.WPF.UILogic;

internal static class ImageInfo
{
    private static object? _rating;

    internal static async Task RenameTask(KeyEventArgs e, TextBox textBox, string newFileName)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        var rename = await FileFunctions.RenameCurrentFileWithErrorChecking(newFileName, Navigation.Pics[Navigation.FolderIndex]).ConfigureAwait(false);
        if (rename.HasValue == false)
        {
            Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("AnErrorOccuredMovingFile"));
        }

        await ConfigureWindows.GetImageInfoWindow?.Dispatcher?.InvokeAsync(Keyboard.ClearFocus);
    }

    internal static async Task UpdateValuesAsync(FileInfo? fileInfo)
    {
        if (fileInfo is null)
        {
            if (Navigation.Pics.Count > 0 && Navigation.Pics.Count > Navigation.FolderIndex)
            {
                fileInfo = new FileInfo(Navigation.Pics[Navigation.FolderIndex]);
            }
        }

        var toReturn = false;

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (ConfigureWindows.GetImageInfoWindow is null or { IsVisible: false })
            {
                toReturn = true;
            }

            if (ConfigureWindows.GetMainWindow.MainImage.Source != null) return;
            Clear();
            toReturn = true;
        }, DispatcherPriority.DataBind);

        if (toReturn)
        {
            return;
        }

        var data = GetImageData.RetrieveData(fileInfo);

        await ConfigureWindows.GetImageInfoWindow.Dispatcher.InvokeAsync(() =>
        {
            if (fileInfo is not null && Navigation.Pics[Navigation.FolderIndex] != fileInfo.FullName)
            {
                return;
            }

            if (data == null)
            {
                Clear();
                return;
            }

            ConfigureWindows.GetImageInfoWindow.FilenameBox.Text = data.Value.FileName;

            ConfigureWindows.GetImageInfoWindow.FolderBox.Text = data.Value.DirectoryName;

            ConfigureWindows.GetImageInfoWindow.FullPathBox.Text = data.Value.Path;

            ConfigureWindows.GetImageInfoWindow.CreatedBox.Text = data.Value.CreationTime;

            ConfigureWindows.GetImageInfoWindow.ModifiedBox.Text = data.Value.LastWriteTime;

            ConfigureWindows.GetImageInfoWindow.AccessedBox.Text = data.Value.LastAccessTime;

            ConfigureWindows.GetImageInfoWindow.BitDepthBox.Text = data.Value.BitDepth;

            ConfigureWindows.GetImageInfoWindow.WidthBox.Text = data.Value.PixelWidth;

            ConfigureWindows.GetImageInfoWindow.HeightBox.Text = data.Value.PixelHeight;

            ConfigureWindows.GetImageInfoWindow.ResolutionBox.Text = data.Value.ResolutionUnit;

            ConfigureWindows.GetImageInfoWindow.SizeMpBox.Text = data.Value.MegaPixels;

            ConfigureWindows.GetImageInfoWindow.PrintSizeCmBox.Text = data.Value.PrintSizeCm;

            ConfigureWindows.GetImageInfoWindow.PrintSizeInBox.Text = data.Value.PrintSizeInch;

            ConfigureWindows.GetImageInfoWindow.AspectRatioBox.Text = data.Value.AspectRatio;

            _rating = data.Value.ExifRating;

            if (ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Count > 0)
            {
                // 0 == GPS
                var latitudeBox = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[1];
                latitudeBox.SetValues(TranslationHelper.GetTranslation("Latitude"), data.Value.Latitude, true);

                var longitudeBox = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[2];
                longitudeBox.SetValues(TranslationHelper.GetTranslation("Longitude"), data.Value.Longitude, true);

                var linkX = (LinkTextBox)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[3];
                linkX.SetURL(data.Value.BingLink, "Bing");

                var linkY = (LinkTextBox)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[4];
                linkY.SetURL(data.Value.GoogleLink, "Google");

                var altitudeBox = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[5];
                altitudeBox.SetValues(TranslationHelper.GetTranslation("Altitude"), data.Value.Altitude, true);

                var title = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[7];
                title.SetValues(TranslationHelper.GetTranslation("Title"), data.Value.Title, true);

                var dateTakenBox = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[8];
                dateTakenBox.SetValues(TranslationHelper.GetTranslation("DateTaken"), data.Value.DateTaken, true);

                var authorBox = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[9];
                authorBox.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("Authors")), data.Value.Authors, true);

                var subject = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[10];
                subject.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("Subject")), data.Value.Subject, true);

                var program = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[11];
                program.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("Software")), data.Value.Software, true);

                var copyright = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[12];
                copyright.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("Copyright")), data.Value.Copyright, true);

                var resolutionUnit = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[14];
                resolutionUnit.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("ResolutionUnit")), data.Value.ResolutionUnit, true);

                var colorRepresentation = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[15];
                colorRepresentation.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("ColorRepresentation")), data.Value.ColorRepresentation, true);

                var compression = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[16];
                compression.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("Compression")), data.Value.Compression, true);

                var compressionBits = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[17];
                compressionBits.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("CompressedBitsPixel")), data.Value.CompressedBitsPixel, true);

                var cameraMaker = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[19];
                cameraMaker.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("CameraMaker")), data.Value.CameraMaker, true);

                var cameraModel = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[20];
                cameraModel.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("CameraModel")), data.Value.CameraModel, true);

                var fstop = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[21];
                fstop.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("Fstop")), data.Value.Fstop, true);

                var exposure = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[22];
                exposure.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("ExposureTime")), data.Value.ExposureTime, true);

                var isoSpeed = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[23];
                isoSpeed.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("ISOSpeed")), data.Value.ISOSpeed, true);

                var exposureBias = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[24];
                exposureBias.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("ExposureBias")), data.Value.ExposureBias, true);

                var maxAperture = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[25];
                maxAperture.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("MaxAperture")), data.Value.MaxAperture, true);

                var focal = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[26];
                focal.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("FocalLength")), data.Value.FocalLength, true);

                var flength35 = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[27];
                flength35.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("FocalLength35mm")), data.Value.FocalLength35mm, true);

                var flashMode = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[28];
                flashMode.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("FlashMode")), data.Value.FlashMode, true);

                var flashEnergy = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[29];
                flashEnergy.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("FlashEnergy")), data.Value.FlashEnergy, true);

                var meteringMode = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[30];
                meteringMode.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("MeteringMode")), data.Value.MeteringMode, true);

                var lensmaker = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[32];
                lensmaker.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("LensMaker")), data.Value.LensMaker, true);

                var lensmodel = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[33];
                lensmodel.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("LensModel")), data.Value.LensModel, true);

                var contrast = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[34];
                contrast.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("Contrast")), data.Value.Contrast, true);

                var brightness = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[35];
                brightness.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("Brightness")), data.Value.Brightness, true);

                var lightSource = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[36];
                lightSource.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("LightSource")), data.Value.LightSource, true);

                var exposureProgram = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[37];
                exposureProgram.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("ExposureProgram")), data.Value.ExposureProgram, true);

                var saturation = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[38];
                saturation.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("Saturation")), data.Value.Saturation, true);

                var sharpness = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[39];
                sharpness.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("Sharpness")), data.Value.Sharpness, true);

                var whiteBalance = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[40];
                whiteBalance.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("WhiteBalance")), data.Value.WhiteBalance, true);

                var photometricInterpolation =
                    (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[41];
                photometricInterpolation.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("PhotometricInterpretation")), data.Value.PhotometricInterpretation, true);

                var digitalZoom = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[42];
                digitalZoom.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("DigitalZoom")), data.Value.DigitalZoom, true);

                var exifversion = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[43];
                exifversion.SetValues(TranslationHelper.GetTranslation(TranslationHelper.GetTranslation("ExifVersion")), data.Value.ExifVersion, true);
            }
            else
            {
                var latitudeBox = new TextBoxInfo(TranslationHelper.GetTranslation("Latitude"), data.Value.Latitude, true);
                var longitudeBox = new TextBoxInfo(TranslationHelper.GetTranslation("Longitude"), data.Value.Longitude, true);

                var altitude = new TextBoxInfo(TranslationHelper.GetTranslation("Altitude"), data.Value.Altitude, true);

                var linkX = new LinkTextBox(data.Value.BingLink, "Bing");
                var linkY = new LinkTextBox(data.Value.GoogleLink, "Google");

                var title = new TextBoxInfo(TranslationHelper.GetTranslation("Title"), data.Value.Title, true);
                var subject = new TextBoxInfo(TranslationHelper.GetTranslation("Subject"), data.Value.Subject, true);

                var authorBox = new TextBoxInfo(TranslationHelper.GetTranslation("Authors"), data.Value.Authors, true);
                var dateTakenBox = new TextBoxInfo(TranslationHelper.GetTranslation("DateTaken"), data.Value.DateTaken, true);

                var program = new TextBoxInfo(TranslationHelper.GetTranslation("Software"), data.Value.Software, true);
                var copyright = new TextBoxInfo(TranslationHelper.GetTranslation("Copyright"), data.Value.Copyright, true);

                var resolutionUnit = new TextBoxInfo(TranslationHelper.GetTranslation("ResolutionUnit"), data.Value.ResolutionUnit, true);
                var colorRepresentation = new TextBoxInfo(TranslationHelper.GetTranslation("ColorRepresentation"), data.Value.ColorRepresentation, true);

                var compression = new TextBoxInfo(TranslationHelper.GetTranslation("Compression"), data.Value.Compression, true);
                var compressionBits = new TextBoxInfo(TranslationHelper.GetTranslation("CompressedBitsPixel"), data.Value.CompressedBitsPixel, true);

                var cameraMaker = new TextBoxInfo(TranslationHelper.GetTranslation("CameraMaker"), data.Value.CameraMaker, true);
                var cameraModel = new TextBoxInfo(TranslationHelper.GetTranslation("CameraModel"), data.Value.CameraModel, true);

                var fstop = new TextBoxInfo(TranslationHelper.GetTranslation("Fstop"), data.Value.Fstop, true);
                var exposure = new TextBoxInfo(TranslationHelper.GetTranslation("ExposureProgram"), data.Value.ExposureProgram, true);

                var isoSpeed = new TextBoxInfo(TranslationHelper.GetTranslation("ISOSpeed"), data.Value.ISOSpeed, true);
                var exposureBias = new TextBoxInfo(TranslationHelper.GetTranslation("ExposureBias"), data.Value.ExposureBias, true);

                var maxAperture = new TextBoxInfo(TranslationHelper.GetTranslation("MaxAperture"), data.Value.MaxAperture, true);

                var focal = new TextBoxInfo(TranslationHelper.GetTranslation("FocalLength"), data.Value.FocalLength, true);
                var flength35 = new TextBoxInfo(TranslationHelper.GetTranslation("FocalLength35mm"), data.Value.FocalLength35mm, true);

                var flashMode = new TextBoxInfo(TranslationHelper.GetTranslation("FlashMode"), data.Value.FlashMode, true);
                var flashEnergy = new TextBoxInfo(TranslationHelper.GetTranslation("FlashEnergy"), data.Value.FlashEnergy, true);

                var metering = new TextBoxInfo(TranslationHelper.GetTranslation("MeteringMode"), data.Value.MeteringMode, true);

                var lensmaker = new TextBoxInfo(TranslationHelper.GetTranslation("LensMaker"), data.Value.LensMaker, true);
                var lensmodel = new TextBoxInfo(TranslationHelper.GetTranslation("LensModel"), data.Value.LensModel, true);

                var contrast = new TextBoxInfo(TranslationHelper.GetTranslation("Contrast"), data.Value.Contrast, true);
                var brightness = new TextBoxInfo(TranslationHelper.GetTranslation("Brightness"), data.Value.Brightness, true);

                var lightSource = new TextBoxInfo(TranslationHelper.GetTranslation("LightSource"), data.Value.LightSource, true);

                var exposureProgram = new TextBoxInfo(TranslationHelper.GetTranslation("ExposureProgram"), data.Value.ExposureProgram, true);

                var saturation = new TextBoxInfo(TranslationHelper.GetTranslation("Saturation"), data.Value.Saturation, true);
                var sharpness = new TextBoxInfo(TranslationHelper.GetTranslation("Sharpness"), data.Value.Sharpness, true);

                var whiteBalance = new TextBoxInfo(TranslationHelper.GetTranslation("WhiteBalance"), data.Value.WhiteBalance, true);

                var photometricInterpolation = new TextBoxInfo(TranslationHelper.GetTranslation("PhotometricInterpretation"), data.Value.PhotometricInterpretation, true);

                var digitalZoom = new TextBoxInfo(TranslationHelper.GetTranslation("DigitalZoom"), data.Value.DigitalZoom, true);

                var exifversion = new TextBoxInfo(TranslationHelper.GetTranslation("ExifVersion"), data.Value.ExifVersion, true);

                var gps = (StackPanel)ConfigureWindows.GetImageInfoWindow.Resources["GPS"];
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(gps);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(latitudeBox);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(longitudeBox);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(linkX);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(linkY);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(altitude);

                var origin = (StackPanel)ConfigureWindows.GetImageInfoWindow.Resources["Origin"];
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(origin);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(title);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(subject);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(authorBox);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(dateTakenBox);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(program);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(copyright);

                var image = (StackPanel)ConfigureWindows.GetImageInfoWindow.Resources["Image"];
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(image);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(resolutionUnit);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(colorRepresentation);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(compression);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(compressionBits);

                var camera = (StackPanel)ConfigureWindows.GetImageInfoWindow.Resources["Camera"];
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(camera);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(cameraMaker);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(cameraModel);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(fstop);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(exposure);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(isoSpeed);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(exposureBias);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(maxAperture);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(focal);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(flength35);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(flashMode);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(flashEnergy);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(metering);

                var film = (StackPanel)ConfigureWindows.GetImageInfoWindow.Resources["Film"];
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(film);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(lensmaker);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(lensmodel);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(contrast);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(brightness);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(lightSource);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(exposureProgram);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(saturation);
                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(sharpness);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(whiteBalance);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(photometricInterpolation);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(digitalZoom);

                ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(exifversion);
            }

            UpdateStars();
        });
    }

    private static void Clear()
    {
        if (ConfigureWindows.GetImageInfoWindow is null)
        {
            return;
        }

        ConfigureWindows.GetImageInfoWindow.FilenameBox.Text =
            ConfigureWindows.GetImageInfoWindow.FolderBox.Text =
                ConfigureWindows.GetImageInfoWindow.FullPathBox.Text =
                    ConfigureWindows.GetImageInfoWindow.CreatedBox.Text =
                        ConfigureWindows.GetImageInfoWindow.ModifiedBox.Text =
                            ConfigureWindows.GetImageInfoWindow.WidthBox.Text =
                                ConfigureWindows.GetImageInfoWindow.HeightBox.Text =
                                    ConfigureWindows.GetImageInfoWindow.ResolutionBox.Text =
                                        ConfigureWindows.GetImageInfoWindow.BitDepthBox.Text =
                                            ConfigureWindows.GetImageInfoWindow.SizeMpBox.Text =
                                                ConfigureWindows.GetImageInfoWindow.PrintSizeCmBox.Text =
                                                    ConfigureWindows.GetImageInfoWindow.PrintSizeInBox.Text =
                                                        ConfigureWindows.GetImageInfoWindow.AspectRatioBox.Text =
                                                            string.Empty;

        _rating = 0;

        UpdateStars(0);

        ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Clear();
    }

    internal static void UpdateStars()
    {
        if (_rating is null)
        {
            UpdateStars(0);
            return;
        }

        var castRating = _rating.GetType();
        if (castRating == typeof(uint))
        {
            var uintRating = (uint)_rating;
            if (uintRating <= int.MaxValue)
            {
                var intRating = (int)uintRating;
                intRating = intRating is >= 0 and <= 5 ? intRating : 0;
                UpdateStars(intRating);
                return;
            }

            UpdateStars(0);
            return;
        }

        if (castRating == typeof(int))
        {
            var intRating = (int)_rating;
            intRating = intRating is >= 0 and <= 5 ? intRating : 0;
            UpdateStars(intRating);
            return;
        }

        try
        {
            if ((string)_rating == string.Empty || (string)_rating == "0")
            {
                UpdateStars(0);
                return;
            }
        }
        catch (Exception)
        {
            UpdateStars(0);
            return;
        }

        var percent = Convert.ToInt32(_rating.ToString());
        var stars = Math.Ceiling(percent / 20d);

        UpdateStars((int)stars);
    }

    internal static void UpdateStars(int stars)
    {
        switch (stars)
        {
            default:
            case 0:
                ConfigureWindows.GetImageInfoWindow.Star1.OutlineStar();
                ConfigureWindows.GetImageInfoWindow.Star2.OutlineStar();
                ConfigureWindows.GetImageInfoWindow.Star3.OutlineStar();
                ConfigureWindows.GetImageInfoWindow.Star4.OutlineStar();
                ConfigureWindows.GetImageInfoWindow.Star5.OutlineStar();
                return;

            case 1:
                ConfigureWindows.GetImageInfoWindow.Star1.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star2.OutlineStar();
                ConfigureWindows.GetImageInfoWindow.Star3.OutlineStar();
                ConfigureWindows.GetImageInfoWindow.Star4.OutlineStar();
                ConfigureWindows.GetImageInfoWindow.Star5.OutlineStar();
                return;

            case 2:
                ConfigureWindows.GetImageInfoWindow.Star1.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star2.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star3.OutlineStar();
                ConfigureWindows.GetImageInfoWindow.Star4.OutlineStar();
                ConfigureWindows.GetImageInfoWindow.Star5.OutlineStar();
                return;

            case 3:
                ConfigureWindows.GetImageInfoWindow.Star1.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star2.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star3.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star4.OutlineStar();
                ConfigureWindows.GetImageInfoWindow.Star5.OutlineStar();
                return;

            case 4:
                ConfigureWindows.GetImageInfoWindow.Star1.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star2.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star3.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star4.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star5.OutlineStar();
                return;

            case 5:
                ConfigureWindows.GetImageInfoWindow.Star1.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star2.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star3.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star4.FillStar();
                ConfigureWindows.GetImageInfoWindow.Star5.FillStar();
                return;
        }
    }
}