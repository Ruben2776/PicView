using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PicView.ConfigureSettings;

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
            if (_cropTool.Height <= 0 && _cropTool.Width <= 0)
            {
                ShowText(false);
            }
            else
            {
                ShowText(true);
            }

            double calculateTop = _cropTool.TopLeftY - Border.ActualHeight - OffsetTop;
            if (calculateTop < 0)
            {
                calculateTop = OffsetTop;
            }

            Canvas.SetLeft(Border, _cropTool.TopLeftX + OffsetLeft);
            Canvas.SetTop(Border, calculateTop);
            TextBlock.Text = $"{Application.Current.Resources["Width"]} : {_cropTool.Width}, {Application.Current.Resources["Height"]}: {_cropTool.Height}";
        }
    }
}