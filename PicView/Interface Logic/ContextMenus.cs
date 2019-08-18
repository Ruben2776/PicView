using PicView.Native;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.Copy_Paste;
using static PicView.DeleteFiles;
using static PicView.FileFunctions;
using static PicView.FileLists;
using static PicView.Open_Save;
using static PicView.Wallpaper;
using static PicView.Helper;
using static PicView.Variables;
using static PicView.Navigation;

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

            var opencm = new MenuItem
            {
                Header = "Open",
                InputGestureText = "Ctrl + O"
            };
            var opencmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M1717 931q0-35-53-35h-1088q-40 0-85.5 21.5t-71.5 52.5l-294 363q-18 24-18 40 0 35 53 35h1088q40 0 86-22t71-53l294-363q18-22 18-39zm-1141-163h768v-160q0-40-28-68t-68-28h-576q-40 0-68-28t-28-68v-64q0-40-28-68t-68-28h-320q-40 0-68 28t-28 68v853l256-315q44-53 116-87.5t140-34.5zm1269 163q0 62-46 120l-295 363q-43 53-116 87.5t-140 34.5h-1088q-92 0-158-66t-66-158v-960q0-92 66-158t158-66h320q92 0 158 66t66 158v32h544q92 0 158 66t66 158v160h192q54 0 99 24.5t67 70.5q15 32 15 68z"),
                Stretch = Stretch.Fill
            };
            opencmIcon.Width = opencmIcon.Height = 12;
            opencmIcon.Fill = scbf;
            opencm.Icon = opencmIcon;
            opencm.Click += (s, x) => Open();
            cm.Items.Add(opencm);

            var savecm = new MenuItem()
            {
                Header = "Save",
                InputGestureText = "Ctrl + S"
            };
            var savecmIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M512 1536h768v-384h-768v384zm896 0h128v-896q0-14-10-38.5t-20-34.5l-281-281q-10-10-34-20t-39-10v416q0 40-28 68t-68 28h-576q-40 0-68-28t-28-68v-416h-128v1280h128v-416q0-40 28-68t68-28h832q40 0 68 28t28 68v416zm-384-928v-320q0-13-9.5-22.5t-22.5-9.5h-192q-13 0-22.5 9.5t-9.5 22.5v320q0 13 9.5 22.5t22.5 9.5h192q13 0 22.5-9.5t9.5-22.5zm640 32v928q0 40-28 68t-68 28h-1344q-40 0-68-28t-28-68v-1344q0-40 28-68t68-28h928q40 0 88 20t76 48l280 280q28 28 48 76t20 88z"),
                Stretch = Stretch.Fill
            };
            savecmIcon.Width = savecmIcon.Height = 12;
            savecmIcon.Fill = scbf;
            savecm.Icon = savecmIcon;
            savecm.Click += (s, x) => SaveFiles();
            cm.Items.Add(savecm);

            var printcm = new MenuItem
            {
                Header = "Print",
                InputGestureText = "Ctrl + P"
            };
            var printcmIcon = new System.Windows.Shapes.Path();
            printcmIcon.Data = Geometry.Parse("M448 1536h896v-256h-896v256zm0-640h896v-384h-160q-40 0-68-28t-28-68v-160h-640v640zm1152 64q0-26-19-45t-45-19-45 19-19 45 19 45 45 19 45-19 19-45zm128 0v416q0 13-9.5 22.5t-22.5 9.5h-224v160q0 40-28 68t-68 28h-960q-40 0-68-28t-28-68v-160h-224q-13 0-22.5-9.5t-9.5-22.5v-416q0-79 56.5-135.5t135.5-56.5h64v-544q0-40 28-68t68-28h672q40 0 88 20t76 48l152 152q28 28 48 76t20 88v256h64q79 0 135.5 56.5t56.5 135.5z");
            printcmIcon.Stretch = Stretch.Fill;
            printcmIcon.Width = printcmIcon.Height = 12;
            printcmIcon.Fill = scbf;
            printcm.Icon = printcmIcon;
            printcm.Click += (s, x) => Print(PicPath);
            cm.Items.Add(printcm);

            cm.Items.Add(new Separator());
            var recentcm = new MenuItem
            {
                Header = "Recent files"
            };
            var recentcmIcon = new System.Windows.Shapes.Path();
            recentcmIcon.Data = Geometry.Parse("M288,48H136c-22.092,0-40,17.908-40,40v336c0,22.092,17.908,40,40,40h240c22.092,0,40-17.908,40-40V176L288,48z M272,192 V80l112, 112H272z");
            recentcmIcon.Stretch = Stretch.Fill;
            recentcmIcon.Width = recentcmIcon.Height = 12;
            recentcmIcon.Fill = scbf;
            recentcm.Icon = recentcmIcon;
            cm.Items.Add(recentcm);

            var sortcm = new MenuItem
            {
                Header = "Sort files by"
            };
            var sortcmIcon = new System.Windows.Shapes.Path();
            sortcmIcon.Data = Geometry.Parse("M666 481q-60 92-137 273-22-45-37-72.5t-40.5-63.5-51-56.5-63-35-81.5-14.5h-224q-14 0-23-9t-9-23v-192q0-14 9-23t23-9h224q250 0 410 225zm1126 799q0 14-9 23l-320 320q-9 9-23 9-13 0-22.5-9.5t-9.5-22.5v-192q-32 0-85 .5t-81 1-73-1-71-5-64-10.5-63-18.5-58-28.5-59-40-55-53.5-56-69.5q59-93 136-273 22 45 37 72.5t40.5 63.5 51 56.5 63 35 81.5 14.5h256v-192q0-14 9-23t23-9q12 0 24 10l319 319q9 9 9 23zm0-896q0 14-9 23l-320 320q-9 9-23 9-13 0-22.5-9.5t-9.5-22.5v-192h-256q-48 0-87 15t-69 45-51 61.5-45 77.5q-32 62-78 171-29 66-49.5 111t-54 105-64 100-74 83-90 68.5-106.5 42-128 16.5h-224q-14 0-23-9t-9-23v-192q0-14 9-23t23-9h224q48 0 87-15t69-45 51-61.5 45-77.5q32-62 78-171 29-66 49.5-111t54-105 64-100 74-83 90-68.5 106.5-42 128-16.5h256v-192q0-14 9-23t23-9q12 0 24 10l319 319q9 9 9 23z");
            sortcmIcon.Stretch = Stretch.Fill;
            sortcmIcon.Width = sortcmIcon.Height = 12;
            sortcmIcon.Fill = scbf;
            sortcm.Icon = sortcmIcon;
            var sortcmChild0 = new RadioButton();
            sortcmChild0.Content = "File name";
            sortcmChild0.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 0;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild0);
            var sortcmChild1 = new RadioButton();
            sortcmChild1.Content = "File Size";
            sortcmChild1.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 1;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild1);
            var sortcmChild2 = new RadioButton();
            sortcmChild2.Content = "Creation time";
            sortcmChild2.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 2;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild2);
            var sortcmChild3 = new RadioButton();
            sortcmChild3.Content = "File extension";
            sortcmChild3.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 3;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild3);
            var sortcmChild4 = new RadioButton();
            sortcmChild4.Content = "Last access time";
            sortcmChild4.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 4;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild4);
            var sortcmChild5 = new RadioButton();
            sortcmChild5.Content = "Last write time";
            sortcmChild5.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 5;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild5);
            var sortcmChild6 = new RadioButton();
            sortcmChild6.Content = "Random";
            sortcmChild6.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 6;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild6);
            cm.Items.Add(sortcm);
            cm.Items.Add(new Separator());

            var wallcm = new MenuItem
            {
                Header = "Set as wallpaper"
            };
            wallcm.Click += (s, x) => SetWallpaper(PicPath, WallpaperStyle.Fill);
            var wallcmIcon = new System.Windows.Shapes.Path();
            wallcmIcon.Data = Geometry.Parse(CameraIconSVG);
            wallcmIcon.Stretch = Stretch.Fill;
            wallcmIcon.Width = wallcmIcon.Height = 12;
            wallcmIcon.Fill = scbf;
            wallcm.Icon = wallcmIcon;
            cm.Items.Add(wallcm);
            cm.Items.Add(new Separator());

            var lcdcm = new MenuItem
            {
                Header = "Locate on disk",
                InputGestureText = "F3",
                ToolTip = "Opens the current image on your drive"
            };
            lcdcm.Click += (s, x) => Open_In_Explorer();
            cm.Items.Add(lcdcm);

            var fildecm = new MenuItem
            {
                Header = "File Details",
                InputGestureText = "Ctrl + I"
            };
            fildecm.Click += (s, x) => NativeMethods.ShowFileProperties(PicPath);
            cm.Items.Add(fildecm);
            cm.Items.Add(new Separator());

            var cppcm = new MenuItem
            {
                Header = "Copy picture",
                InputGestureText = "Ctrl + C"
            };
            var cppcmIcon = new System.Windows.Shapes.Path();
            cppcmIcon.Data = Geometry.Parse("M1696 384q40 0 68 28t28 68v1216q0 40-28 68t-68 28h-960q-40 0-68-28t-28-68v-288h-544q-40 0-68-28t-28-68v-672q0-40 20-88t48-76l408-408q28-28 76-48t88-20h416q40 0 68 28t28 68v328q68-40 128-40h416zm-544 213l-299 299h299v-299zm-640-384l-299 299h299v-299zm196 647l316-316v-416h-384v416q0 40-28 68t-68 28h-416v640h512v-256q0-40 20-88t48-76zm956 804v-1152h-384v416q0 40-28 68t-68 28h-416v640h896z");
            cppcmIcon.Stretch = Stretch.Fill;
            cppcmIcon.Width = cppcmIcon.Height = 12;
            cppcmIcon.Fill = scbf;
            cppcm.Icon = cppcmIcon;
            cppcm.Click += (s, x) => CopyPic();
            cm.Items.Add(cppcm);

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

            cm.Items.Add(new Separator());
            var clcm = new MenuItem
            {
                Header = "Close",
                InputGestureText = "Esc",
                StaysOpenOnClick = false
            };
            //var mclcmIcon = new System.Windows.Shapes.Path();
            //mclcmIcon.Data = Geometry.Parse("M443.6,387.1L312.4,255.4l131.5-130c5.4-5.4,5.4-14.2,0-19.6l-37.4-37.6c-2.6-2.6-6.1-4-9.8-4c-3.7,0-7.2,1.5-9.8,4  L256,197.8L124.9,68.3c-2.6-2.6-6.1-4-9.8-4c-3.7,0-7.2,1.5-9.8,4L68,105.9c-5.4,5.4-5.4,14.2,0,19.6l131.5,130L68.4,387.1  c-2.6,2.6-4.1,6.1-4.1,9.8c0,3.7,1.4,7.2,4.1,9.8l37.4,37.6c2.7,2.7,6.2,4.1,9.8,4.1c3.5,0,7.1-1.3,9.8-4.1L256,313.1l130.7,131.1  c2.7,2.7,6.2,4.1,9.8,4.1c3.5,0,7.1-1.3,9.8-4.1l37.4-37.6c2.6-2.6,4.1-6.1,4.1-9.8C447.7,393.2,446.2,389.7,443.6,387.1z");
            //mclcmIcon.Stretch = Stretch.Fill;
            //mclcmIcon.Width = mclcmIcon.Height = 12;
            //mclcmIcon.Fill = scbf;
            //clcm.Icon = mclcmIcon;
            //clcm.Click += (s, x) => Close();
            //cm.Items.Add(clcm);

            // Add to elements
            mainWindow.img.ContextMenu = mainWindow.bg.ContextMenu = cm;

            // Add left and right ContextMenus
            var cmLeft = new ContextMenu();
            var cmRight = new ContextMenu();

            var nextcm = new MenuItem
            {
                Header = "Next picture",
                InputGestureText = "ᗌ or D",
                ToolTip = "Go to Next image in folder",
                StaysOpenOnClick = true
            };
            nextcm.Click += (s, x) => Pic();
            cmRight.Items.Add(nextcm);

            var prevcm = new MenuItem
            {
                Header = "Previous picture",
                InputGestureText = "ᗏ or A",
                ToolTip = "Go to previous image in folder",
                StaysOpenOnClick = true
            };
            prevcm.Click += (s, x) => Pic(false);
            cmLeft.Items.Add(prevcm);

            var firstcm = new MenuItem
            {
                Header = "First picture",
                InputGestureText = "Ctrl + D or Ctrl + ᗌ",
                ToolTip = "Go to first image in folder"
            };
            firstcm.Click += (s, x) => Pic(false, true);
            cmLeft.Items.Add(firstcm);

            var lastcm = new MenuItem
            {
                Header = "Last picture",
                InputGestureText = "Ctrl + A or Ctrl + ᗏ",
                ToolTip = "Go to last image in folder"
            };
            lastcm.Click += (s, x) => Pic(true, true);
            cmRight.Items.Add(lastcm);

            // Add to elements
            mainWindow.RightButton.ContextMenu = cmRight;
            mainWindow.LeftButton.ContextMenu = cmLeft;

            clickArrowRight.ContextMenu = cmRight;
            clickArrowLeft.ContextMenu = cmLeft;

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
