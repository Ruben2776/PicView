using Microsoft.WindowsAPICodePack.Taskbar;
using PicView.Library.Resources;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static PicView.Library.Fields;

namespace PicView.Library
{
    internal static class Utilities
    {
        #region static helpers

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
        /// Gets the absolute mouse position, relative to screen
        /// </summary>
        /// <returns></returns>
        internal static Point GetMousePos(UIElement element)
        {
            return element.PointToScreen(Mouse.GetPosition(element));
        }


        #endregion

        #region Progress

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

        #endregion

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

        //internal static byte[] BitmapSourceToBytes(BitmapSource bitmapSource)
        //{
        //    var encoder = new PngBitmapEncoder();
        //    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
        //    using var memoryStream = new MemoryStream();
        //    encoder.Save(memoryStream);
        //    var bitmap = new BitmapImage();

        //    memoryStream.Position = 0;
        //    memoryStream.Seek(0, SeekOrigin.Begin);
        //    bitmap.BeginInit();
        //    bitmap.StreamSource = memoryStream;
        //    bitmap.EndInit();
        //    using BinaryReader br = new BinaryReader(bitmap.StreamSource);
        //    return br.ReadBytes((int)bitmap.StreamSource.Length);
        //}

        public static string GetDefaultExeConfigPath(ConfigurationUserLevel userLevel)
        {
            try
            {
                var UserConfig = ConfigurationManager.OpenExeConfiguration(userLevel);
                return UserConfig.FilePath;
            }
            catch (ConfigurationException e)
            {
                return e.Filename;
            }
        }

        public static string GetWritingPath()
        {
            return Path.GetDirectoryName(GetDefaultExeConfigPath(ConfigurationUserLevel.PerUserRoamingAndLocal));
        }

    }
}
