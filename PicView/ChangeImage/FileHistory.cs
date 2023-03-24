using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.UILogic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PicView.ChangeImage
{
    internal class FileHistory
    {
        private readonly List<string> fileHistory;
        private const short maxCount = 15;
        private readonly string path;

        public FileHistory()
        {
           fileHistory ??= new List<string>();
            try
            {
                path = FileFunctions.GetWritingPath() + "\\Recent.txt";

                if (!File.Exists(path))
                {
                    using FileStream fs = File.Create(path);
                    fs.Seek(0, SeekOrigin.Begin);
                }
            }
            catch (Exception e)
            {
                Tooltip.ShowTooltipMessage(e.Message);
                path = "";
            }

            ReadFromFile();
        }

        private void ReadFromFile()
        {
            fileHistory.Clear();

            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                using var reader = new StreamReader(path);
                while (reader.Peek() >= 0)
                {
                    fileHistory.Add(reader.ReadLine());
                }
            }
            catch (Exception e)
            {
                Tooltip.ShowTooltipMessage(e.Message);
            }
        }

        /// <summary>
        /// Write all entries to the Recent.txt file
        /// </summary>
        internal void WriteToFile()
        {
            try
            {
                using var writer = new StreamWriter(path);
                foreach (string item in fileHistory)
                {
                    writer.WriteLine(item);
                }
            }
            catch (Exception e)
            {
                Tooltip.ShowTooltipMessage(e.Message);
            }

        }

        internal async Task OpenLastFileAsync()
        {
            if (fileHistory.Count <= 0)
            {
                return;
            }

            UC.GetStartUpUC.ToggleMenu();
            SetTitle.SetLoadingString();

            await LoadPic.LoadPicFromStringAsync(fileHistory.Last()).ConfigureAwait(false);
        }

        internal void Add(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

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

        internal async Task NextAsync()
        {
            if (Navigation.Pics.Count <= 0)
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

        internal async Task PrevAsync()
        {
            if (Navigation.Pics.Count <= 0)
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

        private static MenuItem menuItem(string filePath, int i)
        {
            bool selected;
            if (ErrorHandling.CheckOutOfRange())
            {
                selected = false;
            }
            else
            {
                selected = filePath == Navigation.Pics[Navigation.FolderIndex];
            }

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

            var menuItem = new MenuItem
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

        internal void RefreshRecentItemsMenu()
        {
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