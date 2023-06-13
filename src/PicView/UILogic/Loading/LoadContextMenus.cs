﻿using PicView.ChangeImage;
using PicView.ConfigureSettings;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic.Sizing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.OpenSave;
using static PicView.UILogic.ConfigureWindows;

namespace PicView.UILogic.Loading;

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
        //     Open              \\
        ///////////////////////////
        var openCm = (MenuItem)MainContextMenu.Items[0];
        openCm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + O";
        openCm.Click += async (_, _) => await OpenAsync().ConfigureAwait(false);

        ///////////////////////////
        //     Save             \\
        ///////////////////////////
        var saveCm = (MenuItem)MainContextMenu.Items[1];
        saveCm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + S";
        saveCm.Click += async (s, x) => await SaveFilesAsync(Settings.Default.ShowFileSavingDialog).ConfigureAwait(false);

        ///////////////////////////
        //       Print          \\\
        ///////////////////////////
        var printCm = (MenuItem)MainContextMenu.Items[2];
        printCm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + P";
        printCm.Click += (s, x) => Print(Pics[FolderIndex]);

        ///////////////////////////
        //     Open With        \\\
        ///////////////////////////
        var openWcm = (MenuItem)MainContextMenu.Items[3];
        openWcm.Click += (_, _) => OpenWith();

        // 4 == separator

        ///////////////////////////
        //     Sort by          \\
        ///////////////////////////
        var sortFilesByCm = (MenuItem)MainContextMenu.Items[5];

        // FileName
        var fileNameMenu = (MenuItem)sortFilesByCm.Items[0];
        var fileNameHeader = (RadioButton)fileNameMenu.Header;
        fileNameHeader.IsChecked = Settings.Default.SortPreference == (int)FileLists.SortFilesBy.Name;
        fileNameHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileLists.SortFilesBy.Name).ConfigureAwait(false);
        };

        // FileSize
        var filesizeMenu = (MenuItem)sortFilesByCm.Items[1];
        var filesizeHeader = (RadioButton)filesizeMenu.Header;
        filesizeHeader.IsChecked = Settings.Default.SortPreference == (int)FileLists.SortFilesBy.FileSize;
        filesizeHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileLists.SortFilesBy.FileSize).ConfigureAwait(false);
        };

        // FileExtension
        var fileExtensionMenu = (MenuItem)sortFilesByCm.Items[2];
        var fileExtensionHeader = (RadioButton)fileExtensionMenu.Header;
        fileExtensionHeader.IsChecked = Settings.Default.SortPreference == (int)FileLists.SortFilesBy.Extension;
        fileExtensionHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileLists.SortFilesBy.Extension).ConfigureAwait(false);
        };

        // CreationTime
        var creationTimeMenu = (MenuItem)sortFilesByCm.Items[3];
        var creationTimeHeader = (RadioButton)creationTimeMenu.Header;
        creationTimeHeader.IsChecked = Settings.Default.SortPreference == (int)FileLists.SortFilesBy.CreationTime;
        creationTimeHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileLists.SortFilesBy.CreationTime).ConfigureAwait(false);
        };

        // LastAccessTime
        var lastAccessTimeMenu = (MenuItem)sortFilesByCm.Items[4];
        var lastAccessTimeHeader = (RadioButton)lastAccessTimeMenu.Header;
        lastAccessTimeHeader.IsChecked = Settings.Default.SortPreference == (int)FileLists.SortFilesBy.LastAccessTime;
        lastAccessTimeHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileLists.SortFilesBy.LastAccessTime).ConfigureAwait(false);
        };

        // LastWriteTime
        var lastWriteTimeMenu = (MenuItem)sortFilesByCm.Items[5];
        var lastWriteTimeHeader = (RadioButton)lastWriteTimeMenu.Header;
        lastWriteTimeHeader.IsChecked =
            Settings.Default.SortPreference == (int)FileLists.SortFilesBy.LastWriteTime;
        lastWriteTimeHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileLists.SortFilesBy.LastWriteTime).ConfigureAwait(false);
        };

        // Random
        var randomMenu = (MenuItem)sortFilesByCm.Items[6];
        var randomHeader = (RadioButton)randomMenu.Header;
        randomHeader.IsChecked = Settings.Default.SortPreference == (int)FileLists.SortFilesBy.Random;
        randomHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileLists.SortFilesBy.Random).ConfigureAwait(false);
        };

        // 7 = separator

        // Ascending
        var ascendingMenu = (MenuItem)sortFilesByCm.Items[8];
        var ascendingHeader = (RadioButton)ascendingMenu.Header;
        ascendingHeader.IsChecked = Settings.Default.Ascending;
        ascendingHeader.Click += async (_, _) =>
        {
            Settings.Default.Ascending = true;
            await UpdateUIValues.ChangeSortingAsync(0, true).ConfigureAwait(false);
        };

        // Descending
        var descendingMenu = (MenuItem)sortFilesByCm.Items[9];
        var descendingHeader = (RadioButton)descendingMenu.Header;
        descendingHeader.IsChecked = Settings.Default.Ascending == false;
        descendingHeader.Click += async (_, _) =>
        {
            Settings.Default.Ascending = false;
            await UpdateUIValues.ChangeSortingAsync(0, true).ConfigureAwait(false);
        };

        ///////////////////////////
        //     Recent files      \\
        ///////////////////////////
        //var recentMenu = (MenuItem)MainContextMenu.Items[6];

        ///////////////////////////
        //      Settings         \\
        ///////////////////////////
        var settingsCm = (MenuItem)MainContextMenu.Items[7];

        // Looping
        var loopingMenu = (MenuItem)settingsCm.Items[0];
        var loopingHeader = (CheckBox)loopingMenu.Header;
        loopingHeader.IsChecked = Settings.Default.Looping;
        loopingHeader.Click += (_, _) => UpdateUIValues.SetLooping();
        loopingMenu.Click += (_, _) => { UpdateUIValues.SetLooping(); loopingHeader.IsChecked = !loopingHeader.IsChecked; };

        // Scrolling
        var ScrollingMenu = (MenuItem)settingsCm.Items[1];
        var ScrollingHeader = (CheckBox)ScrollingMenu.Header;
        ScrollingHeader.IsChecked = Settings.Default.ScrollEnabled;
        ScrollingHeader.Click += (_, _) => UpdateUIValues.SetScrolling();
        ScrollingMenu.Click += (_, _) => { UpdateUIValues.SetScrolling(); ScrollingHeader.IsChecked = !ScrollingHeader.IsChecked; };

        // ToogleUI
        var ToogleUIMenu = (MenuItem)settingsCm.Items[2];
        ToogleUIMenu.InputGestureText = $"{Application.Current.Resources["Alt"]} + Z";
        var ToogleUIHeader = (CheckBox)ToogleUIMenu.Header;
        ToogleUIHeader.IsChecked = Settings.Default.ShowInterface;
        ToogleUIHeader.Click += (_, _) => HideInterfaceLogic.ToggleInterface();
        ToogleUIMenu.Click += (_, _) => { HideInterfaceLogic.ToggleInterface(); ToogleUIHeader.IsChecked = !ToogleUIHeader.IsChecked; };

        // Change bg
        var ChangeBackgroundMenu = (MenuItem)settingsCm.Items[3];
        ChangeBackgroundMenu.Click += (_, _) => ConfigColors.ChangeBackground();

        // Topmost
        var TopmostMenu = (MenuItem)settingsCm.Items[4];
        var TopmostHeader = (CheckBox)TopmostMenu.Header;
        TopmostHeader.IsChecked = Settings.Default.TopMost;
        TopmostHeader.Click += (_, _) => UpdateUIValues.SetTopMost();
        TopmostMenu.Click += (_, _) => UpdateUIValues.SetTopMost();

        // Fill Image Height
        var imageHeightMenu = (MenuItem)settingsCm.Items[5];
        var imageHeightHeader = (CheckBox)imageHeightMenu.Header;
        imageHeightHeader.IsChecked = Settings.Default.FillImage;
        imageHeightHeader.Click += UpdateUIValues.SetAutoFill;
        imageHeightMenu.Click += UpdateUIValues.SetAutoFill;

        // Ctrl to zoom
        var ctrlZoomMenu = (MenuItem)settingsCm.Items[6];
        var ctrlZoomHeader = (CheckBox)ctrlZoomMenu.Header;
        ctrlZoomHeader.IsChecked = Settings.Default.CtrlZoom;
        ctrlZoomMenu.Click += (_, _) => UpdateUIValues.SetCtrlToZoom(ctrlZoomHeader.IsChecked.Value);
        ctrlZoomHeader.Click += (_, _) => UpdateUIValues.SetCtrlToZoom(ctrlZoomHeader.IsChecked.Value);

        // 7 = seperator

        // Settings
        var SettingsMenu = (MenuItem)settingsCm.Items[8];
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
        ImageSize.Click += (_, _) => UpdateUIValues.ToggleQuickResize();

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
        copyFileCm.Click += (_, _) => CopyPaste.CopyFile();

        // FileCopyPath
        var FileCopyPathCm = (MenuItem)CopyCm.Items[1];
        FileCopyPathCm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + {Application.Current.Resources["Alt"]} + C";
        FileCopyPathCm.Click += (_, _) => CopyPaste.CopyFilePath();

        // CopyImage
        var CopyImageCm = (MenuItem)CopyCm.Items[2];
        CopyImageCm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + {Application.Current.Resources["Shift"]} + C";
        CopyImageCm.Click += (_, _) => CopyPaste.CopyBitmap();

        // CopyBase64
        var CopyBase64Cm = (MenuItem)CopyCm.Items[3];
        CopyBase64Cm.Click += async delegate { MainContextMenu.IsOpen = false; await Base64.SendToClipboard().ConfigureAwait(false); };

        ///////////////////////////
        ///   Cut File         \\\\
        ///////////////////////////
        var Cutcm = (MenuItem)MainContextMenu.Items[14];
        Cutcm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + X";
        Cutcm.Click += (_, _) => { MainContextMenu.IsOpen = false; CopyPaste.Cut(); };

        ///////////////////////////
        ///   Paste File       \\\\
        ///////////////////////////
        var pastecm = (MenuItem)MainContextMenu.Items[15];
        pastecm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + V";
        pastecm.Click += async delegate { MainContextMenu.IsOpen = false; await CopyPaste.PasteAsync().ConfigureAwait(false); };

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
        fullscreenWindow.Click += (_, _) => WindowSizing.Fullscreen_Restore(!Settings.Default.Fullscreen);

        var minWindow = (MenuItem)WindowContextMenu.Items[1];
        minWindow.Click += (_, _) => SystemCommands.MinimizeWindow(GetMainWindow);

        var closeWindow = (MenuItem)WindowContextMenu.Items[2];
        closeWindow.Click += (_, _) => SystemCommands.CloseWindow(GetMainWindow);

        GetMainWindow.Logo.ContextMenu = WindowContextMenu;
        GetMainWindow.GalleryButton.ContextMenu = WindowContextMenu;
        GetMainWindow.RotateButton.ContextMenu = WindowContextMenu;
        GetMainWindow.FlipButton.ContextMenu = WindowContextMenu;
        GetMainWindow.MinButton.ContextMenu = WindowContextMenu;
        GetMainWindow.FullscreenButton.ContextMenu = WindowContextMenu;
        GetMainWindow.CloseButton.ContextMenu = WindowContextMenu;

        NavigationContextMenu = (ContextMenu)Application.Current.Resources["navCM"];

        var nextCm = (MenuItem)NavigationContextMenu.Items[0];
        nextCm.Click += async (_, _) => await GoToNextImage(NavigateTo.Next).ConfigureAwait(false);

        var prevCm = (MenuItem)NavigationContextMenu.Items[1];
        prevCm.Click += async (_, _) => await GoToNextImage(NavigateTo.Previous).ConfigureAwait(false);

        // 2 = separator
        var firstCm = (MenuItem)NavigationContextMenu.Items[3];
        firstCm.Click += async (_, _) => await GoToNextImage(NavigateTo.First).ConfigureAwait(false);
        firstCm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + D";

        var lastCm = (MenuItem)NavigationContextMenu.Items[4];
        lastCm.Click += async (_, _) => await GoToNextImage(NavigateTo.Last).ConfigureAwait(false);
        lastCm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + A";

        // 5 = separator
        var nextFolderCm = (MenuItem)NavigationContextMenu.Items[6];
        nextFolderCm.Click += async (_, _) => await GoToNextFolder(true).ConfigureAwait(false);
        nextFolderCm.InputGestureText = $"{Application.Current.Resources["Shift"]} + D";

        var prevFolderCm = (MenuItem)NavigationContextMenu.Items[7];
        prevFolderCm.Click += async (_, _) => await GoToNextFolder(false).ConfigureAwait(false);
        prevFolderCm.InputGestureText = $"{Application.Current.Resources["Shift"]} + A";

        GetMainWindow.LeftButton.ContextMenu = NavigationContextMenu;
        GetMainWindow.RightButton.ContextMenu = NavigationContextMenu;
    }
}