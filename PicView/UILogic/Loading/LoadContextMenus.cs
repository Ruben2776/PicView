using PicView.ConfigureSettings;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.Open_Save;
using static PicView.UILogic.ConfigureWindows;

namespace PicView.UILogic.Loading
{
    internal static class LoadContextMenus
    {
        internal static void AddContextMenus()
        {
            // Add main contextmenu
            ConfigureWindows.MainContextMenu = (ContextMenu)Application.Current.Resources["mainCM"];
            ConfigureWindows.GetMainWindow.ParentContainer.ContextMenu = ConfigureWindows.MainContextMenu;
            ConfigureWindows.MainContextMenu.Opened += (_, _) => ChangeImage.History.RefreshRecentItemsMenu();

            ///////////////////////////
            ///     Open           \\\\
            ///////////////////////////
            var opencm = (MenuItem)MainContextMenu.Items[0];
            opencm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + O";
            opencm.Click += async (_, _) => await OpenAsync().ConfigureAwait(false);

            ///////////////////////////
            ///     Save           \\\\
            ///////////////////////////
            var savecm = (MenuItem)MainContextMenu.Items[1];
            savecm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + S";
            savecm.Click += async (s, x) => await SaveFilesAsync().ConfigureAwait(false);

            ///////////////////////////
            ///     Print          \\\\
            ///////////////////////////
            var printcm = (MenuItem)MainContextMenu.Items[2];
            printcm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + P";
            printcm.Click += (s, x) => Print(Pics[FolderIndex]);

            ///////////////////////////
            ///     Open With      \\\\
            ///////////////////////////
            var openwcm = (MenuItem)MainContextMenu.Items[3];
            openwcm.Click += (_, _) => OpenWith(Pics[FolderIndex]);

            // 4 == seperator

            var sortfilesbycm = (MenuItem)MainContextMenu.Items[5];

            // FileName
            var FileNameMenu = (MenuItem)sortfilesbycm.Items[0];
            var FileNameHeader = (RadioButton)FileNameMenu.Header;
            FileNameHeader.IsChecked = Properties.Settings.Default.SortPreference == 0;
            FileNameHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(0).ConfigureAwait(false); };

            // FileSize
            var filesizeMenu = (MenuItem)sortfilesbycm.Items[1];
            var filesizeHeader = (RadioButton)filesizeMenu.Header;
            filesizeHeader.IsChecked = Properties.Settings.Default.SortPreference == 1;
            filesizeHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(1).ConfigureAwait(false); };

