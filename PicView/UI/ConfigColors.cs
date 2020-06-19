using PicView.Library.Resources;
using PicView.UI.Animations;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using static PicView.Library.Fields;

namespace PicView.UI
{
    public static class ConfigColors
    {
        /// <summary>
        /// Update color values for brushes and window border
        /// </summary>
        /// <param name="remove">Remove border?</param>
        public static void UpdateColor(bool remove = false)
        {
            if (remove)
            {
                Application.Current.Resources["WindowBackgroundColorBrush"] = new SolidColorBrush(Colors.Black);
                return;
            }

            var getColor = AnimationHelper.GetPrefferedColorOver();
            var getColorBrush = new SolidColorBrush(getColor);

            Application.Current.Resources["ChosenColor"] = getColor;
            Application.Current.Resources["ChosenColorBrush"] = getColorBrush;

            if (Properties.Settings.Default.WindowBorderColorEnabled)
            {
                try
                {
                    Application.Current.Resources["WindowBackgroundColorBrush"] = getColorBrush;
                }
                catch (System.Exception e)
                {
#if DEBUG
                    Trace.WriteLine(nameof(UpdateColor) + " threw exception:  " + e.Message);
#endif
                    //throw;
                }

            }
        }

        public static void SetColors()
        {
            mainColor = (Color)Application.Current.Resources["MainColor"];

            switch (Properties.Settings.Default.BgColorChoice)
            {
                case 0:
                    mainWindow.imgBorder.Background = new SolidColorBrush(Colors.Transparent);
                    break;
                case 1:
                    mainWindow.imgBorder.Background = new SolidColorBrush(Colors.White);
                    break;
                case 2:
                    mainWindow.imgBorder.Background = DrawingBrushes.CheckerboardDrawingBrush(Colors.White);
                    break;
            }

            backgroundBorderColor = (Color)Application.Current.Resources["BackgroundColorAlt"];
        }

        public static void ChangeBackground()
        {
            ChangeBackground(null, null);
        }

        public static void ChangeBackground(object sender, RoutedEventArgs e)
        {
            var brush = GetBackgroundColorBrush();
            if (brush != null)
            {
                mainWindow.imgBorder.Background = brush;
            }
            else
            {
                mainWindow.imgBorder.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        public static Brush GetBackgroundColorBrush()
        {
            if (mainWindow.imgBorder == null)
            {
                return null;
            }

            Properties.Settings.Default.BgColorChoice++;

            if (Properties.Settings.Default.BgColorChoice > 2)
            {
                Properties.Settings.Default.BgColorChoice = 0;
            }

            switch (Properties.Settings.Default.BgColorChoice)
            {
                case 0:
                    var x = new SolidColorBrush(Colors.Transparent);
                    if (mainWindow.imgBorder.Background == x)
                    {
                        goto case 1;
                    }
                    return x;
                case 1:
                    return new SolidColorBrush(Colors.White);
                case 2:
                    return DrawingBrushes.CheckerboardDrawingBrush(Colors.White);
                default:
                    return new SolidColorBrush(Colors.Transparent);
            }
        }
    }
}
