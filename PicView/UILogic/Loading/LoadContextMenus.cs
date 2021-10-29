using PicView.ImageHandling;
using PicView.PicGallery;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.Copy_Paste;
using static PicView.FileHandling.DeleteFiles;
using static PicView.FileHandling.Open_Save;
using static PicView.Library.Resources.SvgIcons;
using static PicView.SystemIntegration.Wallpaper;
using static PicView.UILogic.ConfigureWindows;

namespace PicView.UILogic.Loading
{
    internal static class LoadContextMenus
    {
        internal static void AddContextMenus()
        {
            // Add main contextmenu
            MainContextMenu = new ContextMenu();
            var scbf = (SolidColorBrush)Application.Current.Resources["MainColorFadedBrush"];

            ///////////////////////////
            ///////////////////////////
            ///     Open           \\\\
            ///////////////////////////
            ///////////////////////////
            var opencm = new MenuItem
            {
                Header = Application.Current.Resources["Open"],
                InputGestureText = $"{Application.Current.Resources["Ctrl"]} + O"
            };
            var opencmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconFile),
                Stretch = Stretch.Fill
            };
            opencmIcon.Width = opencmIcon.Height = 12;
            opencmIcon.Fill = scbf;
            opencm.Icon = opencmIcon;
            opencm.Click += async (_, _) => await OpenAsync().ConfigureAwait(false);
            MainContextMenu.Items.Add(opencm);

