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

                    ConfigureWindows.GetImageInfoWindow.SizePxBox.Text = data[5];

                    ConfigureWindows.GetImageInfoWindow.ResolutionBox.Text = data[6];

                    ConfigureWindows.GetImageInfoWindow.BitDepthBox.Text = data[7];

                    ConfigureWindows.GetImageInfoWindow.SizeMpBox.Text = data[8];

                    ConfigureWindows.GetImageInfoWindow.PrintSizeCmBox.Text = data[9];

                    ConfigureWindows.GetImageInfoWindow.PrintSizeInBox.Text = data[10];

                    ConfigureWindows.GetImageInfoWindow.AspectRatioBox.Text = data[11];

                    rating = data[12];

                    if (ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Count > 0)
                    {
                        var latitudeBox = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[0];
                        latitudeBox.SetValues(data[13], data[14], false);
                        var longitudeBox = (TextboxInfo)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[1];
                        longitudeBox.SetValues(data[15], data[16], false);
                        var linkX = (LinkTextBox)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[2];
                        linkX.SetURL(data[17], "Bing");
                        var linkY = (LinkTextBox)ConfigureWindows.GetImageInfoWindow.ExifParent.Children[3];
                        linkY.SetURL(data[18], "Google");
                    }
                    else
                    {
                        var latitudeBox = new TextboxInfo(data[13], data[14], false);
                        var longitudeBox = new TextboxInfo(data[15], data[16], false);
                        var linkX = new LinkTextBox(data[17], "Bing");
                        var linkY = new LinkTextBox(data[18], "Google");
                        ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(latitudeBox);
                        ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(longitudeBox);
                        ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(linkX);
                        ConfigureWindows.GetImageInfoWindow.ExifParent.Children.Add(linkY);
                    }
                }
                else
                {
                    ConfigureWindows.GetImageInfoWindow.FilenameBox.Text =

                    ConfigureWindows.GetImageInfoWindow.FolderBox.Text =

                    ConfigureWindows.GetImageInfoWindow.FullPathBox.Text =

                    ConfigureWindows.GetImageInfoWindow.CreatedBox.Text =

                    ConfigureWindows.GetImageInfoWindow.ModifiedBox.Text =

                    ConfigureWindows.GetImageInfoWindow.SizePxBox.Text =

                    ConfigureWindows.GetImageInfoWindow.ResolutionBox.Text =

                    ConfigureWindows.GetImageInfoWindow.BitDepthBox.Text =

                    ConfigureWindows.GetImageInfoWindow.SizeMpBox.Text =

                    ConfigureWindows.GetImageInfoWindow.PrintSizeCmBox.Text =

                    ConfigureWindows.GetImageInfoWindow.PrintSizeInBox.Text =

                    ConfigureWindows.GetImageInfoWindow.AspectRatioBox.Text = string.Empty;

                    rating = 0;
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
