using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PicView.ChangeImage;

internal class FileHistory
{
    private readonly List<string> _fileHistory;
    private const short MaxCount = 15;
    private readonly string _path;

    public FileHistory()
    {
        _fileHistory ??= new List<string>();
        try
        {
            _path = FileFunctions.GetWritingPath() + "\\Recent.txt";

            if (!File.Exists(_path))
            {
                using FileStream fs = File.Create(_path);
                fs.Seek(0, SeekOrigin.Begin);
            }
        }
        catch (Exception e)
        {
            _path = "";
        }

        ReadFromFile();
    }

    private void ReadFromFile()
    {
        _fileHistory.Clear();

        if (!File.Exists(_path))
        {
            return;
        }

        try
        {
            using var reader = new StreamReader(_path);
            while (reader.Peek() >= 0)
            {
                _fileHistory.Add(reader.ReadLine());
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
            using var writer = new StreamWriter(_path);
            foreach (var item in _fileHistory)
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
        if (_fileHistory.Count <= 0)
        {
            return;
        }

        if (!File.Exists(_fileHistory.Last()))
        {
            return;
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (UC.GetStartUpUC is not null)
            {
                UC.GetStartUpUC.ToggleMenu();
                UC.GetStartUpUC.Logo.Visibility = Visibility.Collapsed;
            }

            SetTitle.SetLoadingString();
        }, DispatcherPriority.Normal);

        if (Settings.Default.IncludeSubDirectories)
        {
            if (_fileHistory.Last().IsArchive())
            {
                await LoadPic.LoadPicFromArchiveAsync(_fileHistory.Last()).ConfigureAwait(false);
                return;
            }
            var currentFolder = Path.GetDirectoryName(_fileHistory.Last());
            var parentFolder = Path.GetDirectoryName(currentFolder);
            var fileInfo = new FileInfo(parentFolder);
            Navigation.Pics = await Task.FromResult(FileLists.FileList(fileInfo)).ConfigureAwait(false);
            if (Navigation.Pics.Count > 0)
            {
                Navigation.FolderIndex = Navigation.Pics.IndexOf(_fileHistory.Last());
                await LoadPic.LoadPicAtIndexAsync(Navigation.FolderIndex).ConfigureAwait(false);

                // Fix if Bottom Gallery is enabled
                if (Settings.Default.IsBottomGalleryShown)
                {
                    if (UC.GetPicGallery is { Visibility: Visibility.Collapsed })
                    {
                        var shouldLoadGallery = false;
                        await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
                        {
                            GalleryToggle.ShowBottomGallery();
                            ScaleImage.TryFitImage();
                            if (UC.GetPicGallery.Container.Children.Count <= 0)
                            {
                                shouldLoadGallery = true;
                            }
                        });
                        if (shouldLoadGallery)
                        {
                            await GalleryLoad.LoadAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
            else
            {
                await LoadPic.LoadPicFromStringAsync(_fileHistory.Last()).ConfigureAwait(false);
            }
        }
        else
        {
            await LoadPic.LoadPicFromStringAsync(_fileHistory.Last()).ConfigureAwait(false);
        }
    }

    internal void Add(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        lock (_fileHistory) // index out of range exception when multiple threads accessing it
        {
            if (_fileHistory.Exists(e => e.EndsWith(fileName)))
            {
                return;
            }

            if (_fileHistory.Count >= MaxCount)
            {
                _fileHistory.Remove(_fileHistory[0]);
            }

            _fileHistory.Add(fileName);
        }
    }

    internal async Task NextAsync()
    {
        if (Navigation.Pics.Count <= 0)
        {
            await OpenLastFileAsync().ConfigureAwait(false);
            return;
        }

        var index = _fileHistory.IndexOf(Navigation.Pics[Navigation.FolderIndex]);
        if (Settings.Default.Looping)
        {
            index = (index + 1 + _fileHistory.Count) % _fileHistory.Count;
        }
        else
        {
            index++;
            if (index >= MaxCount) return;
        }

        if (_fileHistory[index] == Navigation.Pics[Navigation.FolderIndex])
        {
            return;
        }

        await LoadPic.LoadPicFromStringAsync(_fileHistory[index]).ConfigureAwait(false);
    }

    internal async Task PrevAsync()
    {
        if (Navigation.Pics.Count <= 0)
        {
            await OpenLastFileAsync().ConfigureAwait(false);
            return;
        }

        var index = _fileHistory.IndexOf(Navigation.Pics[Navigation.FolderIndex]);
        if (Settings.Default.Looping)
        {
            index = (index - 1 + _fileHistory.Count) % _fileHistory.Count;
        }
        else
        {
            index--;
            if (index < 0) { return; }
        }

        if (_fileHistory[index] == Navigation.Pics[Navigation.FolderIndex])
        {
            return;
        }

        await LoadPic.LoadPicFromStringAsync(_fileHistory[index]).ConfigureAwait(false);
    }

    private static MenuItem MenuItem(string filePath, int i)
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
        header = header.Length > 30 ? header.Shorten(30) : header;

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
        var ext5 = !string.IsNullOrWhiteSpace(ext) && ext.Length >= 5 ? ext[..5] : ext;
        menuItem.InputGestureText = ext5;
        return menuItem;
    }

    internal void RefreshRecentItemsMenu()
    {
        try
        {
            var cm = (MenuItem)ConfigureWindows.MainContextMenu.Items[6];

            for (var i = 0; i < MaxCount; i++)
            {
                if (_fileHistory.Count == i)
                {
                    return;
                }

                var item = MenuItem(_fileHistory[i], i);
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
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine(e);
#endif
        }
    }
}