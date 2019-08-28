using PicView.Native;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.Copy_Paste;
using static PicView.DeleteFiles;
using static PicView.Fields;
using static PicView.FileLists;
using static PicView.Helper;
using static PicView.Open_Save;
using static PicView.RecentFiles;
using static PicView.SvgIcons;
using static PicView.Wallpaper;

namespace PicView
{
    internal static class ContextMenus
    {
        internal static void AddContextMenus()
        {
            #region Add ContextMenus

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
            printcm.Click += (s, x) => Print(PicPath);
            cm.Items.Add(printcm);


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
            var sortcmChild0 = new RadioButton
            {
                Content = "File name"
            };
            sortcmChild0.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 0;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild0);

            ///////////////////////////
            ///   File Size        \\\\
            ///////////////////////////
            var sortcmChild1 = new RadioButton
            {
                Content = "File Size"
            };
            sortcmChild1.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 1;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild1);

            ///////////////////////////
            ///   Creatin Time     \\\\
            ///////////////////////////
            var sortcmChild2 = new RadioButton
            {
                Content = "Creation time"
            };
            sortcmChild2.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 2;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild2);

            ///////////////////////////
            ///   File extension   \\\\
            ///////////////////////////
            var sortcmChild3 = new RadioButton
            {
                Content = "File extension"
            };
            sortcmChild3.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 3;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild3);

            ///////////////////////////
            ///   Last Access Time \\\\
            ///////////////////////////
            var sortcmChild4 = new RadioButton
            {
                Content = "Last access time"
            };
            sortcmChild4.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 4;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild4);

            ///////////////////////////
            ///   Last Write Time  \\\\
            ///////////////////////////
            var sortcmChild5 = new RadioButton
            {
                Content = "Last write time"
            };
            sortcmChild5.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 5;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild5);

            ///////////////////////////
            ///   Random        \\\\
            ///////////////////////////
            var sortcmChild6 = new RadioButton
            {
                Content = "Random"
            };
            sortcmChild6.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 6;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
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
            cm.Items.Add(settingscm);

            ///////////////////////////
            ///   Looping          \\\\
            ///////////////////////////
            var settingscmLoop = new MenuItem
            {
                InputGestureText = "L"
            };
            var settingscmLoopHeader = new CheckBox {
                IsChecked = Properties.Settings.Default.Looping,
                Content = "Looping",
                FontSize = 13,
                Width = double.NaN,
                Height = double.NaN
            };            
            settingscmLoopHeader.Style = Application.Current.FindResource("Checkstyle") as Style;
            settingscmLoopHeader.HorizontalAlignment = HorizontalAlignment.Left;
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
            settingscmLoop.Click += (s, x) => { Configs.SetLooping(s, x); settingscmLoopHeader.IsChecked = (bool)settingscmLoopHeader.IsChecked ? false : true; };
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
                Width = double.NaN,
                Height = double.NaN
            };
            settingscmScrollHeader.Click += Configs.SetScrolling;
            settingscmScrollHeader.Style = Application.Current.FindResource("Checkstyle") as Style;
            settingscmScrollHeader.FontSize = 13;
            settingscmScroll.Header = settingscmScrollHeader;
            settingscmScroll.Click += (s, x) => { Configs.SetScrolling(s, x); settingscmScrollHeader.IsChecked = (bool)settingscmScrollHeader.IsChecked ? false : true; };
            settingscm.Items.Add(settingscmScroll);
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
            wallcm.Click += (s, x) => SetWallpaper(PicPath, WallpaperStyle.Fill);
            var wallcmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconCamera),
                Stretch = Stretch.Fill
            };
            wallcmIcon.Width = wallcmIcon.Height = 12;
            wallcmIcon.Fill = scbf;
            wallcm.Icon = wallcmIcon;
            cm.Items.Add(wallcm);
            cm.Items.Add(new Separator());



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
            fildecm.Click += (s, x) => NativeMethods.ShowFileProperties(PicPath);
            cm.Items.Add(fildecm);
            cm.Items.Add(new Separator());

            ///////////////////////////
            ///////////////////////////
            ///   Copy Picture     \\\\
            ///////////////////////////
            ///////////////////////////
            var cppcm = new MenuItem
            {
                Header = "Copy picture",
                InputGestureText = "Ctrl + C"
            };
            var cppcmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(SVGiconCopy),
                Stretch = Stretch.Fill
            };
            cppcmIcon.Width = cppcmIcon.Height = 12;
            cppcmIcon.Fill = scbf;
            cppcm.Icon = cppcmIcon;
            cppcm.Click += (s, x) => CopyPic();
            cm.Items.Add(cppcm);

            ///////////////////////////
            ///////////////////////////
            ///   Cut Picture      \\\\
            ///////////////////////////
            ///////////////////////////
            var cpccm = new MenuItem
            {
                Header = "Cut picture",
                InputGestureText = "Ctrl + X"
            };
            var cpccmIcon = new System.Windows.Shapes.Path();
            cpccmIcon.Data = Geometry.Parse(SVGiconScissor);
            cpccmIcon.Stretch = Stretch.Fill;
            cpccmIcon.Width = cpccmIcon.Height = 12;
            cpccmIcon.Fill = scbf;
            cpccm.Icon = cpccmIcon;
            cpccm.Click += (s, x) => Cut(PicPath);
            cm.Items.Add(cpccm);

            ///////////////////////////
            ///////////////////////////
            ///   Paste Picture    \\\\
            ///////////////////////////
            ///////////////////////////
            var pastecm = new MenuItem
            {
                Header = "Paste picture",
                InputGestureText = "Ctrl + V"
            };
            var pastecmIcon = new System.Windows.Shapes.Path();
            pastecmIcon.Data = Geometry.Parse("M768 1664h896v-640h-416q-40 0-68-28t-28-68v-416h-384v1152zm256-1440v-64q0-13-9.5-22.5t-22.5-9.5h-704q-13 0-22.5 9.5t-9.5 22.5v64q0 13 9.5 22.5t22.5 9.5h704q13 0 22.5-9.5t9.5-22.5zm256 672h299l-299-299v299zm512 128v672q0 40-28 68t-68 28h-960q-40 0-68-28t-28-68v-160h-544q-40 0-68-28t-28-68v-1344q0-40 28-68t68-28h1088q40 0 68 28t28 68v328q21 13 36 28l408 408q28 28 48 76t20 88z");
            pastecmIcon.Stretch = Stretch.Fill;
            pastecmIcon.Width = pastecmIcon.Height = 12;
            pastecmIcon.Fill = scbf;
            pastecm.Icon = pastecmIcon;
            pastecm.Click += (s, x) => Paste();
            cm.Items.Add(pastecm);

            ///////////////////////////
            ///////////////////////////
            ///   Delete Picture   \\\\
            ///////////////////////////
            ///////////////////////////
            var MovetoRecycleBin = new MenuItem
            {
                Header = "Delete picture",
                InputGestureText = "Delete"
            };
            var MovetoRecycleBinIcon = new System.Windows.Shapes.Path();
            MovetoRecycleBinIcon.Data = Geometry.Parse("M836 1169l-15 368-2 22-420-29q-36-3-67-31.5t-47-65.5q-11-27-14.5-55t4-65 12-55 21.5-64 19-53q78 12 509 28zm-387-586l180 379-147-92q-63 72-111.5 144.5t-72.5 125-39.5 94.5-18.5 63l-4 21-190-357q-17-26-18-56t6-47l8-18q35-63 114-188l-140-86zm1231 517l-188 359q-12 29-36.5 46.5t-43.5 20.5l-18 4q-71 7-219 12l8 164-230-367 211-362 7 173q170 16 283 5t170-33zm-785-924q-47 63-265 435l-317-187-19-12 225-356q20-31 60-45t80-10q24 2 48.5 12t42 21 41.5 33 36 34.5 36 39.5 32 35zm655 307l212 363q18 37 12.5 76t-27.5 74q-13 20-33 37t-38 28-48.5 22-47 16-51.5 14-46 12q-34-72-265-436l313-195zm-143-226l142-83-220 373-419-20 151-86q-34-89-75-166t-75.5-123.5-64.5-80-47-46.5l-17-13 405 1q31-3 58 10.5t39 28.5l11 15q39 61 112 190z");
            MovetoRecycleBinIcon.Stretch = Stretch.Fill;
            MovetoRecycleBinIcon.Width = MovetoRecycleBinIcon.Height = 12;
            MovetoRecycleBinIcon.Fill = scbf;
            MovetoRecycleBin.Icon = MovetoRecycleBinIcon;
            MovetoRecycleBin.Click += (s, x) => DeleteFile(PicPath, true);
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
            var mclcmIcon = new System.Windows.Shapes.Path();
            mclcmIcon.Data = Geometry.Parse("M443.6,387.1L312.4,255.4l131.5-130c5.4-5.4,5.4-14.2,0-19.6l-37.4-37.6c-2.6-2.6-6.1-4-9.8-4c-3.7,0-7.2,1.5-9.8,4  L256,197.8L124.9,68.3c-2.6-2.6-6.1-4-9.8-4c-3.7,0-7.2,1.5-9.8,4L68,105.9c-5.4,5.4-5.4,14.2,0,19.6l131.5,130L68.4,387.1  c-2.6,2.6-4.1,6.1-4.1,9.8c0,3.7,1.4,7.2,4.1,9.8l37.4,37.6c2.7,2.7,6.2,4.1,9.8,4.1c3.5,0,7.1-1.3,9.8-4.1L256,313.1l130.7,131.1  c2.7,2.7,6.2,4.1,9.8,4.1c3.5,0,7.1-1.3,9.8-4.1l37.4-37.6c2.6-2.6,4.1-6.1,4.1-9.8C447.7,393.2,446.2,389.7,443.6,387.1z");
            mclcmIcon.Stretch = Stretch.Fill;
            mclcmIcon.Width = mclcmIcon.Height = 12;
            mclcmIcon.Fill = scbf;
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
            var cmTitle = new ContextMenu();

            var clTc = new MenuItem
            {
                Header = "Copy path to clipboard"
            };
            clTc.Click += (s, x) => CopyText();
            cmTitle.Items.Add(clTc);

            mainWindow.Bar.ContextMenu = cmTitle;

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

            #endregion Add ContextMenus

            cm.Opened += (tt, yy) => Recentcm_MouseEnter(recentcm);
        }
    }
}
