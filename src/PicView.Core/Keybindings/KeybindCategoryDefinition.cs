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
        Actions.CheckForUpdates, Actions.ColorPicker, Actions.CutFile, Actions.FileAssociationsWindow, Actions.ResetZoomAndRotations,
        Actions.ResizeImage, Actions.Restart, Actions.Restore, Actions.Rotate0, Actions.Rotate90, Actions.Rotate180, Actions.Rotate270, Actions.RotateLeft,
        Actions.RotateRight, Actions.SaveAsPDF, Actions.SetAsLockScreen, Actions.SetAsLockscreenCentered, Actions.SetAsWallpaper,
        Actions.SetAsWallpaperCentered, Actions.SetAsWallpaperFilled, Actions.SetAsWallpaperFitted, Actions.SetAsWallpaperStretched,
        Actions.SetAsWallpaperTiled, Actions.ShowFavoritesFile, Actions.ShowKeybindingsFile, Actions.ShowRecentHistoryFile,
        Actions.ShowSettingsFile, Actions.SortFilesByLastWriteTime,
        Actions.CloseWindow, Actions.Minimize, Actions.OpenCloseDockedGallery, Actions.ResetSettings,
        Actions.ToggleBottomToolbar, Actions.ToggleConstrainBackgroundColor, Actions.ToggleDockedGalleryInHiddenUI,
        Actions.ToggleFileHistory, Actions.ToggleHoverBar, Actions.ToggleOpeningInSameWindow,
        Actions.ToggleShowFullPathInTitleBar, Actions.ToggleSubdirectories, Actions.ToggleTaskbarProgress,
    ];

    private const string FavoriteActionPrefix = "OpenFavorite";

    /// <summary>
    /// Contextual Actions: generic Actions whose meaning comes from the window's Input Mode. Their Keybinds
    /// form a separate layer, so a Keybind can be bound to one of them and to an ordinary Action.
    /// </summary>
    public static IReadOnlyList<string> ContextualActionNames { get; } = ["Confirm", "PlayPause"];

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
        (Actions.Next, t.NextImage ?? string.Empty),
        (Actions.Next10, t.AdvanceBy10Images ?? string.Empty),
        (Actions.Next100, t.AdvanceBy100Images ?? string.Empty),
        (Actions.Prev, t.PrevImage ?? string.Empty),
        (Actions.Prev10, t.GoBackBy10Images ?? string.Empty),
        (Actions.Prev100, t.GoBackBy100Images ?? string.Empty),
        (Actions.Last, t.LastImage ?? string.Empty),
        (Actions.First, t.FirstImage ?? string.Empty),
        (Actions.NextFolder, t.NextFolder ?? string.Empty),
        (Actions.PrevFolder, t.PrevFolder ?? string.Empty),
        (Actions.NextArchive, t.NextArchive ?? string.Empty),
        (Actions.PrevArchive, t.PrevArchive ?? string.Empty),
        (Actions.FileHistoryNext, t.NextFileInHistory ?? string.Empty),
        (Actions.FileHistoryPrev, t.PreviousFileInHistory ?? string.Empty),
        (Actions.Search, t.Search ?? string.Empty),
        (Actions.ToggleLooping, t.ToggleLooping ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetScrollAndRotateEntries(LanguageModel t) =>
    [
        (Actions.Up, t.RotateRight ?? string.Empty),
        (Actions.ScrollUp, t.ScrollUp ?? string.Empty),
        (Actions.Down, t.RotateLeft ?? string.Empty),
        (Actions.ScrollDown, t.ScrollDown ?? string.Empty),
        (Actions.ScrollToTop, t.ScrollToTop ?? string.Empty),
        (Actions.ScrollToBottom, t.ScrollToBottom ?? string.Empty),
        (Actions.ToggleScroll, t.ToggleScroll ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetZoomEntries(LanguageModel t) =>
    [
        (Actions.ZoomIn, t.ZoomIn ?? string.Empty),
        (Actions.ZoomOut, t.ZoomOut ?? string.Empty),
        (Actions.ResetZoom, t.ResetZoom ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetImageControlEntries(LanguageModel t) =>
    [
        (Actions.SideBySide, t.SideBySide ?? string.Empty),
        (Actions.ZoomToFit, t.ZoomToFit ?? string.Empty),
        (Actions.Flip, t.Flip ?? string.Empty),
        (Actions.Crop, t.Crop ?? string.Empty),
        (Actions.ChangeBackground, t.ChangeBackground ?? string.Empty),
        (Actions.OptimizeImage, t.OptimizeImage ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetInterfaceConfigurationEntries(LanguageModel t) =>
    [
        (Actions.ToggleInterface, t.HideUI ?? string.Empty),
        (Actions.Slideshow, t.Slideshow ?? string.Empty),
        (Actions.ToggleGallery, t.ShowImageGallery ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetFileManagementEntries(LanguageModel t) =>
    [
        (Actions.Open, t.Open ?? string.Empty),
        (Actions.OpenWith, t.OpenWith ?? string.Empty),
        (Actions.OpenInExplorer, t.ShowInFolder ?? string.Empty),
        (Actions.Reload, t.Reload ?? string.Empty),
        (Actions.Save, t.Save ?? string.Empty),
        (Actions.SaveAs, t.SaveAs ?? string.Empty),
        (Actions.Print, t.Print ?? string.Empty),
        (Actions.DeleteFile, t.DeleteFile ?? string.Empty),
        (Actions.DeleteFilePermanently, t.PermanentlyDelete ?? string.Empty),
        (Actions.Rename, t.RenameFile ?? string.Empty),
        (Actions.ShowFileProperties, t.FileProperties ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetSortFilesEntries(LanguageModel t) =>
    [
        (Actions.SortFilesByName, t.FileName ?? string.Empty),
        (Actions.SortFilesBySize, t.FileSize ?? string.Empty),
        (Actions.SortFilesByExtension, t.FileExtension ?? string.Empty),
        (Actions.SortFilesByCreationTime, t.Created ?? string.Empty),
        (Actions.SortFilesByLastAccessTime, t.LastAccessTime ?? string.Empty),
        (Actions.SortFilesRandomly, t.Random ?? string.Empty),
        (Actions.SortFilesAscending, t.Ascending ?? string.Empty),
        (Actions.SortFilesDescending, t.Descending ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetCopyEntries(LanguageModel t) =>
        OperatingSystem.IsMacOS()
            ?
            [
                (Actions.CopyFile, t.CopyFile ?? string.Empty),
                (Actions.CopyFilePath, t.FileCopyPath ?? string.Empty),
                (Actions.CopyImage, t.CopyImage ?? string.Empty),
                (Actions.CopyBase64, (t.Copy ?? "Copy") + " base64"),
                (Actions.Paste, t.FilePaste ?? string.Empty),
                (Actions.DuplicateFile, t.DuplicateFile ?? string.Empty),
            ]
            :
            [
                (Actions.CopyFile, t.CopyFile ?? string.Empty),
                (Actions.CutFile, t.Cut ?? string.Empty),
                (Actions.CopyFilePath, t.FileCopyPath ?? string.Empty),
                (Actions.CopyImage, t.CopyImage ?? string.Empty),
                (Actions.CopyBase64, (t.Copy ?? "Copy") + " base64"),
                (Actions.Paste, t.FilePaste ?? string.Empty),
                (Actions.DuplicateFile, t.DuplicateFile ?? string.Empty),
            ];

    public static List<(string FunctionName, string DisplayName)> GetTabManagementEntries(LanguageModel t) =>
    [
        (Actions.NewTab, t.NewTab ?? string.Empty),
        (Actions.CloseTab, t.CloseTab ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetToolWindowsEntries(LanguageModel t) =>
    [
        (Actions.AboutWindow, t.About ?? string.Empty),
        (Actions.SettingsWindow, t.Settings ?? string.Empty),
        (Actions.ImageInfoWindow, t.ImageInfo ?? string.Empty),
        (Actions.EffectsWindow, t.Effects ?? string.Empty),
        (Actions.ConvertWindow, t.FileConversion ?? string.Empty),
        (Actions.KeybindingsWindow, t.ApplicationShortcuts ?? string.Empty),
        (Actions.BatchResizeWindow, t.BatchResize ?? string.Empty),
        (Actions.ResizeWindow, t.Resize ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetWindowManagementEntries(LanguageModel t) =>
    [
        (Actions.Close, t.Close ?? string.Empty),
        (Actions.NewWindow, t.NewWindow ?? string.Empty),
        (Actions.Center, t.CenterWindow ?? string.Empty),
        (Actions.SetTopMost, t.StayTopMost ?? string.Empty),
        (Actions.ToggleFullscreen, t.ToggleFullscreen ?? string.Empty),
        (Actions.Maximize, t.Maximize ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetWindowScalingEntries(LanguageModel t) =>
    [
        (Actions.AutoFitWindow, t.AutoFitWindow ?? string.Empty),
        (Actions.NormalWindow, t.NormalWindow ?? string.Empty),
    ];

    public static List<(string FunctionName, string DisplayName)> GetStarRatingEntries(LanguageModel t) =>
    [
        (Actions.Set1Star, t._1Star ?? string.Empty),
        (Actions.Set2Star, t._2Star ?? string.Empty),
        (Actions.Set3Star, t._3Star ?? string.Empty),
        (Actions.Set4Star, t._4Star ?? string.Empty),
        (Actions.Set5Star, t._5Star ?? string.Empty),
        (Actions.Set0Star, t.RemoveStarRating ?? string.Empty),
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
        ("PlayPause", t.PlayPause ?? string.Empty),
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
