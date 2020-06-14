using PicView.PreLoading;
using System;
using System.Collections.Generic;
using System.Text;
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

            //var pic = Preloader.Load(Pics[FolderIndex]);

            cropppingTool.CropTool.SetImage(mainWindow.img.Source as BitmapSource);
            cropppingTool.CropTool.SetSize(xWidth, xHeight);
            //cropppingTool.MaxWidth = cropppingTool.CropTool.MaxWidth = xWidth;
            //cropppingTool.MaxHeight = cropppingTool.CropTool.MaxHeight = xHeight;


        }
    }
}
