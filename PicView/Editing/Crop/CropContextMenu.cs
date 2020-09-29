using PicView.UILogic;
using System.Windows;
using System.Windows.Controls;

namespace PicView.Editing.Crop
{
    internal class CropContextMenu
    {
        readonly ContextMenu ContextMenu;
        public CropContextMenu()
        {
            ContextMenu = new ContextMenu();
            var cropCm = new MenuItem
            {
                Header = Application.Current.Resources["CropPicture"] as string,
                InputGestureText = Application.Current.Resources["Enter"] as string
            };
            cropCm.Click += (_,_) => CropFunctions.PerformCrop();

            ContextMenu.Items.Add(cropCm);

            var closeCm = new MenuItem
            {
                Header = Application.Current.Resources["Close"] as string,
                InputGestureText = Application.Current.Resources["Esc"] as string,
            };
            closeCm.Click += (_, _) => { ContextMenu.Visibility = Visibility.Collapsed; CropFunctions.CloseCrop(); };

            ContextMenu.Items.Add(closeCm);

            UC.GetCropppingTool.ContextMenu = ContextMenu;

        }
    }
}
