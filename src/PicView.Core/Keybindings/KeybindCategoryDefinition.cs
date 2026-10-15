using System.Collections.Frozen;
using System.Globalization;
using PicView.Core.Favorites;
using PicView.Core.Localization;

namespace PicView.Core.Keybindings;

/// <summary>
/// Provides the ordered list of (FunctionName, DisplayName) entries for each keybinding category.
/// This is a pure-data mapping used by the KeybindView to dynamically populate KeybindBox controls.
/// </summary>
public static class KeybindCategoryDefinition
{
    /// <summary>
    /// Legacy action names kept for backwards compatibility, mapped to their canonical name.
    /// </summary>
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal)
    {
        ["Fullscreen"] = "ToggleFullscreen",
        ["Stretch"] = "ZoomToFit",
        ["GalleryClick"] = "Confirm",
    };

    /// <summary>
    /// Bindable actions that are not shown in any category of the keybindings view.
    /// </summary>
    private static readonly string[] HiddenActionNames =
    [
        "CheckForUpdates", "ColorPicker", "CutFile", "FileAssociationsWindow", "ResetZoomAndRotations",
        "ResizeImage", "Restart", "Restore", "Rotate0", "Rotate90", "Rotate180", "Rotate270", "RotateLeft",
        "RotateRight", "SaveAsPDF", "SetAsLockScreen", "SetAsLockscreenCentered", "SetAsWallpaper",
        "SetAsWallpaperCentered", "SetAsWallpaperFilled", "SetAsWallpaperFitted", "SetAsWallpaperStretched",
        "SetAsWallpaperTiled", "ShowFavoritesFile", "ShowKeybindingsFile", "ShowRecentHistoryFile",
        "ShowSettingsFile", "SortFilesByLastWriteTime",
    ];

    private const string FavoriteActionPrefix = "OpenFavorite";

    /// <summary>
    /// Contextual Actions: generic Actions whose meaning comes from the window's Input Mode. Their Keybinds
    /// form a separate layer, so a Keybind can be bound to one of them and to an ordinary Action.
    /// </summary>
    public static IReadOnlyList<string> ContextualActionNames { get; } = ["Confirm", "Cancel", "PlayPause", "Copy"];

    /// <summary>
    /// Actions that need a still image, which non-blocking Input Modes such as a playing motion photo disallow.
    /// Tool windows are not among them, since they follow the current image as it changes.
    /// </summary>
    private static readonly string[] StillImageActionNames = ["Crop"];

    /// <summary>
    /// Whether <paramref name="action"/> needs a still image.
    /// </summary>
    public static bool NeedsStillImage(string action) =>
        StillImageActionNames.Contains(Canonicalize(action), StringComparer.Ordinal);

    /// <summary>
    /// Whether <paramref name="action"/> is a Contextual Action.
    /// </summary>
    public static bool IsContextualAction(string action) =>
        ContextualActionNames.Contains(Canonicalize(action), StringComparer.Ordinal);

    /// <summary>
    /// The action opening the favorite in each slot, in slot order.
    /// </summary>
    public static IReadOnlyList<string> FavoriteActionNames { get; } =
        [.. Enumerable.Range(1, FavoritesManager.MaxSlots).Select(FavoriteAction)];

    /// <summary>
    /// Every canonical action name, including actions hidden from the keybindings view.
    /// </summary>
    public static FrozenSet<string> ActionNames { get; } = GetAllCategories(new LanguageModel())
        .SelectMany(c => c.Entries)
        .Select(e => e.FunctionName)
        .Concat(HiddenActionNames)
        .Concat(FavoriteActionNames)
        .ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Returns the action opening the favorite in <paramref name="slot"/>.
    /// </summary>
    public static string FavoriteAction(int slot) =>
        FavoriteActionPrefix + slot.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns the slot that <paramref name="action"/> opens, or <c>null</c> when it is not a favorite action.
    /// </summary>
    public static int? FavoriteSlot(string action)
    {
        if (!action.StartsWith(FavoriteActionPrefix, StringComparison.Ordinal) ||
            !int.TryParse(action.AsSpan(FavoriteActionPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var slot) ||
            slot < 1 || slot > FavoritesManager.MaxSlots)
        {
            return null;
        }

        // Reject non-canonical spellings such as "OpenFavorite01"
        return string.Equals(FavoriteAction(slot), action, StringComparison.Ordinal) ? slot : null;
    }

    /// <summary>
    /// Returns the canonical action name for <paramref name="name"/>. Unknown names are returned unchanged.
    /// </summary>
    public static string Canonicalize(string name) =>
        Aliases.GetValueOrDefault(name, name);

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
        ("FileHistoryNext", t.NextFileInHistory ?? string.Empty),
        ("FileHistoryPrev", t.PreviousFileInHistory ?? string.Empty),
        ("Search", t.Search ?? string.Empty),
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
        ("ZoomToFit", t.ZoomToFit ?? string.Empty),
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
        OperatingSystem.IsMacOS()
            ?
            [
                ("CopyFile", t.CopyFile ?? string.Empty),
                ("CopyFilePath", t.FileCopyPath ?? string.Empty),
                ("CopyImage", t.CopyImage ?? string.Empty),
                ("CopyBase64", (t.Copy ?? "Copy") + " base64"),
                ("Paste", t.FilePaste ?? string.Empty),
                ("DuplicateFile", t.DuplicateFile ?? string.Empty),
            ]
            :
            [
                ("CopyFile", t.CopyFile ?? string.Empty),
                ("CutFile", t.Cut ?? string.Empty),
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
        ("ToggleFullscreen", t.ToggleFullscreen ?? string.Empty),
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
    /// <summary>
    /// The Contextual Actions, in their own category: they are bound in a separate layer and are left out of
    /// <see cref="GetAllCategories"/>.
    /// </summary>
    public static List<(string FunctionName, string DisplayName)> GetContextualEntries(LanguageModel t) =>
    [
        ("Confirm", t.Confirm ?? string.Empty),
        ("Cancel", t.Cancel ?? string.Empty),
        ("PlayPause", t.PlayPause ?? string.Empty),
        ("Copy", t.Copy ?? string.Empty),
    ];

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
