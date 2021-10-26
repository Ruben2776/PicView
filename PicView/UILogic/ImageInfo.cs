using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.Views.UserControls;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PicView.UILogic
{
    internal static class ImageInfo
    {
        static object? rating;

        internal static async Task RenameTask(KeyEventArgs e, TextBox textBox, string file)
        {
            if (e.Key != System.Windows.Input.Key.Enter) { return; }

            e.Handled = true;
            var rename = await FileFunctions.RenameFileWithErrorChecking(file).ConfigureAwait(false);
            if (rename.HasValue == false)
            {
                Tooltip.ShowTooltipMessage(Application.Current.Resources["AnErrorOccuredMovingFile"]);
                return;
            }
            if (rename.Value)
            {
                await ConfigureWindows.GetImageInfoWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                {
                    textBox.CaretIndex = textBox.Text.Length;
                });
            }
        }

        internal static async Task UpdateValuesAsync(FileInfo? fileInfo)
        {
            if (ConfigureWindows.GetImageInfoWindow == null || ConfigureWindows.GetImageInfoWindow != null && ConfigureWindows.GetImageInfoWindow.IsVisible == false)
            {
                return;
            }

            var data = await GetImageData.RetrieveData(fileInfo).ConfigureAwait(false);

            await ConfigureWindows.GetImageInfoWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                if (data != null)
                {
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

                    rating = data[14];

                    if (data.Length > 15)
                    {
                        if (ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Count > 0)
                        {
                            var latitudeBox = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[0];
                            latitudeBox.SetValues(data[15], data[16], true);
                            var longitudeBox = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[1];
                            longitudeBox.SetValues(data[17], data[18], true);

                            var linkX = (LinkTextBox)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[2];
                            linkX.SetURL(data[19], "Bing");
                            var linkY = (LinkTextBox)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[3];
                            linkY.SetURL(data[20], "Google");

                            var authorBox = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[4];
                            authorBox.SetValues(data[21], data[22], true);
                            var dateTakenBox = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[5];
                            dateTakenBox.SetValues(data[23], data[24], true);

                            var program = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[6];
                            program.SetValues(data[25], data[26], true);
                            var copyright = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[7];
                            copyright.SetValues(data[27], data[28], true);

                            var resolutionUnit = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[8];
                            resolutionUnit.SetValues(data[29], data[30], true);
                            var colorRepresentation = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[9];
                            colorRepresentation.SetValues(data[31], data[32], true);

                            var cameraMaker = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[10];
                            cameraMaker.SetValues(data[33], data[34], true);
                            var cameraModel = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[11];
                            cameraModel.SetValues(data[35], data[36], true);

                            var fstop = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[12];
                            fstop.SetValues(data[37], data[38], true);
                            var exposure = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[13];
                            exposure.SetValues(data[39], data[40], true);
                        }
                        else
                        {
                            var latitudeBox = new TextboxInfo(data[15], data[16], true);
                            var longitudeBox = new TextboxInfo(data[17], data[18], true);

                            var linkX = new LinkTextBox(data[19], "Bing");
                            var linkY = new LinkTextBox(data[20], "Google");

                            var authorBox = new TextboxInfo(data[21], data[22], true);
                            var dateTakenBox = new TextboxInfo(data[23], data[24], true);

                            var program = new TextboxInfo(data[25], data[26], true);
                            var copyright = new TextboxInfo(data[27], data[28], true);

                            var resolutionUnit = new TextboxInfo(data[29], data[30], true);
                            var colorRepresentation = new TextboxInfo(data[31], data[32], true);

                            var cameraMaker = new TextboxInfo(data[33], data[34], true);
                            var cameraModel = new TextboxInfo(data[35], data[36], true);

                            var fstop = new TextboxInfo(data[37], data[38], true);
                            var exposure = new TextboxInfo(data[39], data[40], true);

                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(latitudeBox);
                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(longitudeBox);

                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(linkX);
                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(linkY);

                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(authorBox);
                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(dateTakenBox);

                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(program);
                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(copyright);

                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(resolutionUnit);
                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(colorRepresentation);

                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(cameraMaker);
                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(cameraModel);

                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(fstop);
                            ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(exposure);
                        }
                    }
                }
                else
                {
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

                    ConfigureWindows.GetImageInfoWindow.AspectRatioBox.Text = string.Empty;

                    rating = 0;

                    ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Clear();
                }

                UpdateStars();
            });
        }

        internal static void UpdateStars()
        {
            if (rating is null)
            {
                UpdateStars(0);
                return;
            }

            var castRating = rating.GetType();
            if (castRating.Equals(typeof(int))) // Try and convert to int to avoid exception
            {
                int intRating = (int)rating;
                intRating = intRating >= 0 && intRating <= 5 ? intRating : 0;
                UpdateStars((intRating));
                return;
            }

            if ((string)rating == string.Empty || (string)rating == "0")
            {
                UpdateStars(0);
                return;
            }

            int percent = Convert.ToInt32(rating.ToString());
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
}
