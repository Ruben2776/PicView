using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.FileFunctions;

namespace PicView.FileHandling
{
    /// <summary>
    /// Class to handle Most Recently Used files
    /// </summary>
    internal static class RecentFiles
    {
        /// <summary>
        /// File list for Most Recently Used files
        /// </summary>
        private static Queue<string> MRUlist;

        /// <summary>
        /// How many max recent files
        /// </summary>
        private const int MRUcount = 11;

        internal static void Initialize()
        {
            MRUlist = new Queue<string>();

            LoadRecent();
        }

        internal static void LoadRecent()
        {
            MRUlist.Clear();
            try
            {
                // Read file stream
                var listToRead = new StreamReader(GetWritingPath() + "\\Recent.txt");
                string line;

                // Read each line until end of file
                while ((line = listToRead.ReadLine()) != null)
                {
                    MRUlist.Enqueue(line);
                }

                listToRead.Close();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Function to add file to MRU
        /// </summary>
        /// <returns></returns>
        internal static void Add(string fileName)
        {
            if (MRUlist == null) { return; }

            // Prevent duplication on recent list
            if (!MRUlist.Contains(fileName))
            {
                MRUlist.Enqueue(fileName);
            }

            // Keep list number not exceeding max value
            while (MRUlist.Count > MRUcount)
            {
                MRUlist.Dequeue();
            }
        }

        /// <summary>
        /// Returns all values string[]
        /// </summary>
        internal static string[] LoadValues()
        {
            if (MRUlist == null)
            {
                return null;
            }

            return MRUlist.ToArray();
        }

        /// <summary>
        /// Write all entries to the Recent.txt file
        /// </summary>
        internal static void WriteToFile()
        {
            // Create file called "Recent.txt" located on app folder
            var streamWriter = new StreamWriter(GetWritingPath() + "\\Recent.txt");
            foreach (string item in MRUlist)
            {
                // Write list to stream
                streamWriter.WriteLine(item);
            }
            // Write stream to file
            streamWriter.Flush();
            // Close the stream and reclaim memory
            streamWriter.Close();
        }

        /// <summary>
        /// Adds events and submenu items to recent items in the context menu
        /// </summary>
        /// <param name="sender"></param>
        internal static void Recentcm_Opened(object sender)
        {
            // Need to register the object as a MenuItem to use it
            var RecentFilesMenuItem = (MenuItem)sender;

            // Load values and check if succeeded
            var fileNames = LoadValues();
            if (fileNames == null)
            {
                return;
            }

            /// Update old values
            /// If items exist: replace them, else add them
            /// TODO Improve how this is handled and make it more user friendly
            if (RecentFilesMenuItem.Items.Count >= fileNames.Length)
            {
                for (int i = fileNames.Length - 1; i >= 0; i--)
                {
                    // Don't add the same item more than once
                    var item = fileNames[i];
                    if (i != 0 && fileNames[i - 1] == item)
                    {
                        continue;
                    }

                    // Change values
                    var menuItem = (MenuItem)RecentFilesMenuItem.Items[i];
                    var header = Path.GetFileNameWithoutExtension(item);
                    header = header.Length > 30 ? Shorten(header, 30) : header;
                    menuItem.Header = header;
                    menuItem.ToolTip = item;
                    var ext = Path.GetExtension(item);
                    var ext5 = !string.IsNullOrWhiteSpace(ext) && ext.Length >= 5 ? ext.Substring(0, 5) : ext;
                    menuItem.InputGestureText = ext5;
                }
                return;
            }

            // Add if not exist
            for (int i = 0; i < fileNames.Length; i++)
            {
                // Don't add the same item more than once
                var item = fileNames[i];
                if (i != 0 && fileNames[i - 1] == item)
                {
                    continue;
                }

                var cmIcon = new TextBlock
                {
                    Text = (i + 1).ToString(CultureInfo.CurrentCulture),
                    FontFamily = new FontFamily("/PicView;component/Themes/Resources/fonts/#Tex Gyre Heros"),
                    FontSize = 11,
                    Width = 12,
                    Height = 12,
                    Foreground = (SolidColorBrush)Application.Current.Resources["MainColorFadedBrush"]
                };

                var header = Path.GetFileNameWithoutExtension(item);
                header = header.Length > 30 ? Shorten(header, 30) : header;
                // Add items
                var menuItem = new MenuItem()
                {
                    Header = header,
                    ToolTip = item,
                    Icon = cmIcon
                };
                // Set tooltip as argument to avoid subscribing and unsubscribing to events
                menuItem.Click += delegate { LoadPiFrom(menuItem.ToolTip.ToString()); };
                var ext = Path.GetExtension(item);
                var ext5 = !string.IsNullOrWhiteSpace(ext) && ext.Length >= 5 ? ext.Substring(0, 5) : ext;
                menuItem.InputGestureText = ext5;
                RecentFilesMenuItem.Items.Add(menuItem);
            }
        }
    }
}