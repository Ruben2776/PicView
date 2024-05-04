using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.WPF.ChangeImage;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.FileHandling;
using PicView.WPF.ImageHandling;
using PicView.WPF.Shortcuts;
using PicView.WPF.SystemIntegration;
using PicView.WPF.UILogic.Sizing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PicView.Core.Localization;
using PicView.Core.Navigation;
using PicView.Windows.Wallpaper;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.FileHandling.OpenSave;
using static PicView.WPF.UILogic.ConfigureWindows;

namespace PicView.WPF.UILogic.Loading;

internal static class LoadContextMenus
{
    internal static void AddContextMenus()
    {
        // Add main contextmenu
        MainContextMenu = (ContextMenu)Application.Current.Resources["mainCM"];
        GetMainWindow.ParentContainer.ContextMenu = MainContextMenu;
        MainContextMenu.Opened += (_, _) => FileHistoryNavigation.RefreshRecentItemsMenu();

        ///////////////////////////
        //     Open              \\
        ///////////////////////////
        var openCm = (MenuItem)MainContextMenu.Items[0];
        openCm.InputGestureText = TranslationHelper.GetTranslation("Ctrl") + " + O";
        openCm.Click += async (_, _) => await OpenAsync().ConfigureAwait(false);
        openCm.Header = TranslationHelper.GetTranslation("Open");

        ///////////////////////////
        //     Save             \\
        ///////////////////////////
        var saveCm = (MenuItem)MainContextMenu.Items[1];
        saveCm.InputGestureText = TranslationHelper.GetTranslation("Ctrl") + " + S";
        saveCm.Click += async (s, x) =>
            await SaveFilesAsync(SettingsHelper.Settings.UIProperties.ShowFileSavingDialog).ConfigureAwait(false);
        saveCm.Header = TranslationHelper.GetTranslation("Save");

        ///////////////////////////
        //       Print          \\\
        ///////////////////////////
        var printCm = (MenuItem)MainContextMenu.Items[2];
        printCm.InputGestureText = TranslationHelper.GetTranslation("Ctrl") + " + P";
        printCm.Click += (s, x) => Print(Pics[FolderIndex]);
        printCm.Header = TranslationHelper.GetTranslation("Print");

        ///////////////////////////
        //     Open With        \\\
        ///////////////////////////
        var openWcm = (MenuItem)MainContextMenu.Items[3];
        openWcm.InputGestureText = TranslationHelper.GetTranslation("Ctrl") + " + E";
        openWcm.Click += (_, _) => OpenWith();
        openWcm.Header = TranslationHelper.GetTranslation("OpenWith");

        // 4 == separator

        ///////////////////////////
        //     Sort by          \\
        ///////////////////////////
        var sortFilesByCm = (MenuItem)MainContextMenu.Items[5];
        sortFilesByCm.Header = TranslationHelper.GetTranslation("SortFilesBy");

        // FileName
        var fileNameMenu = (MenuItem)sortFilesByCm.Items[0];
        var fileNameHeader = (RadioButton)fileNameMenu.Header;
        fileNameHeader.IsChecked = SettingsHelper.Settings.Sorting.SortPreference == (int)FileListHelper.SortFilesBy.Name;
        fileNameHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileListHelper.SortFilesBy.Name).ConfigureAwait(false);
        };
        fileNameHeader.Content = TranslationHelper.GetTranslation("FileName");

        // FileSize
        var filesizeMenu = (MenuItem)sortFilesByCm.Items[1];
        var filesizeHeader = (RadioButton)filesizeMenu.Header;
        filesizeHeader.IsChecked = SettingsHelper.Settings.Sorting.SortPreference == (int)FileListHelper.SortFilesBy.FileSize;
        filesizeHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileListHelper.SortFilesBy.FileSize).ConfigureAwait(false);
        };
        filesizeHeader.Content = TranslationHelper.GetTranslation("FileSize");

        // FileExtension
        var fileExtensionMenu = (MenuItem)sortFilesByCm.Items[2];
        var fileExtensionHeader = (RadioButton)fileExtensionMenu.Header;
        fileExtensionHeader.IsChecked = SettingsHelper.Settings.Sorting.SortPreference == (int)FileListHelper.SortFilesBy.Extension;
        fileExtensionHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileListHelper.SortFilesBy.Extension).ConfigureAwait(false);
        };
        fileExtensionHeader.Content = TranslationHelper.GetTranslation("FileExtension");

        // CreationTime
        var creationTimeMenu = (MenuItem)sortFilesByCm.Items[3];
        var creationTimeHeader = (RadioButton)creationTimeMenu.Header;
        creationTimeHeader.IsChecked = SettingsHelper.Settings.Sorting.SortPreference == (int)FileListHelper.SortFilesBy.CreationTime;
        creationTimeHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileListHelper.SortFilesBy.CreationTime).ConfigureAwait(false);
        };
        creationTimeHeader.Content = TranslationHelper.GetTranslation("CreationTime");

        // LastAccessTime
        var lastAccessTimeMenu = (MenuItem)sortFilesByCm.Items[4];
        var lastAccessTimeHeader = (RadioButton)lastAccessTimeMenu.Header;
        lastAccessTimeHeader.IsChecked =
            SettingsHelper.Settings.Sorting.SortPreference == (int)FileListHelper.SortFilesBy.LastAccessTime;
        lastAccessTimeHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileListHelper.SortFilesBy.LastAccessTime).ConfigureAwait(false);
        };
        lastAccessTimeHeader.Content = TranslationHelper.GetTranslation("LastAccessTime");

        // LastWriteTime
        var lastWriteTimeMenu = (MenuItem)sortFilesByCm.Items[5];
        var lastWriteTimeHeader = (RadioButton)lastWriteTimeMenu.Header;
        lastWriteTimeHeader.IsChecked =
            SettingsHelper.Settings.Sorting.SortPreference == (int)FileListHelper.SortFilesBy.LastWriteTime;
        lastWriteTimeHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileListHelper.SortFilesBy.LastWriteTime).ConfigureAwait(false);
        };
        lastWriteTimeHeader.Content = TranslationHelper.GetTranslation("LastWriteTime");

        // Random
        var randomMenu = (MenuItem)sortFilesByCm.Items[6];
        var randomHeader = (RadioButton)randomMenu.Header;
        randomHeader.IsChecked = SettingsHelper.Settings.Sorting.SortPreference == (int)FileListHelper.SortFilesBy.Random;
        randomHeader.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await UpdateUIValues.ChangeSortingAsync(FileListHelper.SortFilesBy.Random).ConfigureAwait(false);
        };
        randomHeader.Content = TranslationHelper.GetTranslation("Random");

        // 7 = separator

        // Ascending
        var ascendingMenu = (MenuItem)sortFilesByCm.Items[8];
        var ascendingHeader = (RadioButton)ascendingMenu.Header;
        ascendingHeader.IsChecked = SettingsHelper.Settings.Sorting.Ascending;
        ascendingHeader.Click += async (_, _) =>
        {
            SettingsHelper.Settings.Sorting.Ascending = true;
            await UpdateUIValues.ChangeSortingAsync(0, true).ConfigureAwait(false);
        };
        ascendingHeader.Content = TranslationHelper.GetTranslation("Ascending");

        // Descending
        var descendingMenu = (MenuItem)sortFilesByCm.Items[9];
        var descendingHeader = (RadioButton)descendingMenu.Header;
        descendingHeader.IsChecked = SettingsHelper.Settings.Sorting.Ascending == false;
        descendingHeader.Click += async (_, _) =>
        {
            SettingsHelper.Settings.Sorting.Ascending = false;
            await UpdateUIValues.ChangeSortingAsync(0, true).ConfigureAwait(false);
        };
        descendingHeader.Content = TranslationHelper.GetTranslation("Descending");

        ///////////////////////////
        //     Recent files      \\
        ///////////////////////////
        var recentMenu = (MenuItem)MainContextMenu.Items[6];
        recentMenu.Header = TranslationHelper.GetTranslation("RecentFiles");

        ///////////////////////////
        //      Settings         \\
        ///////////////////////////
        var settingsCm = (MenuItem)MainContextMenu.Items[7];
        settingsCm.Header = TranslationHelper.GetTranslation("Settings");

        // Looping
        var loopingMenu = (MenuItem)settingsCm.Items[0];
        var loopingHeader = (CheckBox)loopingMenu.Header;
        loopingHeader.IsChecked = SettingsHelper.Settings.UIProperties.Looping;
        loopingHeader.Click += (_, _) => UpdateUIValues.SetLooping();
        loopingMenu.Click += (_, _) =>
        {
            UpdateUIValues.SetLooping();
            loopingHeader.IsChecked = !loopingHeader.IsChecked;
        };
        loopingHeader.Content = TranslationHelper.GetTranslation("ToggleLooping");

        // Scrolling
        var ScrollingMenu = (MenuItem)settingsCm.Items[1];
        var ScrollingHeader = (CheckBox)ScrollingMenu.Header;
        ScrollingHeader.IsChecked = SettingsHelper.Settings.Zoom.ScrollEnabled;
        ScrollingHeader.Click += (_, _) => UpdateUIValues.SetScrolling();
        ScrollingMenu.Click += (_, _) =>
        {
            UpdateUIValues.SetScrolling();
            ScrollingHeader.IsChecked = !ScrollingHeader.IsChecked;
        };
        ScrollingHeader.Content = TranslationHelper.GetTranslation("Scrolling");

        // ToogleUI
        var ToogleUIMenu = (MenuItem)settingsCm.Items[2];
        ToogleUIMenu.InputGestureText = TranslationHelper.GetTranslation("Alt") + " + Z";
        var ToogleUIHeader = (CheckBox)ToogleUIMenu.Header;
        ToogleUIHeader.IsChecked = SettingsHelper.Settings.UIProperties.ShowInterface;
        ToogleUIHeader.Click += (_, _) => HideInterfaceLogic.ToggleInterface();
        ToogleUIMenu.Click += (_, _) =>
        {
            HideInterfaceLogic.ToggleInterface();
            ToogleUIHeader.IsChecked = !ToogleUIHeader.IsChecked;
        };
        ToogleUIHeader.Content = TranslationHelper.GetTranslation("ShowHideUI");

        // Change bg
        var ChangeBackgroundMenu = (MenuItem)settingsCm.Items[3];
        ChangeBackgroundMenu.Click += (_, _) => ConfigColors.ChangeBackground();
        ChangeBackgroundMenu.InputGestureText = CustomKeybindings.CustomShortcuts?
            .FirstOrDefault(kv => kv.Value?.Method?.Name == "ChangeBackground")
            .Key.ToString() ?? string.Empty;
        ChangeBackgroundMenu.ToolTip = TranslationHelper.GetTranslation("ChangeBackgroundTooltip");
        ChangeBackgroundMenu.Header = TranslationHelper.GetTranslation("ChangeBackground");

        // Topmost
        var TopmostMenu = (MenuItem)settingsCm.Items[4];
        var TopmostHeader = (CheckBox)TopmostMenu.Header;
        TopmostHeader.IsChecked = SettingsHelper.Settings.WindowProperties.TopMost;
        TopmostHeader.Click += (_, _) => UpdateUIValues.SetTopMost();
        TopmostMenu.Click += (_, _) => UpdateUIValues.SetTopMost();
        TopmostMenu.InputGestureText = CustomKeybindings.CustomShortcuts?
            .FirstOrDefault(kv => kv.Value?.Method?.Name == "SetTopMost")
            .Key.ToString() ?? string.Empty;
        TopmostHeader.Content = TranslationHelper.GetTranslation("StayTopMost");

        // Fill Image Height
        var imageHeightMenu = (MenuItem)settingsCm.Items[5];
        var imageHeightHeader = (CheckBox)imageHeightMenu.Header;
        imageHeightHeader.IsChecked = SettingsHelper.Settings.ImageScaling.StretchImage;
        imageHeightHeader.Click += async (_, _) => await UpdateUIValues.SetAutoFill().ConfigureAwait(false);
        imageHeightMenu.Click += async (_, _) => await UpdateUIValues.SetAutoFill().ConfigureAwait(false);
        imageHeightMenu.InputGestureText = CustomKeybindings.CustomShortcuts?
            .FirstOrDefault(kv => kv.Value?.Method?.Name == "Stretch")
            .Key.ToString() ?? string.Empty;
        imageHeightHeader.Content = TranslationHelper.GetTranslation("StretchImage");

        // Ctrl to zoom
        var ctrlZoomMenu = (MenuItem)settingsCm.Items[6];
        var ctrlZoomHeader = (CheckBox)ctrlZoomMenu.Header;
        ctrlZoomHeader.IsChecked = SettingsHelper.Settings.Zoom.CtrlZoom;
        ctrlZoomMenu.Click += (_, _) => UpdateUIValues.SetCtrlToZoom(ctrlZoomHeader.IsChecked.Value);
        ctrlZoomHeader.Click += (_, _) => UpdateUIValues.SetCtrlToZoom(ctrlZoomHeader.IsChecked.Value);
        ctrlZoomHeader.Content = TranslationHelper.GetTranslation("CtrlToZoom");

        // 7 = seperator

        // Settings
        var SettingsMenu = (MenuItem)settingsCm.Items[8];
        SettingsMenu.Click += (_, _) =>
        {
            SettingsWindow();
            MainContextMenu.IsOpen = false;
        };
        SettingsMenu.ToolTip = TranslationHelper.GetTranslation("Settings");
        SettingsMenu.Header = TranslationHelper.GetTranslation("Settings");

        // 8 = seperator

        ///////////////////////////
        ///   Set as           \\\\
        ///////////////////////////
        var SetAsCm = (MenuItem)MainContextMenu.Items[9];
        SetAsCm.Header = TranslationHelper.GetTranslation("SetAs");
        var SetWallpaperCm = (MenuItem)SetAsCm.Items[0];
        SetWallpaperCm.Click += async (_, _) =>
        {
            MainContextMenu.IsOpen = false;
            await Wallpaper.SetWallpaperAsync(WallpaperHelper.WallpaperStyle.Fill).ConfigureAwait(false);
        };
        SetWallpaperCm.Header = TranslationHelper.GetTranslation("SetAsWallpaper");
        var SetLockCm = (MenuItem)SetAsCm.Items[1];
        SetLockCm.Click += async (_, _) =>
        {
            MainContextMenu.IsOpen = false;
            await LockScreenHelper.SetLockScreenImageAsync().ConfigureAwait(false);
        };
        SetLockCm.Header = TranslationHelper.GetTranslation("SetAsLockScreenImage");

        ///////////////////////////
        ///   ShowInFolder     \\\\
        ///////////////////////////
        var ShowInFolderCm = (MenuItem)MainContextMenu.Items[10];
        ShowInFolderCm.Click += (_, _) => OpenInExplorer(Pics?[FolderIndex]);
        ShowInFolderCm.InputGestureText = CustomKeybindings.CustomShortcuts?
            .FirstOrDefault(kv => kv.Value?.Method?.Name == "OpenInExplorer")
            .Key.ToString() ?? string.Empty;
        ShowInFolderCm.Header = TranslationHelper.GetTranslation("ShowInFolder");

        ///////////////////////////
        ///   Image choices    \\\\
        ///////////////////////////
        var ImageChoices = (MenuItem)MainContextMenu.Items[11];
        ImageChoices.Header = TranslationHelper.GetTranslation("Image");

        var ImageInfoCm = (MenuItem)ImageChoices.Items[0];
        ImageInfoCm.Click += (_, _) => ImageInfoWindow();
        ImageInfoCm.InputGestureText = CustomKeybindings.CustomShortcuts?
            .FirstOrDefault(kv => kv.Value?.Method.Name == "ImageInfoWindow")
            .Key.ToString() ?? string.Empty;
        ImageInfoCm.ToolTip = TranslationHelper.GetTranslation("ShowImageInfo");
        ImageInfoCm.Header = TranslationHelper.GetTranslation("ImageInfo");

        var FileProps = (MenuItem)ImageChoices.Items[1];
        FileProps.Click += (_, _) => _ = UIHelper.ShowFileProperties();
        FileProps.InputGestureText = TranslationHelper.GetTranslation("Ctrl") + " + I";
        FileProps.Header = TranslationHelper.GetTranslation("FileProperties");

        // 2 = seperator

        var ImageSize = (MenuItem)ImageChoices.Items[3];
        ImageSize.Click += (_, _) => UpdateUIValues.ToggleQuickResize();
        ImageSize.InputGestureText = CustomKeybindings.CustomShortcuts?
            .FirstOrDefault(kv => kv.Value?.Method.Name == "ResizeImage")
            .Key.ToString() ?? string.Empty;
        ImageSize.Header = TranslationHelper.GetTranslation("ResizeImage");

        var BatchSize = (MenuItem)ImageChoices.Items[4];
        BatchSize.Click += (_, _) => ResizeWindow();
        BatchSize.InputGestureText = CustomKeybindings.CustomShortcuts?
            .FirstOrDefault(kv => kv.Value?.Method.Name == "ResizeWindow")
            .Key.ToString() ?? string.Empty;
        BatchSize.Header = TranslationHelper.GetTranslation("BatchResize");

        // 12 = seperator

        ///////////////////////////
        ///   Copy             \\\\
        ///////////////////////////
        var CopyCm = (MenuItem)MainContextMenu.Items[13];
        CopyCm.Header = TranslationHelper.GetTranslation("Copy");

        // Copy file
        var copyFileCm = (MenuItem)CopyCm.Items[0];
        copyFileCm.InputGestureText = TranslationHelper.GetTranslation("Ctrl") + " + C";
        copyFileCm.Click += (_, _) => CopyPaste.CopyFile();
        copyFileCm.Header = TranslationHelper.GetTranslation("CopyFile");

        // FileCopyPath
        var FileCopyPathCm = (MenuItem)CopyCm.Items[1];
        FileCopyPathCm.InputGestureText =
            $"{TranslationHelper.GetTranslation("Ctrl")} + {TranslationHelper.GetTranslation("Alt")} + C";
        FileCopyPathCm.Click += (_, _) => CopyPaste.CopyFilePath();
        FileCopyPathCm.Header = TranslationHelper.GetTranslation("FileCopyPath");

        // CopyImage
        var CopyImageCm = (MenuItem)CopyCm.Items[2];
        CopyImageCm.InputGestureText =
            $"{TranslationHelper.GetTranslation("Ctrl")} + {TranslationHelper.GetTranslation("Shift")} + C";
        CopyImageCm.Click += (_, _) => CopyPaste.CopyBitmap();
        CopyImageCm.Header = TranslationHelper.GetTranslation("CopyImage");

        // CopyBase64
        var CopyBase64Cm = (MenuItem)CopyCm.Items[3];
        CopyBase64Cm.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await Base64.SendToClipboard().ConfigureAwait(false);
        };
        CopyBase64Cm.Header = $"{TranslationHelper.GetTranslation("Copy")} base64";

        ///////////////////////////
        ///   Duplicate File         \\\\
        ///////////////////////////
        var Dupcm = (MenuItem)MainContextMenu.Items[14];
        Dupcm.Click += async (_, _) =>
        {
            MainContextMenu.IsOpen = false;
            await CopyPaste.DuplicateFile().ConfigureAwait(false);
        };
        Dupcm.InputGestureText = CustomKeybindings.CustomShortcuts?
            .FirstOrDefault(kv => kv.Value?.Method?.Name == "DuplicateFile")
            .Key.ToString() ?? string.Empty;
        Dupcm.Header = TranslationHelper.GetTranslation("DuplicateFile");

        ///////////////////////////
        ///   Cut File         \\\\
        ///////////////////////////
        var Cutcm = (MenuItem)MainContextMenu.Items[15];
        Cutcm.InputGestureText = TranslationHelper.GetTranslation("Ctrl") + " + X";
        Cutcm.Click += (_, _) =>
        {
            MainContextMenu.IsOpen = false;
            CopyPaste.Cut();
        };
        Cutcm.Header = TranslationHelper.GetTranslation("FileCut");

        ///////////////////////////
        ///   Paste File       \\\\
        ///////////////////////////
        var pastecm = (MenuItem)MainContextMenu.Items[16];
        pastecm.InputGestureText = TranslationHelper.GetTranslation("Ctrl") + " + V";
        pastecm.Click += async delegate
        {
            MainContextMenu.IsOpen = false;
            await CopyPaste.PasteAsync().ConfigureAwait(false);
        };
        pastecm.Header = TranslationHelper.GetTranslation("FilePaste");

        // 17 = seperator

        ///////////////////////////
        ///   Delete File       \\\\
        ///////////////////////////
        var Deletecm = (MenuItem)MainContextMenu.Items[18];
        Deletecm.Click += (_, _) => DeleteFiles.DeleteCurrentFile(
                    Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
        Deletecm.ToolTip = TranslationHelper.GetTranslation("SendCurrentImageToRecycleBin");
        Deletecm.Header = TranslationHelper.GetTranslation("DeleteFile");

        // 19 = seperator

        ///////////////////////////
        ///   Close       \\\\
        ///////////////////////////
        var CloseCm = (MenuItem)MainContextMenu.Items[20];
        CloseCm.Click += (_, _) => SystemCommands.CloseWindow(GetMainWindow);
        CloseCm.Header = TranslationHelper.GetTranslation("Close");

        // Add Window contextmenu
        WindowContextMenu = (ContextMenu)Application.Current.Resources["windowCM"];

        var fullscreenWindow = (MenuItem)WindowContextMenu.Items[0];
        fullscreenWindow.Click += (_, _) => WindowSizing.Fullscreen_Restore(!SettingsHelper.Settings.WindowProperties.Fullscreen);
        fullscreenWindow.Header = TranslationHelper.GetTranslation("ToggleFullscreen");

        var minWindow = (MenuItem)WindowContextMenu.Items[1];
        minWindow.Click += (_, _) => SystemCommands.MinimizeWindow(GetMainWindow);
        minWindow.Header = TranslationHelper.GetTranslation("Minimize");

        var closeWindow = (MenuItem)WindowContextMenu.Items[2];
        closeWindow.Click += (_, _) => SystemCommands.CloseWindow(GetMainWindow);
        closeWindow.Header = TranslationHelper.GetTranslation("Close");

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
        var nextKey = CustomKeybindings.CustomShortcuts?
            .FirstOrDefault(kv => kv.Value?.Method?.Name == "Next")
            .Key;
        nextCm.InputGestureText = nextKey?.ToString();
        nextCm.Header = TranslationHelper.GetTranslation("NextImage");

        var prevCm = (MenuItem)NavigationContextMenu.Items[1];
        prevCm.Click += async (_, _) => await GoToNextImage(NavigateTo.Previous).ConfigureAwait(false);
        var prevKey = CustomKeybindings.CustomShortcuts?
            .FirstOrDefault(kv => kv.Value?.Method?.Name == "Prev")
            .Key;
        prevCm.InputGestureText = prevKey?.ToString();
        prevCm.Header = TranslationHelper.GetTranslation("PrevImage");

        // 2 = separator
        var firstCm = (MenuItem)NavigationContextMenu.Items[3];
        firstCm.Click += async (_, _) => await GoToNextImage(NavigateTo.First).ConfigureAwait(false);
        firstCm.InputGestureText = $"{TranslationHelper.GetTranslation("Ctrl")} + {prevKey?.ToString()}";
        firstCm.Header = TranslationHelper.GetTranslation("FirstImage");

        var lastCm = (MenuItem)NavigationContextMenu.Items[4];
        lastCm.Click += async (_, _) => await GoToNextImage(NavigateTo.Last).ConfigureAwait(false);
        lastCm.InputGestureText = $"{TranslationHelper.GetTranslation("Ctrl")} + {nextKey?.ToString()}";
        lastCm.Header = TranslationHelper.GetTranslation("LastImage");

        // 5 = separator
        var nextFolderCm = (MenuItem)NavigationContextMenu.Items[6];
        nextFolderCm.Click += async (_, _) => await GoToNextFolder(true).ConfigureAwait(false);
        nextFolderCm.InputGestureText = $"{TranslationHelper.GetTranslation("Shift")} + {nextKey?.ToString()}";
        nextFolderCm.Header = TranslationHelper.GetTranslation("NextFolder");

        var prevFolderCm = (MenuItem)NavigationContextMenu.Items[7];
        prevFolderCm.Click += async (_, _) => await GoToNextFolder(false).ConfigureAwait(false);
        prevFolderCm.InputGestureText = $"{TranslationHelper.GetTranslation("Shift")} + {prevKey?.ToString()}";
        prevFolderCm.Header = TranslationHelper.GetTranslation("PrevFolder");

        GetMainWindow.LeftButton.ContextMenu = NavigationContextMenu;
        GetMainWindow.RightButton.ContextMenu = NavigationContextMenu;
    }
}