            // FileExtension
            var FileExtensionMenu = (MenuItem)sortfilesbycm.Items[2];
            var FileExtensionHeader = (RadioButton)FileExtensionMenu.Header;
            FileExtensionHeader.IsChecked = Properties.Settings.Default.SortPreference == 3;
            FileExtensionHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(3).ConfigureAwait(false); };

            // CreationTime
            var CreationTimeMenu = (MenuItem)sortfilesbycm.Items[3];
            var CreationTimeHeader = (RadioButton)CreationTimeMenu.Header;
            CreationTimeHeader.IsChecked = Properties.Settings.Default.SortPreference == 2;
            CreationTimeHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(2).ConfigureAwait(false); };

            // LastAccessTime
            var LastAccessTimeMenu = (MenuItem)sortfilesbycm.Items[4];
            var LastAccessTimeHeader = (RadioButton)LastAccessTimeMenu.Header;
            LastAccessTimeHeader.IsChecked = Properties.Settings.Default.SortPreference == 4;
            LastAccessTimeHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(4).ConfigureAwait(false); };

            // LastWriteTime
            var LastWriteTimeMenu = (MenuItem)sortfilesbycm.Items[5];
            var LastWriteTimeHeader = (RadioButton)LastWriteTimeMenu.Header;
            LastWriteTimeHeader.IsChecked = Properties.Settings.Default.SortPreference == 5;
            LastWriteTimeHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(5).ConfigureAwait(false); };

            // Random
            var RandomMenu = (MenuItem)sortfilesbycm.Items[6];
            var RandomHeader = (RadioButton)RandomMenu.Header;
            RandomHeader.IsChecked = Properties.Settings.Default.SortPreference == 6;
            RandomHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(6).ConfigureAwait(false); };

            // 7 = seperator

            // Ascending
            var AscendingMenu = (MenuItem)sortfilesbycm.Items[8];
            var AscendingHeader = (RadioButton)AscendingMenu.Header;
            AscendingHeader.Checked += async (_, _) =>
            {
                Properties.Settings.Default.Ascending = true;
                await UpdateUIValues.ChangeSortingAsync(0, true).ConfigureAwait(false);
            };
            AscendingHeader.IsChecked = Properties.Settings.Default.Ascending;

            // Descending
            var DescendingMenu = (MenuItem)sortfilesbycm.Items[9];
            var DescendingHeader = (RadioButton)DescendingMenu.Header;
            DescendingHeader.Checked += async (_, _) =>
            {
                Properties.Settings.Default.Ascending = false;
                await UpdateUIValues.ChangeSortingAsync(0, true).ConfigureAwait(false);
            };
            DescendingHeader.IsChecked = Properties.Settings.Default.Ascending == false;


            // 6 == Recent files

            ///////////////////////////
            ///     Settings       \\\\
            ///////////////////////////
            var settingscm = (MenuItem)MainContextMenu.Items[7];

            // Looping
            var LoopingMenu = (MenuItem)settingscm.Items[0];
            var LoopingHeader = (CheckBox)LoopingMenu.Header;
            LoopingHeader.IsChecked = Properties.Settings.Default.Looping;
            LoopingHeader.Click += (_, _) => UpdateUIValues.SetLooping();
            LoopingMenu.Click += (_, _) => { UpdateUIValues.SetLooping(); LoopingHeader.IsChecked = !LoopingHeader.IsChecked; };

            // Scrolling
            var ScrollingMenu = (MenuItem)settingscm.Items[1];
            var ScrollingHeader = (CheckBox)ScrollingMenu.Header;
            ScrollingHeader.IsChecked = Properties.Settings.Default.ScrollEnabled;
            ScrollingHeader.Click += (_, _) => UpdateUIValues.SetScrolling();
            ScrollingMenu.Click += (_, _) => { UpdateUIValues.SetScrolling(); ScrollingHeader.IsChecked = !ScrollingHeader.IsChecked; };

            // ToogleUI
            var ToogleUIMenu = (MenuItem)settingscm.Items[2];
            var ToogleUIHeader = (CheckBox)ToogleUIMenu.Header;
            ToogleUIHeader.IsChecked = Properties.Settings.Default.ShowInterface;
            ToogleUIHeader.Click += (_, _) => HideInterfaceLogic.ToggleInterface();
            ToogleUIMenu.Click += (_, _) => { HideInterfaceLogic.ToggleInterface(); ToogleUIHeader.IsChecked = !ToogleUIHeader.IsChecked; };

            // Change bg
            var ChangeBgMenu = (MenuItem)settingscm.Items[3];
            ChangeBgMenu.Click += (_, _) => ConfigColors.ChangeBackground();

            // Topmost
            var TopmostMenu = (MenuItem)settingscm.Items[4];
            var TopmostHeader = (CheckBox)TopmostMenu.Header;
            TopmostHeader.IsChecked = Properties.Settings.Default.ShowInterface;
            TopmostHeader.Checked += (_, _) => ConfigureWindows.IsMainWindowTopMost = !Properties.Settings.Default.TopMost;
            TopmostHeader.Unchecked += (_, _) => ConfigureWindows.IsMainWindowTopMost = false;

            // 5 = seperator

            // Settings
            var SettingsMenu = (MenuItem)settingscm.Items[6];
            SettingsMenu.Click += (_, _) => { AllSettingsWindow(); MainContextMenu.IsOpen = false; };

            // 8 = seperator

            ///////////////////////////
            ///   SetWallpaper     \\\\
            ///////////////////////////
            var SetWallpaperCm = (MenuItem)MainContextMenu.Items[9];
            SetWallpaperCm.Click += async (_, _) =>
            {
                MainContextMenu.IsOpen = false;
                await SystemIntegration.Wallpaper.SetWallpaperAsync(SystemIntegration.Wallpaper.WallpaperStyle.Fill).ConfigureAwait(false);
            };


            ///////////////////////////
            ///   ShowInFolder     \\\\
            ///////////////////////////
            var ShowInFolderCm = (MenuItem)MainContextMenu.Items[10];
            ShowInFolderCm.Click += (_, _) => FileHandling.Open_Save.Open_In_Explorer();


            ///////////////////////////
            ///   ImageInfo        \\\\
            ///////////////////////////
            var ImageInfoCm = (MenuItem)MainContextMenu.Items[11];
            ImageInfoCm.Click += (_, _) => ConfigureWindows.ImageInfoWindow();

            // 12 = seperator

            ///////////////////////////
            ///   Copy             \\\\
            ///////////////////////////
            var CopyCm = (MenuItem)MainContextMenu.Items[13];

            // Copy file
            var copyFileCm = (MenuItem)CopyCm.Items[0];
            copyFileCm.Click += (_, _) => FileHandling.Copy_Paste.Copyfile();

            // FileCopyPath
            var FileCopyPathCm = (MenuItem)CopyCm.Items[1];
            FileCopyPathCm.Click += (_, _) => FileHandling.Copy_Paste.CopyFilePath();

            // CopyImage
            var CopyImageCm = (MenuItem)CopyCm.Items[2];
            CopyImageCm.Click += (_, _) => FileHandling.Copy_Paste.CopyBitmap();

            // CopyBase64
            var CopyBase64Cm = (MenuItem)CopyCm.Items[3];
            CopyBase64Cm.Click += async delegate { MainContextMenu.IsOpen = false; await ImageHandling.Base64.SendToClipboard().ConfigureAwait(false); };


            ///////////////////////////
            ///   Cut File         \\\\
            ///////////////////////////
            var Cutcm = (MenuItem)MainContextMenu.Items[14];
            Cutcm.Click += (_, _) => { MainContextMenu.IsOpen = false; FileHandling.Copy_Paste.Cut(); };


            ///////////////////////////
            ///   Paste File       \\\\
            ///////////////////////////
            var pastecm = (MenuItem)MainContextMenu.Items[15];
            pastecm.Click += (_, _) => { MainContextMenu.IsOpen = false; FileHandling.Copy_Paste.Paste(); };

            // 16 = seperator

            ///////////////////////////
            ///   Delete File       \\\\
            ///////////////////////////
            var Deletecm = (MenuItem)MainContextMenu.Items[17];
            Deletecm.Click += async (_, _) => await FileHandling.DeleteFiles.DeleteFileAsync(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)).ConfigureAwait(false);

            // 18 = seperator

            ///////////////////////////
            ///   Close       \\\\
            ///////////////////////////
            var CloseCm = (MenuItem)MainContextMenu.Items[19];
            CloseCm.Click += (_, _) => SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);


        }
    }
}