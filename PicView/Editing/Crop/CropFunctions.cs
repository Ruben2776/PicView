using Microsoft.Win32;
using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.UILogic;
using PicView.UILogic.Loading;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.TransformImage.Rotation;
using static PicView.UILogic.UC;

namespace PicView.Editing.Crop
{
    internal static class CropFunctions
    {
        internal static CropService? CropService { get; private set; }

        internal static void StartCrop()
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source == null) { return; }

            if (GetCropppingTool == null)
            {
                LoadControls.LoadCroppingTool();
            }

            GetCropppingTool.Width = RotationAngle == 0 || RotationAngle == 180 ? XWidth : XHeight;
            GetCropppingTool.Height = RotationAngle == 0 || RotationAngle == 180 ? XHeight : XWidth;

            ConfigureWindows.GetMainWindow.TitleText.Text = (string)Application.Current.Resources["CropMessage"];

            if (!ConfigureWindows.GetMainWindow.ParentContainer.Children.Contains(GetCropppingTool))
            {
                ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetCropppingTool);
            }
        }

        internal static async Task PerformCropAsync()
        {
            var sameFile = await SaveCrop().ConfigureAwait(false);
            if (sameFile)
            {
                Preloader.Remove(FolderIndex);
                await LoadPic.LoadPiFromFileAsync(Pics[FolderIndex]).ConfigureAwait(false);
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                CloseCrop();
            });
        }

        internal static void CloseCrop()
        {
            if (Pics.Count == 0)
            {
                SetTitle.SetTitleString((int)ConfigureWindows.GetMainWindow.MainImage.Source.Width, (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height);
            }
            else
            {
                SetTitle.SetTitleString((int)ConfigureWindows.GetMainWindow.MainImage.Source.Width, (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height, FolderIndex, null);
            }
            ConfigureWindows.GetMainWindow.ParentContainer.Children.Remove(GetCropppingTool);
        }

        internal static void InitilizeCrop()
        {
            GetCropppingTool.Width = RotationAngle == 0 || RotationAngle == 180 ? XWidth : XHeight;
            GetCropppingTool.Height = RotationAngle == 0 || RotationAngle == 180 ? XHeight : XWidth;

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

        internal static async Task<bool> SaveCrop()
        {
            string filename;
            string? directory;
            if (ErrorHandling.CheckOutOfRange() == false)
            {
                filename = Path.GetRandomFileName();
                directory = null;
            }
            else
            {
                filename = Path.GetFileName(Pics[FolderIndex]);
                directory = Path.GetDirectoryName(filename);
            }

            var Savedlg = new SaveFileDialog
            {
                Filter = Open_Save.FilterFiles,
                Title = $"{Application.Current.Resources["SaveImage"]} - {SetTitle.AppName}",
                FileName = filename,
            };

            if (directory is not null)
            {
                Savedlg.InitialDirectory = directory;
            }

            if (!Savedlg.ShowDialog().HasValue)
            {
                return false;
            }

            Open_Save.IsDialogOpen = true;

            var crop = GetCrop();
            var source = ConfigureWindows.GetMainWindow.MainImage.Source as BitmapSource;
            var effectApplied = ConfigureWindows.GetMainWindow.MainImage.Effect != null;

            var success = await SaveImages.SaveImageAsync(RotationAngle, Flipped, source, null, Savedlg.FileName, crop, effectApplied).ConfigureAwait(false);
            if (success)
            {
                return Savedlg.FileName == Pics[FolderIndex];
            }
            return false;
        }

        /// <summary>
        /// Get crop and calculate it via AspectRatio
        /// </summary>
        /// <returns></returns>
        internal static Int32Rect? GetCrop()
        {
            var cropArea = CropService.GetCroppedArea(); // Contains the dimensions and coordinates of cropped area

            if (cropArea == null) { return null; }

            // TODO add support for zooming in
            int x, y, width, height;

            x = Convert.ToInt32(cropArea.CroppedRectAbsolute.X / AspectRatio);
            y = Convert.ToInt32(cropArea.CroppedRectAbsolute.Y / AspectRatio);

            switch (RotationAngle) // Degress the image has been rotated by
            {
                case 0:
                case 180:
                    width = Convert.ToInt32(cropArea.CroppedRectAbsolute.Width / AspectRatio);
                    height = Convert.ToInt32(cropArea.CroppedRectAbsolute.Height / AspectRatio);
                    break;
                default:
                    width = Convert.ToInt32(cropArea.CroppedRectAbsolute.Height / AspectRatio);
                    height = Convert.ToInt32(cropArea.CroppedRectAbsolute.Width / AspectRatio);
                    break;
            }

            return new Int32Rect(x, y, width, height);
        }
    }
}