using PicView.PreLoading;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static PicView.Error_Handling.Error_Handling;
using static PicView.File_Logic.FileLists;
using static PicView.Helpers.Helper;
using static PicView.Helpers.Variables;
using static PicView.Image_Logic.Navigation;
using static PicView.Interface_Logic.Interface;

namespace PicView.File_Logic
{
    class FileFunctions
    {

        internal static bool RenameFile(string PicPath, string PicNewPath)
        {
            if (File.Exists(PicNewPath))
                return false;

            try
            {
                File.Move(PicPath, PicNewPath);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Return file size in a readable format
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        /// Credits to http://www.somacon.com/p576.php
        internal static string GetSizeReadable(long i)
        {
            string sign = (i < 0 ? "-" : string.Empty);
            double readable = (i < 0 ? -i : i);
            char suffix;

            if (i >= 0x40000000) // Gigabyte
            {
                suffix = 'G';
                readable = (i >> 20);
            }
            else if (i >= 0x100000) // Megabyte
            {
                suffix = 'M';
                readable = (i >> 10);
            }
            else if (i >= 0x400) // Kilobyte
            {
                suffix = 'K';
                readable = i;
            }
            else
            {
                return i.ToString(sign + "0 B"); // Byte
            }
            readable /= 1024;

            return sign + readable.ToString("0.## ") + suffix + 'B';
        }

        

        #region File Methods

        /// <summary>
        /// Adds events and submenu items to recent items in the context menu
        /// </summary>
        /// <param name="sender"></param>
        internal static void Recentcm_MouseEnter(object sender)
        {
            // Need to register the object as a MenuItem to use it
            var RecentFilesMenuItem = (MenuItem)sender;

            // Load values and check if succeeded
            var fileNames = RecentFiles.LoadValues();
            if (fileNames == null)
                return;

            // Update old values
            // If items exist: replace them, else add them
            if (RecentFilesMenuItem.Items.Count >= fileNames.Length)
            {
                for (int i = fileNames.Length - 1; i >= 0; i--)
                {
                    // Don't add the same item more than once
                    var item = fileNames[i];
                    if (i != 0 && fileNames[i - 1] == item)
                        continue;

                    // Change values
                    var menuItem = (MenuItem)RecentFilesMenuItem.Items[i];
                    var header = Path.GetFileName(item);
                    header = header.Length > 30 ? Shorten(header, 30) : header;
                    menuItem.Header = header;
                    menuItem.ToolTip = item;
                    var ext = Path.GetExtension(item);
                    var ext5 = !string.IsNullOrWhiteSpace(ext) && ext.Length >= 5 ? ext.Substring(0, 5) : ext;
                    //ext5 = ext5.Length == 5 ? ext.Replace("?", string.Empty) : ext5;
                    menuItem.InputGestureText = ext5;
                }
                return;
            }

            // Add if not exist
            for (int i = fileNames.Length - 1; i >= 0; i--)
            {
                // Don't add the same item more than once
                var item = fileNames[i];
                if (i != 0 && fileNames[i - 1] == item)
                    continue;

                var cmIcon = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse(CameraIconSVG),
                    Stretch = Stretch.Fill,
                    Width = 12,
                    Height = 12,
                    Fill = (SolidColorBrush)Application.Current.Resources["MainColorFadedBrush"]
                };

                var header = Path.GetFileName(item);
                header = header.Length > 30 ? Shorten(header, 30) : header;
                // Add items
                var menuItem = new MenuItem()
                {
                    Header = header,
                    ToolTip = item
                };
                // Set tooltip as argument to avoid subscribing and unsubscribing to events
                menuItem.Click += (x, xx) => Pic(menuItem.ToolTip.ToString());
                menuItem.Icon = cmIcon;
                var ext = Path.GetExtension(item);
                var ext5 = !string.IsNullOrWhiteSpace(ext) && ext.Length >= 5 ? ext.Substring(0, 5) : ext;
                //ext5 = ext5.Length == 5 ? ext.Replace("?", string.Empty) : ext5;
                menuItem.InputGestureText = ext5;
                RecentFilesMenuItem.Items.Add(menuItem);
            }
        }

        internal static bool FilePathHasInvalidChars(string path)
        {
            return (!string.IsNullOrEmpty(path) && path.IndexOfAny(Path.GetInvalidPathChars()) >= 0);
        }

        internal static string MakeValidFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(name, invalidRegStr, "_");
        }

        #endregion File Methods
    }

   
}
