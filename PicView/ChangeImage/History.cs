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

        internal static void InstantiateFileHistory()
        {
            fileHistory = new List<string>();

            string path = FileFunctions.GetWritingPath() + "\\Recent.txt";
            StreamReader? listToRead = null;

            if (File.Exists(path))
            {
                try
                {
                    listToRead = new StreamReader(path);
                }
                catch (System.Exception)
                {
                    return; // Putting in try catch prevents error when file list is empty
                }

                if (listToRead == null) { return; }

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
                try
                {
                    using FileStream fs = File.Create(path);
                    fs.Seek(0, SeekOrigin.Begin);
                }
                catch (System.Exception)
                {
                    return;
                }

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

            try
            {
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
            catch (System.Exception)
            {
                // Putting in try catch prevents error when file list is empty
            }
        }

        internal static async Task OpenLastFileAsync()
        {
            if (fileHistory is null)
            {
                InstantiateFileHistory();
            }

            if (fileHistory.Count <= 0)
            {
                return;
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                UC.ToggleStartUpUC(true);
            });

            await LoadPic.LoadPicFromStringAsync(fileHistory.Last()).ConfigureAwait(false);
        }

        /// <summary>
        /// Function to add file to MRU
        /// </summary>
        /// <returns></returns>
        internal static void Add(string fileName)
        {
            if (fileHistory == null) { InstantiateFileHistory(); }

            lock (fileHistory) // index out of range exception when multiple threads accessing it
            {
                if (fileHistory.Exists(e => e.EndsWith(fileName)))
                {
                    return;
                }

                if (fileHistory.Count >= maxCount)
                {
                    fileHistory.Remove(fileHistory[0]);
                }

                fileHistory.Add(fileName);
            }
        }

        internal static async Task NextAsync()
        {
            if (Navigation.Pics.Count == 0)
            {
                await OpenLastFileAsync().ConfigureAwait(false);
                return;
            }

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

            await LoadPic.LoadPicFromStringAsync(fileHistory[index]).ConfigureAwait(false);
        }

        internal static async Task PrevAsync()
        {
            if (Navigation.Pics.Count == 0)
            {
                await OpenLastFileAsync().ConfigureAwait(false);
                return;
            }

            var index = fileHistory.IndexOf(Navigation.Pics[Navigation.FolderIndex]);
            index--;

            if (index < 0) { return; }

            if (fileHistory[index] == Navigation.Pics[Navigation.FolderIndex])
            {
                return;
            }

            await LoadPic.LoadPicFromStringAsync(fileHistory[index]).ConfigureAwait(false);
        }

        static MenuItem menuItem(string filePath, int i)
        {
            var selected = i == maxCount - 1;
            var mainColor = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
            var accentColor = (SolidColorBrush)Application.Current.Resources["ChosenColorBrush"];
            var cmIcon = new TextBlock
            {
                Text = (i + 1).ToString(CultureInfo.CurrentCulture),
                FontFamily = new FontFamily("/PicView;component/Themes/Resources/fonts/#Tex Gyre Heros"),
                FontSize = 11,
                Width = 12,
                Height = 12,
                Foreground = (SolidColorBrush)Application.Current.Resources["IconColorBrush"]
            };

            var header = Path.GetFileNameWithoutExtension(filePath);
            header = header.Length > 30 ? FileFunctions.Shorten(header, 30) : header;

            var menuItem = new MenuItem()
            {
                Header = header,
                ToolTip = filePath,
                Icon = cmIcon,
                Foreground = selected ? accentColor : mainColor,
                FontWeight = selected ? FontWeights.Bold : FontWeights.Normal,
            };

            menuItem.MouseEnter += (_, _) => menuItem.Foreground = new SolidColorBrush(Colors.White);
            menuItem.MouseLeave += (_, _) => menuItem.Foreground = (SolidColorBrush)Application.Current.Resources["IconColorBrush"];

            menuItem.Click += async (_, _) => await LoadPic.LoadPicFromStringAsync(menuItem.ToolTip.ToString()).ConfigureAwait(false);
            var ext = Path.GetExtension(filePath);
            var ext5 = !string.IsNullOrWhiteSpace(ext) && ext.Length >= 5 ? ext.Substring(0, 5) : ext;
            menuItem.InputGestureText = ext5;
            return menuItem;
        }

        internal static void RefreshRecentItemsMenu()
        {
            if (fileHistory == null) { InstantiateFileHistory(); }

            var cm = (MenuItem)ConfigureWindows.MainContextMenu.Items[6];

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
