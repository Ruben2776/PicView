using ImageMagick;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static PicView.Fields;
using static PicView.UC;

namespace PicView
{
    internal static class ImageCropping
    {
        internal static void StartCrop()
        {
            if (cropppingTool == null)
            {
                LoadControls.LoadCroppingTool();
                //cropppingTool.CropTool.SetStretch(Stretch.None);
                var chosenColorBrush = Application.Current.Resources["ChosenColorBrush"] as SolidColorBrush;
                cropppingTool.CropTool.SetBackground(
                    new SolidColorBrush(Color.FromArgb(
                        30,
                        chosenColorBrush.Color.R,
                        chosenColorBrush.Color.G,
                        chosenColorBrush.Color.B
                    )));
            }

            if (mainWindow.img.Source == null) { return; }

            if (!mainWindow.bg.Children.Contains(cropppingTool))
            {
                mainWindow.bg.Children.Add(cropppingTool);
            }

            var i = BitmapSource.Create( // Create dummy image
                2,
                2,
                96,
                96,
                PixelFormats.Indexed1,
                new BitmapPalette(new List<Color> { Colors.Transparent }),
                new byte[] { 0, 0, 0, 0 },
                1);

            cropppingTool.CropTool.SetImage(i);
            cropppingTool.CropTool.SetSize(mainWindow.img.Source.Width, mainWindow.img.Source.Height);
            cropppingTool.CropTool.Width = xWidth;
            cropppingTool.CropTool.Height = xHeight;
        }

        internal static async void SaveCrop()
        {
            if (string.IsNullOrEmpty(Pics[FolderIndex])) return;

            var Savedlg = new SaveFileDialog()
            {
                Filter = FilterFiles,
                Title = "Save image - PicView",
                FileName = Path.GetFileName(Pics[FolderIndex])
            };

            if (!Savedlg.ShowDialog().Value) return;

            IsDialogOpen = true;

            var crop = GetCrop();
            var success = false;
            await Task.Run(() =>
                success = SaveImages.TrySaveImage(crop, Pics[FolderIndex], Savedlg.FileName)).ConfigureAwait(true);

            if (!success)
            {
                Tooltip.ShowTooltipMessage($"An error occured while saving {Pics[FolderIndex]} to {Savedlg.FileName}");
            }
        }

        internal static Int32Rect GetCrop()
        {
            var cropArea = cropppingTool.CropTool.CropService.GetCroppedArea();

            // Incorrect coordinates calculated if image resized to fit app, help!
            var x = Convert.ToInt32(cropArea.CroppedRectAbsolute.X);
            var y = Convert.ToInt32(cropArea.CroppedRectAbsolute.Y);
            var width = Convert.ToInt32(cropArea.CroppedRectAbsolute.Width);
            var height = Convert.ToInt32(cropArea.CroppedRectAbsolute.Height);

            return new Int32Rect(x, y, width, height);
        }
    }
}
