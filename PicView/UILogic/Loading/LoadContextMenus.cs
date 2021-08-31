using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.SystemIntegration;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.Copy_Paste;
using static PicView.FileHandling.DeleteFiles;
using static PicView.FileHandling.Open_Save;
using static PicView.FileHandling.RecentFiles;
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
            cm = new ContextMenu();
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
            opencm.Click += (s, x) => Open();
            cm.Items.Add(opencm);

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
            savecm.Click += (s, x) => SaveFiles();
            cm.Items.Add(savecm);

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
            cm.Items.Add(printcm);

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
            openwcm.Click += (s, x) => OpenWith(Pics[FolderIndex]);
            cm.Items.Add(openwcm);

            ///////////////////////////
            ///////////////////////////
            ///     Recent Files   \\\\
            ///////////////////////////
            ///////////////////////////
            cm.Items.Add(new Separator());
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
            cm.Items.Add(recentcm);

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
            sortcmChild0Header.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(0); cm.IsOpen = false; };
            sortcmChild0.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(0); cm.IsOpen = false; };
            sortcmChild0.Header = sortcmChild0Header;
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
            sortcmChild1Header.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(1); cm.IsOpen = false; };
            sortcmChild1.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(1); cm.IsOpen = false; };
            sortcmChild1.Header = sortcmChild1Header;
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
            sortcmChild2Header.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(2); cm.IsOpen = false; };
            sortcmChild2.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(2); cm.IsOpen = false; };
            sortcmChild2.Header = sortcmChild2Header;
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
            sortcmChild3Header.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(3); cm.IsOpen = false; };
            sortcmChild3.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(3); cm.IsOpen = false; };
            sortcmChild3.Header = sortcmChild3Header;
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
            sortcmChild4Header.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(4); cm.IsOpen = false; };
            sortcmChild4.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(4); cm.IsOpen = false; };
            sortcmChild4.Header = sortcmChild4Header;
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
            sortcmChild5Header.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(5); cm.IsOpen = false; };
            sortcmChild5.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(5); cm.IsOpen = false; };
            sortcmChild5.Header = sortcmChild5Header;
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
            sortcmChild6Header.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(6); cm.IsOpen = false; };
            sortcmChild6.Click += delegate { ConfigureSettings.UpdateUIValues.ChangeSorting(6); cm.IsOpen = false; };
            sortcmChild6.Header = sortcmChild6Header;
            sortcm.Items.Add(sortcmChild6);
            cm.Items.Add(sortcm);

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
            cm.Items.Add(settingscm);

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
            settingscmLoop.Click += (s, x) => { ConfigureSettings.UpdateUIValues.SetLooping(s, x); };
            settingscmLoopHeader.Click += (s, x) => { ConfigureSettings.UpdateUIValues.SetLooping(s, x); };
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
                if (GalleryFunctions.IsOpen)
                {
                    altcmHeader.IsChecked = Properties.Settings.Default.ShowInterface;
                    return;
                }
                HideInterfaceLogic.ToggleInterface();
            };
            altcm.Header = altcmHeader;
            altcm.Click += delegate { HideInterfaceLogic.ToggleInterface(); };
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
            transcmHeader.Click += ConfigureSettings.ConfigColors.ChangeBackground;
            transcm.Header = transcmHeader;
            transcm.Click += ConfigureSettings.ConfigColors.ChangeBackground;
            settingscm.Items.Add(transcm);

            ///////////////////////////
            /// Settings shortcut  \\\\
            ///////////////////////////
            settingscm.Items.Add(new Separator());

            var settingscmShortcut = new MenuItem
            {
                Header = Application.Current.Resources["ShowAllSettingsWindow"],
                InputGestureText = "F4"
            };
            settingscmShortcut.Click += (_, _) => AllSettingsWindow();
            settingscm.Items.Add(settingscmShortcut);

            cm.Items.Add(new Separator());

            ///////////////////////////
            ///////////////////////////
            ///  Set as            \\\\
            ///////////////////////////
            ///////////////////////////

            var setAsCm = new MenuItem
            {
                Header = Application.Current.Resources["SetAs"],
            };

            var setAsCmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconCamera),
                Stretch = Stretch.Fill
            };
            setAsCmIcon.Width = setAsCmIcon.Height = 12;
            setAsCmIcon.Fill = scbf;
            setAsCm.Icon = setAsCmIcon;

            var wallcm = new MenuItem
            {
                Header = Application.Current.Resources["SetAsWallpaper"],
            };
            wallcm.Click += (s, x) => SetWallpaper(WallpaperStyle.Fill);
            var wallcmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconCamera),
                Stretch = Stretch.Fill
            };
            wallcmIcon.Width = wallcmIcon.Height = 12;
            wallcmIcon.Fill = scbf;
            wallcm.Icon = wallcmIcon;
            setAsCm.Items.Add(wallcm);

            var lockCm = new MenuItem
            {
                Header = Application.Current.Resources["SetAsLockScreenImage"],
            };
            lockCm.Click += async delegate { await Lockscreen.ChangeLockScreenBackground(Pics[FolderIndex]).ConfigureAwait(false); };
            var lockCmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconCamera),
                Stretch = Stretch.Fill
            };
            lockCmIcon.Width = lockCmIcon.Height = 12;
            lockCmIcon.Fill = scbf;
            lockCm.Icon = lockCmIcon;

            lockCm.IsEnabled = false;
            lockCm.Opacity = .5;

            setAsCm.Items.Add(lockCm);

            cm.Items.Add(setAsCm);

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
            cm.Items.Add(lcdcm);

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
            cm.Items.Add(fildecm);
            cm.Items.Add(new Separator());

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
                Header = Application.Current.Resources["CopyFile"],
                ToolTip = Application.Current.Resources["CopyFile"] + $" [{Application.Current.Resources["Ctrl"]} + " +
                $"{Application.Current.Resources["Shift"]}  + C]",
            };
            cppcm.Click += (s, x) => Copyfile();
            cpm.Items.Add(cppcm);

            ///////////////////////////
            ///   Copy base64      \\\\
            ///////////////////////////
            var cpxcm = new MenuItem
            {
                Header = Application.Current.Resources["Copy"] + " base64",
                ToolTip = Application.Current.Resources["Copy"] + " base64"
                + $" [{Application.Current.Resources["Ctrl"]} + " +
                $"{Application.Current.Resources["Alt"]}  + C]",
            };
            cpxcm.Click += async delegate { await Base64.SendToClipboard().ConfigureAwait(false); };
            cpm.Items.Add(cpxcm);

            ///////////////////////////
            ///   Copy bitmap      \\\\
            ///////////////////////////
            var cpxbm = new MenuItem
            {
                Header = Application.Current.Resources["CopyImage"],
                ToolTip = Application.Current.Resources["CopyImageTooltip"]
                + $" [{Application.Current.Resources["Ctrl"]}  + C]",
            };
            cpxbm.Click += (s, x) => CopyBitmap();
            cpm.Items.Add(cpxbm);

            ///////////////////////////
            ///   Copy file path   \\\\
            ///////////////////////////
            var cppfm = new MenuItem
            {
                Header = Application.Current.Resources["FileCopyPath"],
            };
            cppfm.Click += (s, x) => CopyText();
            cpm.Items.Add(cppfm);

            cm.Items.Add(cpm);

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
            cpccm.Click += (s, x) => Cut(Pics[FolderIndex]);
            cm.Items.Add(cpccm);

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
            pastecm.Click += (s, x) => Paste();
            cm.Items.Add(pastecm);

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
            MovetoRecycleBin.Click += delegate { DeleteFile(true); };
            cm.Items.Add(MovetoRecycleBin);

            ///////////////////////////
            ///////////////////////////
            ///   Close            \\\\
            ///////////////////////////
            ///////////////////////////
            cm.Items.Add(new Separator());
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
            clcm.Click += (s, x) => { cm.Visibility = Visibility.Collapsed; SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow); };
            cm.Items.Add(clcm);

            // Add to elements
            ConfigureWindows.GetMainWindow.MainImage.ContextMenu = ConfigureWindows.GetMainWindow.ParentContainer.ContextMenu = cm;
            cm.Opened += (tt, yy) => Recentcm_Opened(recentcm);

#if DEBUG
            Trace.WriteLine("Contextmenu loaded");
#endif
        }
    }
}