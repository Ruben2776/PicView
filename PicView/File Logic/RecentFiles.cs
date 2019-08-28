using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.FileFunctions;
using static PicView.Navigation;
using static PicView.SvgIcons;

namespace PicView
{
    /// <summary>
    /// Class to handle Most Recently Used files
    /// </summary>
    internal static class RecentFiles
    {

        /// <summary>
        /// File list for Most Recently Used files
        /// </summary>
        internal static Queue<string> MRUlist;

        /// <summary>
        /// How many max recent files
        /// </summary>
        const int MRUcount = 7;

        static bool zipped;

        internal static void Initialize()
        {
            MRUlist = new Queue<string>();
            zipped = false;

            LoadRecent();
        }

        internal static void LoadRecent()
        {
            MRUlist.Clear();
            try
            {
                // Read file stream
                var listToRead = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "\\Recent.txt");
                string line;

                // Read each line until end of file
                while ((line = listToRead.ReadLine()) != null)
                    MRUlist.Enqueue(line);

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
            // Don't add zipped files
            if (zipped) 
                return;

            // Prevent duplication on recent list
            if (!(MRUlist.Contains(fileName)))
                MRUlist.Enqueue(fileName);

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
                return null;
            return MRUlist.ToArray();
        }

        /// <summary>
        /// Write all entries to the Recent.txt file
        /// </summary>
        internal static void WriteToFile()
        {
            // Create file called "Recent.txt" located on app folder
            var streamWriter = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\Recent.txt");
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

        internal static void SetZipped(string zipfile, bool isZipped = true)
        {
            if (!string.IsNullOrWhiteSpace(zipfile))
                Add(zipfile);
            zipped = isZipped;
        }


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
                    var header = Path.GetFileNameWithoutExtension(item);
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
                    Data = Geometry.Parse(SVGiconCamera),
                    Stretch = Stretch.Fill,
                    Width = 12,
                    Height = 12,
                    Fill = (SolidColorBrush)Application.Current.Resources["MainColorFadedBrush"]
                };

                var header = Path.GetFileNameWithoutExtension(item);
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

    }
}
