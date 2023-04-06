using PicView.ConfigureSettings;
using PicView.UILogic.TransformImage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PicView.Editing.Crop.Tools
{
    internal class TextTool
    {
        private const int OffsetTop = 3;
        private const int OffsetLeft = 10;

        private TextBlock TextBlock { get; }
        public Border Border { get; }

        private readonly CropTool _cropTool;

        public TextTool(CropTool cropTool)
        {
            _cropTool = cropTool;

            Border = new Border
            {
                Background = new SolidColorBrush(ConfigColors.BackgroundBorderColor),
                Padding = new Thickness(9),
                BorderBrush = (SolidColorBrush)Application.Current.Resources["BorderBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Visibility = Visibility.Hidden
            };
            TextBlock = new TextBlock
            {
                FontFamily = new FontFamily("/PicView;component/Themes/Resources/fonts/#Tex Gyre Heros"),
                FontSize = 16,
                Foreground = new SolidColorBrush(ConfigColors.MainColor),
            };
            Border.Child = TextBlock;
        }

        /// <summary>
        /// Manage visibility of text
        /// </summary>
        /// <param name="isVisible">Set current visibility</param>
        public void ShowText(bool isVisible)
        {
            Border.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// Update (redraw) text label
        /// </summary>
        public void Redraw()
        {
            ShowText(_cropTool.Height > 0 && _cropTool.Width > 0);

            double calculateTop = _cropTool.TopLeftY - Border.ActualHeight - OffsetTop;
            if (calculateTop < 0)
            {
                calculateTop = OffsetTop;
            }

            Canvas.SetLeft(Border, _cropTool.TopLeftX + OffsetLeft);
            Canvas.SetTop(Border, calculateTop);
            var zoomValue = ZoomLogic.ZoomValue == 0 ? 1 : ZoomLogic.ZoomValue;
            var aspectRatio = UILogic.Sizing.ScaleImage.AspectRatio;
            double aspectZoomValue = aspectRatio * zoomValue;
            var width = Math.Round(_cropTool.Width / zoomValue);
            var height = Math.Round(_cropTool.Height / zoomValue);
            TextBlock.Text = $"{Application.Current.Resources["Width"]} : {width}, {Application.Current.Resources["Height"]}: {height}";
        }
    }
}