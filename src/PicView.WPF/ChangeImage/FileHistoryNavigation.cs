﻿using PicView.Core.Config;
using PicView.Core.Navigation;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PicView.Core.Extensions;

namespace PicView.WPF.ChangeImage;

internal static class FileHistoryNavigation
{
    private static FileHistory? _fileHistory;

    internal static void Add(string file)
    {
        _fileHistory ??= new FileHistory();
        _fileHistory.Add(file);
    }

    internal static void Remove(string file)
    {
        _fileHistory ??= new FileHistory();
        _fileHistory.Remove(file);
    }

    internal static void Rename(string oldPath, string newPath)
    {
        _fileHistory ??= new FileHistory();
        _fileHistory.Rename(oldPath, newPath);
    }

    internal static bool Contains(string file)
    {
        _fileHistory ??= new FileHistory();
        return _fileHistory.Contains(file);
    }

    internal static string GetLastFile()
    {
        _fileHistory ??= new FileHistory();
        return _fileHistory.GetLastFile() ?? string.Empty;
    }

    internal static async Task OpenLastFileAsync()
    {
        if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/recent.txt")) == false)
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ruben2776/PicView/Config/recent.txt")) == false)
            {
                return;
            }
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (UC.GetStartUpUC is not null)
            {
                UC.GetStartUpUC.ToggleMenu();
                UC.GetStartUpUC.Logo.Visibility = Visibility.Collapsed;
            }

            SetTitle.SetLoadingString();
            UC.GetSpinWaiter.Visibility = Visibility.Visible;
        }, DispatcherPriority.Normal);

        _fileHistory ??= new FileHistory();
        string? file;
        file = _fileHistory.Contains(SettingsHelper.Settings.StartUp.LastFile) ?
            SettingsHelper.Settings.StartUp.LastFile : _fileHistory.GetLastFile();

        if (file is null)
        {
            if (ErrorHandling.CheckOutOfRange())
            {
                ErrorHandling.Unload(true);
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    UC.GetStartUpUC?.ShowMenuAndLogo();
                });
            }

            return;
        }

        await LoadPic.LoadPicFromStringAsync(file).ConfigureAwait(false);
    }

    internal static async Task NextAsync()
    {
        if (Navigation.Pics.Count <= 0)
        {
            await OpenLastFileAsync().ConfigureAwait(false);
            return;
        }

        _fileHistory ??= new FileHistory();
        var file = await Task.FromResult(_fileHistory.GetNextEntry(SettingsHelper.Settings.UIProperties.Looping, Navigation.FolderIndex, Navigation.Pics)).ConfigureAwait(false);

        if (Navigation.Pics.Contains(file))
        {
            if (file == Navigation.Pics[Navigation.FolderIndex])
            {
                return;
            }
            // If the gallery is open, deselect current index
            if (UC.GetPicGallery is not null)
            {
                await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
                {
                    UC.GetPicGallery.Scroller.CanContentScroll = true; // Disable animations
                    // Deselect current item
                    GalleryNavigation.SetSelected(GalleryNavigation.SelectedGalleryItem, false);
                    GalleryNavigation.SetSelected(Navigation.FolderIndex, false);
                });
            }
            await LoadPic.LoadPicAtIndexAsync(Navigation.Pics.IndexOf(file)).ConfigureAwait(false);
            return;
        }

        await LoadPic.LoadPicFromStringAsync(file).ConfigureAwait(false);
    }

    internal static async Task PrevAsync()
    {
        if (Navigation.Pics.Count <= 0)
        {
            await OpenLastFileAsync().ConfigureAwait(false);
            return;
        }

        _fileHistory ??= new FileHistory();
        var file = await Task.FromResult(_fileHistory.GetPreviousEntry(SettingsHelper.Settings.UIProperties.Looping, Navigation.FolderIndex, Navigation.Pics)).ConfigureAwait(false);

        if (Navigation.Pics.Contains(file))
        {
            if (file == Navigation.Pics[Navigation.FolderIndex])
            {
                return;
            }
            // If the gallery is open, deselect current index
            if (UC.GetPicGallery is not null)
            {
                await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
                {
                    UC.GetPicGallery.Scroller.CanContentScroll = true; // Disable animations
                    // Deselect current item
                    GalleryNavigation.SetSelected(GalleryNavigation.SelectedGalleryItem, false);
                    GalleryNavigation.SetSelected(Navigation.FolderIndex, false);
                });
            }
            await LoadPic.LoadPicAtIndexAsync(Navigation.Pics.IndexOf(file)).ConfigureAwait(false);
            return;
        }

        await LoadPic.LoadPicFromStringAsync(file).ConfigureAwait(false);
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
        menuItem.MouseLeave += (_, _) =>
            menuItem.Foreground = (SolidColorBrush)Application.Current?.Resources["IconColorBrush"] ?? Brushes.WhiteSmoke;

        menuItem.Click += async (_, _) =>
            await LoadPic.LoadPicFromStringAsync(menuItem.ToolTip.ToString()).ConfigureAwait(false);
        var ext = Path.GetExtension(filePath);
        var ext5 = !string.IsNullOrWhiteSpace(ext) && ext.Length >= 5 ? ext[..5] : ext;
        menuItem.InputGestureText = ext5;
        return menuItem;
    }

    internal static void RefreshRecentItemsMenu()
    {
        _fileHistory ??= new FileHistory();
        try
        {
            var cm = (MenuItem)ConfigureWindows.MainContextMenu?.Items[6]!;

            for (var i = 0; i < FileHistory.MaxCount; i++)
            {
                if (_fileHistory.GetCount() == i)
                {
                    return;
                }

                var item = MenuItem(_fileHistory.GetEntryAt(i), i);
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

    internal static void WriteToFile()
    {
        _fileHistory ??= new FileHistory();
        _fileHistory.WriteToFile();
    }
}