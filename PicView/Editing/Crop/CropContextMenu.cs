using PicView.UILogic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PicView.Editing.Crop
{
    internal class CropContextMenu
    {
        public CropContextMenu()
        {
            var ContextMenu = new ContextMenu();

            var cropIcon = new Path
            {
                Width = 12,
                Height = 12,
                Data = Geometry.Parse("M488 352h-40V109.25l59.31-59.31c6.25-6.25 6.25-16.38 0-22.63L484.69 4.69c-6.25-6.25-16.38-6.25-22.63 0L402.75 64H192v96h114.75L160 306.75V24c0-13.26-10.75-24-24-24H88C74.75 0 64 10.74 64 24v40H24C10.75 64 0 74.74 0 88v48c0 13.25 10.75 24 24 24h40v264c0 13.25 10.75 24 24 24h232v-96H205.25L352 205.25V488c0 13.25 10.75 24 24 24h48c13.25 0 24-10.75 24-24v-40h40c13.25 0 24-10.75 24-24v-48c0-13.26-10.75-24-24-24z"),
                Stretch = Stretch.Fill,
                Fill = (SolidColorBrush)Application.Current.Resources["MainColorFadedBrush"]
            };

            var cropCm = new MenuItem
            {
                Header = Application.Current.Resources["CropPicture"] as string,
                InputGestureText = Application.Current.Resources["Enter"] as string,
                Icon = cropIcon
            };
            cropCm.Click += (_,_) => CropFunctions.PerformCrop();

            ContextMenu.Items.Add(cropCm);

            var closeIcon = new Path
            {
                Data = Geometry.Parse(Library.Resources.SvgIcons.SVGiconClose),
                Stretch = Stretch.Fill,
                Width = 12,
                Height = 12,
                Fill = (SolidColorBrush)Application.Current.Resources["MainColorFadedBrush"]
            };

            var closeCm = new MenuItem
            {
                Header = Application.Current.Resources["Close"] as string,
                InputGestureText = Application.Current.Resources["Esc"] as string,
                Icon = closeIcon
            };
            closeCm.Click += (_, _) => { ContextMenu.Visibility = Visibility.Collapsed; CropFunctions.CloseCrop(); };

            ContextMenu.Items.Add(closeCm);

            UC.GetCropppingTool.ContextMenu = ContextMenu;

        }
    }
}