            ///////////////////////////
            ///////////////////////////
            ///     Save           \\\\
            ///////////////////////////
            ///////////////////////////
            var savecm = new MenuItem()
            {
                Header = Application.Current.Resources["Save"],
                InputGestureText = $"{Application.Current.Resources["Ctrl"]} + S"
            };
            var savecmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconSave),
                Stretch = Stretch.Fill
            };
            savecmIcon.Width = savecmIcon.Height = 12;
            savecmIcon.Fill = scbf;
            savecm.Icon = savecmIcon;
            savecm.Click += async (s, x) => await SaveFilesAsync().ConfigureAwait(false);
            MainContextMenu.Items.Add(savecm);

            ///////////////////////////
            ///////////////////////////
            ///     Print          \\\\
            ///////////////////////////
            ///////////////////////////
            var printcm = new MenuItem
            {
                Header = Application.Current.Resources["Print"],
                InputGestureText = $"{Application.Current.Resources["Ctrl"]} + P"
            };
            var printcmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconPrint),
                Stretch = Stretch.Fill
            };
            printcmIcon.Width = printcmIcon.Height = 12;
            printcmIcon.Fill = scbf;
            printcm.Icon = printcmIcon;
            printcm.Click += (s, x) => Print(Pics[FolderIndex]);
            MainContextMenu.Items.Add(printcm);

            ///////////////////////////
            ///////////////////////////
            ///     Open With      \\\\
            ///////////////////////////
            ///////////////////////////
            var openwcm = new MenuItem
            {
                Header = Application.Current.Resources["OpenWith"],
                InputGestureText = "E"
            };
            var openwIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconSend),
                Stretch = Stretch.Fill
            };
            openwIcon.Width = openwIcon.Height = 12;
            openwIcon.Fill = scbf;
            openwcm.Icon = openwIcon;
            openwcm.Click += (_, _) => OpenWith(Pics[FolderIndex]);
            MainContextMenu.Items.Add(openwcm);

            ///////////////////////////
            ///////////////////////////
            ///     Recent Files   \\\\
            ///////////////////////////
            ///////////////////////////
            MainContextMenu.Items.Add(new Separator());
            var recentcm = new MenuItem
            {
                Header = Application.Current.Resources["RecentFiles"],
            };
            var recentcmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconPaper),
                Stretch = Stretch.Fill
            };
            recentcmIcon.Width = recentcmIcon.Height = 12;
            recentcmIcon.Fill = scbf;
            recentcm.Icon = recentcmIcon;
            MainContextMenu.Items.Add(recentcm);

            ///////////////////////////
            ///////////////////////////
            ///     Sort Files     \\\\
            ///////////////////////////
            ///////////////////////////
            var sortcm = new MenuItem
            {
                Header = Application.Current.Resources["SortFilesBy"],
            };
            var sortcmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconSort),
                Stretch = Stretch.Fill
            };
            sortcmIcon.Width = sortcmIcon.Height = 12;
            sortcmIcon.Fill = scbf;
            sortcm.Icon = sortcmIcon;

            ///////////////////////////
            ///   File Name        \\\\
            ///////////////////////////
            var sortcmChild0 = new MenuItem();
            var sortcmChild0Header = new RadioButton
            {
                Content = Application.Current.Resources["FileName"],
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 0
            };
            sortcmChild0Header.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(0).ConfigureAwait(false); };
            sortcmChild0.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(0).ConfigureAwait(false); };
            sortcmChild0.Header = sortcmChild0Header;

            if (Properties.Settings.Default.DarkTheme == false)
            {
                sortcmChild0Header.MouseEnter += (_, _) => sortcmChild0Header.Foreground = new SolidColorBrush(Colors.White);
                sortcmChild0.MouseEnter += (_, _) => sortcmChild0.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                sortcmChild0.MouseLeave += (_, _) => sortcmChild0.Foreground = txt;
                sortcmChild0Header.MouseLeave += (_, _) => sortcmChild0Header.Foreground = txt;
            }

            sortcm.Items.Add(sortcmChild0);

            ///////////////////////////
            ///   File Size        \\\\
            ///////////////////////////
            var sortcmChild1 = new MenuItem();
            var sortcmChild1Header = new RadioButton
            {
                Content = Application.Current.Resources["FileSize"],
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 1
            };
            sortcmChild1Header.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(1).ConfigureAwait(false); };
            sortcmChild1.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(1).ConfigureAwait(false); };
            sortcmChild1.Header = sortcmChild1Header;

            if (Properties.Settings.Default.DarkTheme == false)
            {
                sortcmChild1Header.MouseEnter += (_, _) => sortcmChild1Header.Foreground = new SolidColorBrush(Colors.White);
                sortcmChild1.MouseEnter += (_, _) => sortcmChild1.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                sortcmChild1.MouseLeave += (_, _) => sortcmChild1.Foreground = txt;
                sortcmChild1Header.MouseLeave += (_, _) => sortcmChild1Header.Foreground = txt;
            }

            sortcm.Items.Add(sortcmChild1);

            ///////////////////////////
            ///   Creation Time     \\\\
            ///////////////////////////
            var sortcmChild2 = new MenuItem();
            var sortcmChild2Header = new RadioButton
            {
                Content = Application.Current.Resources["CreationTime"],
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 2
            };
            sortcmChild2Header.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(2).ConfigureAwait(false); };
            sortcmChild2.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(2).ConfigureAwait(false); };
            sortcmChild2.Header = sortcmChild2Header;

            if (Properties.Settings.Default.DarkTheme == false)
            {
                sortcmChild2Header.MouseEnter += (_, _) => sortcmChild2Header.Foreground = new SolidColorBrush(Colors.White);
                sortcmChild2.MouseEnter += (_, _) => sortcmChild2.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                sortcmChild2.MouseLeave += (_, _) => sortcmChild2.Foreground = txt;
                sortcmChild2Header.MouseLeave += (_, _) => sortcmChild2Header.Foreground = txt;
            }

            sortcm.Items.Add(sortcmChild2);

            ///////////////////////////
            ///   File extension   \\\\
            ///////////////////////////
            var sortcmChild3 = new MenuItem();
            var sortcmChild3Header = new RadioButton
            {
                Content = Application.Current.Resources["FileExtension"],
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 3
            };
            sortcmChild3Header.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(3).ConfigureAwait(false); };
            sortcmChild3.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(3).ConfigureAwait(false); };
            sortcmChild3.Header = sortcmChild3Header;

            if (Properties.Settings.Default.DarkTheme == false)
            {
                sortcmChild3Header.MouseEnter += (_, _) => sortcmChild3Header.Foreground = new SolidColorBrush(Colors.White);
                sortcmChild3.MouseEnter += (_, _) => sortcmChild3.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                sortcmChild3.MouseLeave += (_, _) => sortcmChild3.Foreground = txt;
                sortcmChild3Header.MouseLeave += (_, _) => sortcmChild3Header.Foreground = txt;
            }

            sortcm.Items.Add(sortcmChild3);

            ///////////////////////////
            ///   Last Access Time \\\\
            ///////////////////////////
            var sortcmChild4 = new MenuItem();
            var sortcmChild4Header = new RadioButton
            {
                Content = Application.Current.Resources["LastAccessTime"],
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 4
            };
            sortcmChild4Header.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(4).ConfigureAwait(false); };
            sortcmChild4.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(4).ConfigureAwait(false); };
            sortcmChild4.Header = sortcmChild4Header;

            if (Properties.Settings.Default.DarkTheme == false)
            {
                sortcmChild4Header.MouseEnter += (_, _) => sortcmChild4Header.Foreground = new SolidColorBrush(Colors.White);
                sortcmChild4.MouseEnter += (_, _) => sortcmChild4.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                sortcmChild4.MouseLeave += (_, _) => sortcmChild4.Foreground = txt;
                sortcmChild4Header.MouseLeave += (_, _) => sortcmChild4Header.Foreground = txt;
            }

            sortcm.Items.Add(sortcmChild4);

            ///////////////////////////
            ///   Last Write Time  \\\\
            ///////////////////////////
            var sortcmChild5 = new MenuItem();
            var sortcmChild5Header = new RadioButton
            {
                Content = Application.Current.Resources["LastWriteTime"],
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 5
            };
            sortcmChild5Header.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(5).ConfigureAwait(false); };
            sortcmChild5.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(5).ConfigureAwait(false); };
            sortcmChild5.Header = sortcmChild5Header;

            if (Properties.Settings.Default.DarkTheme == false)
            {
                sortcmChild5Header.MouseEnter += (_, _) => sortcmChild5Header.Foreground = new SolidColorBrush(Colors.White);
                sortcmChild5.MouseEnter += (_, _) => sortcmChild5.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                sortcmChild5.MouseLeave += (_, _) => sortcmChild5.Foreground = txt;
                sortcmChild5Header.MouseLeave += (_, _) => sortcmChild5Header.Foreground = txt;
            }

            sortcm.Items.Add(sortcmChild5);

            ///////////////////////////
            ///   Random        \\\\
            ///////////////////////////
            var sortcmChild6 = new MenuItem();
            var sortcmChild6Header = new RadioButton
            {
                Content = Application.Current.Resources["Random"],
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 6
            };
            sortcmChild6Header.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(6).ConfigureAwait(false); };
            sortcmChild6.Click += async delegate { MainContextMenu.IsOpen = false; await ConfigureSettings.UpdateUIValues.ChangeSortingAsync(6).ConfigureAwait(false); };
            sortcmChild6.Header = sortcmChild6Header;
            sortcm.Items.Add(sortcmChild6);

            if (Properties.Settings.Default.DarkTheme == false)
            {
                sortcmChild6Header.MouseEnter += (_, _) => sortcmChild6Header.Foreground = new SolidColorBrush(Colors.White);
                sortcmChild6.MouseEnter += (_, _) => sortcmChild6.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                sortcmChild6.MouseLeave += (_, _) => sortcmChild6.Foreground = txt;
                sortcmChild6Header.MouseLeave += (_, _) => sortcmChild6Header.Foreground = txt;
            }

            MainContextMenu.Items.Add(sortcm);

            ///////////////////////////
            ///////////////////////////
            ///     Settings       \\\\
            ///////////////////////////
            ///////////////////////////
            var settingscm = new MenuItem
            {
                Header = Application.Current.Resources["Settings"],
            };
            var settingscmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconWrench),
                Stretch = Stretch.Fill
            };
            settingscmIcon.Width = settingscmIcon.Height = 12;
            settingscmIcon.Fill = scbf;
            settingscm.Icon = settingscmIcon;
            MainContextMenu.Items.Add(settingscm);

            ///////////////////////////
            ///   Looping          \\\\
            ///////////////////////////
            var settingscmLoop = new MenuItem
            {
                InputGestureText = "L"
            };
            var settingscmLoopHeader = new CheckBox
            {
                IsChecked = Properties.Settings.Default.Looping,
                Content = Application.Current.Resources["Looping"],
                FontSize = 13,
                MinWidth = 125,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = double.NaN,
                Height = double.NaN
            };
            settingscmLoop.Header = settingscmLoopHeader;
            settingscmLoop.Click += (_, _) => ConfigureSettings.UpdateUIValues.SetLooping();
            settingscmLoopHeader.Click += (_, _) => ConfigureSettings.UpdateUIValues.SetLooping();

            if (Properties.Settings.Default.DarkTheme == false)
            {
                settingscmLoopHeader.MouseEnter += (_, _) => settingscmLoopHeader.Foreground = new SolidColorBrush(Colors.White);
                settingscmLoop.MouseEnter += (_, _) => settingscmLoop.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                settingscmLoop.MouseLeave += (_, _) => settingscmLoop.Foreground = txt;
                settingscmLoopHeader.MouseLeave += (_, _) => settingscmLoopHeader.Foreground = txt;
            }

            settingscm.Items.Add(settingscmLoop);

            ///////////////////////////
            ///   Scroll         \\\\
            ///////////////////////////
            var settingscmScroll = new MenuItem
            {
                InputGestureText = "X"
            };
            var settingscmScrollHeader = new CheckBox
            {
                IsChecked = Properties.Settings.Default.ScrollEnabled,
                Content = Application.Current.Resources["Scrolling"],
                FontSize = 13,
                MinWidth = 125,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = double.NaN,
                Height = double.NaN
            };
            settingscmScrollHeader.Click += ConfigureSettings.UpdateUIValues.SetScrolling;
            settingscmScroll.Header = settingscmScrollHeader;
            settingscmScroll.Click += (s, x) => { ConfigureSettings.UpdateUIValues.SetScrolling(s, x); settingscmScrollHeader.IsChecked = !(bool)settingscmScrollHeader.IsChecked; };

            if (Properties.Settings.Default.DarkTheme == false)
            {
                settingscmScrollHeader.MouseEnter += (_, _) => settingscmScrollHeader.Foreground = new SolidColorBrush(Colors.White);
                settingscmScroll.MouseEnter += (_, _) => settingscmScroll.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                settingscmScroll.MouseLeave += (_, _) => settingscmScroll.Foreground = txt;
                settingscmScrollHeader.MouseLeave += (_, _) => settingscmScrollHeader.Foreground = txt;
            }

            settingscm.Items.Add(settingscmScroll);

            ///////////////////////////
            ///   Alt interface    \\\\
            ///////////////////////////
            var altcm = new MenuItem
            {
                InputGestureText = "Alt + Z"
            };
            var altcmHeader = new CheckBox
            {
                Content = Application.Current.Resources["ShowHideUI"],
                IsChecked = Properties.Settings.Default.ShowInterface,
                FontSize = 13,
                MinWidth = 125,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = double.NaN,
                Height = double.NaN
            };
            altcmHeader.Click += delegate
            {
                if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen)
                {
                    altcmHeader.IsChecked = Properties.Settings.Default.ShowInterface;
                    return;
                }
                HideInterfaceLogic.ToggleInterface();
            };
            altcm.Header = altcmHeader;
            altcm.Click += delegate { HideInterfaceLogic.ToggleInterface(); };
            altcmHeader.Click += delegate { HideInterfaceLogic.ToggleInterface(); };

            if (Properties.Settings.Default.DarkTheme == false)
            {
                altcmHeader.MouseEnter += (_, _) => altcmHeader.Foreground = new SolidColorBrush(Colors.White);
                altcm.MouseEnter += (_, _) => altcm.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                altcm.MouseLeave += (_, _) => altcm.Foreground = txt;
                altcmHeader.MouseLeave += (_, _) => altcmHeader.Foreground = txt;
            }

            settingscm.Items.Add(altcm);

            ///////////////////////////
            ///   Transparent bg   \\\\
            ///////////////////////////
            var transcm = new MenuItem
            {
                InputGestureText = "T"
            };
            var transcmHeader = new Button
            {
                Content = Application.Current.Resources["ChangeBg"],
                ToolTip = Application.Current.Resources["ChangeBgTooltip"],
                FontSize = 13,
                Width = double.NaN,
                Height = double.NaN,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            transcm.Header = transcmHeader;
            transcmHeader.Click += ConfigureSettings.ConfigColors.ChangeBackground;
            transcm.Click += ConfigureSettings.ConfigColors.ChangeBackground;

            if (Properties.Settings.Default.DarkTheme == false)
            {
                transcmHeader.MouseEnter += (_, _) => transcmHeader.Foreground = new SolidColorBrush(Colors.White);
                transcm.MouseEnter += (_, _) => transcm.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                transcm.MouseLeave += (_, _) => transcm.Foreground = txt;
                transcmHeader.MouseLeave += (_, _) => transcmHeader.Foreground = txt;
            }
            settingscm.Items.Add(transcm);

            ///////////////////////////
            /// Settings shortcut  \\\\
            ///////////////////////////
            settingscm.Items.Add(new Separator());
            var stsbtn = new Button()
            {
                Content = Application.Current.Resources["SettingsWindow"],
                ToolTip = Application.Current.Resources["ShowAllSettingsWindow"],
                FontSize = 13,
                Width = double.NaN,
                Height = double.NaN,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            stsbtn.Click += (_, _) => AllSettingsWindow();

            var settingscmShortcut = new MenuItem
            {
                Header = stsbtn,
                InputGestureText = "F4"
            };

            if (Properties.Settings.Default.DarkTheme == false)
            {
                stsbtn.MouseEnter += (_, _) => stsbtn.Foreground = new SolidColorBrush(Colors.White);
                settingscmShortcut.MouseEnter += (_, _) => settingscmShortcut.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                settingscmShortcut.MouseLeave += (_, _) => settingscmShortcut.Foreground = txt;
                stsbtn.MouseLeave += (_, _) => stsbtn.Foreground = txt;
            }

            stsbtn.Click += (_, _) => { AllSettingsWindow(); MainContextMenu.IsOpen = false; };
            settingscmShortcut.Click += (_, _) => { AllSettingsWindow(); MainContextMenu.IsOpen = false; };
            settingscm.Items.Add(settingscmShortcut);

            MainContextMenu.Items.Add(new Separator());

            ///////////////////////////
            ///////////////////////////
            ///  Set as            \\\\
            ///////////////////////////
            ///////////////////////////

            var setAsCm = new MenuItem
            {
                Header = Application.Current.Resources["SetAsWallpaper"],
            };

            var setAsCmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconCamera),
                Stretch = Stretch.Fill
            };
            setAsCmIcon.Width = setAsCmIcon.Height = 12;
            setAsCmIcon.Fill = scbf;
            setAsCm.Icon = setAsCmIcon;
            setAsCm.Click += async (_, _) => { MainContextMenu.IsOpen = false; await SetWallpaperAsync(WallpaperStyle.Fill).ConfigureAwait(false); };

            MainContextMenu.Items.Add(setAsCm);

            ///////////////////////////
            ///////////////////////////
            ///   Locate on disk   \\\\
            ///////////////////////////
            ///////////////////////////
            var lcdcm = new MenuItem
            {
                Header = Application.Current.Resources["ShowInFolder"],
                InputGestureText = "F3"
            };
            var lcdcmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconSearch),
                Stretch = Stretch.Fill
            };
            lcdcmIcon.Width = lcdcmIcon.Height = 12;
            lcdcmIcon.Fill = scbf;
            lcdcm.Icon = lcdcmIcon;
            lcdcm.Click += (s, x) => Open_In_Explorer();
            MainContextMenu.Items.Add(lcdcm);

            ///////////////////////////
            ///////////////////////////
            ///   Image Info       \\\\
            ///////////////////////////
            ///////////////////////////
            var fildecm = new MenuItem
            {
                Header = Application.Current.Resources["ImageInfo"],
            };
            var fildecmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconPaperDetails),
                Stretch = Stretch.Fill
            };
            fildecmIcon.Width = fildecmIcon.Height = 12;
            fildecmIcon.Fill = scbf;
            fildecm.Icon = fildecmIcon;
            fildecm.Click += (_, _) => ImageInfoWindow();
            MainContextMenu.Items.Add(fildecm);
            MainContextMenu.Items.Add(new Separator());

            ///////////////////////////
            ///////////////////////////
            ///   Copy             \\\\
            ///////////////////////////
            ///////////////////////////
            var cpm = new MenuItem
            {
                Header = Application.Current.Resources["Copy"],
            };

            var cpmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconCopy),
                Stretch = Stretch.Fill
            };
            cpmIcon.Width = cpmIcon.Height = 12;
            cpmIcon.Fill = scbf;
            cpm.Icon = cpmIcon;

            ///////////////////////////
            ///   Copy file        \\\\
            ///////////////////////////
            var cppcm = new MenuItem
            {
                ToolTip = Application.Current.Resources["CopyFile"] + $" [{Application.Current.Resources["Ctrl"]} + " +
                $"{Application.Current.Resources["Shift"]}  + C]",
            };
            var cppcmbtn = new Button()
            {
                Content = Application.Current.Resources["CopyFile"],
                ToolTip = Application.Current.Resources["CopyFile"] + $" [{Application.Current.Resources["Ctrl"]} + " +
                $"{Application.Current.Resources["Shift"]}  + C]",
                FontSize = 13,
                Width = double.NaN,
                Height = double.NaN,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            cppcm.Header = cppcmbtn;
            cppcmbtn.Click += (_, _) => Copyfile();
            cppcm.Click += (_, _) => Copyfile();

            if (Properties.Settings.Default.DarkTheme == false)
            {
                cppcmbtn.MouseEnter += (_, _) => cppcmbtn.Foreground = new SolidColorBrush(Colors.White);
                cppcm.MouseEnter += (_, _) => cppcm.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                cppcm.MouseLeave += (_, _) => cppcm.Foreground = txt;
                cppcmbtn.MouseLeave += (_, _) => cppcmbtn.Foreground = txt;
            }

            cpm.Items.Add(cppcm);

            ///////////////////////////
            ///   Copy base64      \\\\
            ///////////////////////////
            var cpxcmbtn = new Button()
            {
                Content = Application.Current.Resources["Copy"] + " base64",
                ToolTip = Application.Current.Resources["Copy"] + " base64"
                + $" [{Application.Current.Resources["Ctrl"]} + " +
                $"{Application.Current.Resources["Alt"]}  + C]",
                FontSize = 13,
                Width = double.NaN,
                Height = double.NaN,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var cpxcm = new MenuItem
            {
                Header = cpxcmbtn,
                ToolTip = Application.Current.Resources["Copy"] + " base64"
                    + $" [{Application.Current.Resources["Ctrl"]} + " +
                    $"{Application.Current.Resources["Alt"]}  + C]",
            };

            cpxcmbtn.Click += async delegate { MainContextMenu.IsOpen = false; await Base64.SendToClipboard().ConfigureAwait(false); };
            cpxcm.Click += async delegate { MainContextMenu.IsOpen = false; await Base64.SendToClipboard().ConfigureAwait(false); };

            if (Properties.Settings.Default.DarkTheme == false)
            {
                cpxcmbtn.MouseEnter += (_, _) => cpxcmbtn.Foreground = new SolidColorBrush(Colors.White);
                cpxcm.MouseEnter += (_, _) => cpxcm.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                cpxcm.MouseLeave += (_, _) => cpxcm.Foreground = txt;
                cpxcmbtn.MouseLeave += (_, _) => cpxcmbtn.Foreground = txt;
            }
            cpm.Items.Add(cpxcm);

            ///////////////////////////
            ///   Copy bitmap      \\\\
            ///////////////////////////

            var cpxbmbtn = new Button()
            {
                Content = Application.Current.Resources["CopyImage"],
                ToolTip = Application.Current.Resources["CopyImageTooltip"]
                + $" [{Application.Current.Resources["Ctrl"]}  + C]",
                FontSize = 13,
                Width = double.NaN,
                Height = double.NaN,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var cpxbm = new MenuItem
            {
                Header = cpxbmbtn,
                ToolTip = Application.Current.Resources["CopyImageTooltip"]
                + $" [{Application.Current.Resources["Ctrl"]}  + C]",
            };
            cpxbm.Click += (_, _) => { CopyBitmap(); MainContextMenu.IsOpen = false; };
            cpxbmbtn.Click += (_, _) => { CopyBitmap(); MainContextMenu.IsOpen = false; };

            if (Properties.Settings.Default.DarkTheme == false)
            {
                cpxbmbtn.MouseEnter += (_, _) => cpxbmbtn.Foreground = new SolidColorBrush(Colors.White);
                cpxbm.MouseEnter += (_, _) => cpxbm.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                cpxbm.MouseLeave += (_, _) => cpxbm.Foreground = txt;
                cpxbmbtn.MouseLeave += (_, _) => cpxbmbtn.Foreground = txt;
            }

            cpm.Items.Add(cpxbm);

            ///////////////////////////
            ///   Copy file path   \\\\
            ///////////////////////////

            var cppfmbtn = new Button()
            {
                Content = Application.Current.Resources["FileCopyPath"],
                FontSize = 13,
                Width = double.NaN,
                Height = double.NaN,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var cppfm = new MenuItem
            {
                Header = cppfmbtn,
            };
            cppfm.Click += (_, _) => { CopyText(); MainContextMenu.IsOpen = false; };
            cppfmbtn.Click += (_, _) => { CopyText(); MainContextMenu.IsOpen = false; };

            if (Properties.Settings.Default.DarkTheme == false)
            {
                cppfmbtn.MouseEnter += (_, _) => cppfmbtn.Foreground = new SolidColorBrush(Colors.White);
                cppfm.MouseEnter += (_, _) => cppfm.Foreground = new SolidColorBrush(Colors.White);

                var txt = (SolidColorBrush)Application.Current.Resources["MainColorBrush"];
                cppfm.MouseLeave += (_, _) => cppfm.Foreground = txt;
                cppfmbtn.MouseLeave += (_, _) => cppfmbtn.Foreground = txt;
            }

            cpm.Items.Add(cppfm);

            MainContextMenu.Items.Add(cpm);

            ///////////////////////////
            ///////////////////////////
            ///   Cut File         \\\\
            ///////////////////////////
            ///////////////////////////
            var cpccm = new MenuItem
            {
                Header = Application.Current.Resources["FileCut"],
                InputGestureText = $"{Application.Current.Resources["Ctrl"]} + X"
            };
            var cpccmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconScissor),
                Stretch = Stretch.Fill
            };
            cpccmIcon.Width = cpccmIcon.Height = 12;
            cpccmIcon.Fill = scbf;
            cpccm.Icon = cpccmIcon;
            cpccm.Click += (_, _) => { Cut(Pics[FolderIndex]); MainContextMenu.IsOpen = false; };
            MainContextMenu.Items.Add(cpccm);

            ///////////////////////////
            ///////////////////////////
            ///   Paste File       \\\\
            ///////////////////////////
            ///////////////////////////
            var pastecm = new MenuItem
            {
                Header = Application.Current.Resources["FilePaste"],
                InputGestureText = $"{Application.Current.Resources["Ctrl"]} + V"
            };
            var pastecmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconPaste),
                Stretch = Stretch.Fill,
                Width = 12,
                Height = 12,
                Fill = scbf
            };
            pastecm.Icon = pastecmIcon;
            pastecm.Click += async (_, _) => { MainContextMenu.IsOpen = false; Paste(); };
            MainContextMenu.Items.Add(pastecm);

            ///////////////////////////
            ///////////////////////////
            ///   Delete File      \\\\
            ///////////////////////////
            ///////////////////////////
            var MovetoRecycleBin = new MenuItem
            {
                Header = Application.Current.Resources["DeleteFile"],
                InputGestureText = (string)Application.Current.Resources["Delete"]
            };
            var MovetoRecycleBinIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconRecycle),
                Stretch = Stretch.Fill,
                Width = 12,
                Height = 12,
                Fill = scbf
            };
            MovetoRecycleBin.Icon = MovetoRecycleBinIcon;
            MovetoRecycleBin.Click += async (_, _) => await DeleteFileAsync(true);
            MainContextMenu.Items.Add(MovetoRecycleBin);

            ///////////////////////////
            ///////////////////////////
            ///   Close            \\\\
            ///////////////////////////
            ///////////////////////////
            MainContextMenu.Items.Add(new Separator());
            var clcm = new MenuItem
            {
                Header = Application.Current.Resources["Close"],
                InputGestureText = (string)Application.Current.Resources["Esc"],
                StaysOpenOnClick = false
            };
            var mclcmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconClose),
                Stretch = Stretch.Fill,
                Width = 12,
                Height = 12,
                Fill = scbf
            };
            clcm.Icon = mclcmIcon;
            clcm.Click += (s, x) => { MainContextMenu.Visibility = Visibility.Collapsed; SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow); };
            MainContextMenu.Items.Add(clcm);

            // Add to elements
            ConfigureWindows.GetMainWindow.ParentContainer.ContextMenu = MainContextMenu;
            MainContextMenu.Opened += (tt, yy) => ChangeImage.History.RefreshRecentItemsMenu();

#if DEBUG
            Trace.WriteLine("Contextmenu loaded");
#endif
        }
    }
}