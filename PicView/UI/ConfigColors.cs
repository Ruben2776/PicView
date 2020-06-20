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
        /// Helper for user color settings
        /// </summary>
        internal static Color backgroundBorderColor;

        /// <summary>
        /// Helper for user color settings
        /// </summary>
        internal static Color mainColor;

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

        public static void ChangeBackground(object sender, RoutedEventArgs e)
        {
            if (mainWindow.imgBorder == null)
            {
                return;
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
                    mainWindow.imgBorder.Background = x;
                    break;

                case 1:
                    mainWindow.imgBorder.Background = new SolidColorBrush(Colors.White);
                    break;

                case 2:
                    var _ = DrawingBrushes.CheckerboardDrawingBrush(Colors.White);
                    if (_ != null)
                    {
                        mainWindow.imgBorder.Background = _;
                    }
                    break;

                default:
                    mainWindow.imgBorder.Background = new SolidColorBrush(Colors.Transparent);
                    break;
            }
        }

        public static Brush GetBackgroundColorBrush()
        {
            return Properties.Settings.Default.BgColorChoice switch
            {
                0 => new SolidColorBrush(Colors.Transparent),
                1 => new SolidColorBrush(Colors.White),
                2 => DrawingBrushes.CheckerboardDrawingBrush(Colors.White),
                _ => new SolidColorBrush(Colors.Transparent),
            };
        }
    }
}