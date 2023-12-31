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

        var data = await GetImageData.RetrieveData(fileInfo).ConfigureAwait(false);

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

            ConfigureWindows.GetImageInfoWindow.FilenameBox.Text = data[0];

            ConfigureWindows.GetImageInfoWindow.FolderBox.Text = data[1];

            ConfigureWindows.GetImageInfoWindow.FullPathBox.Text = data[2];

            ConfigureWindows.GetImageInfoWindow.CreatedBox.Text = data[3];

            ConfigureWindows.GetImageInfoWindow.ModifiedBox.Text = data[4];

            ConfigureWindows.GetImageInfoWindow.AccessedBox.Text = data[5];

            ConfigureWindows.GetImageInfoWindow.BitDepthBox.Text = data[6];

            ConfigureWindows.GetImageInfoWindow.WidthBox.Text = data[7];

            ConfigureWindows.GetImageInfoWindow.HeightBox.Text = data[8];

            ConfigureWindows.GetImageInfoWindow.ResolutionBox.Text = data[9];

            ConfigureWindows.GetImageInfoWindow.SizeMpBox.Text = data[10];

            ConfigureWindows.GetImageInfoWindow.PrintSizeCmBox.Text = data[11];

            ConfigureWindows.GetImageInfoWindow.PrintSizeInBox.Text = data[12];

            ConfigureWindows.GetImageInfoWindow.AspectRatioBox.Text = data[13];

            _rating = data[14];

            if (data.Length > 15)
            {
                if (ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Count > 0)
                {
                    // 0 == GPS
                    var latitudeBox = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[1];
                    latitudeBox.SetValues(data[15], data[16], true);
                    var longitudeBox = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[2];
                    longitudeBox.SetValues(data[17], data[18], true);

                    var linkX = (LinkTextBox)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[3];
                    linkX.SetURL(data[19], "Bing");
                    var linkY = (LinkTextBox)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[4];
                    linkY.SetURL(data[20], "Google");

                    var latitude = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[5];
                    latitude.SetValues(data[21], data[22], true);

                    // 6 == Origin
                    var title = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[7];
                    title.SetValues(data[23], data[24], true);
                    var dateTakenBox = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[8];
                    dateTakenBox.SetValues(data[25], data[26], true);

                    var authorBox = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[9];
                    authorBox.SetValues(data[27], data[28], true);
                    var subject = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[10];
                    subject.SetValues(data[29], data[30], true);

                    var program = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[11];
                    program.SetValues(data[31], data[32], true);
                    var copyright = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[12];
                    copyright.SetValues(data[33], data[34], true);

                    // 13 == Image
                    var resolutionUnit = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[14];
                    resolutionUnit.SetValues(data[35], data[36], true);
                    var colorRepresentation =
                        (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[15];
                    colorRepresentation.SetValues(data[37], data[38], true);

                    var compression = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[16];
                    compression.SetValues(data[39], data[40], true);
                    var compressionBits = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[17];
                    compressionBits.SetValues(data[41], data[42], true);

                    // 18 == Camera
                    var cameraMaker = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[19];
                    cameraMaker.SetValues(data[43], data[44], true);
                    var cameraModel = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[20];
                    cameraModel.SetValues(data[45], data[46], true);

                    var fstop = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[21];
                    fstop.SetValues(data[47], data[48], true);
                    var exposure = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[22];
                    exposure.SetValues(data[49], data[50], true);

                    var isoSpeed = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[23];
                    isoSpeed.SetValues(data[51], data[52], true);
                    var exposureBias = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[24];
                    exposureBias.SetValues(data[53], data[54], true);

                    var maxAperture = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[25];
                    maxAperture.SetValues(data[55], data[56], true);

                    var focal = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[26];
                    focal.SetValues(data[57], data[58], true);
                    var flength35 = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[27];
                    flength35.SetValues(data[59], data[60], true);

                    var flashMode = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[28];
                    flashMode.SetValues(data[61], data[62], true);
                    var flashEnergy = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[29];
                    flashEnergy.SetValues(data[63], data[64], true);

                    var meteringMode = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[30];
                    meteringMode.SetValues(data[65], data[66], true);

                    // 31 == Film
                    var lensmaker = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[32];
                    lensmaker.SetValues(data[67], data[68], true);
                    var lensmodel = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[33];
                    lensmodel.SetValues(data[69], data[70], true);

                    var flashManufacturer =
                        (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[34];
                    flashManufacturer.SetValues(data[71], data[72], true);
                    var flashModel = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[35];
                    flashModel.SetValues(data[73], data[74], true);

                    var camSerialNumber = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[36];
                    camSerialNumber.SetValues(data[75], data[76], true);

                    var contrast = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[37];
                    contrast.SetValues(data[77], data[78], true);
                    var brightness = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[38];
                    brightness.SetValues(data[79], data[80], true);

                    var lightSource = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[39];
                    lightSource.SetValues(data[81], data[82], true);

                    var exposureProgram = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[40];
                    exposureProgram.SetValues(data[83], data[84], true);

                    var saturation = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[41];
                    saturation.SetValues(data[85], data[86], true);
                    var sharpness = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[42];
                    sharpness.SetValues(data[87], data[88], true);

                    var whiteBalance = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[43];
                    whiteBalance.SetValues(data[89], data[90], true);
                    var photometricInterpolation =
                        (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[44];
                    photometricInterpolation.SetValues(data[91], data[92], true);

                    var digitalZoom = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[45];
                    digitalZoom.SetValues(data[93], data[94], true);

                    var exifversion = (TextBoxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[46];
                    exifversion.SetValues(data[95], data[96], true);
                }
                else
                {
                    var latitudeBox = new TextBoxInfo(data[15], data[16], true);
                    var longitudeBox = new TextBoxInfo(data[17], data[18], true);

                    var linkX = new LinkTextBox(data[19], "Bing");
                    var linkY = new LinkTextBox(data[20], "Google");

                    var altitude = new TextBoxInfo(data[21], data[22], true);

                    var title = new TextBoxInfo(data[23], data[24], true);
                    var subject = new TextBoxInfo(data[25], data[26], true);

                    var authorBox = new TextBoxInfo(data[27], data[28], true);
                    var dateTakenBox = new TextBoxInfo(data[29], data[30], true);

                    var program = new TextBoxInfo(data[31], data[32], true);
                    var copyright = new TextBoxInfo(data[33], data[34], true);

                    var resolutionUnit = new TextBoxInfo(data[35], data[36], true);
                    var colorRepresentation = new TextBoxInfo(data[37], data[38], true);

                    var compression = new TextBoxInfo(data[39], data[40], true);
                    var compressionBits = new TextBoxInfo(data[41], data[42], true);

                    var cameraMaker = new TextBoxInfo(data[43], data[44], true);
                    var cameraModel = new TextBoxInfo(data[45], data[46], true);

                    var fstop = new TextBoxInfo(data[47], data[48], true);
                    var exposure = new TextBoxInfo(data[49], data[50], true);

                    var isoSpeed = new TextBoxInfo(data[51], data[52], true);
                    var exposureBias = new TextBoxInfo(data[53], data[54], true);

                    var maxAperture = new TextBoxInfo(data[55], data[56], true);

                    var focal = new TextBoxInfo(data[57], data[58], true);
                    var flength35 = new TextBoxInfo(data[59], data[60], true);

                    var flashMode = new TextBoxInfo(data[61], data[62], true);
                    var flashEnergy = new TextBoxInfo(data[63], data[64], true);

                    var metering = new TextBoxInfo(data[65], data[66], true);

                    var lensmaker = new TextBoxInfo(data[67], data[68], true);
                    var lensmodel = new TextBoxInfo(data[69], data[70], true);

                    var flashManufacturer = new TextBoxInfo(data[71], data[72], true);
                    var flashModel = new TextBoxInfo(data[73], data[74], true);

                    var camSerialNumber = new TextBoxInfo(data[75], data[76], true);

                    var contrast = new TextBoxInfo(data[77], data[78], true);
                    var brightness = new TextBoxInfo(data[79], data[80], true);

                    var lightSource = new TextBoxInfo(data[81], data[82], true);

                    var exposureProgram = new TextBoxInfo(data[83], data[84], true);

                    var saturation = new TextBoxInfo(data[85], data[86], true);
                    var sharpness = new TextBoxInfo(data[87], data[88], true);

                    var whiteBalance = new TextBoxInfo(data[89], data[90], true);

                    var photometricInterpolation = new TextBoxInfo(data[91], data[92], true);

                    var digitalZoom = new TextBoxInfo(data[93], data[94], true);

                    var exifversion = new TextBoxInfo(data[95], data[96], true);

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

                    ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(flashManufacturer);
                    ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(flashModel);

                    ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(camSerialNumber);

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
        if (castRating == typeof(int)) // Try and convert to int to avoid exception
        {
            var intRating = (int)_rating;
            intRating = intRating is >= 0 and <= 5 ? intRating : 0;
            UpdateStars((intRating));
            return;
        }

        if ((string)_rating == string.Empty || (string)_rating == "0")
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