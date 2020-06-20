using PicView.ImageHandling;
using PicView.SystemIntegration;
using PicView.UI.PicGallery;
using PicView.UI.Sizing;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.FileHandling.Copy_Paste;
using static PicView.FileHandling.DeleteFiles;
using static PicView.FileHandling.Open_Save;
using static PicView.FileHandling.RecentFiles;
using static PicView.Library.Fields;
using static PicView.Library.Resources.SvgIcons;
using static PicView.Library.Utilities;
using static PicView.SystemIntegration.Wallpaper;

namespace PicView.UI.Loading
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
                Header = "Open",
                InputGestureText = "Ctrl + O"
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
                Header = "Save",
                InputGestureText = "Ctrl + S"
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
                Header = "Print",
                InputGestureText = "Ctrl + P"
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
                Header = "Open with...",
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
                Header = "Recent files"
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
                Header = "Sort files by"
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
                Content = "File name",
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 0
            };
            sortcmChild0Header.Click += delegate { UpdateUIValues.ChangeSorting(0); cm.IsOpen = false; };
            sortcmChild0.Click += delegate { UpdateUIValues.ChangeSorting(0); cm.IsOpen = false; };
            sortcmChild0.Header = sortcmChild0Header;
            sortcm.Items.Add(sortcmChild0);

            ///////////////////////////
            ///   File Size        \\\\
            ///////////////////////////
            var sortcmChild1 = new MenuItem();
            var sortcmChild1Header = new RadioButton
            {
                Content = "File Size",
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 1
            };
            sortcmChild1Header.Click += delegate { UpdateUIValues.ChangeSorting(1); cm.IsOpen = false; };
            sortcmChild1.Click += delegate { UpdateUIValues.ChangeSorting(1); cm.IsOpen = false; };
            sortcmChild1.Header = sortcmChild1Header;
            sortcm.Items.Add(sortcmChild1);

            ///////////////////////////
            ///   Creation Time     \\\\
            ///////////////////////////
            var sortcmChild2 = new MenuItem();
            var sortcmChild2Header = new RadioButton
            {
                Content = "Creation time",
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 2
            };
            sortcmChild2Header.Click += delegate { UpdateUIValues.ChangeSorting(2); cm.IsOpen = false; };
            sortcmChild2.Click += delegate { UpdateUIValues.ChangeSorting(2); cm.IsOpen = false; };
            sortcmChild2.Header = sortcmChild2Header;
            sortcm.Items.Add(sortcmChild2);

            ///////////////////////////
            ///   File extension   \\\\
            ///////////////////////////
            var sortcmChild3 = new MenuItem();
            var sortcmChild3Header = new RadioButton
            {
                Content = "File extension",
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 3
            };
            sortcmChild3Header.Click += delegate { UpdateUIValues.ChangeSorting(3); cm.IsOpen = false; };
            sortcmChild3.Click += delegate { UpdateUIValues.ChangeSorting(3); cm.IsOpen = false; };
            sortcmChild3.Header = sortcmChild3Header;
            sortcm.Items.Add(sortcmChild3);

            ///////////////////////////
            ///   Last Access Time \\\\
            ///////////////////////////
            var sortcmChild4 = new MenuItem();
            var sortcmChild4Header = new RadioButton
            {
                Content = "Last access time",
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 4
            };
            sortcmChild4Header.Click += delegate { UpdateUIValues.ChangeSorting(4); cm.IsOpen = false; };
            sortcmChild4.Click += delegate { UpdateUIValues.ChangeSorting(4); cm.IsOpen = false; };
            sortcmChild4.Header = sortcmChild4Header;
            sortcm.Items.Add(sortcmChild4);

            ///////////////////////////
            ///   Last Write Time  \\\\
            ///////////////////////////
            var sortcmChild5 = new MenuItem();
            var sortcmChild5Header = new RadioButton
            {
                Content = "Last write time",
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 5
            };
            sortcmChild5Header.Click += delegate { UpdateUIValues.ChangeSorting(5); cm.IsOpen = false; };
            sortcmChild5.Click += delegate { UpdateUIValues.ChangeSorting(5); cm.IsOpen = false; };
            sortcmChild5.Header = sortcmChild5Header;
            sortcm.Items.Add(sortcmChild5);

            ///////////////////////////
            ///   Random        \\\\
            ///////////////////////////
            var sortcmChild6 = new MenuItem();
            var sortcmChild6Header = new RadioButton
            {
                Content = "Random",
                BorderThickness = new Thickness(0, 0, 0, 0),
                MinWidth = 125,
                IsChecked = Properties.Settings.Default.SortPreference == 6
            };
            sortcmChild6Header.Click += delegate { UpdateUIValues.ChangeSorting(6); cm.IsOpen = false; };
            sortcmChild6.Click += delegate { UpdateUIValues.ChangeSorting(6); cm.IsOpen = false; };
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
                Header = "Setings"
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
                Content = "Looping",
                FontSize = 13,
                MinWidth = 125,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = double.NaN,
                Height = double.NaN
            };
            settingscmLoop.Header = settingscmLoopHeader;
            //var settingscmLoopIcon = new System.Windows.Shapes.Path
            //{
            //    Data = Geometry.Parse(InfiniteIconSVG),
            //    Stretch = Stretch.Fill,
            //    Width = 13,
            //    Height = 13,
            //    Fill = scbf
            //};
            //settingscmLoop.Icon = settingscmLoopIcon;
            settingscmLoop.Click += (s, x) => { UpdateUIValues.SetLooping(s, x); };
            settingscmLoopHeader.Click += (s, x) => { UpdateUIValues.SetLooping(s, x); };
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
                Content = "Scrolling",
                FontSize = 13,
                MinWidth = 125,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = double.NaN,
                Height = double.NaN
            };
            settingscmScrollHeader.Click += UpdateUIValues.SetScrolling;
            settingscmScroll.Header = settingscmScrollHeader;
            settingscmScroll.Click += (s, x) => { UpdateUIValues.SetScrolling(s, x); settingscmScrollHeader.IsChecked = (bool)settingscmScrollHeader.IsChecked ? false : true; };
            settingscm.Items.Add(settingscmScroll);

            ///////////////////////////
            ///   Auto fit         \\\\
            ///////////////////////////
            var fitcm = new MenuItem
            {
                InputGestureText = "1 || 2"
            };
            var fitcmHeader = new CheckBox
            {
                Content = "Fit to window/image",
                IsChecked = Properties.Settings.Default.AutoFitWindow,
                FontSize = 13,
                MinWidth = 125,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = double.NaN,
                Height = double.NaN
            };
            fitcmHeader.Click += delegate { WindowLogic.SetWindowBehaviour = WindowLogic.SetWindowBehaviour ? false : true; };
            fitcm.Header = fitcmHeader;
            fitcm.Click += delegate { WindowLogic.SetWindowBehaviour = WindowLogic.SetWindowBehaviour ? false : true; };
            settingscm.Items.Add(fitcm);

            ///////////////////////////
            ///   Alt interface    \\\\
            ///////////////////////////
            var altcm = new MenuItem
            {
                InputGestureText = "Alt + Z"
            };
            var altcmHeader = new CheckBox
            {
                Content = "Show/hide interface",
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
                Content = "Change background",
                ToolTip = "Change between background color for images with transparent background",
                FontSize = 13,
                Width = double.NaN,
                Height = double.NaN,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            transcmHeader.Click += ConfigColors.ChangeBackground;
            transcm.Header = transcmHeader;
            transcm.Click += ConfigColors.ChangeBackground;
            settingscm.Items.Add(transcm);

            cm.Items.Add(new Separator());

            ///////////////////////////
            ///////////////////////////
            ///  Set as Wallpaper  \\\\
            ///////////////////////////
            ///////////////////////////
            var wallcm = new MenuItem
            {
                Header = "Set as wallpaper"
            };
            wallcm.Click += (s, x) => SetWallpaper(Pics[FolderIndex], WallpaperStyle.Fill);
            var wallcmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconCamera),
                Stretch = Stretch.Fill
            };
            wallcmIcon.Width = wallcmIcon.Height = 12;
            wallcmIcon.Fill = scbf;
            wallcm.Icon = wallcmIcon;
            cm.Items.Add(wallcm);

            ///////////////////////////
            ///////////////////////////
            ///   Locate on disk   \\\\
            ///////////////////////////
            ///////////////////////////
            var lcdcm = new MenuItem
            {
                Header = "Locate on disk",
                InputGestureText = "F3",
                ToolTip = "Opens the current image on your drive"
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
            ///   File Details     \\\\
            ///////////////////////////
            ///////////////////////////
            var fildecm = new MenuItem
            {
                Header = "File Details",
                InputGestureText = "Ctrl + I"
            };
            var fildecmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconPaperDetails),
                Stretch = Stretch.Fill
            };
            fildecmIcon.Width = fildecmIcon.Height = 12;
            fildecmIcon.Fill = scbf;
            fildecm.Icon = fildecmIcon;
            fildecm.Click += (s, x) => NativeMethods.ShowFileProperties(Pics[FolderIndex]);
            cm.Items.Add(fildecm);
            cm.Items.Add(new Separator());

            ///////////////////////////
            ///////////////////////////
            ///   Copy             \\\\
            ///////////////////////////
            ///////////////////////////
            var cpm = new MenuItem
            {
                Header = "Copy",
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
                Header = "Copy file",
                ToolTip = "Copy file to clipboard holder [Ctrl + C]",
            };
            cppcm.Click += (s, x) => Copyfile();
            cpm.Items.Add(cppcm);

            ///////////////////////////
            ///   Copy base64      \\\\
            ///////////////////////////
            var cpxcm = new MenuItem
            {
                Header = "Copy base64",
                ToolTip = "Copy as base64 clipboard holder [Ctrl + Shift + C]",
            };
            cpxcm.Click += (s, x) => Base64.SendToClipboard();
            cpm.Items.Add(cpxcm);

            ///////////////////////////
            ///   Copy bitmap      \\\\
            ///////////////////////////
            var cpxbm = new MenuItem
            {
                Header = "Copy image",
                ToolTip = "Copy as Windows clipboard image [Ctrl + Alt + C]",
            };
            cpxbm.Click += (s, x) => Base64.SendToClipboard();
            cpm.Items.Add(cpxbm);

            ///////////////////////////
            ///   Copy file path   \\\\
            ///////////////////////////
            var cppfm = new MenuItem
            {
                Header = "Copy file path",
                ToolTip = "Copy file path to clipboard holder",
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
                Header = "Cut file",
                InputGestureText = "Ctrl + X"
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
                Header = "Paste file",
                InputGestureText = "Ctrl + V"
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
                Header = "Delete file",
                InputGestureText = "Delete"
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
            MovetoRecycleBin.Click += (s, x) => DeleteFile(Pics[FolderIndex], true);
            cm.Items.Add(MovetoRecycleBin);

            ///////////////////////////
            ///////////////////////////
            ///   Close            \\\\
            ///////////////////////////
            ///////////////////////////
            cm.Items.Add(new Separator());
            var clcm = new MenuItem
            {
                Header = "Close",
                InputGestureText = "Esc",
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
            clcm.Click += (s, x) => { cm.Visibility = Visibility.Collapsed; SystemCommands.CloseWindow(mainWindow); };
            cm.Items.Add(clcm);

            // Add to elements
            mainWindow.img.ContextMenu = mainWindow.bg.ContextMenu = mainWindow.LowerBar.ContextMenu = cm;

            // Add left and right ContextMenus
            //var cmLeft = new ContextMenu();
            //var cmRight = new ContextMenu();

            //var nextcm = new MenuItem
            //{
            //    Header = "Next picture",
            //    InputGestureText = "ᗌ or D",
            //    ToolTip = "Go to Next image",
            //    StaysOpenOnClick = true
            //};
            //nextcm.Click += (s, x) => Pic();
            //cmRight.Items.Add(nextcm);

            //var prevcm = new MenuItem
            //{
            //    Header = "Previous picture",
            //    InputGestureText = "ᗏ or A",
            //    ToolTip = "Go to previous image in folder",
            //    StaysOpenOnClick = true
            //};
            //prevcm.Click += (s, x) => Pic(false);
            //cmLeft.Items.Add(prevcm);

            //var firstcm = new MenuItem
            //{
            //    Header = "First picture",
            //    InputGestureText = "Ctrl + D or Ctrl + ᗌ",
            //    ToolTip = "Go to first image in folder"
            //};
            //firstcm.Click += (s, x) => Pic(false, true);
            //cmLeft.Items.Add(firstcm);

            //var lastcm = new MenuItem
            //{
            //    Header = "Last picture",
            //    InputGestureText = "Ctrl + A or Ctrl + ᗏ",
            //    ToolTip = "Go to last image in folder"
            //};
            //lastcm.Click += (s, x) => Pic(true, true);
            //cmRight.Items.Add(lastcm);

            //// Add to elements
            //mainWindow.RightButton.ContextMenu = cmRight;
            //mainWindow.LeftButton.ContextMenu = cmLeft;

            //clickArrowRight.ContextMenu = cmRight;
            //clickArrowLeft.ContextMenu = cmLeft;

            // Add Title contextMenu
            //var cmTitle = new ContextMenu();

            //var clTc = new MenuItem
            //{
            //    Header = "Copy path to clipboard"
            //};
            //clTc.Click += (s, x) => CopyText();
            //cmTitle.Items.Add(clTc);

            //mainWindow.Bar.ContextMenu = cmTitle;

            switch (Properties.Settings.Default.SortPreference)
            {
                case 0:
                    sortcmChild0.IsChecked = true;
                    break;

                case 1:
                    sortcmChild1.IsChecked = true;
                    break;

                case 2:
                    sortcmChild2.IsChecked = true;
                    break;

                case 3:
                    sortcmChild3.IsChecked = true;
                    break;

                case 4:
                    sortcmChild4.IsChecked = true;
                    break;

                case 5:
                    sortcmChild5.IsChecked = true;
                    break;

                case 6:
                    sortcmChild6.IsChecked = true;
                    break;

                default:
                    sortcmChild0.IsChecked = true;
                    break;
            }

            cm.Opened += (tt, yy) => Recentcm_MouseEnter(recentcm);

#if DEBUG
            Trace.WriteLine("Contextmenu loaded");
#endif
        }
    }
}