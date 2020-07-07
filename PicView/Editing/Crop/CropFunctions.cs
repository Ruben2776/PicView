using Microsoft.Win32;
using PicView.ChangeImage;
using PicView.ImageHandling;
using PicView.UI;
using PicView.UI.Loading;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static PicView.Library.Fields;
using static PicView.UI.UserControls.UC;

namespace PicView.Editing.Crop
{
    internal class CropFunctions
    {
        public static CropService CropService { get; private set; }

        internal static void StartCrop()
        {
            if (TheMainWindow.MainImage.Source == null) { return; }

            if (GetCropppingTool == null)
            {
                LoadControls.LoadCroppingTool();
            }

            GetCropppingTool.Width = Rotateint == 0 || Rotateint == 180 ? xWidth : xHeight;
            GetCropppingTool.Height = Rotateint == 0 || Rotateint == 180 ? xHeight : xWidth;

            TheMainWindow.TitleText.Text = "Press Esc to close, Enter to save"; // TODO add to translation

            if (!TheMainWindow.ParentContainer.Children.Contains(GetCropppingTool))
            {
                TheMainWindow.ParentContainer.Children.Add(GetCropppingTool);
            }

            CanNavigate = false;
        }

        internal static void InitilizeCrop()
        {
            GetCropppingTool.Width = Rotateint == 0 || Rotateint == 180 ? xWidth : xHeight;
            GetCropppingTool.Height = Rotateint == 0 || Rotateint == 180 ? xHeight : xWidth;

            CropService = new CropService(GetCropppingTool);

            var chosenColorBrush = Application.Current.Resources["ChosenColorBrush"] as SolidColorBrush;
            GetCropppingTool.RootGrid.Background =
                new SolidColorBrush(Color.FromArgb(
                    25,
                    chosenColorBrush.Color.R,
                    chosenColorBrush.Color.G,
                    chosenColorBrush.Color.B
                ));

            GetCropppingTool.RootGrid.PreviewMouseDown += (s, e) => CropService.Adorner.RaiseEvent(e);
            GetCropppingTool.RootGrid.PreviewMouseLeftButtonUp += (s, e) => CropService.Adorner.RaiseEvent(e);
        }

        internal static async void SaveCrop()
        {
            var fileName = Navigation.Pics.Count == 0 ? Path.GetRandomFileName()
                : Path.GetFileName(Navigation.Pics[Navigation.FolderIndex]);

            var Savedlg = new SaveFileDialog()
            {
                Filter = FilterFiles,
                Title = "Save image - PicView",
                FileName = fileName
            };

            if (!Savedlg.ShowDialog().Value)
            {
                return;
            }

            IsDialogOpen = true;

            var crop = GetCrop();
            var success = false;

            if (Navigation.Pics.Count > 0)
            {
                await Task.Run(() =>
                    success = SaveImages.TrySaveImage(
                        crop,
                        Navigation.Pics[Navigation.FolderIndex],
                        Savedlg.FileName)).ConfigureAwait(false);
            }
            else
            {
                // Fixes saving if from web
                // TODO add working method for copied images
                var source = TheMainWindow.MainImage.Source as BitmapSource;
                await Task.Run(() =>
                    success = SaveImages.TrySaveImage(
                        crop,
                        source,
                        Savedlg.FileName)).ConfigureAwait(false);
            }
            await TheMainWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (!success)
                {
                    Tooltip.ShowTooltipMessage(
                        $"An error occured while saving {fileName} to {Savedlg.FileName}");
                    // TODO add to translation
                }

                TheMainWindow.ParentContainer.Children.Remove(GetCropppingTool);
            }));
        }

        internal static Int32Rect GetCrop()
        {
            var cropArea = CropService.GetCroppedArea();

            int x, y, width, height;

            if (AspectRatio != 0)
            {
                if (Rotateint == 0 || Rotateint == 180)
                {
                    x = Convert.ToInt32(cropArea.CroppedRectAbsolute.X / AspectRatio);
                    y = Convert.ToInt32(cropArea.CroppedRectAbsolute.Y / AspectRatio);
                    width = Convert.ToInt32(cropArea.CroppedRectAbsolute.Width / AspectRatio);
                    height = Convert.ToInt32(cropArea.CroppedRectAbsolute.Height / AspectRatio);
                }
                else
                {
                    x = Convert.ToInt32(cropArea.CroppedRectAbsolute.Y / AspectRatio);
                    y = Convert.ToInt32(cropArea.CroppedRectAbsolute.X / AspectRatio);
                    width = Convert.ToInt32(cropArea.CroppedRectAbsolute.Height / AspectRatio);
                    height = Convert.ToInt32(cropArea.CroppedRectAbsolute.Width / AspectRatio);
                }
            }
            else
            {
                if (Rotateint == 0 || Rotateint == 180)
                {
                    x = Convert.ToInt32(cropArea.CroppedRectAbsolute.X);
                    y = Convert.ToInt32(cropArea.CroppedRectAbsolute.Y);
                    width = Convert.ToInt32(cropArea.CroppedRectAbsolute.Width);
                    height = Convert.ToInt32(cropArea.CroppedRectAbsolute.Height);
                }
                else
                {
                    x = Convert.ToInt32(cropArea.CroppedRectAbsolute.Y);
                    y = Convert.ToInt32(cropArea.CroppedRectAbsolute.X);
                    width = Convert.ToInt32(cropArea.CroppedRectAbsolute.Height);
                    height = Convert.ToInt32(cropArea.CroppedRectAbsolute.Width);
                }
            }

            return new Int32Rect(x, y, width, height);
        }
    }
}