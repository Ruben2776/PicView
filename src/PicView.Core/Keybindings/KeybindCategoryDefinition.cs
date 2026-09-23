using PicView.Core.Localization;

namespace PicView.Core.Keybindings;

/// <summary>
/// Provides the ordered list of (FunctionName, DisplayName) entries for each keybinding category.
/// This is a pure-data mapping used by the KeybindView to dynamically populate KeybindBox controls.
/// </summary>
public static class KeybindCategoryDefinition
{
    public static List<(string FunctionName, string DisplayName)> GetNavigationEntries(LanguageModel t) =>
    [
        ("Next", t.NextImage ?? string.Empty),
        ("Next10", t.AdvanceBy10Images ?? string.Empty),
        ("Next100", t.AdvanceBy100Images ?? string.Empty),
        ("Prev", t.PrevImage ?? string.Empty),
        ("Prev10", t.GoBackBy10Images ?? string.Empty),
        ("Prev100", t.GoBackBy100Images ?? string.Empty),
        ("Last", t.LastImage ?? string.Empty),
        ("First", t.FirstImage ?? string.Empty),
        ("NextFolder", t.NextFolder ?? string.Empty),
        ("PrevFolder", t.PrevFolder ?? string.Empty),
        ("NextArchive", t.NextArchive ?? string.Empty),
        ("PrevArchive", t.PrevArchive ?? string.Empty),
        ("Search", t.Search ?? string.Empty),
        ("GalleryClick", t.SelectGalleryThumb ?? string.Empty),
        ("ToggleLooping", t.ToggleLooping ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetScrollAndRotateEntries(LanguageModel t) =>
    [
        ("Up", t.RotateRight ?? string.Empty),
        ("ScrollUp", t.ScrollUp ?? string.Empty),
        ("Down", t.RotateLeft ?? string.Empty),
        ("ScrollDown", t.ScrollDown ?? string.Empty),
        ("ScrollToTop", t.ScrollToTop ?? string.Empty),
        ("ScrollToBottom", t.ScrollToBottom ?? string.Empty),
        ("ToggleScroll", t.ToggleScroll ?? string.Empty),
        ("ChangeCtrlZoom", t.CtrlToZoom ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetZoomEntries(LanguageModel t) =>
    [
        ("ZoomIn", t.ZoomIn ?? string.Empty),
        ("ZoomOut", t.ZoomOut ?? string.Empty),
        ("ResetZoom", t.ResetZoom ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetImageControlEntries(LanguageModel t) =>
    [
        ("SideBySide", t.SideBySide ?? string.Empty),
        ("Stretch", t.Stretch ?? string.Empty),
        ("Flip", t.Flip ?? string.Empty),
        ("Crop", t.Crop ?? string.Empty),
        ("ChangeBackground", t.ChangeBackground ?? string.Empty),
        ("OptimizeImage", t.OptimizeImage ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetInterfaceConfigurationEntries(LanguageModel t) =>
    [
        ("ToggleInterface", t.HideUI ?? string.Empty),
        ("Slideshow", t.Slideshow ?? string.Empty),
        ("ToggleGallery", t.ShowImageGallery ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetFileManagementEntries(LanguageModel t) =>
    [
        ("Open", t.Open ?? string.Empty),
        ("OpenWith", t.OpenWith ?? string.Empty),
        ("OpenInExplorer", t.ShowInFolder ?? string.Empty),
        ("Reload", t.Reload ?? string.Empty),
        ("Save", t.Save ?? string.Empty),
        ("SaveAs", t.SaveAs ?? string.Empty),
        ("Print", t.Print ?? string.Empty),
        ("DeleteFile", t.DeleteFile ?? string.Empty),
        ("DeleteFilePermanently", t.PermanentlyDelete ?? string.Empty),
        ("Rename", t.RenameFile ?? string.Empty),
        ("ShowFileProperties", t.FileProperties ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetSortFilesEntries(LanguageModel t) =>
    [
        ("SortFilesByName", t.FileName ?? string.Empty),
        ("SortFilesBySize", t.FileSize ?? string.Empty),
        ("SortFilesByExtension", t.FileExtension ?? string.Empty),
        ("SortFilesByCreationTime", t.Created ?? string.Empty),
        ("SortFilesByLastAccessTime", t.LastAccessTime ?? string.Empty),
        ("SortFilesRandomly", t.Random ?? string.Empty),
        ("SortFilesAscending", t.Ascending ?? string.Empty),
        ("SortFilesDescending", t.Descending ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetCopyEntries(LanguageModel t) =>
    [
        ("CopyFile", t.CopyFile ?? string.Empty),
        ("CopyFilePath", t.FileCopyPath ?? string.Empty),
        ("CopyImage", t.CopyImage ?? string.Empty),
        ("CopyBase64", (t.Copy ?? "Copy") + " base64"),
        ("Paste", t.FilePaste ?? string.Empty),
        ("DuplicateFile", t.DuplicateFile ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetTabManagementEntries(LanguageModel t) =>
    [
        ("NewTab", t.NewTab ?? string.Empty),
        ("CloseTab", t.CloseTab ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetToolWindowsEntries(LanguageModel t) =>
    [
        ("AboutWindow", t.About ?? string.Empty),
        ("SettingsWindow", t.Settings ?? string.Empty),
        ("ImageInfoWindow", t.ImageInfo ?? string.Empty),
        ("EffectsWindow", t.Effects ?? string.Empty),
        ("ConvertWindow", t.FileConversion ?? string.Empty),
        ("KeybindingsWindow", t.ApplicationShortcuts ?? string.Empty),
        ("BatchResizeWindow", t.BatchResize ?? string.Empty),
        ("ResizeWindow", t.Resize ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetWindowManagementEntries(LanguageModel t) =>
    [
        ("Close", t.Close ?? string.Empty),
        ("NewWindow", t.NewWindow ?? string.Empty),
        ("Center", t.CenterWindow ?? string.Empty),
        ("SetTopMost", t.StayTopMost ?? string.Empty),
        ("Fullscreen", t.ToggleFullscreen ?? string.Empty),
        ("Maximize", t.Maximize ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetWindowScalingEntries(LanguageModel t) =>
    [
        ("AutoFitWindow", t.AutoFitWindow ?? string.Empty),
        ("NormalWindow", t.NormalWindow ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetStarRatingEntries(LanguageModel t) =>
    [
        ("Set1Star", t._1Star ?? string.Empty),
        ("Set2Star", t._2Star ?? string.Empty),
        ("Set3Star", t._3Star ?? string.Empty),
        ("Set4Star", t._4Star ?? string.Empty),
        ("Set5Star", t._5Star ?? string.Empty),
        ("Set0Star", t.RemoveStarRating ?? string.Empty),
    ];

    /// <summary>
    /// Returns all categories with their entries in display order.
    /// Each tuple contains (CategoryName, Entries) where CategoryName is the internal identifier.
    /// </summary>
    public static List<(string CategoryName, List<(string FunctionName, string DisplayName)> Entries)> GetAllCategories(
        LanguageModel t) =>
    [
        ("Navigation", GetNavigationEntries(t)),
        ("ScrollAndRotate", GetScrollAndRotateEntries(t)),
        ("Zoom", GetZoomEntries(t)),
        ("ImageControl", GetImageControlEntries(t)),
        ("InterfaceConfiguration", GetInterfaceConfigurationEntries(t)),
        ("FileManagement", GetFileManagementEntries(t)),
        ("SortFilesBy", GetSortFilesEntries(t)),
        ("Copy", GetCopyEntries(t)),
        ("TabManagement", GetTabManagementEntries(t)),
        ("ToolWindows", GetToolWindowsEntries(t)),
        ("WindowManagement", GetWindowManagementEntries(t)),
        ("WindowScaling", GetWindowScalingEntries(t)),
        ("SetStarRating", GetStarRatingEntries(t)),
    ];
}
