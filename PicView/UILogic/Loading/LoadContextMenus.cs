using PicView.ChangeImage;
using PicView.ConfigureSettings;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic.Sizing;
using PicView.Views.UserControls.Buttons;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.Open_Save;
using static PicView.UILogic.ConfigureWindows;

namespace PicView.UILogic.Loading
{
    internal static class LoadContextMenus
    {
        internal static void AddContextMenus()
        {
            GetFileHistory ??= new FileHistory();

            // Add main contextmenu
            MainContextMenu = (ContextMenu)Application.Current.Resources["mainCM"];
            GetMainWindow.ParentContainer.ContextMenu = MainContextMenu;
            MainContextMenu.Opened += (_, _) => GetFileHistory.RefreshRecentItemsMenu();

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
            FileNameHeader.IsChecked = Settings.Default.SortPreference == 0;
            FileNameHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(0).ConfigureAwait(false); };

            // FileSize
            var filesizeMenu = (MenuItem)sortfilesbycm.Items[1];
            var filesizeHeader = (RadioButton)filesizeMenu.Header;
            filesizeHeader.IsChecked = Settings.Default.SortPreference == 1;
            filesizeHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(1).ConfigureAwait(false); };

            // FileExtension
            var FileExtensionMenu = (MenuItem)sortfilesbycm.Items[2];
            var FileExtensionHeader = (RadioButton)FileExtensionMenu.Header;
            FileExtensionHeader.IsChecked = Settings.Default.SortPreference == 3;
            FileExtensionHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(3).ConfigureAwait(false); };

            // CreationTime
            var CreationTimeMenu = (MenuItem)sortfilesbycm.Items[3];
            var CreationTimeHeader = (RadioButton)CreationTimeMenu.Header;
            CreationTimeHeader.IsChecked = Settings.Default.SortPreference == 2;
            CreationTimeHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(2).ConfigureAwait(false); };

            // LastAccessTime
            var LastAccessTimeMenu = (MenuItem)sortfilesbycm.Items[4];
            var LastAccessTimeHeader = (RadioButton)LastAccessTimeMenu.Header;
            LastAccessTimeHeader.IsChecked = Settings.Default.SortPreference == 4;
            LastAccessTimeHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(4).ConfigureAwait(false); };

            // LastWriteTime
            var LastWriteTimeMenu = (MenuItem)sortfilesbycm.Items[5];
            var LastWriteTimeHeader = (RadioButton)LastWriteTimeMenu.Header;
            LastWriteTimeHeader.IsChecked = Settings.Default.SortPreference == 5;
            LastWriteTimeHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(5).ConfigureAwait(false); };

            // Random
            var RandomMenu = (MenuItem)sortfilesbycm.Items[6];
            var RandomHeader = (RadioButton)RandomMenu.Header;
            RandomHeader.IsChecked = Settings.Default.SortPreference == 6;
            RandomHeader.Click += async delegate { MainContextMenu.IsOpen = false; await UpdateUIValues.ChangeSortingAsync(6).ConfigureAwait(false); };

            // 7 = seperator

            // Ascending
            var AscendingMenu = (MenuItem)sortfilesbycm.Items[8];
            var AscendingHeader = (RadioButton)AscendingMenu.Header;
            AscendingHeader.IsChecked = Settings.Default.Ascending;
            AscendingHeader.Click += async (_, _) =>
            {
                Settings.Default.Ascending = true;
                await UpdateUIValues.ChangeSortingAsync(0, true).ConfigureAwait(false);
            };

            // Descending
            var DescendingMenu = (MenuItem)sortfilesbycm.Items[9];
            var DescendingHeader = (RadioButton)DescendingMenu.Header;
            DescendingHeader.IsChecked = Settings.Default.Ascending == false;
            DescendingHeader.Click += async (_, _) =>
            {
                Settings.Default.Ascending = false;
                await UpdateUIValues.ChangeSortingAsync(0, true).ConfigureAwait(false);
            };

            // 6 == Recent files

            ///////////////////////////
            ///     Settings       \\\\
            ///////////////////////////
            var settingscm = (MenuItem)MainContextMenu.Items[7];

            // Looping
            var LoopingMenu = (MenuItem)settingscm.Items[0];
            var LoopingHeader = (CheckBox)LoopingMenu.Header;
            LoopingHeader.IsChecked = Settings.Default.Looping;
            LoopingHeader.Click += (_, _) => UpdateUIValues.SetLooping();
            LoopingMenu.Click += (_, _) => { UpdateUIValues.SetLooping(); LoopingHeader.IsChecked = !LoopingHeader.IsChecked; };

            // Scrolling
            var ScrollingMenu = (MenuItem)settingscm.Items[1];
            var ScrollingHeader = (CheckBox)ScrollingMenu.Header;
            ScrollingHeader.IsChecked = Settings.Default.ScrollEnabled;
            ScrollingHeader.Click += (_, _) => UpdateUIValues.SetScrolling();
            ScrollingMenu.Click += (_, _) => { UpdateUIValues.SetScrolling(); ScrollingHeader.IsChecked = !ScrollingHeader.IsChecked; };

            // ToogleUI
            var ToogleUIMenu = (MenuItem)settingscm.Items[2];
            ToogleUIMenu.InputGestureText = $"{Application.Current.Resources["Alt"]} + Z";
            var ToogleUIHeader = (CheckBox)ToogleUIMenu.Header;
            ToogleUIHeader.IsChecked = Settings.Default.ShowInterface;
            ToogleUIHeader.Click += (_, _) => HideInterfaceLogic.ToggleInterface();
            ToogleUIMenu.Click += (_, _) => { HideInterfaceLogic.ToggleInterface(); ToogleUIHeader.IsChecked = !ToogleUIHeader.IsChecked; };

            // Change bg
            var ChangeBackgroundMenu = (MenuItem)settingscm.Items[3];
            ChangeBackgroundMenu.Click += (_, _) => ConfigColors.ChangeBackground();

            // Topmost
            var TopmostMenu = (MenuItem)settingscm.Items[4];
            var TopmostHeader = (CheckBox)TopmostMenu.Header;
            TopmostHeader.IsChecked = Settings.Default.TopMost;
            TopmostHeader.Click += (_, _) => UpdateUIValues.SetTopMost();
            TopmostMenu.Click += (_, _) => UpdateUIValues.SetTopMost();

            // Fill Image Height
            var imageHeightMenu = (MenuItem)settingscm.Items[5];
            var imageHeightHeader = (CheckBox)imageHeightMenu.Header;
            imageHeightHeader.IsChecked = Settings.Default.FillImage;
            imageHeightHeader.Click += UpdateUIValues.SetAutoFill;
            imageHeightMenu.Click += UpdateUIValues.SetAutoFill;

            // 6 = seperator

            // Settings
            var SettingsMenu = (MenuItem)settingscm.Items[7];
            SettingsMenu.Click += (_, _) => { AllSettingsWindow(); MainContextMenu.IsOpen = false; };

            // 8 = seperator

            ///////////////////////////
            ///   Set as           \\\\
            ///////////////////////////
            var SetAsCm = (MenuItem)MainContextMenu.Items[9];
            var SetWallpaperCm = (MenuItem)SetAsCm.Items[0];
            SetWallpaperCm.Click += async (_, _) =>
            {
                MainContextMenu.IsOpen = false;
                await Wallpaper.SetWallpaperAsync(Wallpaper.WallpaperStyle.Fill).ConfigureAwait(false);
            };
            var SetLockCm = (MenuItem)SetAsCm.Items[1];
            SetLockCm.Click += async (_, _) =>
            {
                MainContextMenu.IsOpen = false;
                await LockScreenHelper.SetLockScreenImageAsync().ConfigureAwait(false);
            };

            ///////////////////////////
            ///   ShowInFolder     \\\\
            ///////////////////////////
            var ShowInFolderCm = (MenuItem)MainContextMenu.Items[10];
            ShowInFolderCm.Click += (_, _) => Open_In_Explorer();

            ///////////////////////////
            ///   Image choices    \\\\
            ///////////////////////////
            var ImageChoices = (MenuItem)MainContextMenu.Items[11];

            var ImageInfoCm = (MenuItem)ImageChoices.Items[0];
            ImageInfoCm.Click += (_, _) => ImageInfoWindow();

            var FileProps = (MenuItem)ImageChoices.Items[1];
            FileProps.Click += (_, _) => FileProperties.ShowFileProperties();
            FileProps.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + I";

            // 2 = seperator

            var ImageSize = (MenuItem)ImageChoices.Items[3];
            ImageSize.Click += (_, _) => ResizeButton.ToggleQuickResize();

            var BatchSize = (MenuItem)ImageChoices.Items[4];
            BatchSize.Click += (_, _) => ResizeWindow();

            // 12 = seperator

            ///////////////////////////
            ///   Copy             \\\\
            ///////////////////////////
            var CopyCm = (MenuItem)MainContextMenu.Items[13];

            // Copy file
            var copyFileCm = (MenuItem)CopyCm.Items[0];
            copyFileCm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + C";
            copyFileCm.Click += (_, _) => Copy_Paste.CopyFile();

            // FileCopyPath
            var FileCopyPathCm = (MenuItem)CopyCm.Items[1];
            FileCopyPathCm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + {Application.Current.Resources["Alt"]} + C";
            FileCopyPathCm.Click += (_, _) => Copy_Paste.CopyFilePath();

            // CopyImage
            var CopyImageCm = (MenuItem)CopyCm.Items[2];
            CopyImageCm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + {Application.Current.Resources["Shift"]} + C";
            CopyImageCm.Click += (_, _) => Copy_Paste.CopyBitmap();

            // CopyBase64
            var CopyBase64Cm = (MenuItem)CopyCm.Items[3];
            CopyBase64Cm.Click += async delegate { MainContextMenu.IsOpen = false; await Base64.SendToClipboard().ConfigureAwait(false); };

            ///////////////////////////
            ///   Cut File         \\\\
            ///////////////////////////
            var Cutcm = (MenuItem)MainContextMenu.Items[14];
            Cutcm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + X";
            Cutcm.Click += (_, _) => { MainContextMenu.IsOpen = false; Copy_Paste.Cut(); };

            ///////////////////////////
            ///   Paste File       \\\\
            ///////////////////////////
            var pastecm = (MenuItem)MainContextMenu.Items[15];
            pastecm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + V";
            pastecm.Click += async delegate { MainContextMenu.IsOpen = false; await Copy_Paste.PasteAsync().ConfigureAwait(false); };

            // 16 = seperator

            ///////////////////////////
            ///   Delete File       \\\\
            ///////////////////////////
            var Deletecm = (MenuItem)MainContextMenu.Items[17];
            Deletecm.Click += async (_, _) => await DeleteFiles.DeleteFileAsync(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)).ConfigureAwait(false);

            // 18 = seperator

            ///////////////////////////
            ///   Close       \\\\
            ///////////////////////////
            var CloseCm = (MenuItem)MainContextMenu.Items[19];
            CloseCm.Click += (_, _) => SystemCommands.CloseWindow(GetMainWindow);





            // Add Window contextmenu
            WindowContextMenu = (ContextMenu)Application.Current.Resources["windowCM"];

            var fullscreenWindow = (MenuItem)WindowContextMenu.Items[0];
            fullscreenWindow.Click += (_,_) => WindowSizing.Fullscreen_Restore(!Settings.Default.Fullscreen);

            var minWindow = (MenuItem)WindowContextMenu.Items[1];
            minWindow.Click += (_, _) => SystemCommands.MinimizeWindow(ConfigureWindows.GetMainWindow);

            var closeWindow = (MenuItem)WindowContextMenu.Items[2];
            closeWindow.Click += (_, _) => SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);

            GetMainWindow.Logo.ContextMenu = WindowContextMenu;
            GetMainWindow.GalleryButton.ContextMenu = WindowContextMenu;
            GetMainWindow.RotateButton.ContextMenu = WindowContextMenu;
            GetMainWindow.FlipButton.ContextMenu = WindowContextMenu;
            GetMainWindow.MinButton.ContextMenu = WindowContextMenu;
            GetMainWindow.FullscreenButton.ContextMenu = WindowContextMenu;
            GetMainWindow.CloseButton.ContextMenu = WindowContextMenu;
        }
    }
}