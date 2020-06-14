using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
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
            }

            if (mainWindow.img.Source == null) { return; }

            if (!mainWindow.bg.Children.Contains(cropppingTool))
            {
                mainWindow.bg.Children.Add(cropppingTool);
            }

            cropppingTool.CropTool.SetImage(mainWindow.img.Source as BitmapSource);
            cropppingTool.CropTool.SetSize(xWidth, xHeight);
            cropppingTool.CropTool.Width = xWidth;
            cropppingTool.CropTool.Height = xHeight;
        }

        internal static void SaveCrop()
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

            //var cropArea = cropppingTool.CropTool.CropService.GetCroppedArea();

            //var x = Convert.ToInt32(cropArea.CroppedRectAbsolute.X);
            //var y = Convert.ToInt32(cropArea.CroppedRectAbsolute.Y);
            //var width = Convert.ToInt32(cropArea.CroppedRectAbsolute.Width);
            //var height = Convert.ToInt32(cropArea.CroppedRectAbsolute.Height);

            //var sourceBitmap = new BitmapImage(new Uri(Pics[FolderIndex]));

            //var cb = new CroppedBitmap(sourceBitmap, new Int32Rect(x, y, width, height));

            //using var fileStream = new FileStream(Savedlg.FileName, FileMode.Create);
            //BitmapEncoder encoder = new PngBitmapEncoder();
            //encoder.Frames.Add(BitmapFrame.Create(cb));
            //encoder.Save(fileStream);


            var crop = GetCrop();
            SaveImages.TrySaveImage(crop, Savedlg.FileName);

            //var cropArea = cropppingTool.CropTool.CropService.GetCroppedArea();

            //Rectangle cropRect = new Rectangle((int)cropArea.CroppedRectAbsolute.X,
            //                (int)cropArea.CroppedRectAbsolute.Y, (int)cropArea.CroppedRectAbsolute.Width,
            //                (int)cropArea.CroppedRectAbsolute.Height);

            //Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            //using (Graphics g = Graphics.FromImage(target))
            //{
            //    g.DrawImage(Image.FromFile(Pics[FolderIndex]), new Rectangle(0, 0, target.Width, target.Height),
            //        cropRect,
            //        GraphicsUnit.Pixel);
            //}

            //target.Save(Savedlg.FileName);
            //target.Dispose();
        }

        internal static CroppedBitmap GetCrop()
        {
            if (mainWindow.img.Source == null) { return null; }

            var cropArea = cropppingTool.CropTool.CropService.GetCroppedArea();

            var x = Convert.ToInt32(cropArea.CroppedRectAbsolute.X);
            var y = Convert.ToInt32(cropArea.CroppedRectAbsolute.Y);
            var width = Convert.ToInt32(cropArea.CroppedRectAbsolute.Width);
            var height = Convert.ToInt32(cropArea.CroppedRectAbsolute.Height);

            return new CroppedBitmap(new BitmapImage(new Uri(Pics[FolderIndex])), new Int32Rect(x, y, width, height));       //select region rect
        }
    }
}
