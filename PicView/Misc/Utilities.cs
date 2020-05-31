using Microsoft.WindowsAPICodePack.Taskbar;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using static PicView.Fields;

namespace PicView
{
    internal static class Utilities
    {
        /// <summary>
        /// Greatest Common Divisor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        internal static int GCD(int x, int y)
        {
            return y == 0 ? x : GCD(y, x % y);
        }

        /// <summary>
        /// Show progress on taskbar
        /// </summary>
        /// <param name="i">index</param>
        /// <param name="ii">size</param>
        internal static void Progress(int i, int ii)
        {
            TaskbarManager prog = TaskbarManager.Instance;
            prog.SetProgressState(TaskbarProgressBarState.Normal);
            prog.SetProgressValue(i, ii);
        }

        /// <summary>
        /// Stop showing taskbar progress, return to default
        /// </summary>
        internal static void NoProgress()
        {
            TaskbarManager prog = TaskbarManager.Instance;
            prog.SetProgressState(TaskbarProgressBarState.NoProgress);
        }

        /// <summary>
        /// Sends the file to Windows print system
        /// </summary>
        /// <param name="path">The file path</param>
        internal static bool Print(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (!File.Exists(path))
            {
                return false;
            }

            using (var p = new Process())
            {
                p.StartInfo.FileName = path;
                p.StartInfo.Verb = "print";
                p.StartInfo.UseShellExecute = true;
                p.Start();
            }
            return true;
        }

        internal static void UpdateColor(bool remove = false)
        {
            if (remove)
            {
                Application.Current.Resources["WindowBackgroundColorBrush"] = new SolidColorBrush(Colors.Black);
                return;
            }

            Application.Current.Resources["ChosenColor"] = AnimationHelper.GetPrefferedColorOver();
            Application.Current.Resources["ChosenColorBrush"] = new SolidColorBrush(AnimationHelper.GetPrefferedColorOver());

            if (Properties.Settings.Default.WindowBorderColorEnabled)
            {
                try
                {
                    //var bgBrush = Application.Current.Resources["WindowBackgroundColorBrush"] as SolidColorBrush;
                    //bgBrush.Color = AnimationHelper.GetPrefferedColorOver();
                    Application.Current.Resources["WindowBackgroundColorBrush"] = new SolidColorBrush(AnimationHelper.GetPrefferedColorOver());
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

        internal static void SetColors()
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

        internal static void ChangeBackground()
        {
            ChangeBackground(null, null);
        }

        internal static void ChangeBackground(object sender, RoutedEventArgs e)
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

        internal static Brush GetBackgroundColorBrush()
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
