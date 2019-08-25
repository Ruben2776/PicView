using System.Windows;

namespace PicView
{
    internal static class Settings
    {

        internal static void SetSlidetimer()
        {
            switch (Properties.Settings.Default.Slidetimeren.ToString("0"))
            {
                case "1":
                    Properties.Settings.Default.Slidetimeren = 1000;
                    break;

                case "2":
                    Properties.Settings.Default.Slidetimeren = 2000;
                    break;

                case "3":
                    Properties.Settings.Default.Slidetimeren = 3000;
                    break;

                case "4":
                    Properties.Settings.Default.Slidetimeren = 4000;
                    break;

                case "5":
                    Properties.Settings.Default.Slidetimeren = 5000;
                    break;

                case "6":
                    Properties.Settings.Default.Slidetimeren = 6000;
                    break;

                case "7":
                    Properties.Settings.Default.Slidetimeren = 7000;
                    break;

                case "8":
                    Properties.Settings.Default.Slidetimeren = 8000;
                    break;

                case "9":
                    Properties.Settings.Default.Slidetimeren = 9000;
                    break;

                case "10":
                    Properties.Settings.Default.Slidetimeren = 10000;
                    break;

                case "11":
                    Properties.Settings.Default.Slidetimeren = 11000;
                    break;

                case "12":
                    Properties.Settings.Default.Slidetimeren = 12000;
                    break;

                case "13":
                    Properties.Settings.Default.Slidetimeren = 13000;
                    break;

                case "14":
                    Properties.Settings.Default.Slidetimeren = 140000;
                    break;

                case "15":
                    Properties.Settings.Default.Slidetimeren = 15000;
                    break;
            }
        }


        internal static void SetLooping(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.Looping)
            {
                Properties.Settings.Default.Looping = false;
            }
            else
            {
                Properties.Settings.Default.Looping = true;
            }
        }

        internal static void SetBgColorEnabled(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WindowBorderColorEnabled = Properties.Settings.Default.WindowBorderColorEnabled ? false : true;
        }
    }
}
