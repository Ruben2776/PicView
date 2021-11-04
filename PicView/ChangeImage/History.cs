using PicView.FileHandling;
using PicView.UILogic;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PicView.ChangeImage
{
    internal static class History
    {
        static List<string>? fileHistory;
        const short maxCount = 15;

        internal static void InstantiateQ()
        {
            fileHistory = new List<string>();

            string path = FileFunctions.GetWritingPath() + "\\Recent.txt";

            if (File.Exists(path))
            {
                var listToRead = new StreamReader(path);

                using (listToRead)
                {
                    while (listToRead.Peek() >= 0)
                    {
                        fileHistory.Add(listToRead.ReadLine());
                    }
                }
            }
            else
            {
                WriteToFile();
            }
        }

        /// <summary>
        /// Write all entries to the Recent.txt file
        /// </summary>
        internal static void WriteToFile()
        {
            if (fileHistory is null)
            {
                fileHistory = new List<string>();
            }

            // Create file called "Recent.txt" located on app folder
            var streamWriter = new StreamWriter(FileFunctions.GetWritingPath() + "\\Recent.txt");

            foreach (string item in fileHistory)
            {
                // Write list to stream
                streamWriter.WriteLine(item);
            }

            // Write stream to file
            streamWriter.Flush();
            // Close the stream and reclaim memory
            streamWriter.Close();
        }

        internal static async Task OpenLastFileAsync()
        {
            if (fileHistory is null)
            {
                try // Putting in try catch prevents error when file list is empty
                {
                    InstantiateQ();
                }
                catch (System.Exception)
                {
                    return;
                }
            }

            if (fileHistory.Count <= 0)
            {
                return;
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                UC.ToggleStartUpUC(true);
            });

            await LoadPic.LoadPiFromFileAsync(fileHistory.Last()).ConfigureAwait(false);
        }

        /// <summary>
        /// Function to add file to MRU
        /// </summary>
        /// <returns></returns>
        internal static void Add(string fileName)
        {
            if (fileHistory == null) { InstantiateQ(); }

            lock (fileHistory) // index out of range exception when multiple threads accessing it
            {
                if (fileHistory.Exists(e => e.EndsWith(fileName)))
                {
                    return;
                }

                if (fileHistory.Count >= maxCount)
                {
                    fileHistory.Remove(fileHistory[fileHistory.Count - 1]);
                }

                fileHistory.Add(fileName);
            }
        }

        internal static async Task NextAsync()
        {
            var index = fileHistory.IndexOf(Navigation.Pics[Navigation.FolderIndex]);
            index++;

            if (index >= maxCount)
            {
                return;
            }

            if (fileHistory[index] == Navigation.Pics[Navigation.FolderIndex])
            {
                return;
            }

            await LoadPic.LoadPiFromFileAsync(fileHistory[index]).ConfigureAwait(false);
        }

        internal static async Task PrevAsync()
        {
            var index = fileHistory.IndexOf(Navigation.Pics[Navigation.FolderIndex]);
            index--;

            if (index < 0) { return; }

            if (fileHistory[index] == Navigation.Pics[Navigation.FolderIndex])
            {
                return;
            }

            await LoadPic.LoadPiFromFileAsync(fileHistory[index]).ConfigureAwait(false);
        }

        static MenuItem menuItem(string filePath, int i)
        {
            var cmIcon = new TextBlock
            {
                Text = (i + 1).ToString(CultureInfo.CurrentCulture),
                FontFamily = new FontFamily("/PicView;component/Themes/Resources/fonts/#Tex Gyre Heros"),
                FontSize = 11,
                Width = 12,
                Height = 12,
                Foreground = (SolidColorBrush)Application.Current.Resources["MainColorFadedBrush"]
            };

            var header = Path.GetFileNameWithoutExtension(filePath);
            header = header.Length > 30 ? FileFunctions.Shorten(header, 30) : header;

            var menuItem = new MenuItem()
            {
                Header = header,
                ToolTip = filePath,
                Icon = cmIcon
            };

            menuItem.Click += async (_, _) => await LoadPic.LoadPiFromFileAsync(menuItem.ToolTip.ToString()).ConfigureAwait(false);
            var ext = Path.GetExtension(filePath);
            var ext5 = !string.IsNullOrWhiteSpace(ext) && ext.Length >= 5 ? ext.Substring(0, 5) : ext;
            menuItem.InputGestureText = ext5;
            return menuItem;
        }

        internal static void RefreshRecentItemsMenu()
        {
            if (fileHistory == null) { InstantiateQ(); }

            var cm = (MenuItem)ConfigureWindows.MainContextMenu.Items[5];
            for (int i = 0; i < maxCount; i++)
            {
                if (fileHistory.Count == i)
                {
                    return;
                }

                var item = menuItem(fileHistory[i], i);
                if (item is null) { break; }
                if (cm.Items.Count <= i)
                {
                    cm.Items.Add(item);
                }
                else
                {
                    cm.Items[i] = item;
                }
            }
        }
    }
}
