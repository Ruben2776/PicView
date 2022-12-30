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
            cropCm.Click += async (_, _) => await CropFunctions.PerformCropAsync().ConfigureAwait(false);

            ContextMenu.Items.Add(cropCm);

            var closeIcon = new Path
            {
                Data = Geometry.Parse("M443.6,387.1L312.4,255.4l131.5-130c5.4-5.4,5.4-14.2,0-19.6l-37.4-37.6c-2.6-2.6-6.1-4-9.8-4c-3.7,0-7.2,1.5-9.8,4  L256,197.8L124.9,68.3c-2.6-2.6-6.1-4-9.8-4c-3.7,0-7.2,1.5-9.8,4L68,105.9c-5.4,5.4-5.4,14.2,0,19.6l131.5,130L68.4,387.1  c-2.6,2.6-4.1,6.1-4.1,9.8c0,3.7,1.4,7.2,4.1,9.8l37.4,37.6c2.7,2.7,6.2,4.1,9.8,4.1c3.5,0,7.1-1.3,9.8-4.1L256,313.1l130.7,131.1  c2.7,2.7,6.2,4.1,9.8,4.1c3.5,0,7.1-1.3,9.8-4.1l37.4-37.6c2.6-2.6,4.1-6.1,4.1-9.8C447.7,393.2,446.2,389.7,443.6,387.1z"),
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